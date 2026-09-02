using Microsoft.AspNetCore.SignalR;
using RideHailing.LocationService.Application;
using RideHailing.LocationService.Security;

namespace RideHailing.LocationService.Hubs;

public sealed class DriverHub(IDriverService driverService, IDriverIdentity driverIdentity) : Hub
{
    public async Task Connect(CancellationToken cancellationToken)
    {
        var driverId = GetDriverId();
        await driverService.ConnectAsync(driverId, Context.ConnectionId, cancellationToken);
        Context.Items["DriverId"] = driverId;
    }

    public Task UpdateLocation(double latitude, double longitude, CancellationToken cancellationToken) =>
        driverService.UpdateLocationAsync(GetDriverId(), latitude, longitude, cancellationToken);

    public Task Heartbeat(CancellationToken cancellationToken) =>
        driverService.HeartbeatAsync(GetDriverId(), cancellationToken);

    public Task SetAvailable(CancellationToken cancellationToken) =>
        driverService.SetAvailableAsync(GetDriverId(), cancellationToken);

    public Task SetBusy(CancellationToken cancellationToken) =>
        driverService.SetBusyAsync(GetDriverId(), cancellationToken);

    public Task SetOffline(CancellationToken cancellationToken) =>
        driverService.SetOfflineAsync(GetDriverId(), cancellationToken);

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        if (Context.Items.TryGetValue("DriverId", out var value) && value is Guid driverId)
            await driverService.DisconnectAsync(driverId, Context.ConnectionId, CancellationToken.None);

        await base.OnDisconnectedAsync(exception);
    }

    private Guid GetDriverId() => driverIdentity.GetDriverId(Context.User ?? throw new HubException("User context is unavailable."));
}
