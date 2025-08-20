using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text.Json;
using UniEnroll.Domain.Common;
using UniEnroll.Messaging.Abstractions;

namespace UniEnroll.Messaging.RabbitMQ;

public sealed class RabbitConsumer(
    IOptions<RabbitMqOptions> opts,
    IEmailSender sender,
    ILogger<RabbitConsumer> log) : BackgroundService
{
    private readonly JsonSerializerOptions _json = new(JsonSerializerDefaults.Web);

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        var o = opts.Value;

        var factory = new ConnectionFactory
        {
            UserName = o.UserName,
            Password = o.Password,
            ClientProvidedName = "unienroll-email-worker"
        };
        var endpoints = new[] { new AmqpTcpEndpoint(o.HostName, o.Port) };

        await using var conn = await factory.CreateConnectionAsync(endpoints, factory.ClientProvidedName!, ct);

        var chOptions = new CreateChannelOptions(publisherConfirmationsEnabled: true, publisherConfirmationTrackingEnabled: true);
        await using var ch = await conn.CreateChannelAsync(chOptions, ct);

        // Ensure queue exists (publisher already declares exchange/bindings)
        await ch.QueueDeclareAsync(
            queue: "email.outgoing",
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null,
            cancellationToken: ct);

        // Prefetch up to 10 unacked messages per consumer
        await ch.BasicQosAsync(prefetchSize: 0, prefetchCount: 10, global: false, ct);

        var consumer = new AsyncEventingBasicConsumer(ch);

        consumer.ReceivedAsync += async (obj, args) =>
        {
            try
            {
                // Deserialize directly from the body span
                var msg = JsonSerializer.Deserialize<EmailMessage>(args.Body.Span, _json)
                          ?? throw new InvalidOperationException("Empty/invalid email payload");

                await sender.SendAsync(msg, ct);
                await ch.BasicAckAsync(args.DeliveryTag, multiple: false, cancellationToken: ct);
            }
            catch (Exception ex)
            {
                log.LogError(ex, "Failed to process email message (DeliveryTag={Tag})", args.DeliveryTag);
                await ch.BasicNackAsync(args.DeliveryTag, multiple: false, requeue: true, cancellationToken: ct);
            }
        };

        // Start consuming (async API in v7)
        await ch.BasicConsumeAsync(
            consumer: consumer,
            queue: "email.outgoing",
            autoAck: false,
            consumerTag: "email-worker",
            noLocal: false,
            exclusive: false,
            arguments: null,
            cancellationToken: ct);

        // Keep the background service alive until cancellation
        try
        {
            while (!ct.IsCancellationRequested)
                await Task.Delay(1000, ct);
        }
        catch (OperationCanceledException) { /* graceful shutdown */ }
    }
}
