using UniEnroll.Domain.Common;

namespace UniEnroll.Messaging.Abstractions;

public interface IEmailSender
{
    Task SendAsync(EmailMessage msg, CancellationToken ct);
}
