using Confluent.Kafka;

namespace RideHailing.DriverService.Infrastructure.Messaging;

public sealed class KafkaPublisher : IKafkaPublisher
{
    private readonly IProducer<string, string> _producer;

    public KafkaPublisher(IConfiguration configuration)
    {
        var bootstrapServers = configuration["Kafka:BootstrapServers"]
                                ?? throw new InvalidOperationException(
                                    "Kafka bootstrap servers are not configured.");

        var config = new ProducerConfig
        {
            BootstrapServers = bootstrapServers,

            // Wait for Kafka acknowledgement.
            Acks = Acks.All,

            // Retry transient failures.
            MessageSendMaxRetries = 5,

            // Keep messages ordered for the same key.
            EnableIdempotence = true
        };

        _producer =
            new ProducerBuilder<string, string>(config)
                .Build();
    }

    public async Task PublishAsync(
        string topic,
        string key,
        string message,
        CancellationToken cancellationToken)
    {
        await _producer.ProduceAsync(
            topic,
            new Message<string, string>
            {
                Key = key,
                Value = message
            },
            cancellationToken);
    }
}