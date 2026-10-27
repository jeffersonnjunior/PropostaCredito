using Domain.Enums;
using Domain.Services;

namespace Domain.Entities;

public sealed class Proposta
{
    public Guid Id { get; private set; }
    public Guid ClienteId { get; private set; }
    public string Cpf { get; private set; } = null!;
    public int Score { get; private set; }
    public StatusProposta Status { get; private set; }
    public int QuantidadeCartoes { get; private set; }
    public decimal LimitePorCartao { get; private set; }
    public decimal LimiteTotal { get; private set; }
    public DateTime CriadoEmUtc { get; private set; }

    private Proposta()
    {
    }

    public static Proposta Criar(Guid clienteId, string cpfDigitos)
    {
        var score = ScoreCreditoService.Calcular(cpfDigitos);
        var regra = RegraPropostaService.Aplicar(score);

        return new Proposta
        {
            Id = Guid.NewGuid(),
            ClienteId = clienteId,
            Cpf = cpfDigitos,
            Score = score,
            Status = regra.Status,
            QuantidadeCartoes = regra.QuantidadeCartoes,
            LimitePorCartao = regra.LimitePorCartao,
            LimiteTotal = regra.LimiteTotal,
            CriadoEmUtc = DateTime.UtcNow
        };
    }
}
