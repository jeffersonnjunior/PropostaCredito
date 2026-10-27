using Application.Interfaces;
using Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories;

public sealed class ProcessedEventStore : IProcessedEventStore
{
    private readonly PropostaDbContext _db;

    public ProcessedEventStore(PropostaDbContext db)
    {
        _db = db;
    }

    public Task<bool> ExistsAsync(Guid eventId, CancellationToken cancellationToken = default)
    {
        return _db.ProcessedEvents.AnyAsync(x => x.EventId == eventId, cancellationToken);
    }

    public async Task MarkProcessedAsync(
        Guid eventId,
        string eventType,
        CancellationToken cancellationToken = default)
    {
        await _db.ProcessedEvents.AddAsync(new ProcessedEvent
        {
            EventId = eventId,
            EventType = eventType,
            ProcessedAtUtc = DateTime.UtcNow
        }, cancellationToken);
    }
}
