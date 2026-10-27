using Application.Events;
using Application.Interfaces;
using Application.Propostas.GerarProposta;
using Domain.Entities;
using FluentAssertions;
using NSubstitute;

namespace PropostaCredito.Tests.Application;

public class GerarPropostaFromClienteCadastradoHandlerTests
{
    private readonly IPropostaRepository _propostas = Substitute.For<IPropostaRepository>();
    private readonly IProcessedEventStore _processed = Substitute.For<IProcessedEventStore>();
    private readonly IOutboxWriter _outbox = Substitute.For<IOutboxWriter>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();

    private GerarPropostaFromClienteCadastradoHandler CreateSut() =>
        new(_propostas, _processed, _outbox, _uow);

    private static ClienteCadastradoEvent CreateEvent(Guid eventId, Guid clienteId, string cpf) =>
        new(
            eventId,
            "ClienteCadastrado",
            1,
            DateTime.UtcNow,
            clienteId,
            new ClienteCadastradoPayload("Maria", cpf, "maria@email.com"));

    [Fact]
    public async Task HandleAsync_QuandoEventoJaProcessado_NaoDeveGerarProposta()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        _processed.ExistsAsync(eventId, Arg.Any<CancellationToken>()).Returns(true);
        var sut = CreateSut();

        // Act
        await sut.HandleAsync(CreateEvent(eventId, Guid.NewGuid(), "52998224725"));

        // Assert
        await _propostas.DidNotReceive().AddAsync(Arg.Any<Proposta>(), Arg.Any<CancellationToken>());
        await _outbox.DidNotReceive().EnqueueAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_QuandoNovoCliente_DevePersistirPropostaEOutbox()
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
        await _propostas.Received(1).AddAsync(Arg.Any<Proposta>(), Arg.Any<CancellationToken>());
        await _outbox.Received(1).EnqueueAsync(
            "PropostaGerada",
            Arg.Any<string>(),
            Arg.Any<Guid>(),
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
        await _propostas.DidNotReceive().AddAsync(Arg.Any<Proposta>(), Arg.Any<CancellationToken>());
        await _processed.Received(1).MarkProcessedAsync(eventId, "ClienteCadastrado", Arg.Any<CancellationToken>());
    }
}
