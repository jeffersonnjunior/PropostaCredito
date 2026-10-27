using Application.Interfaces;
using Application.Propostas.GerarProposta;
using Domain.Entities;
using NSubstitute;

namespace PropostaCredito.Tests.Application;

public class GerarPropostaReactionTests
{
    [Fact]
    public async Task ExecuteAsync_DevePersistirPropostaEEnfileirarOutbox()
    {
        // Arrange
        var propostas = Substitute.For<IPropostaRepository>();
        var outbox = Substitute.For<IOutboxWriter>();
        var sut = new GerarPropostaReaction(propostas, outbox);
        var proposta = Proposta.Criar(Guid.NewGuid(), "52998224725");

        // Act
        await sut.ExecuteAsync(proposta);

        // Assert
        await propostas.Received(1).AddAsync(proposta, Arg.Any<CancellationToken>());
        await outbox.Received(1).EnqueueAsync(
            "PropostaGerada",
            Arg.Any<string>(),
            Arg.Any<Guid>(),
            Arg.Any<CancellationToken>());
    }
}
