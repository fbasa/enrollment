using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace UniEnroll.Application.Common.Idempotency;

public sealed class IdempotencyMiddleware(RequestDelegate next, ILogger<IdempotencyMiddleware> log)
{
    public async Task Invoke(HttpContext ctx)
    {
        if (HttpMethods.IsPost(ctx.Request.Method) ||
            HttpMethods.IsPut(ctx.Request.Method) ||
            HttpMethods.IsPatch(ctx.Request.Method))
        {
            // Enforce header on endpoints that require idempotency (you can mark them with metadata)
            if (!ctx.Request.Headers.TryGetValue("X-Idempotency-Key", out var key) || string.IsNullOrWhiteSpace(key))
            {
                ctx.Response.StatusCode = StatusCodes.Status400BadRequest;
                await ctx.Response.WriteAsJsonAsync(new
                {
                    title = "Missing X-Idempotency-Key",
                    status = 400
                });
                return;
            }
        }

        await next(ctx); // Actual idempotency handled by MediatR behavior
    }
}
