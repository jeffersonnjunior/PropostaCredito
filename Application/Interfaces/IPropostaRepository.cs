using Domain.Entities;

namespace Application.Interfaces;

public interface IPropostaRepository
{
    Task AddAsync(Proposta proposta, CancellationToken cancellationToken = default);
    Task<Proposta?> GetByClienteIdAsync(Guid clienteId, CancellationToken cancellationToken = default);
    Task<bool> ExistsByClienteIdAsync(Guid clienteId, CancellationToken cancellationToken = default);
}
