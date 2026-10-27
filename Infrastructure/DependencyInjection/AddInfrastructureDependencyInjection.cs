using Application.Interfaces;
using Infrastructure.Messaging;
using Infrastructure.Messaging.RabbitMq;
using Infrastructure.Outbox;
using Infrastructure.Persistence;
using Infrastructure.Persistence.Context;
using Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Infrastructure.DependencyInjection;

public static class AddInfrastructureDependencyInjection
{
    /// <summary>
    /// Persistência e ports compartilhados (Api + Worker).
    /// </summary>
    public static void DependencyInjectionInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<RabbitMqOptions>(configuration.GetSection(RabbitMqOptions.SectionName));

        var connectionString = configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException(
                "Connection string 'Default' não configurada. Defina ConnectionStrings:Default para o SQL Server.");

        services.AddDbContext<PropostaDbContext>(options =>
        {
            options.UseSqlServer(connectionString, sql =>
                sql.MigrationsAssembly(typeof(PropostaDbContext).Assembly.FullName));
            options.EnableDetailedErrors();
        });

        services.AddScoped<IPropostaRepository, PropostaRepository>();
        services.AddScoped<IProcessedEventStore, ProcessedEventStore>();
        services.AddScoped<IOutboxWriter, OutboxWriter>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddSingleton<IMessageBus, RabbitMqMessageBus>();

        services.AddHealthChecks()
            .AddDbContextCheck<PropostaDbContext>();
    }

    /// <summary>
    /// Background workers (consumer + outbox) — apenas no projeto Worker.
    /// </summary>
    public static void DependencyInjectionWorkers(this IServiceCollection services)
    {
        services.AddHostedService<OutboxProcessorHostedService>();
        services.AddHostedService<ClienteCadastradoConsumerHostedService>();
    }

    public static async Task MigrateDatabaseAsync(
        this IServiceProvider services,
        CancellationToken cancellationToken = default)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PropostaDbContext>();
        var logger = scope.ServiceProvider
            .GetRequiredService<ILoggerFactory>()
            .CreateLogger("DatabaseBootstrap");

        for (var attempt = 1; attempt <= 10; attempt++)
        {
            try
            {
                await db.Database.MigrateAsync(cancellationToken);
                return;
            }
            catch (Exception ex) when (attempt < 10)
            {
                logger.LogWarning(
                    ex,
                    "SQL Server indisponível (tentativa {Attempt}/10). Aguardando...",
                    attempt);
                await Task.Delay(TimeSpan.FromSeconds(3), cancellationToken);
            }
        }
    }
}
