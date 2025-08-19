using MediatR;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;

namespace UniEnroll.Api.Caching.CacheInvalidator;

public record OfferingChanged(long OfferingId) : INotification;

public sealed class OfferingCacheInvalidator(IDistributedCache dist, IMemoryCache mem)
  : INotificationHandler<OfferingChanged>
{
    public async Task Handle(OfferingChanged n, CancellationToken ct)
    {
        // list keys are many; rely on short TTL (30s) to avoid complex tag invalidation
        var key = $"offerings:detail:{n.OfferingId}";
        if (dist is not null)
        {
            await dist.RemoveAsync(key, ct); 
        }
        mem.Remove(key);
    }
}
