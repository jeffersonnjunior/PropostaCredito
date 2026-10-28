using Domain.Enums;
using Domain.Exceptions;
using Domain.Services;
using FluentAssertions;

namespace PropostaCredito.Tests.Domain;

public class RegraPropostaServiceTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(50)]
    [InlineData(100)]
    public void Aplicar_ScoreAte100_DeveNegar(int score)
    {
        // Arrange & Act
        var result = RegraPropostaService.Aplicar(score);

        // Assert
        result.Status.Should().Be(StatusProposta.Negada);
        result.QuantidadeCartoes.Should().Be(0);
        result.LimitePorCartao.Should().Be(0);
        result.LimiteTotal.Should().Be(0);
    }

    [Theory]
    [InlineData(101)]
    [InlineData(300)]
    [InlineData(500)]
    public void Aplicar_Score101a500_DeveAprovarUmCartao(int score)
    {
        // Arrange & Act
        var result = RegraPropostaService.Aplicar(score);

        // Assert
        result.Status.Should().Be(StatusProposta.Aprovada);
        result.QuantidadeCartoes.Should().Be(1);
        result.LimitePorCartao.Should().Be(1000m);
        result.LimiteTotal.Should().Be(1000m);
    }

    [Theory]
    [InlineData(501)]
    [InlineData(800)]
    [InlineData(1000)]
    public void Aplicar_Score501a1000_DeveAprovarDoisCartoes(int score)
    {
        // Arrange & Act
        var result = RegraPropostaService.Aplicar(score);

        // Assert
        result.Status.Should().Be(StatusProposta.Aprovada);
        result.QuantidadeCartoes.Should().Be(2);
        result.LimitePorCartao.Should().Be(5000m);
        result.LimiteTotal.Should().Be(10000m);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(1001)]
    public void Aplicar_ScoreForaDaFaixa_DeveFalhar(int score)
    {
        // Arrange & Act
        var act = () => RegraPropostaService.Aplicar(score);

        // Assert
        act.Should().Throw<DomainException>();
    }
}
