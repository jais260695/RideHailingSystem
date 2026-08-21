namespace RideHailing.DriverService.Contracts.Events;

public sealed record DriverActivatedEvent(
    Guid EventId,
    Guid DriverId,
    DateTime OccurredAtUtc);