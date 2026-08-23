namespace RideHailing.DriverService.Domain.Entities;

public sealed class OutboxMessage
{
    public Guid Id { get; private set; }

    public string Type { get; private set; } = null!;

    public string Payload { get; private set; } = null!;

    public DateTime OccurredAtUtc { get; private set; }

    public DateTime? ProcessedAtUtc { get; private set; }

    public DateTime NextAttemptAtUtc { get; private set; }

    public DateTime? LockedUntilUtc { get; private set; }

    public Guid? LockId { get; private set; }

    public int RetryCount { get; private set; }

    public string? Error { get; private set; }

    private OutboxMessage()
    {
    }

    public OutboxMessage( string type, string payload)
    {
        Id = Guid.NewGuid();
        Type = type;
        Payload = payload;
        OccurredAtUtc = DateTime.UtcNow;
        // Message is immediately eligible for processing.
        NextAttemptAtUtc = OccurredAtUtc;
        RetryCount = 0;
    }

    public void Claim(Guid lockId, TimeSpan lockDuration)
    {
        LockId = lockId;
        LockedUntilUtc = DateTime.UtcNow.Add(lockDuration);
    }

    public void MarkProcessed()
    {
        ProcessedAtUtc = DateTime.UtcNow;
        LockedUntilUtc = null;
        LockId = null;
        Error = null;
    }

    public void MarkFailed(string error)
    {
        RetryCount++;
        LockedUntilUtc = null;
        LockId = null;
        Error = error;
        NextAttemptAtUtc =DateTime.UtcNow.Add(CalculateRetryDelay(RetryCount));
    }

    private static TimeSpan CalculateRetryDelay(int retryCount)
    {
        // 2, 4, 8, 16, 32 ... seconds
        // capped at 5 minutes.
        var seconds = Math.Min(Math.Pow(2, retryCount),300);

        return TimeSpan.FromSeconds(seconds);
    }
}