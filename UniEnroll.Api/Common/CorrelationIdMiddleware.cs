using System.Diagnostics;

namespace UniEnroll.Api.Common;

public sealed class CorrelationIdMiddleware(RequestDelegate next, ILogger<CorrelationIdMiddleware> logger)
{
    public async Task Invoke(HttpContext ctx)
    {
        if (!ctx.Request.Headers.TryGetValue(Headers.CorrelationId, out var cid) || string.IsNullOrWhiteSpace(cid))
            cid = Guid.NewGuid().ToString();

        ctx.Response.Headers[Headers.CorrelationId] = cid!;
        using var scope = logger.BeginScope(new Dictionary<string, object?> { ["CorrelationId"] = cid.ToString() });

        Activity.Current?.SetTag("correlation.id", cid.ToString());
        await next(ctx);
    }
}

