using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using System.Text.Json;
using UniEnroll.Domain.Common;
using UniEnroll.Messaging.Abstractions;

namespace UniEnroll.Messaging.RabbitMQ;

public sealed class RabbitMqEmailQueue : IEmailQueue, IAsyncDisposable, IDisposable
{
    private readonly RabbitMqOptions _o;
    private readonly ILogger<RabbitMqEmailQueue> _log;
    private readonly JsonSerializerOptions _json = new(JsonSerializerDefaults.Web);
    private readonly SemaphoreSlim _initLock = new(1, 1);

    private IConnection? _conn;   // v7 async connection
    private IChannel? _ch;        // v7 replaces IModel with IChannel

    public RabbitMqEmailQueue(IOptions<RabbitMqOptions> options, ILogger<RabbitMqEmailQueue> log)
    {
        _o = options.Value;
        _log = log;
    }

    public async Task EnqueueAsync(EmailMessage msg, CancellationToken ct)
    {
        var ch = await EnsureChannelAsync(ct);

        var body = JsonSerializer.SerializeToUtf8Bytes(msg, _json);
        var props = new BasicProperties
        {
            ContentType = "application/json",
            DeliveryMode  = DeliveryModes.Persistent,                        
        };

        await ch.BasicPublishAsync(
            exchange: _o.Exchange,
            routingKey: _o.RoutingKey,
            mandatory: false,
            basicProperties: props,
            body: body,
            cancellationToken: ct);
    }

    private async Task<IChannel> EnsureChannelAsync(CancellationToken ct)
    {
        if (_ch is not null) return _ch;

        await _initLock.WaitAsync(ct);
        try
        {
            if (_ch is not null) return _ch;

            var factory = new ConnectionFactory
            {
                UserName = _o.UserName,
                Password = _o.Password,
                ClientProvidedName = "unienroll-api"
            };

            var endpoints = new[] { new AmqpTcpEndpoint(_o.HostName, _o.Port) };

            _conn = await factory.CreateConnectionAsync(endpoints, factory.ClientProvidedName!, ct);

            // v7 requires CreateChannelOptions
            var chOptions = new CreateChannelOptions(publisherConfirmationsEnabled:true,publisherConfirmationTrackingEnabled:true);
            _ch = await _conn.CreateChannelAsync(chOptions, ct);

            // (optional) enable publisher confirms in v7:
            // await _ch.ConfirmSelectAsync(ct);

            // Topology
            await _ch.ExchangeDeclareAsync(_o.Exchange, ExchangeType.Topic, durable: true, autoDelete: false, cancellationToken: ct);
            await _ch.QueueDeclareAsync("email.outgoing", durable: true, exclusive: false, autoDelete: false, arguments: null, cancellationToken: ct);
            await _ch.QueueBindAsync("email.outgoing", _o.Exchange, _o.RoutingKey, arguments: null, cancellationToken: ct);

            _log.LogInformation("RabbitMQ email queue wired: {Host}:{Port}", _o.HostName, _o.Port);
            return _ch;
        }
        finally
        {
            _initLock.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        try { if (_ch is not null) await _ch.DisposeAsync(); } catch { }
        try { if (_conn is not null) await _conn.DisposeAsync(); } catch { }
        _initLock.Dispose();
    }

    public void Dispose() => DisposeAsync().GetAwaiter().GetResult();
}
