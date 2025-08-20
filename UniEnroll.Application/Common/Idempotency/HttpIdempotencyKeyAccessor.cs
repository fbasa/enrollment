using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace UniEnroll.Application.Common.Idempotency;

public sealed class HttpIdempotencyKeyAccessor(IHttpContextAccessor http) : IIdempotencyKeyAccessor
{
    public (string? Key, string Method, string Path, string UserScope) Read()
    {
        var ctx = http.HttpContext!;
        ctx.Request.Headers.TryGetValue("X-Idempotency-Key", out var key);
        var user = ctx.User?.FindFirstValue("sub") ?? ctx.User?.Identity?.Name ?? "anon";
        return (key.ToString(), ctx.Request.Method ?? "POST", ctx.Request.Path.Value ?? "/", user);
    }
}
