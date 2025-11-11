using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using WarrantyRepairLedger.Models;

namespace WarrantyRepairLedger.Data;

public class LedgerDbContext(DbContextOptions<LedgerDbContext> options) : DbContext(options)
{
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Repair> Repairs => Set<Repair>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var dateOnlyConverter = new ValueConverter<DateOnly, DateTime>(
            dateOnly => dateOnly.ToDateTime(TimeOnly.MinValue),
            dateTime => DateOnly.FromDateTime(dateTime));
        var dateOnlyComparer = new ValueComparer<DateOnly>(
            (left, right) => left == right,
            value => value.GetHashCode(),
            value => value
        );

        modelBuilder.Entity<Product>(entity =>
        {
            entity.Property(p => p.Name)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(p => p.Serial)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(p => p.Brand)
                .HasMaxLength(150);

            entity.Property(p => p.Retailer)
                .HasMaxLength(150);

            entity.Property(p => p.Price)
                .HasPrecision(12, 2);

            entity.Property(p => p.PurchaseDate)
                .HasConversion(dateOnlyConverter);

            entity.Property(p => p.PurchaseDate)
                .Metadata.SetValueComparer(dateOnlyComparer);

            entity.Property(p => p.WarrantyMonths)
                .HasDefaultValue(24);

            entity.HasIndex(p => p.Serial)
                .IsUnique();
        });

        modelBuilder.Entity<Repair>(entity =>
        {
            entity.Property(r => r.Status)
                .HasConversion<string>()
                .HasMaxLength(20)
                .HasDefaultValue(RepairStatus.Open);

            entity.Property(r => r.Cost)
                .HasPrecision(12, 2);

            entity.HasOne(r => r.Product)
                .WithMany(p => p.Repairs)
                .HasForeignKey(r => r.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(r => new { r.ProductId, r.Status });
        });
    }
}
