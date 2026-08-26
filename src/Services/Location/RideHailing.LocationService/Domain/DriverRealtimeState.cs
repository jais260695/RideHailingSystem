namespace RideHailing.LocationService.Domain;

public sealed record DriverRealtimeState(
    Guid DriverId,
    DriverLifecycleState LifecycleState,
    DriverOperationalState OperationalState,
    DriverLocation? Location,
    bool IsConnected,
    DateTime? LastHeartbeatUtc);