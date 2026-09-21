using RideHailing.LocationService.Infrastructure.Postgres;

namespace RideHailing.LocationService.Background;

public sealed class OutboxPublisherWorker(IServiceScopeFactory scopeFactory, ILogger<OutboxPublisherWorker> logger) : BackgroundService
{
    private const int BatchSize = 100;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessBatchAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error processing outbox.");
            }

            await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
        }
    }

    private async Task ProcessBatchAsync(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IOutboxRepository>();
        var publisher = scope.ServiceProvider.GetRequiredService<IKafkaPublisher>();
        // Claim pending messages using DB-level locking to avoid multiple workers
        // processing the same messages concurrently.
        var messages = await repository.GetAndClaimPendingAsync(BatchSize,TimeSpan.FromMinutes(1),6,cancellationToken);

        foreach (var message in messages)
        {
            try
            {
                await publisher.PublishRawAsync("driver.events", message.AggregateId, message.EventType, message.Payload, message.EventId, cancellationToken);
                await repository.MarkPublishedAsync(message.Id, cancellationToken);
            }
            catch (Exception ex)
            {
                var nextAttempt = DateTime.UtcNow.AddSeconds(CalculateRetryDelay(message.RetryCount));
                await repository.MarkFailedAsync(message.Id, ex.Message, nextAttempt, cancellationToken);
                logger.LogError(ex, "Failed publishing outbox message {MessageId}.", message.Id);
            }
        }
    }

    private static int CalculateRetryDelay(int retryCount)
    {
        var seconds = Math.Pow(2, Math.Min(retryCount, 6));
        return (int)Math.Min(seconds, 60);
    }
}