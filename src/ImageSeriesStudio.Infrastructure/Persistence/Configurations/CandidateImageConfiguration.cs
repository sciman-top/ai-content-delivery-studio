using ImageSeriesStudio.Core.Projects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ImageSeriesStudio.Infrastructure.Persistence.Configurations;

internal sealed class CandidateImageConfiguration : IEntityTypeConfiguration<CandidateImage>
{
    public void Configure(EntityTypeBuilder<CandidateImage> entity)
    {
        entity.HasKey(candidate => candidate.Id);
        entity.Property(candidate => candidate.AssetPath).IsRequired();
        entity.Property(candidate => candidate.MetadataPath).IsRequired();
        entity.HasMany(candidate => candidate.ReviewResults)
            .WithOne()
            .HasForeignKey(review => review.CandidateImageId)
            .OnDelete(DeleteBehavior.Cascade);
        entity.Navigation(candidate => candidate.ReviewResults).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
