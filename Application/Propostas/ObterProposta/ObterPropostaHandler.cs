using Application.Interfaces;

namespace Application.Propostas.ObterProposta;

public sealed class ObterPropostaHandler
{
    private readonly IPropostaRepository _propostas;

    public ObterPropostaHandler(IPropostaRepository propostas)
    {
        _propostas = propostas;
    }

    public async Task<PropostaResponse?> HandleAsync(
        Guid clienteId,
        CancellationToken cancellationToken = default)
    {
        var proposta = await _propostas.GetByClienteIdAsync(clienteId, cancellationToken);
        if (proposta is null)
            return null;

        return new PropostaResponse(
            proposta.Id,
            proposta.ClienteId,
            proposta.Score,
            proposta.Status.ToString(),
            proposta.QuantidadeCartoes,
            proposta.LimitePorCartao,
            proposta.LimiteTotal,
            proposta.CriadoEmUtc);
    }
}
