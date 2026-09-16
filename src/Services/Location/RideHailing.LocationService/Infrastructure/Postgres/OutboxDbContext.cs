using Microsoft.EntityFrameworkCore;

namespace RideHailing.LocationService.Infrastructure.Postgres;

public sealed class OutboxDbContext : DbContext
{
    public OutboxDbContext(DbContextOptions<OutboxDbContext> options) : base(options) 
    { 
    }

    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<OutboxMessage>();

        entity.ToTable("outbox_messages");
        entity.HasKey(x => x.Id);
        entity.HasIndex(x => x.EventId).IsUnique();
        entity.Property(x => x.EventType).HasMaxLength(200).IsRequired();
        entity.Property(x => x.AggregateType).HasMaxLength(100).IsRequired();
        entity.Property(x => x.AggregateId).HasMaxLength(100).IsRequired();
        entity.Property(x => x.Payload).IsRequired();
        entity.Property(x => x.OccurredAtUtc).IsRequired();

        entity.HasIndex(x => new { x.PublishedAtUtc, x.NextAttemptAtUtc, x.LockedUntilUtc });
    }
}