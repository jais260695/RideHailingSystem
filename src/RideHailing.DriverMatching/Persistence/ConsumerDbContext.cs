using Microsoft.EntityFrameworkCore;

namespace RideHailing.DriverMatching.Persistence;

public sealed class ConsumerDbContext : DbContext
{
    public ConsumerDbContext(DbContextOptions<ConsumerDbContext> options) : base(options)
    {
    }

    public DbSet<ProcessedEvent> ProcessedEvents => Set<ProcessedEvent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<ProcessedEvent>();

        entity.ToTable("processed_events");

        entity.HasKey(x => new
        {
            x.ConsumerGroup,
            x.EventId
        });

        entity.Property(x => x.ConsumerGroup)
            .HasMaxLength(200)
            .IsRequired();

        entity.Property(x => x.EventId)
            .IsRequired();

        entity.Property(x => x.ProcessedAtUtc)
            .IsRequired();
    }
}