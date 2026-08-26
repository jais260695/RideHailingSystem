using RideHailing.LocationService.Domain;

namespace RideHailing.LocationService.Application;

public interface ILocationStore
{
    Task SetLifecycleStateAsync(Guid driverId, DriverLifecycleState state, CancellationToken cancellationToken);
    Task ConnectDriverAsync(Guid driverId, string connectionId, CancellationToken cancellationToken);
    Task DisconnectDriverAsync(Guid driverId, string connectionId, CancellationToken cancellationToken);
    Task SetAvailableAsync(Guid driverId, CancellationToken cancellationToken);
    Task SetOfflineAsync(Guid driverId, CancellationToken cancellationToken);
    Task SetBusyAsync(Guid driverId, CancellationToken cancellationToken);
    Task UpdateLocationAsync(Guid driverId, double latitude, double longitude, CancellationToken cancellationToken);
    Task UpdateHeartbeatAsync(Guid driverId, CancellationToken cancellationToken);
    Task<DriverRealtimeState?> GetStateAsync(Guid driverId, CancellationToken cancellationToken);
    Task<IReadOnlyList<Guid>> FindNearbyAvailableDriversAsync(double latitude, double longitude, double radiusInKm, int maxResults, CancellationToken cancellationToken);
}