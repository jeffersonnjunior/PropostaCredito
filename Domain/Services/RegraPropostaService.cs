using Domain.Enums;
using Domain.Exceptions;

namespace Domain.Services;

public sealed record ResultadoRegraProposta(
    StatusProposta Status,
    int QuantidadeCartoes,
    decimal LimitePorCartao,
    decimal LimiteTotal);

public static class RegraPropostaService
{
    public static ResultadoRegraProposta Aplicar(int score)
    {
        if (score is < 0 or > 1000)
            throw new DomainException($"Score fora da faixa permitida: {score}.");

        if (score <= 100)
        {
            return new ResultadoRegraProposta(
                StatusProposta.Negada,
                QuantidadeCartoes: 0,
                LimitePorCartao: 0m,
                LimiteTotal: 0m);
        }

        if (score <= 500)
        {
            return new ResultadoRegraProposta(
                StatusProposta.Aprovada,
                QuantidadeCartoes: 1,
                LimitePorCartao: 1000m,
                LimiteTotal: 1000m);
        }

        // 501..1000 → até 2 cartões; regra travada: emite 2 x R$ 5.000
        return new ResultadoRegraProposta(
            StatusProposta.Aprovada,
            QuantidadeCartoes: 2,
            LimitePorCartao: 5000m,
            LimiteTotal: 10000m);
    }
}
