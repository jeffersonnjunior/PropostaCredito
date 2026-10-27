namespace Application.Propostas.ObterProposta;

public sealed record PropostaResponse(
    Guid Id,
    Guid ClienteId,
    int Score,
    string Status,
    int QuantidadeCartoes,
    decimal LimitePorCartao,
    decimal LimiteTotal,
    DateTime CriadoEmUtc);
