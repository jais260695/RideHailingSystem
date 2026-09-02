namespace RideHailing.LocationService.Application.Events;

public sealed record DriverLocationUpdatedEvent(Guid DriverId, double Latitude, double Longitude, DateTime OccurredAtUtc) : IDriverEvent;