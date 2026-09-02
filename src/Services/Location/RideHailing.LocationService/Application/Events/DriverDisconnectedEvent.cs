namespace RideHailing.LocationService.Application.Events;

public sealed record DriverDisconnectedEvent(Guid DriverId,string ConnectionId,DateTime OccurredAtUtc) : IDriverEvent;