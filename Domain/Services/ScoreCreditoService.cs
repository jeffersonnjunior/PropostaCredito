using Domain.Exceptions;

namespace Domain.Services;

/// <summary>
/// Score determinístico 0..1000 a partir do CPF (reproduzível em testes e demo).
/// </summary>
public static class ScoreCreditoService
{
    public static int Calcular(string cpfDigitos)
    {
        if (string.IsNullOrWhiteSpace(cpfDigitos) || cpfDigitos.Length != 11 || !cpfDigitos.All(char.IsDigit))
            throw new DomainException("CPF inválido para cálculo de score.");

        // long.Parse do CPF % 1001 → faixa fechada [0, 1000]
        var valor = long.Parse(cpfDigitos);
        return (int)(valor % 1001);
    }
}
