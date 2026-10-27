using Domain.Entities;

namespace Application.Propostas.GerarProposta;

/// <summary>
/// Side-effects da geração de proposta (persistência + outbox).
/// Orquestração/idempotência ficam no Handler.
/// </summary>
public interface IGerarPropostaReaction
{
    Task ExecuteAsync(Proposta proposta, CancellationToken cancellationToken = default);
}
