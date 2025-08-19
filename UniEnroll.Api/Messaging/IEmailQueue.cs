namespace UniEnroll.Api.Messaging;

public interface IEmailQueue
{
    Task EnqueueAsync(EmailMessage msg, CancellationToken ct);
}
