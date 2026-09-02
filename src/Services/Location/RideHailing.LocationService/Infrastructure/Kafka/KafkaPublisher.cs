using Confluent.Kafka;
using RideHailing.LocationService.Application.Events;
using System.Text.Json;

namespace RideHailing.LocationService.Infrastructure.Kafka;

public sealed class KafkaPublisher : IKafkaPublisher
{
    private readonly IProducer<string, string> _producer;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public KafkaPublisher(IProducer<string, string> producer)
    {
        _producer = producer;
    }

    public async Task PublishAsync<T>(
        string topic,
        string key,
        T message,
        CancellationToken cancellationToken = default)
        where T : IDriverEvent
    {
        var envelope = new DriverEventEnvelope(
            typeof(T).Name,
            message.DriverId,
            message.OccurredAtUtc,
            message);

        var payload = JsonSerializer.Serialize(envelope, JsonOptions);

        await _producer.ProduceAsync(
            topic,
            new Message<string, string>
            {
                Key = key,
                Value = payload
            },
            cancellationToken);
    }
}