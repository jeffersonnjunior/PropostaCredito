namespace Application.Events;

public sealed record PropostaGeradaEvent(
    Guid EventId,
    string EventType,
    int SchemaVersion,
    DateTime OccurredAt,
    Guid ClienteId,
    PropostaGeradaPayload Payload);

public sealed record PropostaGeradaPayload(
    Guid PropostaId,
    int Score,
    string Status,
    int QuantidadeCartoes,
    decimal LimitePorCartao,
    decimal LimiteTotal);
