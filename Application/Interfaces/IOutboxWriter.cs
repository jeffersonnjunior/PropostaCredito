namespace Application.Interfaces;

public interface IOutboxWriter
{
    Task EnqueueAsync(
        string eventType,
        string payloadJson,
        Guid eventId,
        CancellationToken cancellationToken = default);
}
