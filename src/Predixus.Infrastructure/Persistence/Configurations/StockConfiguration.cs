using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Predixus.Domain.Entities;

namespace Predixus.Infrastructure.Persistence.Configurations;

public class StockConfiguration : IEntityTypeConfiguration<Stock>
{
    public void Configure(EntityTypeBuilder<Stock> builder)
    {
        builder.ToTable("stocks");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.Symbol)
            .IsRequired()
            .HasMaxLength(20);

        builder.HasIndex(s => s.Symbol)
            .IsUnique();

        builder.Property(s => s.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(s => s.Sector)
            .HasMaxLength(100);

        builder.Property(s => s.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(s => s.CreatedAt)
            .IsRequired();

        builder.HasMany(s => s.Prices)
            .WithOne(p => p.Stock)
            .HasForeignKey(p => p.StockId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(s => s.Predictions)
            .WithOne(p => p.Stock)
            .HasForeignKey(p => p.StockId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
