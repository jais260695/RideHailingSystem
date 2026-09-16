namespace RideHailing.LocationService.Application.Events;

public sealed record DriverConnectedEvent(Guid EventId, Guid DriverId, string ConnectionId, DateTime OccurredAtUtc) : IDriverEvent;