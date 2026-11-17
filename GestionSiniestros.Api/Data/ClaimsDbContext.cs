using GestionSiniestros.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace GestionSiniestros.Api.Data;

public class ClaimsDbContext(DbContextOptions<ClaimsDbContext> options) : DbContext(options)
{
    public DbSet<Claim> Claims => Set<Claim>();

    public DbSet<ClaimStatusHistory> ClaimStatusHistories => Set<ClaimStatusHistory>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var utcConverter = new ValueConverter<DateTime, DateTime>(value => value, value => DateTime.SpecifyKind(value, DateTimeKind.Utc));

        modelBuilder.Entity<Claim>(entity =>
        {
            entity.ToTable("Claims");
            entity.HasKey(c => c.Id);

            entity.Property(c => c.PolicyNumber).HasMaxLength(50).IsRequired();
            entity.Property(c => c.InsuredName).HasMaxLength(200).IsRequired();
            entity.Property(c => c.ClaimType).HasMaxLength(100).IsRequired();
            entity.Property(c => c.EstimatedAmount).HasPrecision(18, 2);
            entity.Property(c => c.Status).HasConversion<string>().HasMaxLength(20);
            entity.Property(c => c.CreatedAt).HasConversion(utcConverter);
            entity.Property(c => c.UpdatedAt).HasConversion(utcConverter);

            // Índice no único para la búsqueda por prefijo de póliza.
            entity.HasIndex(c => c.PolicyNumber);

            entity.HasMany(c => c.StatusHistory)
                  .WithOne(h => h.Claim)
                  .HasForeignKey(h => h.ClaimId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ClaimStatusHistory>(entity =>
        {
            entity.ToTable("ClaimStatusHistory");
            entity.HasKey(h => h.Id);

            entity.Property(h => h.PreviousStatus).HasConversion<string>().HasMaxLength(20);
            entity.Property(h => h.NewStatus).HasConversion<string>().HasMaxLength(20);
            entity.Property(h => h.ChangedAt).HasConversion(utcConverter);

            entity.HasIndex(h => h.ClaimId);
        });
    }
}
