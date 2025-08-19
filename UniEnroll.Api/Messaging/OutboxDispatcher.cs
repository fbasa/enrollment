using Polly;
using Polly.Retry;
using UniEnroll.Api.Infrastructure.Repositories;

namespace UniEnroll.Api.Messaging;

public sealed class OutboxDispatcher(IEmailOutboxRepository repo, IEmailQueue queue, ILogger<OutboxDispatcher> log) : BackgroundService
{
    private static readonly AsyncRetryPolicy Retry = Policy
        .Handle<Exception>()
        .WaitAndRetryAsync(3, i => TimeSpan.FromSeconds(Math.Pow(2, i)));

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var batch = await repo.TakePendingAsync(50, stoppingToken);
                if (batch.Count == 0)
                {
                    await Task.Delay(1500, stoppingToken);
                    continue;
                }

                foreach (var (id, msg) in batch)
                {
                    await Retry.ExecuteAsync(async ct =>
                    {
                        await queue.EnqueueAsync(msg, ct);
                        await repo.MarkEnqueuedAsync(id, ct);
                    }, stoppingToken);
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                log.LogError(ex, "Outbox dispatch loop error");
                await Task.Delay(3000, stoppingToken);
            }
        }
    }
}
