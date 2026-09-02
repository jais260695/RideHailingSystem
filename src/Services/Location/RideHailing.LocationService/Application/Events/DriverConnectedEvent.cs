namespace RideHailing.LocationService.Application.Events;

public sealed record DriverConnectedEvent(Guid DriverId, string ConnectionId, DateTime OccurredAtUtc) : IDriverEvent;