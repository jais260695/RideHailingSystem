using RideHailing.LocationService.Application;
using RideHailing.LocationService.Domain;
using StackExchange.Redis;

namespace RideHailing.LocationService.Infrastructure.Redis;

public sealed class RedisLocationStore : ILocationStore
{
    private const int HeartbeatTtlSeconds = 30;
    private readonly IDatabase _database;

    public RedisLocationStore(IConnectionMultiplexer redis) => _database = redis.GetDatabase();

    public async Task SetLifecycleStateAsync(Guid driverId, DriverLifecycleState state, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await _database.ScriptEvaluateAsync(
            RedisScripts.SetLifecycle,
            new RedisKey[] { RedisKeys.Lifecycle(driverId), RedisKeys.State(driverId), RedisKeys.AvailableLocations },
            new RedisValue[] { driverId.ToString(), state.ToString(), DriverOperationalState.Offline.ToString(), DateTime.UtcNow.ToString("O") });
    }

    public async Task ConnectDriverAsync(Guid driverId, string connectionId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (string.IsNullOrWhiteSpace(connectionId)) throw new ArgumentException("Connection ID cannot be empty.", nameof(connectionId));

        var result = await _database.ScriptEvaluateAsync(
            RedisScripts.ConnectDriver,
            new RedisKey[] { RedisKeys.Lifecycle(driverId), RedisKeys.State(driverId), RedisKeys.Connections(driverId), RedisKeys.Heartbeat(driverId) },
            new RedisValue[] { DriverLifecycleState.Active.ToString(), connectionId, HeartbeatTtlSeconds, DriverOperationalState.Offline.ToString(), DateTime.UtcNow.ToString("O") });

        if ((long)result == -1) throw new InvalidOperationException($"Driver {driverId} is not active.");
    }

    public async Task DisconnectDriverAsync(Guid driverId, string connectionId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (string.IsNullOrWhiteSpace(connectionId)) throw new ArgumentException("Connection ID cannot be empty.", nameof(connectionId));

        await _database.ScriptEvaluateAsync(
            RedisScripts.DisconnectDriver,
            new RedisKey[] { RedisKeys.State(driverId), RedisKeys.Connections(driverId), RedisKeys.AvailableLocations },
            new RedisValue[] { driverId.ToString(), connectionId, DriverOperationalState.Offline.ToString(), DateTime.UtcNow.ToString("O") });
    }

    public async Task<DriverOperationalState> SetAvailableAsync(Guid driverId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var result = await _database.ScriptEvaluateAsync(
            RedisScripts.SetAvailable,
            new RedisKey[] {
                RedisKeys.Lifecycle(driverId),
                RedisKeys.State(driverId),
                RedisKeys.Location(driverId),
                RedisKeys.Heartbeat(driverId),
                RedisKeys.Connections(driverId),
                RedisKeys.AvailableLocations
            },
            new RedisValue[] {
                driverId.ToString(),
                (int)DriverLifecycleState.Active,
                (int)DriverOperationalState.Busy,
                (int)DriverOperationalState.Offline,
                (int)DriverOperationalState.Available,
                DateTime.UtcNow.ToString("O")
            });

        var (resultCode, previousState) = ParseTransitionResult(result);
        if (resultCode != 1) ThrowForSetAvailableResult(driverId, resultCode);
        return ParsePreviousState(previousState);
    }

    public async Task<DriverOperationalState> SetOfflineAsync(Guid driverId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var result = await _database.ScriptEvaluateAsync(
            RedisScripts.SetOffline,
            new RedisKey[] {
                RedisKeys.Lifecycle(driverId),
                RedisKeys.State(driverId),
                RedisKeys.AvailableLocations
            },
            new RedisValue[] {
                driverId.ToString(),
                (int)DriverLifecycleState.Active,
                (int)DriverOperationalState.Offline,
                (int)DriverOperationalState.Available,
                DateTime.UtcNow.ToString("O")
            });

        var (resultCode, previousState) = ParseTransitionResult(result);
        if (resultCode != 1) ThrowForSetOfflineResult(driverId, resultCode);
        return ParsePreviousState(previousState);
    }

    public async Task<DriverOperationalState> SetBusyAsync(Guid driverId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var result = await _database.ScriptEvaluateAsync(
            RedisScripts.SetBusy,
            new RedisKey[] {
                RedisKeys.Lifecycle(driverId),
                RedisKeys.State(driverId),
                RedisKeys.AvailableLocations
            },
            new RedisValue[] {
                driverId.ToString(),
                (int)DriverLifecycleState.Active,
                (int)DriverOperationalState.Available,
                (int)DriverOperationalState.Busy,
                DateTime.UtcNow.ToString("O")
            });

        var (resultCode, previousState) = ParseTransitionResult(result);
        if (resultCode != 1) ThrowForSetBusyResult(driverId, resultCode);
        return ParsePreviousState(previousState);
    }

    public async Task UpdateLocationAsync(Guid driverId, double latitude, double longitude, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ValidateCoordinates(latitude, longitude);

        var result = await _database.ScriptEvaluateAsync(
            RedisScripts.UpdateLocation,
            new RedisKey[] {
                RedisKeys.Lifecycle(driverId),
                RedisKeys.State(driverId),
                RedisKeys.Location(driverId),
                RedisKeys.Heartbeat(driverId),
                RedisKeys.AvailableLocations
            },
            new RedisValue[] {
                driverId.ToString(),
                DriverLifecycleState.Active.ToString(),
                latitude,
                longitude,
                DateTime.UtcNow.ToString("O"),
                HeartbeatTtlSeconds
            });

        if ((long)result == -1) throw new InvalidOperationException($"Driver {driverId} is not active.");
    }

