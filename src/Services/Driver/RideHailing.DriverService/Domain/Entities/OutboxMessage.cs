namespace RideHailing.DriverService.Domain.Entities;

public sealed class OutboxMessage
{
    public Guid Id { get; private set; }

    public string Type { get; private set; } = null!;

    public string Payload { get; private set; } = null!;

    public DateTime OccurredAtUtc { get; private set; }

    public DateTime? ProcessedAtUtc { get; private set; }

    public int RetryCount { get; private set; }

    public string? Error { get; private set; }

    private OutboxMessage()
    {
    }

    public OutboxMessage(
        string type,
        string payload)
    {
        Id = Guid.NewGuid();
        Type = type;
        Payload = payload;
        OccurredAtUtc = DateTime.UtcNow;
        RetryCount = 0;
    }

    public void MarkProcessed()
    {
        ProcessedAtUtc = DateTime.UtcNow;
        Error = null;
    }

    public void MarkFailed(string error)
    {
        RetryCount++;
        Error = error;
    }
}