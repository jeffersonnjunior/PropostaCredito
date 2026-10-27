using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Retry;
using RabbitMQ.Client;

namespace Infrastructure.Messaging.RabbitMq;

public sealed class RabbitMqMessageBus : IMessageBus, IAsyncDisposable
{
    private readonly RabbitMqOptions _options;
    private readonly ILogger<RabbitMqMessageBus> _logger;
    private readonly AsyncRetryPolicy _retryPolicy;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private IConnection? _connection;
    private IChannel? _channel;

    public RabbitMqMessageBus(IOptions<RabbitMqOptions> options, ILogger<RabbitMqMessageBus> logger)
    {
        _options = options.Value;
        _logger = logger;
        _retryPolicy = Policy
            .Handle<Exception>()
            .WaitAndRetryAsync(
                3,
                attempt => TimeSpan.FromMilliseconds(200 * Math.Pow(2, attempt)),
                (ex, delay, attempt, _) =>
                {
                    _logger.LogWarning(
                        ex,
                        "Falha ao publicar no RabbitMQ (tentativa {Attempt}). Nova tentativa em {Delay}.",
                        attempt,
                        delay);
                });
    }

    public async Task PublishAsync(
        string eventType,
        string payloadJson,
        CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled)
        {
            _logger.LogInformation(
                "RabbitMq desabilitado. Evento {EventType} não publicado: {Payload}",
                eventType,
                payloadJson);
            return;
        }

        await _retryPolicy.ExecuteAsync(async ct =>
        {
            var channel = await EnsureChannelAsync(ct);
            var body = Encoding.UTF8.GetBytes(payloadJson);
            var props = new BasicProperties
            {
                ContentType = "application/json",
                DeliveryMode = DeliveryModes.Persistent,
                Type = eventType,
                MessageId = Guid.NewGuid().ToString()
            };

            await channel.BasicPublishAsync(
                exchange: _options.PublishExchange,
                routingKey: _options.PublishRoutingKey,
                mandatory: false,
                basicProperties: props,
                body: body,
                cancellationToken: ct);

            _logger.LogInformation(
                "Evento {EventType} publicado no exchange {Exchange}",
                eventType,
                _options.PublishExchange);
        }, cancellationToken);
    }

    private async Task<IChannel> EnsureChannelAsync(CancellationToken cancellationToken)
    {
        if (_channel is { IsOpen: true })
            return _channel;

        await _gate.WaitAsync(cancellationToken);
        try
        {
            if (_channel is { IsOpen: true })
                return _channel;

            if (_connection is null || !_connection.IsOpen)
            {
                var factory = new ConnectionFactory
                {
                    HostName = _options.Host,
                    Port = _options.Port,
                    UserName = _options.UserName,
                    Password = _options.Password
                };
                _connection = await factory.CreateConnectionAsync(cancellationToken);
            }

            _channel = await _connection.CreateChannelAsync(cancellationToken: cancellationToken);
            await _channel.ExchangeDeclareAsync(
                exchange: _options.PublishExchange,
                type: ExchangeType.Topic,
                durable: true,
                autoDelete: false,
                cancellationToken: cancellationToken);

            return _channel;
        }
        finally
        {
            _gate.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_channel is not null)
            await _channel.CloseAsync();
        if (_connection is not null)
            await _connection.CloseAsync();
        _gate.Dispose();
    }
}
