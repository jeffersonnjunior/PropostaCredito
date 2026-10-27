using System.Text.Json;
using Application.Events;
using Application.Interfaces;
using Domain.Entities;

namespace Application.Propostas.GerarProposta;

public sealed class GerarPropostaReaction : IGerarPropostaReaction
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly IPropostaRepository _propostas;
    private readonly IOutboxWriter _outbox;

    public GerarPropostaReaction(IPropostaRepository propostas, IOutboxWriter outbox)
    {
        _propostas = propostas;
        _outbox = outbox;
    }

    public async Task ExecuteAsync(Proposta proposta, CancellationToken cancellationToken = default)
    {
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
    }
}
