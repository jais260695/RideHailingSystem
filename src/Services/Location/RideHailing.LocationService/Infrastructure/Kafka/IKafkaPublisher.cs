public interface IKafkaPublisher
{
    Task PublishRawAsync(
        string topic,
        string key,
        string eventType,
        string payload,
        Guid eventId,
        CancellationToken cancellationToken);
}