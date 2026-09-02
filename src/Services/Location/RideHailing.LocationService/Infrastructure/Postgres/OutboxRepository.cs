using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace RideHailing.LocationService.Infrastructure.Postgres;

public sealed class OutboxRepository : IOutboxRepository
{
    private readonly IDbContextFactory<OutboxDbContext> _contextFactory;
    public OutboxRepository(IDbContextFactory<OutboxDbContext> contextFactory) => _contextFactory = contextFactory;

    public async Task AddAsync(OutboxMessage message, CancellationToken cancellationToken)
    {
        await using var db = await _contextFactory.CreateDbContextAsync(cancellationToken);
        db.OutboxMessages.Add(message);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<OutboxMessage>> GetPendingAsync(int batchSize, CancellationToken cancellationToken)
    {
        await using var db = await _contextFactory.CreateDbContextAsync(cancellationToken);

        return await db.OutboxMessages
            .Where(x => x.PublishedAtUtc == null &&
                        (x.NextAttemptAtUtc == null || x.NextAttemptAtUtc <= DateTime.UtcNow))
            .OrderBy(x => x.OccurredAtUtc)
            .Take(batchSize)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<OutboxMessage>> GetAndClaimPendingAsync(int batchSize, TimeSpan lockDuration, int maxRetries, CancellationToken cancellationToken)
    {
        await using var db = await _contextFactory.CreateDbContextAsync(cancellationToken);

        // Use a transaction + SELECT ... FOR UPDATE SKIP LOCKED to atomically claim rows for this worker.
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

        var now = DateTime.UtcNow;
        var messages = await db.OutboxMessages.FromSqlInterpolated($"""
                            SELECT *
                            FROM outbox_messages
                            WHERE PublishedAtUtc IS NULL
                              AND (NextAttemptAtUtc IS NULL OR NextAttemptAtUtc <= {now})
                              AND RetryCount < {maxRetries}
                              AND (LockId IS NULL OR LockedUntilUtc <= {now})
                            ORDER BY OccurredAtUtc
                            FOR UPDATE SKIP LOCKED
                            LIMIT {batchSize}
                            """).ToListAsync(cancellationToken);

        var lockId = Guid.NewGuid();
        foreach (var message in messages)
        {
            message.LockId = lockId;
            message.LockedUntilUtc = DateTime.UtcNow.Add(lockDuration);
        }

        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return messages;
    }

    public async Task MarkPublishedAsync(Guid messageId, CancellationToken cancellationToken)
    {
        await using var db = await _contextFactory.CreateDbContextAsync(cancellationToken);

        await db.OutboxMessages
            .Where(x => x.Id == messageId)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(x => x.PublishedAtUtc, DateTime.UtcNow),
                cancellationToken);
    }

    public async Task MarkFailedAsync(Guid messageId, string error, DateTime nextAttemptAtUtc, CancellationToken cancellationToken)
    {
        await using var db = await _contextFactory.CreateDbContextAsync(cancellationToken);

        await db.OutboxMessages
            .Where(x => x.Id == messageId)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(x => x.RetryCount, x => x.RetryCount + 1)
                    .SetProperty(x => x.LastError, error)
                    .SetProperty(x => x.NextAttemptAtUtc, nextAttemptAtUtc),
                cancellationToken);
    }
}