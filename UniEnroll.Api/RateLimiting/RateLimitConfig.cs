using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;
using UniEnroll.Application;
using UniEnroll.Application.Common;
using UniEnroll.Application.Errors;

namespace UniEnroll.Api.RateLimiting;

public static class RateLimitConfig
{
    public static void Configure(RateLimiterOptions opt, IConfiguration config)
    {
        var s = config.GetSection("RateLimiting:FixedWindow");
        var limit = s.GetValue("PermitLimit", 100);
        var windowSeconds = s.GetValue("WindowSeconds", 10);
        var queueLimit = s.GetValue("QueueLimit", 50);

        opt.AddPolicy("fixed", httpContext =>
            RateLimitPartition.GetFixedWindowLimiter(
                partitionKey: httpContext.User?.Identity?.Name ?? httpContext.Connection.RemoteIpAddress?.ToString() ?? "anon",
                factory: _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = limit,
                    Window = TimeSpan.FromSeconds(windowSeconds),
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                    QueueLimit = queueLimit
                }));

        opt.OnRejected = async (ctx, token) =>
        {
            var problem = new ProblemDetails
            {
                Title = "Too Many Requests",
                Status = StatusCodes.Status429TooManyRequests,
                Type = $"https://errors.example/{ErrorCodes.RateLimited}",
                Detail = "Request rate is limited. Please retry later.",
                Instance = ctx.HttpContext.Request.Path
            };
            if (ctx.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retry))
                ctx.HttpContext.Response.Headers.RetryAfter = ((int)retry.TotalSeconds).ToString();

            if (!ctx.HttpContext.Response.Headers.ContainsKey(Headers.CorrelationId))
                ctx.HttpContext.Response.Headers[Headers.CorrelationId] = Guid.NewGuid().ToString();

            problem.Extensions["correlationId"] = ctx.HttpContext.Response.Headers[Headers.CorrelationId].ToString();
            problem.Extensions["traceId"] = ctx.HttpContext.TraceIdentifier;

            ctx.HttpContext.Response.StatusCode = 429;
            ctx.HttpContext.Response.ContentType = "application/problem+json";
            await ctx.HttpContext.Response.WriteAsJsonAsync(problem, cancellationToken: token);
        };

        opt.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    }
}


//public static class RateLimitConfig
//{
//    public static void Configure(RateLimiterOptions opt, IConfiguration config)
//    {
//        var s = config.GetSection("RateLimiting:FixedWindow");
//        var limit = s.GetValue("PermitLimit", 100);
//        var windowSeconds = s.GetValue("WindowSeconds", 10);
//        var queueLimit = s.GetValue("QueueLimit", 50);

//        opt.AddPolicy("fixed", httpContext =>
//            RateLimitPartition.GetFixedWindowLimiter(
//                partitionKey: httpContext.User?.Identity?.Name ?? httpContext.Connection.RemoteIpAddress?.ToString() ?? "anon",
//                factory: _ => new FixedWindowRateLimiterOptions
//                {
//                    PermitLimit = limit,
//                    Window = TimeSpan.FromSeconds(windowSeconds),
//                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
//                    QueueLimit = queueLimit
//                }));
//        opt.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
//    }
//}
