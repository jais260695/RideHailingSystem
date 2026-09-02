using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace RideHailing.LocationService.Infrastructure.Postgres;

public interface IOutboxRepository
{
    Task AddAsync(OutboxMessage message, CancellationToken cancellationToken);
    Task<IReadOnlyList<OutboxMessage>> GetPendingAsync(int batchSize, CancellationToken cancellationToken);
    Task<IReadOnlyList<OutboxMessage>> GetAndClaimPendingAsync(int batchSize, TimeSpan lockDuration, int maxRetries, CancellationToken cancellationToken);
    Task MarkPublishedAsync(Guid messageId, CancellationToken cancellationToken);
    Task MarkFailedAsync(Guid messageId, string error, DateTime nextAttemptAtUtc, CancellationToken cancellationToken);
}