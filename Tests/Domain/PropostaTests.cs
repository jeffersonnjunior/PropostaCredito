using Domain.Entities;
using Domain.Enums;
using FluentAssertions;

namespace PropostaCredito.Tests.Domain;

public class PropostaTests
{
    [Fact]
    public void Criar_ScoreAte100_DeveNegarSemCartoes()
    {
        // Arrange — CPF sintético com score 77 (77 % 1001 = 77)
        const string cpf = "00000000077";
        var clienteId = Guid.NewGuid();

        // Act
        var proposta = Proposta.Criar(clienteId, cpf);

        // Assert
        proposta.Score.Should().Be(77);
        proposta.Status.Should().Be(StatusProposta.Negada);
        proposta.QuantidadeCartoes.Should().Be(0);
        proposta.LimiteTotal.Should().Be(0);
    }

    [Fact]
    public void Criar_Score101a500_DeveAprovarUmCartao()
    {
        // Arrange — score 455
        const string cpf = "00000000455";
        var clienteId = Guid.NewGuid();

        // Act
        var proposta = Proposta.Criar(clienteId, cpf);

        // Assert
        proposta.Score.Should().Be(455);
        proposta.Status.Should().Be(StatusProposta.Aprovada);
        proposta.QuantidadeCartoes.Should().Be(1);
        proposta.LimitePorCartao.Should().Be(1000m);
        proposta.LimiteTotal.Should().Be(1000m);
    }

    [Fact]
    public void Criar_Score501a1000_DeveAprovarDoisCartoes()
    {
        // Arrange — score 800
        const string cpf = "00000000800";
        var clienteId = Guid.NewGuid();

        // Act
        var proposta = Proposta.Criar(clienteId, cpf);

        // Assert
        proposta.Score.Should().Be(800);
        proposta.Status.Should().Be(StatusProposta.Aprovada);
        proposta.QuantidadeCartoes.Should().Be(2);
        proposta.LimitePorCartao.Should().Be(5000m);
        proposta.LimiteTotal.Should().Be(10000m);
    }
}
