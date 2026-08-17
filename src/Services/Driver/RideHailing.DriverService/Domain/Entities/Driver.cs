namespace RideHailing.DriverService.Domain.Entities;

public sealed class Driver
{
    public Guid Id { get; private set; }

    public string Name { get; private set; } = null!;

    public string Email { get; private set; } = null!;

    public string PhoneNumber { get; private set; } = null!;

    public string LicenseNumber { get; private set; } = null!;

    public decimal Rating { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public Vehicle? Vehicle { get; private set; }

    private Driver()
    {
    }

    public Driver(
        string name,
        string email,
        string phoneNumber,
        string licenseNumber)
    {
        Id = Guid.NewGuid();
        Name = name;
        Email = email;
        PhoneNumber = phoneNumber;
        LicenseNumber = licenseNumber;
        Rating = 5.0m;
        CreatedAtUtc = DateTime.UtcNow;
    }

    public void AddVehicle(Vehicle vehicle)
    {
        if (Vehicle is not null)
            throw new InvalidOperationException(
                "Driver already has a vehicle.");

        Vehicle = vehicle;
    }
}