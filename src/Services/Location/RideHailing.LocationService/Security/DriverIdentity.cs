using System.Security.Claims;

namespace RideHailing.LocationService.Security;

public sealed class DriverIdentity : IDriverIdentity
{
    public Guid GetDriverId(ClaimsPrincipal user)
    {
        if (user?.Identity?.IsAuthenticated != true)
            throw new UnauthorizedAccessException("Driver is not authenticated.");

        var value = user.FindFirstValue("driver_id") ?? user.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!Guid.TryParse(value, out var driverId) || driverId == Guid.Empty)
            throw new UnauthorizedAccessException("Authenticated user does not contain a valid driver ID.");

        return driverId;
    }
}