using StackExchange.Redis;

namespace RideHailing.DriverService.Infrastructure.Redis;

public sealed class DriverLocationStore(IConnectionMultiplexer redis)
{
    private const string AvailableDriversKey = "drivers:available";

    public async Task UpdateLocationAsync(
        Guid driverId,
        double longitude,
        double latitude)
    {
        var database = redis.GetDatabase();

        await database.GeoAddAsync(
            AvailableDriversKey,
            longitude,
            latitude,
            driverId.ToString());
    }

    public async Task RemoveDriverAsync(Guid driverId)
    {
        var database = redis.GetDatabase();

        await database.SortedSetRemoveAsync(
            AvailableDriversKey,
            driverId.ToString());
    }
}