using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Predixus.Domain.Entities;

namespace Predixus.Infrastructure.Persistence.Configurations;

public class PredictionConfiguration : IEntityTypeConfiguration<Prediction>
{
    public void Configure(EntityTypeBuilder<Prediction> builder)
    {
        builder.ToTable("predictions");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.StockId)
            .IsRequired();

        builder.Property(p => p.UserId)
            .IsRequired();

        builder.Property(p => p.PredictedAt)
            .IsRequired();

        builder.Property(p => p.ForecastDays)
            .IsRequired();

        builder.Property(p => p.ConfidenceScore)
            .IsRequired()
            .HasColumnType("decimal(5,4)");

        builder.Property(p => p.CreatedAt)
            .IsRequired();

        builder.HasMany(p => p.Points)
            .WithOne()
            .HasForeignKey(pp => pp.PredictionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
