namespace RideHailing.LocationService.Infrastructure.Postgres;

public sealed class OutboxMessage
{
    public Guid Id { get; set; }

    public Guid EventId { get; set; }
    public string EventType { get; set; } = null!;

    public string AggregateType { get; set; } = null!;

    public string AggregateId { get; set; } = null!;

    public string Payload { get; set; } = null!;

    public DateTime OccurredAtUtc { get; set; }

    public DateTime? PublishedAtUtc { get; set; }

    public int RetryCount { get; set; }

    public DateTime? NextAttemptAtUtc { get; set; }

    public string? LastError { get; set; }

    public DateTime? LockedUntilUtc { get; set; }

    public Guid? LockId { get; set; }
}