namespace Application.Events;

public sealed record ClienteCadastradoEvent(
    Guid EventId,
    string EventType,
    int SchemaVersion,
    DateTime OccurredAt,
    Guid ClienteId,
    ClienteCadastradoPayload Payload);

public sealed record ClienteCadastradoPayload(
    string Nome,
    string Cpf,
    string Email);
