namespace RideHailing.DriverService.Domain.Entities;

public sealed class Vehicle
{
    public Guid Id { get; private set; }

    public Guid DriverId { get; private set; }

    public string Make { get; private set; } = null!;

    public string Model { get; private set; } = null!;

    public string Color { get; private set; } = null!;

    public string LicensePlate { get; private set; } = null!;

    public int ManufacturingYear { get; private set; }

    private Vehicle()
    {
    }

    public Vehicle(
        Guid driverId,
        string make,
        string model,
        string color,
        string licensePlate,
        int manufacturingYear)
    {
        Id = Guid.NewGuid();
        DriverId = driverId;
        Make = make;
        Model = model;
        Color = color;
        LicensePlate = licensePlate;
        ManufacturingYear = manufacturingYear;
    }
}