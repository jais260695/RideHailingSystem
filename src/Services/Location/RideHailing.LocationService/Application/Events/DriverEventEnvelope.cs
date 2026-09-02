namespace RideHailing.LocationService.Application.Events;

public sealed record DriverEventEnvelope(
    string EventType,
    Guid DriverId,
    DateTime OccurredAtUtc,
    object Data);