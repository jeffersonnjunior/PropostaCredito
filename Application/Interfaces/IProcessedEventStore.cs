namespace Application.Interfaces;

public interface IProcessedEventStore
{
    Task<bool> ExistsAsync(Guid eventId, CancellationToken cancellationToken = default);
    Task MarkProcessedAsync(Guid eventId, string eventType, CancellationToken cancellationToken = default);
}
