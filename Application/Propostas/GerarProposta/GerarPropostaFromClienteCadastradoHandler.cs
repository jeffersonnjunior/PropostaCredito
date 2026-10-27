using System.Text.Json;
using Application.Events;
using Application.Interfaces;
using Domain.Entities;

namespace Application.Propostas.GerarProposta;

public sealed class GerarPropostaFromClienteCadastradoHandler
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly IPropostaRepository _propostas;
    private readonly IProcessedEventStore _processedEvents;
    private readonly IOutboxWriter _outbox;
    private readonly IUnitOfWork _unitOfWork;

    public GerarPropostaFromClienteCadastradoHandler(
        IPropostaRepository propostas,
        IProcessedEventStore processedEvents,
        IOutboxWriter outbox,
        IUnitOfWork unitOfWork)
    {
        _propostas = propostas;
        _processedEvents = processedEvents;
        _outbox = outbox;
        _unitOfWork = unitOfWork;
    }

    public async Task HandleAsync(
        ClienteCadastradoEvent @event,
        CancellationToken cancellationToken = default)
    {
        // Idempotência por eventId (at-least-once do broker)
        if (await _processedEvents.ExistsAsync(@event.EventId, cancellationToken))
            return;

        // Também evita 2 propostas para o mesmo cliente (reentrega com outro eventId)
        if (await _propostas.ExistsByClienteIdAsync(@event.ClienteId, cancellationToken))
        {
            await _processedEvents.MarkProcessedAsync(@event.EventId, @event.EventType, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return;
        }

        var cpfDigitos = new string(@event.Payload.Cpf.Where(char.IsDigit).ToArray());
        var proposta = Proposta.Criar(@event.ClienteId, cpfDigitos);

        await _propostas.AddAsync(proposta, cancellationToken);

        var outboxEventId = Guid.NewGuid();
        var propostaGerada = new PropostaGeradaEvent(
            EventId: outboxEventId,
            EventType: "PropostaGerada",
            SchemaVersion: 1,
            OccurredAt: DateTime.UtcNow,
            ClienteId: proposta.ClienteId,
            Payload: new PropostaGeradaPayload(
                proposta.Id,
                proposta.Score,
                proposta.Status.ToString(),
                proposta.QuantidadeCartoes,
                proposta.LimitePorCartao,
                proposta.LimiteTotal));

        var payloadJson = JsonSerializer.Serialize(propostaGerada, JsonOptions);
        await _outbox.EnqueueAsync("PropostaGerada", payloadJson, outboxEventId, cancellationToken);
        await _processedEvents.MarkProcessedAsync(@event.EventId, @event.EventType, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
