using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Predixus.Domain.Entities;

namespace Predixus.Infrastructure.Persistence.Configurations;

public class StockPriceConfiguration : IEntityTypeConfiguration<StockPrice>
{
    public void Configure(EntityTypeBuilder<StockPrice> builder)
    {
        builder.ToTable("stock_prices");

        builder.HasKey(sp => sp.Id);

        builder.HasIndex(sp => new { sp.StockId, sp.Date })
            .IsUnique();

        builder.Property(sp => sp.Date)
            .IsRequired();

        builder.Property(sp => sp.Open)
            .IsRequired()
            .HasColumnType("decimal(18,4)");

        builder.Property(sp => sp.High)
            .IsRequired()
            .HasColumnType("decimal(18,4)");

        builder.Property(sp => sp.Low)
            .IsRequired()
            .HasColumnType("decimal(18,4)");

        builder.Property(sp => sp.Close)
            .IsRequired()
            .HasColumnType("decimal(18,4)");

        builder.Property(sp => sp.Volume)
            .IsRequired();

        builder.Property(sp => sp.CreatedAt)
            .IsRequired();

        builder.Ignore(sp => sp.DailyChangePercent);
    }
}
