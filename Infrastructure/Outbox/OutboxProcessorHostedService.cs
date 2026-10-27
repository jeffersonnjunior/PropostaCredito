using Infrastructure.Messaging;
using Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Outbox;

public sealed class OutboxProcessorHostedService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<OutboxProcessorHostedService> _logger;

    public OutboxProcessorHostedService(
        IServiceScopeFactory scopeFactory,
        ILogger<OutboxProcessorHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Outbox processor (Proposta) iniciado.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessBatchAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Erro no ciclo do outbox processor.");
            }

            await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
        }
    }

    private async Task ProcessBatchAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PropostaDbContext>();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        var pending = await db.OutboxMessages
            .Where(x => x.ProcessedAtUtc == null && x.Attempts < 5)
            .OrderBy(x => x.OccurredAtUtc)
            .Take(20)
            .ToListAsync(cancellationToken);

        foreach (var message in pending)
        {
            try
            {
                await bus.PublishAsync(message.EventType, message.PayloadJson, cancellationToken);
                message.ProcessedAtUtc = DateTime.UtcNow;
                message.LastError = null;
            }
            catch (Exception ex)
            {
                message.Attempts += 1;
                message.LastError = ex.Message;
                _logger.LogWarning(
                    ex,
                    "Falha ao publicar outbox {OutboxId} (tentativa {Attempts})",
                    message.Id,
                    message.Attempts);
            }
        }

        if (pending.Count > 0)
            await db.SaveChangesAsync(cancellationToken);
    }
}
