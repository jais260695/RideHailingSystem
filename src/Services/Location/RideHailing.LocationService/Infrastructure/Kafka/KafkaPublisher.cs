using Confluent.Kafka;
using System.Text;

namespace RideHailing.LocationService.Infrastructure.Kafka;

public sealed class KafkaPublisher(IProducer<string, string> producer) : IKafkaPublisher
{
    public async Task PublishRawAsync(string topic, string key, string eventType, string payload, Guid eventId, CancellationToken cancellationToken)
    {
        var msg = new Message<string, string>
        {
            Key = key,
            Value = payload,
            Headers = new Headers { 
                                    new Header("event-type", Encoding.UTF8.GetBytes(eventType)), 
                                    new Header("event-id", Encoding.UTF8.GetBytes(eventId.ToString())) 
                                  }
        };

        await producer.ProduceAsync(topic, msg, cancellationToken);
    }
}