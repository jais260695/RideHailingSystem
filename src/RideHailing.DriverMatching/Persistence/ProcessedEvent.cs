namespace RideHailing.DriverMatching.Persistence;

public sealed class ProcessedEvent
{
    public string ConsumerGroup { get; set; } = null!;

    public Guid EventId { get; set; }

    public DateTime ProcessedAtUtc { get; set; }
}