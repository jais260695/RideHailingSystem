using Microsoft.AspNetCore.SignalR;
using RideHailing.LocationService.Application;
using RideHailing.LocationService.Security;

namespace RideHailing.LocationService.Hubs;

public sealed class DriverHub : Hub
{
    private readonly ILocationStore _locationStore;
    private readonly IDriverIdentity _driverIdentity;
    private readonly ILogger<DriverHub> _logger;

    public DriverHub(ILocationStore locationStore, IDriverIdentity driverIdentity, ILogger<DriverHub> logger)
    {
        _locationStore = locationStore ?? throw new ArgumentNullException(nameof(locationStore));
        _driverIdentity = driverIdentity ?? throw new ArgumentNullException(nameof(driverIdentity));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task Connect(CancellationToken cancellationToken)
    {
        var driverId = GetDriverId();
        await _locationStore.ConnectDriverAsync(driverId, Context.ConnectionId, cancellationToken);
        Context.Items["DriverId"] = driverId;
        _logger.LogInformation("Driver {DriverId} connected. Connection {ConnectionId}", driverId, Context.ConnectionId);
    }

    public async Task UpdateLocation(double latitude, double longitude, CancellationToken cancellationToken)
    {
        var driverId = GetDriverId();
        await _locationStore.UpdateLocationAsync(driverId, latitude, longitude, cancellationToken);
    }

    public async Task Heartbeat(CancellationToken cancellationToken)
    {
        var driverId = GetDriverId();
        await _locationStore.UpdateHeartbeatAsync(driverId, cancellationToken);
    }

    public async Task SetAvailable(CancellationToken cancellationToken)
    {
        var driverId = GetDriverId();
        await _locationStore.SetAvailableAsync(driverId, cancellationToken);
    }

    public async Task SetBusy(CancellationToken cancellationToken)
    {
        var driverId = GetDriverId();
        await _locationStore.SetBusyAsync(driverId, cancellationToken);
    }

    public async Task SetOffline(CancellationToken cancellationToken)
    {
        var driverId = GetDriverId();
        await _locationStore.SetOfflineAsync(driverId, cancellationToken);
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        if (Context.Items.TryGetValue("DriverId", out var value) && value is Guid driverId)
        {
            try
            {
                await _locationStore.DisconnectDriverAsync(driverId, Context.ConnectionId, CancellationToken.None);
                _logger.LogInformation("Driver {DriverId} disconnected. Connection {ConnectionId}", driverId, Context.ConnectionId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process disconnect for driver {DriverId}", driverId);
            }
        }

        await base.OnDisconnectedAsync(exception);
    }

    private Guid GetDriverId() =>
        _driverIdentity.GetDriverId(Context.User ?? throw new HubException("User context is unavailable."));
}