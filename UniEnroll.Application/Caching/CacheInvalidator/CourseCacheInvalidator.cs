using MediatR;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;

namespace UniEnroll.Application.Caching.CacheInvalidator;

public record CourseChanged(long CourseId) : INotification;

public class CourseCacheInvalidator(IDistributedCache dist, IMemoryCache mem) : INotificationHandler<CourseChanged>
{
    public async Task Handle(CourseChanged c, CancellationToken ct)
    {
        if (dist is not null)
        {
            await dist.RemoveAsync($"courses:detail:{c.CourseId}", ct);
        }
        else
        {
            mem.Remove($"courses:detail:{c.CourseId}");
        }
    }
}