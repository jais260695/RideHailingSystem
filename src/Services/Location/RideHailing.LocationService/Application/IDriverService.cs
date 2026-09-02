namespace RideHailing.LocationService.Application;

public interface IDriverService
{
    Task ConnectAsync(Guid driverId, string connectionId, CancellationToken cancellationToken);
    Task DisconnectAsync(Guid driverId, string connectionId, CancellationToken cancellationToken);
    Task SetAvailableAsync(Guid driverId, CancellationToken cancellationToken);
    Task SetBusyAsync(Guid driverId, CancellationToken cancellationToken);
    Task SetOfflineAsync(Guid driverId, CancellationToken cancellationToken);
    Task UpdateLocationAsync(Guid driverId, double latitude, double longitude, CancellationToken cancellationToken);
    Task HeartbeatAsync(Guid driverId, CancellationToken cancellationToken);
}
