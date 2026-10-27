using System.Text;
using System.Text.Json;
using Application.Events;
using Application.Propostas.GerarProposta;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Infrastructure.Messaging.RabbitMq;

public sealed class ClienteCadastradoConsumerHostedService : BackgroundService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly RabbitMqOptions _options;
    private readonly ILogger<ClienteCadastradoConsumerHostedService> _logger;
    private IConnection? _connection;
    private IChannel? _channel;

    public ClienteCadastradoConsumerHostedService(
        IServiceScopeFactory scopeFactory,
        IOptions<RabbitMqOptions> options,
        ILogger<ClienteCadastradoConsumerHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled)
        {
            _logger.LogWarning("Consumer RabbitMq desabilitado.");
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await EnsureTopologyAndConsumeAsync(stoppingToken);
                await Task.Delay(Timeout.Infinite, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Consumer desconectado. Reconectando em 5s...");
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }
    }

    private async Task EnsureTopologyAndConsumeAsync(CancellationToken cancellationToken)
    {
        var factory = new ConnectionFactory
        {
            HostName = _options.Host,
            Port = _options.Port,
            UserName = _options.UserName,
            Password = _options.Password
        };

        _connection = await factory.CreateConnectionAsync(cancellationToken);
        _channel = await _connection.CreateChannelAsync(cancellationToken: cancellationToken);

        await _channel.ExchangeDeclareAsync(
            _options.ConsumeExchange, ExchangeType.Topic, durable: true, cancellationToken: cancellationToken);

        await _channel.QueueDeclareAsync(
            _options.ConsumeDlq, durable: true, exclusive: false, autoDelete: false, cancellationToken: cancellationToken);

        var args = new Dictionary<string, object?>
        {
            ["x-dead-letter-exchange"] = "",
            ["x-dead-letter-routing-key"] = _options.ConsumeDlq
        };

        await _channel.QueueDeclareAsync(
            _options.ConsumeQueue,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: args,
            cancellationToken: cancellationToken);

        await _channel.QueueBindAsync(
            _options.ConsumeQueue,
            _options.ConsumeExchange,
            _options.ConsumeRoutingKey,
            cancellationToken: cancellationToken);

        await _channel.BasicQosAsync(0, 1, false, cancellationToken);

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.ReceivedAsync += OnReceivedAsync;

        await _channel.BasicConsumeAsync(
            _options.ConsumeQueue,
            autoAck: false,
            consumer: consumer,
            cancellationToken: cancellationToken);

        _logger.LogInformation(
            "Consumindo fila {Queue} (exchange {Exchange}, key {Key})",
            _options.ConsumeQueue,
            _options.ConsumeExchange,
            _options.ConsumeRoutingKey);
    }

    private async Task OnReceivedAsync(object sender, BasicDeliverEventArgs ea)
    {
        if (_channel is null)
            return;

        try
        {
            var json = Encoding.UTF8.GetString(ea.Body.ToArray());
            var @event = JsonSerializer.Deserialize<ClienteCadastradoEvent>(json, JsonOptions)
                ?? throw new InvalidOperationException("Payload ClienteCadastrado inválido.");

            _logger.LogInformation(
                "Job: {JobName} | EventId={EventId} | ClienteId={ClienteId}",
                GerarPropostaFromClienteCadastradoHandler.JobDisplayName,
                @event.EventId,
                @event.ClienteId);

            using var scope = _scopeFactory.CreateScope();
            var handler = scope.ServiceProvider.GetRequiredService<GerarPropostaFromClienteCadastradoHandler>();
            await handler.HandleAsync(@event);

            await _channel.BasicAckAsync(ea.DeliveryTag, multiple: false);
            _logger.LogInformation(
                "Job: {JobName} concluído | EventId={EventId} | ClienteId={ClienteId}",
                GerarPropostaFromClienteCadastradoHandler.JobDisplayName,
                @event.EventId,
                @event.ClienteId);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Job: {JobName} falhou. Enviando para DLQ.",
                GerarPropostaFromClienteCadastradoHandler.JobDisplayName);
            await _channel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: false);
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_channel is not null)
            await _channel.CloseAsync(cancellationToken);
        if (_connection is not null)
            await _connection.CloseAsync(cancellationToken);
        await base.StopAsync(cancellationToken);
    }
}
