namespace RideHailing.DriverService.Infrastructure.Messaging;

public interface IKafkaPublisher
{
    Task PublishAsync(
        string topic,
        string key,
        string message,
        CancellationToken cancellationToken);
}