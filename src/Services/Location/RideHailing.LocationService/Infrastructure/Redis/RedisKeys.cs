namespace RideHailing.LocationService.Infrastructure.Redis;

internal static class RedisKeys
{
    public static string Lifecycle(Guid driverId) => $"driver:{driverId}:lifecycle";
    public static string State(Guid driverId) => $"driver:{driverId}:state";
    public static string Location(Guid driverId) => $"driver:{driverId}:location";
    public static string Heartbeat(Guid driverId) => $"driver:{driverId}:heartbeat";
    public static string Connections(Guid driverId) => $"driver:{driverId}:connections";
    public const string AvailableLocations = "drivers:available:locations";
}