using RideHailing.LocationService.Domain;

namespace RideHailing.LocationService.Application.Events;

public sealed record DriverAvailabilityChangedEvent(Guid EventId, Guid DriverId, DriverOperationalState PreviousState,
    DriverOperationalState CurrentState, DateTime OccurredAtUtc) : IDriverEvent;