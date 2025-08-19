using System.Threading.Channels;

namespace UniEnroll.Api.Messaging;

public sealed class InMemoryEmailQueue(Channel<EmailMessage> channel) : IEmailQueue
{
    public Task EnqueueAsync(EmailMessage msg, CancellationToken ct) => channel.Writer.WriteAsync(msg, ct).AsTask();
}
