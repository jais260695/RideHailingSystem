using RideHailing.DriverMatching.Persistence;
using RideHailing.LocationService.Application.Events;

namespace RideHailing.DriverMatching.Handlers;

public sealed class DriverEventHandler
{
    private const string ConsumerGroup = "driver-matching";

    private readonly ConsumerDbContext _db;
    private readonly ProcessedEventRepository _processedEvents;
    private readonly ILogger<DriverEventHandler> _logger;

    public DriverEventHandler(ConsumerDbContext db, ProcessedEventRepository processedEvents, ILogger<DriverEventHandler> logger)
        => (_db, _processedEvents, _logger) = (db, processedEvents, logger);

    public async Task HandleAsync(IDriverEvent @event, CancellationToken cancellationToken)
    {
        await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);

        var isNewEvent = await _processedEvents.TryMarkProcessedAsync(ConsumerGroup, @event.EventId, cancellationToken);
        if (!isNewEvent)
        {
            _logger.LogInformation("Ignoring duplicate event {EventId}", @event.EventId);
            await transaction.CommitAsync(cancellationToken);
            return;
        }

        await ProcessEventAsync(@event, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    private Task ProcessEventAsync(IDriverEvent @event, CancellationToken cancellationToken)
        => @event switch
        {
            DriverAvailabilityChangedEvent availability => HandleAvailabilityChangedAsync(availability, cancellationToken),
            DriverLocationUpdatedEvent location => HandleLocationUpdatedAsync(location, cancellationToken),
            DriverConnectedEvent connected => HandleConnectedAsync(connected, cancellationToken),
            DriverDisconnectedEvent disconnected => HandleDisconnectedAsync(disconnected, cancellationToken),
            _ => throw new InvalidOperationException($"Unsupported event type: {@event.GetType().Name}")
        };

    private Task HandleAvailabilityChangedAsync(DriverAvailabilityChangedEvent @event, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Driver {DriverId}: {PreviousState} -> {CurrentState}", @event.DriverId, @event.PreviousState, @event.CurrentState);
        return Task.CompletedTask;
    }

    private Task HandleLocationUpdatedAsync(DriverLocationUpdatedEvent @event, CancellationToken cancellationToken) => Task.CompletedTask;
    private Task HandleConnectedAsync(DriverConnectedEvent @event, CancellationToken cancellationToken) => Task.CompletedTask;
    private Task HandleDisconnectedAsync(DriverDisconnectedEvent @event, CancellationToken cancellationToken) => Task.CompletedTask;
}