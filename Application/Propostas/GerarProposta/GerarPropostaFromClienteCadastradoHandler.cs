using Application.Events;
using Application.Interfaces;
using Domain.Entities;

namespace Application.Propostas.GerarProposta;

/// <summary>
/// Orquestra o job "GerarPropostaCredito": gates de idempotência + domínio + reaction.
/// </summary>
public sealed class GerarPropostaFromClienteCadastradoHandler
{
    public const string JobDisplayName = "GerarPropostaCredito";

    private readonly IPropostaRepository _propostas;
    private readonly IProcessedEventStore _processedEvents;
    private readonly IGerarPropostaReaction _reaction;
    private readonly IUnitOfWork _unitOfWork;

    public GerarPropostaFromClienteCadastradoHandler(
        IPropostaRepository propostas,
        IProcessedEventStore processedEvents,
        IGerarPropostaReaction reaction,
        IUnitOfWork unitOfWork)
    {
        _propostas = propostas;
        _processedEvents = processedEvents;
        _reaction = reaction;
        _unitOfWork = unitOfWork;
    }

    public async Task HandleAsync(
        ClienteCadastradoEvent @event,
        CancellationToken cancellationToken = default)
    {
        // Job chaveado por eventId (at-least-once do broker)
        if (await _processedEvents.ExistsAsync(@event.EventId, cancellationToken))
            return;

        if (await _propostas.ExistsByClienteIdAsync(@event.ClienteId, cancellationToken))
        {
            await _processedEvents.MarkProcessedAsync(@event.EventId, @event.EventType, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return;
        }

        var cpfDigitos = new string(@event.Payload.Cpf.Where(char.IsDigit).ToArray());
        var proposta = Proposta.Criar(@event.ClienteId, cpfDigitos);

        await _reaction.ExecuteAsync(proposta, cancellationToken);
        await _processedEvents.MarkProcessedAsync(@event.EventId, @event.EventType, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
