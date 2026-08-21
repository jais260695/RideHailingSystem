using Microsoft.EntityFrameworkCore;
using RideHailing.DriverService.Domain.Entities;

namespace RideHailing.DriverService.Infrastructure.Persistence;

public sealed class DriverDbContext(
    DbContextOptions<DriverDbContext> options)
    : DbContext(options)
{
    public DbSet<Driver> Drivers => Set<Driver>();

    public DbSet<Vehicle> Vehicles => Set<Vehicle>();

    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    protected override void OnModelCreating(
        ModelBuilder modelBuilder)
    {
        ConfigureDriver(modelBuilder);
        ConfigureVehicle(modelBuilder);
        ConfigureOutbox(modelBuilder);
    }

    private static void ConfigureDriver(
        ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<Driver>();

        entity.ToTable("drivers");

        entity.HasKey(x => x.Id);

        entity.Property(x => x.Name)
            .HasMaxLength(150)
            .IsRequired();

        entity.Property(x => x.Email)
            .HasMaxLength(320)
            .IsRequired();

        entity.Property(x => x.PhoneNumber)
            .HasMaxLength(20)
            .IsRequired();

        entity.Property(x => x.LicenseNumber)
            .HasMaxLength(50)
            .IsRequired();

        entity.Property(x => x.Rating)
            .HasPrecision(3, 2)
            .IsRequired();

        entity.Property(x => x.CreatedAtUtc)
            .IsRequired();

        entity.HasIndex(x => x.Email)
            .IsUnique();

        entity.HasIndex(x => x.PhoneNumber)
            .IsUnique();

        entity.HasIndex(x => x.LicenseNumber)
            .IsUnique();

        entity.Property(x => x.Version)
            .HasColumnName("xmin")
            .IsRowVersion();

        entity.Property(x => x.Status)
            .IsRequired();

        entity.HasOne(x => x.Vehicle)
            .WithOne()
            .HasForeignKey<Vehicle>(
                x => x.DriverId)
            .OnDelete(DeleteBehavior.Cascade);
    }

    private static void ConfigureVehicle(
        ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<Vehicle>();

        entity.ToTable("vehicles");

        entity.HasKey(x => x.Id);

        entity.Property(x => x.Make)
            .HasMaxLength(100)
            .IsRequired();

        entity.Property(x => x.Model)
            .HasMaxLength(100)
            .IsRequired();

        entity.Property(x => x.Color)
            .HasMaxLength(50)
            .IsRequired();

        entity.Property(x => x.LicensePlate)
            .HasMaxLength(30)
            .IsRequired();

        entity.Property(x => x.ManufacturingYear)
            .IsRequired();

        entity.HasIndex(x => x.LicensePlate)
            .IsUnique();

        entity.HasIndex(x => x.DriverId)
            .IsUnique();
    }

    private static void ConfigureOutbox(
    ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<OutboxMessage>();

        entity.ToTable("outbox_messages");

        entity.HasKey(x => x.Id);

        entity.Property(x => x.Type)
            .HasMaxLength(200)
            .IsRequired();

        entity.Property(x => x.Payload)
            .IsRequired();

        entity.Property(x => x.OccurredAtUtc)
            .IsRequired();

        entity.Property(x => x.RetryCount)
            .IsRequired();

        entity.HasIndex(x => new
        {
            x.ProcessedAtUtc,
            x.OccurredAtUtc
        });
    }
}