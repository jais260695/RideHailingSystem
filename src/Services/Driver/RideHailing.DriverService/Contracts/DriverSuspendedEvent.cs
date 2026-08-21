namespace RideHailing.DriverService.Contracts.Events;

public sealed record DriverSuspendedEvent(
    Guid EventId,
    Guid DriverId,
    DateTime OccurredAtUtc);