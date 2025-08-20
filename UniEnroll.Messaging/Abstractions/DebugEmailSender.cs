using Microsoft.Extensions.Logging;
using UniEnroll.Domain.Common;

namespace UniEnroll.Messaging.Abstractions;

public sealed class DebugEmailSender(ILogger<DebugEmailSender> log) : IEmailSender
{
    public Task SendAsync(EmailMessage msg, CancellationToken ct)
    {
        log.LogInformation("EMAIL - {To} | {Subject}\nHTML? {HasHtml} TEXT? {HasText}\nMeta: {Meta}",
            msg.ToEmail, msg.Subject, !string.IsNullOrWhiteSpace(msg.BodyHtml), !string.IsNullOrWhiteSpace(msg.BodyText), msg.Metadata);
        return Task.CompletedTask;
    }
}
