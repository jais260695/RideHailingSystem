namespace RideHailing.LocationService.Domain;

public sealed record DriverLocation(
    double Latitude,
    double Longitude,
    DateTime UpdatedAtUtc);