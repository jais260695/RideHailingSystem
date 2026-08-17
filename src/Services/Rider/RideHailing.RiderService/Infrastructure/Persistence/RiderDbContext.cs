using Microsoft.EntityFrameworkCore;
using RideHailing.RiderService.Domain.Entities;

namespace RideHailing.RiderService.Infrastructure.Persistence;

public sealed class RiderDbContext(DbContextOptions<RiderDbContext> options) : DbContext(options)
{
    public DbSet<Rider> Riders => Set<Rider>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Rider>(entity =>
        {
            entity.ToTable("riders");

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

            entity.Property(x => x.CreatedAtUtc)
                .IsRequired();

            entity.HasIndex(x => x.Email)
                .IsUnique();

            entity.HasIndex(x => x.PhoneNumber)
                .IsUnique();
        });
    }
}