using System.Threading.Channels;

namespace UniEnroll.Api.Messaging;

public sealed class EmailChannelWorker(Channel<EmailMessage> channel, IEmailSender sender, ILogger<EmailChannelWorker> log) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var msg in channel.Reader.ReadAllAsync(stoppingToken))
        {
            try { await sender.SendAsync(msg, stoppingToken); }
            catch (Exception ex) { log.LogError(ex, "InMemory email send failed to {To}", msg.ToEmail); }
        }
    }
}
