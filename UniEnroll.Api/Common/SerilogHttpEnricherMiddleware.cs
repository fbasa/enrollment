using Serilog.Context;

namespace UniEnroll.Api.Common;

public sealed class SerilogHttpEnricherMiddleware(RequestDelegate next)
{
    public async Task Invoke(HttpContext ctx)
    {
        // Ensure CorrelationId header exists (middleware earlier sets it; fallback here just in case)
        var cid = ctx.Response.Headers.TryGetValue(Headers.CorrelationId, out var v)
                  ? v.ToString()
                  : Guid.NewGuid().ToString();

        using (LogContext.PushProperty("CorrelationId", cid))
        using (LogContext.PushProperty("TraceId", ctx.TraceIdentifier))
        using (LogContext.PushProperty("UserName", ctx.User?.Identity?.Name ?? "anon"))
        using (LogContext.PushProperty("Path", ctx.Request.Path.Value))
        using (LogContext.PushProperty("Method", ctx.Request.Method))
        using (LogContext.PushProperty("ClientIp", ctx.Connection.RemoteIpAddress?.ToString()))
        {
            await next(ctx);
        }
    }
}
