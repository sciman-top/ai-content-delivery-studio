using ContentDeliveryStudio.Core.Projects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ContentDeliveryStudio.Infrastructure.Persistence.Configurations;

internal sealed class ImageSeriesConfiguration : IEntityTypeConfiguration<ImageSeries>
{
    public void Configure(EntityTypeBuilder<ImageSeries> entity)
    {
        entity.HasKey(series => series.Id);
        entity.Property(series => series.Title).IsRequired();
        entity.HasMany(series => series.Items)
            .WithOne()
            .HasForeignKey(item => item.SeriesId)
            .OnDelete(DeleteBehavior.Cascade);
        entity.HasMany(series => series.CreativeBriefs)
            .WithOne()
            .HasForeignKey(brief => brief.SeriesId)
            .OnDelete(DeleteBehavior.Cascade);
        entity.Navigation(series => series.Items).UsePropertyAccessMode(PropertyAccessMode.Field);
        entity.Navigation(series => series.CreativeBriefs).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
