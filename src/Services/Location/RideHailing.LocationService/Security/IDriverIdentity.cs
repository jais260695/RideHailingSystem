using System.Security.Claims;

namespace RideHailing.LocationService.Security;

public interface IDriverIdentity
{
    Guid GetDriverId(ClaimsPrincipal user);
}