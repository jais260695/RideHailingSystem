using RideHailing.DriverService.Domain.Enums;

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

    public DriverLifecycleStatus Status { get; private set; }

    public uint Version { get; private set; }

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
        Status = DriverLifecycleStatus.Active;
        Rating = 5.0m;
        CreatedAtUtc = DateTime.UtcNow;
        Version = 1;
    }

    public void UpdateProfile(
        string name,
        string phoneNumber)
    {
        Name = name;
        PhoneNumber = phoneNumber;
    }

    public void AddVehicle(Vehicle vehicle)
    {
        if (Vehicle is not null)
        {
            throw new InvalidOperationException(
                "Driver already has a vehicle.");
        }

        Vehicle = vehicle;
    }

    public void Suspend()
    {
        if (Status == DriverLifecycleStatus.Deactivated)
        {
            throw new InvalidOperationException(
                "A deactivated driver cannot be suspended.");
        }

        Status = DriverLifecycleStatus.Suspended;
    }

    public void Activate()
    {
        if (Status == DriverLifecycleStatus.Deactivated)
        {
            throw new InvalidOperationException(
                "A deactivated driver cannot be activated.");
        }

        Status = DriverLifecycleStatus.Active;
    }

    public void Deactivate()
    {
        Status = DriverLifecycleStatus.Deactivated;
    }
}