namespace RideHailing.LocationService.Application.Events;

public interface IDriverEvent
{
    Guid EventId { get; }
    Guid DriverId { get; }
    DateTime OccurredAtUtc { get; }
}