using Microsoft.EntityFrameworkCore;

namespace RideHailing.DriverMatching.Persistence;

public sealed class ProcessedEventRepository
{
    private readonly ConsumerDbContext _db;
    public ProcessedEventRepository(ConsumerDbContext db) => _db = db;

    public async Task<bool> TryMarkProcessedAsync(string consumerGroup, Guid eventId, CancellationToken cancellationToken)
    {
        var affectedRows = await _db.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO processed_events (consumer_group, event_id, processed_at_utc)
            VALUES ({consumerGroup}, {eventId}, {DateTime.UtcNow})
            ON CONFLICT (consumer_group, event_id) DO NOTHING
            """, cancellationToken);

        return affectedRows == 1;
    }
}