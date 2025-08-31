using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;
using OpenTelemetry;
using Serilog.Context;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace UniEnroll.Application.Common;

public sealed class CorrelationIdMiddleware(RequestDelegate next, ILogger<CorrelationIdMiddleware> logger)
{
    //Only ASCII letters/digits, colon, underscore, hyphen; min 35 max 128 chars long.
    private static readonly Regex Allowed = new(@"^[A-Za-z0-9:_\-]{35,128}$", RegexOptions.Compiled);

    public async Task Invoke(HttpContext ctx)
    {
        // 1) pick id & validate from header or new Guid
        var cid = ctx.Request.Headers.TryGetValue(Headers.CorrelationId, out var hv) && Allowed.IsMatch(hv!)
                    ? hv.ToString()
                    : Guid.NewGuid().ToString();

        // 2) store where everyone can see it
        ctx.Response.Headers[Headers.CorrelationId] = cid!;
        ctx.Items[Headers.CorrelationId] = cid;
        ctx.TraceIdentifier = cid!;

        using var scope = logger.BeginScope(new Dictionary<string, object?> { ["CorrelationId"] = cid.ToString() });

        // 3) enrich logs + OTel baggage for cross-process visibility
        /*
            Adds a span attribute on the current OpenTelemetry Activity (trace span).
            Visible in tracing backends (e.g., Jaeger/Tempo/OTLP) when you inspect the span.
            Per-span only; doesn’t automatically go into your logs, and doesn’t propagate to other services unless you also put it in baggage or copy it on child spans.
            Requires an active Activity (may be null if tracing isn’t enabled or no span is started yet).
        */
        Activity.Current?.SetTag(Headers.CorrelationLogKey, cid.ToString());

        /*  Serilog-specific
            Adds a Serilog log property to all log events written in this scope (flows via AsyncLocal across async/await).
            Shows up in your logs (e.g., in Seq/ELK/MSSQL sink), not in traces—unless you have explicit log/trace correlation config.
            Does not propagate to downstream services.
        */
        using var _ = LogContext.PushProperty(Headers.CorrelationLogKey, cid);

        /* OpenTelemetry-specific
            Puts the key/value into W3C Baggage (distributed context).
            OpenTelemetry HTTP client instrumentation will send it to downstream services via the baggage header and make it available to their spans/handlers.
        */
        Baggage.SetBaggage(Headers.CorrelationLogKey, cid);

        // 4) echo back to client
        ctx.Response.OnStarting(() =>
        {
            if (!ctx.Response.Headers.ContainsKey(Headers.CorrelationId))
                ctx.Response.Headers[Headers.CorrelationId] = cid;
            return Task.CompletedTask;
        });

        await next(ctx);
    }
}