    public async Task UpdateHeartbeatAsync(Guid driverId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var result = await _database.ScriptEvaluateAsync(
            RedisScripts.UpdateHeartbeat,
            new RedisKey[] { RedisKeys.Lifecycle(driverId), RedisKeys.Heartbeat(driverId) },
            new RedisValue[] { DriverLifecycleState.Active.ToString(), HeartbeatTtlSeconds });

        if ((long)result == -1) throw new InvalidOperationException($"Driver {driverId} is not active.");
    }

    public async Task<DriverRealtimeState?> GetStateAsync(Guid driverId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var lifecycleValue = await _database.StringGetAsync(RedisKeys.Lifecycle(driverId));
        if (lifecycleValue.IsNullOrEmpty) return null;

        var lifecycle = Enum.Parse<DriverLifecycleState>(lifecycleValue!);
        var operationalState = await GetOperationalStateAsync(driverId);
        var location = await GetLocationAsync(driverId);
        var connectionCount = await _database.SetLengthAsync(RedisKeys.Connections(driverId));
        var heartbeatExists = await _database.KeyExistsAsync(RedisKeys.Heartbeat(driverId));
        DateTime? heartbeat = heartbeatExists ? DateTime.UtcNow : null;

        return new DriverRealtimeState(driverId, lifecycle, operationalState, location, connectionCount > 0, heartbeat);
    }

    public async Task<IReadOnlyList<Guid>> FindNearbyAvailableDriversAsync(double latitude, double longitude, double radiusInKm, int maxResults, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ValidateCoordinates(latitude, longitude);
        if (radiusInKm <= 0) throw new ArgumentOutOfRangeException(nameof(radiusInKm));
        if (maxResults <= 0) throw new ArgumentOutOfRangeException(nameof(maxResults));

        var results = await _database.GeoRadiusAsync(RedisKeys.AvailableLocations, longitude, latitude, radiusInKm, GeoUnit.Kilometers, count: maxResults, order: Order.Ascending);
        return results.Select(x => Guid.Parse(x.Member.ToString())).ToList();
    }

    private async Task<DriverOperationalState> GetOperationalStateAsync(Guid driverId)
    {
        var state = await _database.HashGetAsync(RedisKeys.State(driverId), "state");
        if (state.IsNullOrEmpty) return DriverOperationalState.Offline;
        return Enum.Parse<DriverOperationalState>(state!);
    }

    private async Task<DriverLocation?> GetLocationAsync(Guid driverId)
    {
        var values = await _database.HashGetAsync(RedisKeys.Location(driverId), new RedisValue[] { "latitude", "longitude", "updatedAtUtc" });
        if (values[0].IsNullOrEmpty || values[1].IsNullOrEmpty || values[2].IsNullOrEmpty) return null;
        return new DriverLocation((double)values[0], (double)values[1], DateTime.Parse(values[2]!));
    }

    private static void ThrowForSetAvailableResult(Guid driverId, long result)
    {
        switch (result)
        {
            case -1: throw new InvalidOperationException($"Driver {driverId} is not active.");
            case -2: throw new InvalidOperationException($"Driver {driverId} is not connected.");
            case -3: throw new InvalidOperationException($"Driver {driverId} heartbeat has expired.");
            case -4: throw new InvalidOperationException($"Driver {driverId} has no location.");
            case -5: throw new InvalidOperationException($"Driver {driverId} cannot transition to Available from current operational state.");
            case 1: return;
            default: throw new InvalidOperationException($"Unexpected SetAvailable result: {result}");
        }
    }

    private static void ThrowForSetOfflineResult(Guid driverId, long result)
    {
        switch (result)
        {
            case -1: throw new InvalidOperationException($"Driver {driverId} is not active.");
            case -2: throw new InvalidOperationException($"Driver {driverId} is busy and cannot go offline.");
            case 1: return;
            default: throw new InvalidOperationException($"Unexpected SetOffline result: {result}");
        }
    }

    private static void ThrowForSetBusyResult(Guid driverId, long result)
    {
        switch (result)
        {
            case -1: throw new InvalidOperationException($"Driver {driverId} is not active.");
            case -2: throw new InvalidOperationException($"Driver {driverId} is not available.");
            case 1: return;
            default: throw new InvalidOperationException($"Unexpected SetBusy result: {result}");
        }
    }

    private static void ValidateCoordinates(double latitude, double longitude)
    {
        if (latitude is < -90 or > 90) throw new ArgumentOutOfRangeException(nameof(latitude));
        if (longitude is < -180 or > 180) throw new ArgumentOutOfRangeException(nameof(longitude));
    }

    private static (long Result, long PreviousState) ParseTransitionResult(RedisResult result)
    {
        var values = (RedisResult[])result!;
        return ((long)values[0]!, (long)values[1]!);
    }

    private static DriverOperationalState ParsePreviousState(long value)
    {
        if (!Enum.IsDefined(typeof(DriverOperationalState), (int)value))
            throw new InvalidOperationException($"Invalid Redis operational state: {value}");
        return (DriverOperationalState)value;
    }
}