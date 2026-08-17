namespace RideHailing.RiderService.Domain.Entities;

public sealed class Rider
{
    public Guid Id { get; private set; }

    public string Name { get; private set; } = null!;

    public string Email { get; private set; } = null!;

    public string PhoneNumber { get; private set; } = null!;

    public DateTime CreatedAtUtc { get; private set; }

    private Rider()
    {
    }

    public Rider(
        string name,
        string email,
        string phoneNumber)
    {
        Id = Guid.NewGuid();
        Name = name;
        Email = email;
        PhoneNumber = phoneNumber;
        CreatedAtUtc = DateTime.UtcNow;
    }
}