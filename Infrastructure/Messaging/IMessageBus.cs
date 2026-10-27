namespace Infrastructure.Messaging;

public interface IMessageBus
{
    Task PublishAsync(string eventType, string payloadJson, CancellationToken cancellationToken = default);
}
