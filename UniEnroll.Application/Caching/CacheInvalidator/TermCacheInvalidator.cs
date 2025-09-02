using MediatR;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;

namespace UniEnroll.Application.Caching.CacheInvalidator;


public sealed record TermChanged : INotification;

public sealed class TermCacheInvalidator(IDistributedCache dist, IMemoryCache memory) : INotificationHandler<TermChanged>
{
    public async Task Handle(TermChanged _, CancellationToken ct)
    {
        if (dist is not null)
        {
            // evict distributed cache if configured (Redis), otherwise skip
            await dist.RemoveAsync(OutputCachePolicyNames.TermsList, ct);
        }
        else
        {
            // evict in-process memory cache (used when no Redis)
            memory.Remove(OutputCachePolicyNames.TermsList);
        }
    }
}

