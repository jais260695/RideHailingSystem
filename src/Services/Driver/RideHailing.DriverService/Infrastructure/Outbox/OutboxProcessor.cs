using System;
using System.Threading;
using System.Threading.Tasks;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RideHailing.DriverService.Infrastructure.Messaging;
using RideHailing.DriverService.Infrastructure.Persistence;

namespace RideHailing.DriverService.Infrastructure.Outbox;

public sealed class OutboxProcessor : BackgroundService
{
    private const int BatchSize = 100;

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IKafkaPublisher _kafkaPublisher;
    private readonly ILogger<OutboxProcessor> _logger;

    public OutboxProcessor(
        IServiceScopeFactory scopeFactory,
        IKafkaPublisher kafkaPublisher,
        ILogger<OutboxProcessor> logger)
    {
        _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
        _kafkaPublisher = kafkaPublisher ?? throw new ArgumentNullException(nameof(kafkaPublisher));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Outbox processor started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var processed = await ProcessBatchAsync(stoppingToken);

                if (processed == 0)
                {
                    await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing outbox.");
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }

        _logger.LogInformation("Outbox processor stopped.");
    }

    private async Task<int> ProcessBatchAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();

        var db = scope.ServiceProvider.GetRequiredService<DriverDbContext>();

        var messages = await db.OutboxMessages
            .Where(x => x.ProcessedAtUtc == null)
            .OrderBy(x => x.OccurredAtUtc)
            .Take(BatchSize)
            .ToListAsync(cancellationToken);

        foreach (var message in messages)
        {
            await ProcessMessageAsync(db, message, cancellationToken);
        }

        return messages.Count;
    }

    private async Task ProcessMessageAsync(
        DriverDbContext db,
        Domain.Entities.OutboxMessage message,
        CancellationToken cancellationToken)
    {
        try
        {
            var topic = GetTopic(message.Type);

            await _kafkaPublisher.PublishAsync(
                topic,
                message.Id.ToString(),
                message.Payload,
                cancellationToken);

            message.MarkProcessed();

            await db.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Published outbox message {MessageId} of type {MessageType}",
                message.Id,
                message.Type);
        }
        catch (Exception ex)
        {
            message.MarkFailed(ex.Message);

            await db.SaveChangesAsync(cancellationToken);

            _logger.LogError(ex, "Failed to publish outbox message {MessageId}", message.Id);
        }
    }

    private static string GetTopic(string eventType)
    {
        return eventType switch
        {
            "DriverSuspendedEvent" => "driver-events",
            "DriverActivatedEvent" => "driver-events",
            "DriverDeactivatedEvent" => "driver-events",
            _ => throw new InvalidOperationException($"Unknown event type: {eventType}")
        };
    }
}