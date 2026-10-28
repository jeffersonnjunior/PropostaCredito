using Application.Events;
using Application.Interfaces;
using Application.Propostas.GerarProposta;
using Domain.Entities;
using Domain.Enums;
using NSubstitute;

namespace PropostaCredito.Tests.Application;

public class GerarPropostaFromClienteCadastradoHandlerTests
{
    private readonly IPropostaRepository _propostas = Substitute.For<IPropostaRepository>();
    private readonly IProcessedEventStore _processed = Substitute.For<IProcessedEventStore>();
    private readonly IGerarPropostaReaction _reaction = Substitute.For<IGerarPropostaReaction>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();

    private GerarPropostaFromClienteCadastradoHandler CreateSut() =>
        new(_propostas, _processed, _reaction, _uow);

    private static ClienteCadastradoEvent CreateEvent(Guid eventId, Guid clienteId, string cpf) =>
        new(
            eventId,
            "ClienteCadastrado",
            1,
            DateTime.UtcNow,
            clienteId,
            new ClienteCadastradoPayload("Maria", cpf, "maria@email.com"));

    [Fact]
    public async Task HandleAsync_QuandoEventoJaProcessado_NaoDeveExecutarReaction()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        _processed.ExistsAsync(eventId, Arg.Any<CancellationToken>()).Returns(true);
        var sut = CreateSut();

        // Act
        await sut.HandleAsync(CreateEvent(eventId, Guid.NewGuid(), "52998224725"));

        // Assert
        await _reaction.DidNotReceive().ExecuteAsync(Arg.Any<Proposta>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_QuandoNovoCliente_DeveOrquestrarReactionEMarcarProcessado()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var clienteId = Guid.NewGuid();
        _processed.ExistsAsync(eventId, Arg.Any<CancellationToken>()).Returns(false);
        _propostas.ExistsByClienteIdAsync(clienteId, Arg.Any<CancellationToken>()).Returns(false);
        var sut = CreateSut();

        // Act
        await sut.HandleAsync(CreateEvent(eventId, clienteId, "52998224725"));

        // Assert
        await _reaction.Received(1).ExecuteAsync(
            Arg.Is<Proposta>(p => p.ClienteId == clienteId),
            Arg.Any<CancellationToken>());
        await _processed.Received(1).MarkProcessedAsync(eventId, "ClienteCadastrado", Arg.Any<CancellationToken>());
        await _uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_QuandoClienteJaTemProposta_DeveSerIdempotente()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var clienteId = Guid.NewGuid();
        _processed.ExistsAsync(eventId, Arg.Any<CancellationToken>()).Returns(false);
        _propostas.ExistsByClienteIdAsync(clienteId, Arg.Any<CancellationToken>()).Returns(true);
        var sut = CreateSut();

        // Act
        await sut.HandleAsync(CreateEvent(eventId, clienteId, "52998224725"));

        // Assert
        await _reaction.DidNotReceive().ExecuteAsync(Arg.Any<Proposta>(), Arg.Any<CancellationToken>());
        await _processed.Received(1).MarkProcessedAsync(eventId, "ClienteCadastrado", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_QuandoScoreBaixo_DeveGerarPropostaNegada()
    {
        // Arrange — score 77 → Negada / 0 cartões
        var eventId = Guid.NewGuid();
        var clienteId = Guid.NewGuid();
        _processed.ExistsAsync(eventId, Arg.Any<CancellationToken>()).Returns(false);
        _propostas.ExistsByClienteIdAsync(clienteId, Arg.Any<CancellationToken>()).Returns(false);
        var sut = CreateSut();

        // Act
        await sut.HandleAsync(CreateEvent(eventId, clienteId, "00000000077"));

        // Assert
        await _reaction.Received(1).ExecuteAsync(
            Arg.Is<Proposta>(p =>
                p.Status == StatusProposta.Negada &&
                p.QuantidadeCartoes == 0),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_QuandoScoreMedio_DeveGerarUmCartao()
    {
        // Arrange — score 455 → 1 cartão R$ 1.000
        var eventId = Guid.NewGuid();
        var clienteId = Guid.NewGuid();
        _processed.ExistsAsync(eventId, Arg.Any<CancellationToken>()).Returns(false);
        _propostas.ExistsByClienteIdAsync(clienteId, Arg.Any<CancellationToken>()).Returns(false);
        var sut = CreateSut();

        // Act
        await sut.HandleAsync(CreateEvent(eventId, clienteId, "00000000455"));

        // Assert
        await _reaction.Received(1).ExecuteAsync(
            Arg.Is<Proposta>(p =>
                p.Status == StatusProposta.Aprovada &&
                p.QuantidadeCartoes == 1 &&
                p.LimitePorCartao == 1000m),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_QuandoScoreAlto_DeveGerarDoisCartoes()
    {
        // Arrange — score 800 → 2 cartões R$ 5.000
        var eventId = Guid.NewGuid();
        var clienteId = Guid.NewGuid();
        _processed.ExistsAsync(eventId, Arg.Any<CancellationToken>()).Returns(false);
        _propostas.ExistsByClienteIdAsync(clienteId, Arg.Any<CancellationToken>()).Returns(false);
        var sut = CreateSut();

        // Act
        await sut.HandleAsync(CreateEvent(eventId, clienteId, "00000000800"));

        // Assert
        await _reaction.Received(1).ExecuteAsync(
            Arg.Is<Proposta>(p =>
                p.Status == StatusProposta.Aprovada &&
                p.QuantidadeCartoes == 2 &&
                p.LimitePorCartao == 5000m),
            Arg.Any<CancellationToken>());
    }
}
