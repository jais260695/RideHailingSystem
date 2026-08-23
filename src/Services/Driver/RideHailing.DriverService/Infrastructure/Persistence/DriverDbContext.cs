using Microsoft.EntityFrameworkCore;
using RideHailing.DriverService.Domain.Entities;

namespace RideHailing.DriverService.Infrastructure.Persistence;

public sealed class DriverDbContext(DbContextOptions<DriverDbContext> options) : DbContext(options)
{
    public DbSet<Driver> Drivers => Set<Driver>();
    public DbSet<Vehicle> Vehicles => Set<Vehicle>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ConfigureDriver(modelBuilder);
        ConfigureVehicle(modelBuilder);
        ConfigureOutbox(modelBuilder);
    }

    private static void ConfigureDriver(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<Driver>();
        entity.ToTable("drivers");
        entity.HasKey(x => x.Id);
        entity.Property(x => x.Name).HasMaxLength(200).IsRequired();
        entity.Property(x => x.Email).HasMaxLength(320).IsRequired();
        entity.Property(x => x.PhoneNumber).HasMaxLength(20).IsRequired();
        entity.Property(x => x.LicenseNumber).HasMaxLength(100).IsRequired();
        entity.Property(x => x.Rating).HasPrecision(3, 2).IsRequired();
        entity.Property(x => x.Status).IsRequired();
        // PostgreSQL xmin optimistic concurrency.
        entity.Property(x => x.Version).HasColumnName("xmin").IsRowVersion();
        entity.Property(x => x.CreatedAtUtc).IsRequired();
        entity.HasIndex(x => x.Email).IsUnique();
        entity.HasIndex(x => x.LicenseNumber).IsUnique();
    }

    private static void ConfigureVehicle(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<Vehicle>();
        entity.ToTable("vehicles");
        entity.HasKey(x => x.Id);
        // Keep your existing Vehicle configuration here.
    }

    private static void ConfigureOutbox(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<OutboxMessage>();
        entity.ToTable("outbox_messages");
        entity.HasKey(x => x.Id);
        entity.Property(x => x.Type).HasMaxLength(200).IsRequired();
        entity.Property(x => x.Payload).IsRequired();
        entity.Property(x => x.OccurredAtUtc).IsRequired();
        entity.Property(x => x.NextAttemptAtUtc).IsRequired();
        entity.Property(x => x.ProcessedAtUtc).IsRequired(false);
        entity.Property(x => x.LockedUntilUtc).IsRequired(false);
        entity.Property(x => x.LockId).IsRequired(false);
        entity.Property(x => x.RetryCount).IsRequired();
        entity.Property(x => x.Error).IsRequired(false);
        entity.HasIndex(x => new { x.NextAttemptAtUtc, x.OccurredAtUtc, x.LockedUntilUtc });
        entity.HasIndex(x => x.LockId);
    }
}