using MediatR;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;

namespace UniEnroll.Api.Caching.CacheInvalidator;


public sealed record TermChanged : INotification;

public sealed class TermCacheInvalidator(
    IServiceProvider sp,          // lets us get IDistributedCache only if it exists
    IMemoryCache memory)
  : INotificationHandler<TermChanged>
{
    public async Task Handle(TermChanged _, CancellationToken ct)
    {
        // evict distributed cache if configured (Redis), otherwise skip
        var dist = sp.GetService<IDistributedCache>();
        if (dist is not null) await dist.RemoveAsync(OutputCachePolicyNames.TermsList, ct);

        // evict in-process memory cache (used when no Redis)
        memory.Remove(OutputCachePolicyNames.TermsList);
    }
}

