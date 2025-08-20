using UniEnroll.Domain.Common;

namespace UniEnroll.Messaging.Abstractions;

public interface IEmailQueue
{
    Task EnqueueAsync(EmailMessage msg, CancellationToken ct);
}
