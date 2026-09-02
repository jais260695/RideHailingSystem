namespace RideHailing.LocationService.Application.Events;
public interface IDriverEvent
{
    Guid DriverId { get; }

    DateTime OccurredAtUtc { get; }
}