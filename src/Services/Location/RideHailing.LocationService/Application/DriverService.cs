using RideHailing.LocationService.Application.Events;
using RideHailing.LocationService.Domain;
using RideHailing.LocationService.Infrastructure.Postgres;

namespace RideHailing.LocationService.Application;

public sealed class DriverService(ILocationStore locationStore, IOutboxRepository outboxRepository) : IDriverService
{
    public async Task ConnectAsync(Guid driverId, string connectionId, CancellationToken cancellationToken)
    {
        await locationStore.ConnectDriverAsync(driverId, connectionId, cancellationToken);
        await outboxRepository.AddAsync(DriverEventSerializer.Create(new DriverConnectedEvent(driverId, connectionId, DateTime.UtcNow)), cancellationToken);
    }

    public async Task DisconnectAsync(Guid driverId, string connectionId, CancellationToken cancellationToken)
    {
        await locationStore.DisconnectDriverAsync(driverId, connectionId, cancellationToken);
        await outboxRepository.AddAsync(DriverEventSerializer.Create(new DriverDisconnectedEvent(driverId, connectionId, DateTime.UtcNow)), cancellationToken);
    }

    public async Task SetAvailableAsync(Guid driverId, CancellationToken cancellationToken)
    {
        var previousState = await locationStore.SetAvailableAsync(driverId, cancellationToken);
        var currentState = DriverOperationalState.Available;
        await outboxRepository.AddAsync(DriverEventSerializer.Create(new DriverAvailabilityChangedEvent(driverId, previousState, currentState, DateTime.UtcNow)), cancellationToken);
    }

    public async Task SetBusyAsync(Guid driverId, CancellationToken cancellationToken)
    {
        var previousState = await locationStore.SetBusyAsync(driverId, cancellationToken);
        await outboxRepository.AddAsync(DriverEventSerializer.Create(new DriverAvailabilityChangedEvent(driverId, previousState, DriverOperationalState.Busy, DateTime.UtcNow)), cancellationToken);
    }

    public async Task SetOfflineAsync(Guid driverId, CancellationToken cancellationToken)
    {
        var previousState = await locationStore.SetOfflineAsync(driverId, cancellationToken);
        await outboxRepository.AddAsync(DriverEventSerializer.Create(new DriverAvailabilityChangedEvent(driverId, previousState, DriverOperationalState.Offline, DateTime.UtcNow)), cancellationToken);
    }

    public async Task UpdateLocationAsync(Guid driverId, double latitude, double longitude, CancellationToken cancellationToken)
    {
        await locationStore.UpdateLocationAsync(driverId, latitude, longitude, cancellationToken);
        await outboxRepository.AddAsync(DriverEventSerializer.Create(new DriverLocationUpdatedEvent(driverId, latitude, longitude, DateTime.UtcNow)), cancellationToken);
    }

    public Task HeartbeatAsync(Guid driverId, CancellationToken cancellationToken) =>
        locationStore.UpdateHeartbeatAsync(driverId, cancellationToken);
}