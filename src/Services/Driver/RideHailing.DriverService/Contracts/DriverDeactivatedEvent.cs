namespace RideHailing.DriverService.Contracts.Events;

public sealed record DriverDeactivatedEvent(
    Guid EventId,
    Guid DriverId,
    DateTime OccurredAtUtc);