using Microsoft.Extensions.Primitives;
using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;

namespace UniEnroll.Api.Common;

// Simple in-memory idempotency store with short TTL for demo/local use
public sealed class IdempotencyMiddleware(RequestDelegate next, ILogger<IdempotencyMiddleware> logger)
{
    private static readonly ConcurrentDictionary<string, (DateTimeOffset Expiry, IResult Result)> _cache = new();

    public async Task Invoke(HttpContext ctx)
    {
        if (!HttpMethods.IsPost(ctx.Request.Method) && !HttpMethods.IsPut(ctx.Request.Method))
        {
            await next(ctx); return;
        }

        if (!ctx.Request.Headers.TryGetValue(Headers.IdempotencyKey, out StringValues key) || StringValues.IsNullOrEmpty(key))
        {
            await next(ctx); return;
        }

        var cacheKey = $"{key}:{ctx.Request.Path}";
        // return cached
        if (_cache.TryGetValue(cacheKey, out var entry) && entry.Expiry > DateTimeOffset.UtcNow)
        {
            await entry.Result.ExecuteAsync(ctx); return;
        }

        // Capture response
        var originalBody = ctx.Response.Body;
        using var mem = new MemoryStream();
        ctx.Response.Body = mem;

        await next(ctx);

        mem.Position = 0;
        using var sr = new StreamReader(mem, Encoding.UTF8, leaveOpen: true);
        var body = await sr.ReadToEndAsync();
        mem.Position = 0;
        ctx.Response.Body = originalBody;

        var result = Results.Text(body, ctx.Response.ContentType ?? "application/json", Encoding.UTF8, ctx.Response.StatusCode);
        _cache[cacheKey] = (DateTimeOffset.UtcNow.AddMinutes(10), result);

        await result.ExecuteAsync(ctx);
    }
}