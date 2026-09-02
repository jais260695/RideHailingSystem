using RideHailing.LocationService.Domain;

namespace RideHailing.LocationService.Application.Events;

public sealed record DriverAvailabilityChangedEvent(Guid DriverId,DriverOperationalState PreviousState,
    DriverOperationalState CurrentState,DateTime OccurredAtUtc) : IDriverEvent;