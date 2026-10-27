using Domain.Exceptions;
using Domain.Services;
using FluentAssertions;

namespace PropostaCredito.Tests.Domain;

public class ScoreCreditoServiceTests
{
    [Fact]
    public void Calcular_ComCpfValido_DeveRetornarEntre0e1000()
    {
        // Arrange
        const string cpf = "52998224725";

        // Act
        var score = ScoreCreditoService.Calcular(cpf);

        // Assert
        score.Should().BeInRange(0, 1000);
        score.Should().Be((int)(52998224725L % 1001));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("123")]
    public void Calcular_ComCpfInvalido_DeveFalhar(string? cpf)
    {
        var act = () => ScoreCreditoService.Calcular(cpf!);
        act.Should().Throw<DomainException>();
    }
}
