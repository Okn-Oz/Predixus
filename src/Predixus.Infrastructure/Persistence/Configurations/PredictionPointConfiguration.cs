using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Predixus.Domain.Entities;

namespace Predixus.Infrastructure.Persistence.Configurations;

public class PredictionPointConfiguration : IEntityTypeConfiguration<PredictionPoint>
{
    public void Configure(EntityTypeBuilder<PredictionPoint> builder)
    {
        builder.ToTable("prediction_points");

        builder.HasKey(pp => pp.Id);

        builder.Property(pp => pp.PredictionId)
            .IsRequired();

        builder.Property(pp => pp.DayOffset)
            .IsRequired();

        builder.Property(pp => pp.PredictedPrice)
            .IsRequired()
            .HasColumnType("decimal(18,4)");

        builder.Property(pp => pp.ActualPrice)
            .HasColumnType("decimal(18,4)");

        builder.Property(pp => pp.CreatedAt)
            .IsRequired();
    }
}
