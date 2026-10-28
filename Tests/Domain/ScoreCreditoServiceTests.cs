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

    [Fact]
    public void Calcular_DeveSerDeterministicoParaMesmoCpf()
    {
        // Arrange
        const string cpf = "39053344705";

        // Act
        var a = ScoreCreditoService.Calcular(cpf);
        var b = ScoreCreditoService.Calcular(cpf);

        // Assert
        a.Should().Be(b);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("123")]
    [InlineData("abcdefghijk")]
    public void Calcular_ComCpfInvalido_DeveFalhar(string? cpf)
    {
        // Arrange & Act
        var act = () => ScoreCreditoService.Calcular(cpf!);

        // Assert
        act.Should().Throw<DomainException>();
    }
}
