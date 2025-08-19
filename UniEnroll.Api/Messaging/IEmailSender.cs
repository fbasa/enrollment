namespace UniEnroll.Api.Messaging;

public interface IEmailSender
{
    Task SendAsync(EmailMessage msg, CancellationToken ct);
}
