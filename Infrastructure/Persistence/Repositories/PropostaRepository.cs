using Application.Interfaces;
using Domain.Entities;
using Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories;

public sealed class PropostaRepository : IPropostaRepository
{
    private readonly PropostaDbContext _db;

    public PropostaRepository(PropostaDbContext db)
    {
        _db = db;
    }

    public async Task AddAsync(Proposta proposta, CancellationToken cancellationToken = default)
    {
        await _db.Propostas.AddAsync(proposta, cancellationToken);
    }

    public Task<Proposta?> GetByClienteIdAsync(Guid clienteId, CancellationToken cancellationToken = default)
    {
        return _db.Propostas.AsNoTracking()
            .FirstOrDefaultAsync(x => x.ClienteId == clienteId, cancellationToken);
    }

    public Task<bool> ExistsByClienteIdAsync(Guid clienteId, CancellationToken cancellationToken = default)
    {
        return _db.Propostas.AnyAsync(x => x.ClienteId == clienteId, cancellationToken);
    }
}
