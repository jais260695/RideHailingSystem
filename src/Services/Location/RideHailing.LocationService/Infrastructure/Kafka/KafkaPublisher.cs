using Confluent.Kafka;
using System.Text;

namespace RideHailing.LocationService.Infrastructure.Kafka;

public sealed class KafkaPublisher(IProducer<string, string> producer) : IKafkaPublisher
{
    public async Task PublishRawAsync(string topic, string key, string eventType, string payload, CancellationToken cancellationToken)
    {
        var msg = new Message<string, string>
        {
            Key = key,
            Value = payload,
            Headers = new Headers { new Header("event-type", Encoding.UTF8.GetBytes(eventType)) }
        };

        await producer.ProduceAsync(topic, msg, cancellationToken);
    }
}