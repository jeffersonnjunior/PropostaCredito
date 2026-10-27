namespace Infrastructure.Persistence;

public sealed class ProcessedEvent
{
    public Guid EventId { get; set; }
    public string EventType { get; set; } = null!;
    public DateTime ProcessedAtUtc { get; set; }
}
