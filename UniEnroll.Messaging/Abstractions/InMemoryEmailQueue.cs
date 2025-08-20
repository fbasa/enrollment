using System.Threading.Channels;
using UniEnroll.Domain.Common;

namespace UniEnroll.Messaging.Abstractions;

public sealed class InMemoryEmailQueue(Channel<EmailMessage> channel) : IEmailQueue
{
    public Task EnqueueAsync(EmailMessage msg, CancellationToken ct) => channel.Writer.WriteAsync(msg, ct).AsTask();
}
