using Confluent.Kafka;
using RideHailing.DriverMatching.Handlers;
using RideHailing.LocationService.Application.Events;
using System.Text.Json;

namespace RideHailing.DriverMatching.Consumers;

public sealed class DriverEventConsumer : BackgroundService
{
    private const string Topic = "driver.events";
    private const string ConsumerGroup = "driver-matching";
    private readonly IConfiguration _configuration;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<DriverEventConsumer> _logger;
    private IConsumer<string, string>? _consumer;

    public DriverEventConsumer(IConfiguration configuration, IServiceScopeFactory scopeFactory, ILogger<DriverEventConsumer> logger)
    {
        _configuration = configuration;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var config = new ConsumerConfig { BootstrapServers = _configuration["Kafka:BootstrapServers"], GroupId = ConsumerGroup, AutoOffsetReset = AutoOffsetReset.Earliest, EnableAutoCommit = false, EnableAutoOffsetStore = false };
        _consumer = new ConsumerBuilder<string, string>(config).Build();
        _consumer.Subscribe(Topic);

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                ConsumeResult<string, string> result;
                try { result = _consumer.Consume(stoppingToken); }
                catch (ConsumeException ex) { _logger.LogError(ex, "Kafka consume error"); continue; }

                try
                {
                    await ProcessMessageAsync(result, stoppingToken);
                    _consumer.Commit(result);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed processing event at {Offset}", result.TopicPartitionOffset);
                    // DO NOT COMMIT. Kafka will redeliver the message.
                }
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { }
        finally { _consumer.Close(); _consumer.Dispose(); }
    }

    private async Task ProcessMessageAsync(ConsumeResult<string, string> result, CancellationToken cancellationToken)
    {
        var @event = DeserializeEvent(result.Message.Headers, result.Message.Value);
        using var scope = _scopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<DriverEventHandler>();
        await handler.HandleAsync(@event, cancellationToken);
    }

    private static IDriverEvent DeserializeEvent(Headers headers, string payload)
    {
        var eventTypeBytes = headers.LastHeader("event-type")?.GetValueBytes();
        if (eventTypeBytes is null) throw new InvalidOperationException("Kafka message does not contain event-type header.");
        var eventType = System.Text.Encoding.UTF8.GetString(eventTypeBytes);
        return eventType switch
        {
            nameof(DriverAvailabilityChangedEvent) => JsonSerializer.Deserialize<DriverAvailabilityChangedEvent>(payload)!,
            nameof(DriverLocationUpdatedEvent) => JsonSerializer.Deserialize<DriverLocationUpdatedEvent>(payload)!,
            nameof(DriverConnectedEvent) => JsonSerializer.Deserialize<DriverConnectedEvent>(payload)!,
            nameof(DriverDisconnectedEvent) => JsonSerializer.Deserialize<DriverDisconnectedEvent>(payload)!,
            _ => throw new InvalidOperationException($"Unknown driver event type: {eventType}")
        };
    }
}