using MediatR;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace UniEnroll.Api.Common.Idempotency;


public sealed class IdempotencyBehavior<TRequest, TResponse>(
    IDistributedCache cache,
    IServiceProvider sp,
    IIdempotencyKeyAccessor keyAccessor,
    ILogger<IdempotencyBehavior<TRequest, TResponse>> log,
    IOptions<IdempotencyOptions> opts) : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private static readonly JsonSerializerOptions JsonOpts = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = false
    };

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct)
    {
        // Only apply to explicitly idempotent commands
        if (request is not IIdempotentRequest)
            return await next();

        var (headerKey, method, path, userScope) = keyAccessor.Read();
        if (string.IsNullOrWhiteSpace(headerKey))
        {
            log.LogDebug("IdempotencyBehavior skipped: missing header.");
            return await next();
        }

        // Build a stable request hash (based on TRequest serialization)
        var reqJson = JsonSerializer.Serialize(request, JsonOpts);
        var reqHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(reqJson)));

        // Compose cache keys
        var baseKey = $"idem:{method}:{path}:{userScope}:{headerKey}";
        var resultKey = $"{baseKey}:{reqHash}:result";
        var lockKey = $"{baseKey}:{reqHash}:lock";

        // 1) Return cached result if present
        var cached = await cache.GetStringAsync(resultKey, ct);
        if (!string.IsNullOrEmpty(cached))
        {
            log.LogInformation("Idempotency hit for {Key}", baseKey);
            return JsonSerializer.Deserialize<TResponse>(cached, JsonOpts)!;
        }

        // Try to acquire a Redis lock if a multiplexer is available
        var mux = sp.GetService<IConnectionMultiplexer>();
        if (mux is not null)
        {
            var db = mux.GetDatabase();
            var token = Guid.NewGuid().ToString("N");

            // Attempt SET NX with TTL
            var acquired = await db.StringSetAsync(lockKey, token, expiry: opts.Value.LockTtl, when: When.NotExists);
            if (!acquired)
            {
                // Someone else is processing the same request; wait for their result briefly
                var waited = TimeSpan.Zero;
                while (waited < opts.Value.WaitForExisting)
                {
                    await Task.Delay(100, ct);
                    waited += TimeSpan.FromMilliseconds(100);
                    var c2 = await cache.GetStringAsync(resultKey, ct);
                    if (!string.IsNullOrEmpty(c2))
                        return JsonSerializer.Deserialize<TResponse>(c2, JsonOpts)!;
                }
                // Still nothing: treat as conflict-in-progress
                throw new InvalidOperationException("Duplicate request in progress for the same idempotency key.");
            }

            try
            {
                // Call handler
                var response = await next();

                // Store result for TTL
                await cache.SetStringAsync(resultKey,
                    JsonSerializer.Serialize(response, JsonOpts),
                    new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = opts.Value.StoreTtl }, ct);

                return response;
            }
            finally
            {
                // Best-effort lock release (compare-and-delete)
                const string lua = """
                    if redis.call('get', KEYS[1]) == ARGV[1] then
                        return redis.call('del', KEYS[1])
                    else
                        return 0
                    end
                    """;
                try { await db.ScriptEvaluateAsync(lua, [(RedisKey)lockKey], [token]); } catch { /* ignore */ }
            }
        }
        else
        {
            // No Redis multiplexer - no strong in-flight protection.
            // We still do cache-aside replay after completion.
            var response = await next();
            await cache.SetStringAsync(resultKey,
                JsonSerializer.Serialize(response, JsonOpts),
                new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = opts.Value.StoreTtl }, ct);
            return response;
        }
    }
}
