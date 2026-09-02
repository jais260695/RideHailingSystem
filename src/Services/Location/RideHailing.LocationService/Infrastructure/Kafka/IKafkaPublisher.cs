using RideHailing.LocationService.Application.Events;

namespace RideHailing.LocationService.Infrastructure.Kafka;

public interface IKafkaPublisher
{
    Task PublishAsync<T>(string topic, string key, T message, CancellationToken cancellationToken = default)
        where T : IDriverEvent;
}