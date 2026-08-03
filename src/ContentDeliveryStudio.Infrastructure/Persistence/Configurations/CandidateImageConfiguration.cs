using ContentDeliveryStudio.Core.Projects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.Text.Json;

namespace ContentDeliveryStudio.Infrastructure.Persistence.Configurations;

internal sealed class CandidateImageConfiguration : IEntityTypeConfiguration<CandidateImage>
{
    public void Configure(EntityTypeBuilder<CandidateImage> entity)
    {
        entity.HasKey(candidate => candidate.Id);
        entity.Property(candidate => candidate.AssetPath).IsRequired();
        entity.Property(candidate => candidate.MetadataPath).IsRequired();
        entity.Property(candidate => candidate.GenerationTaskId).IsRequired(false);
        entity.Property(candidate => candidate.EditProvenance)
            .HasConversion(
                provenance => provenance == null
                    ? null
                    : JsonSerializer.Serialize(provenance, JsonSerializerOptions.Default),
                json => string.IsNullOrWhiteSpace(json)
                    ? null
                    : JsonSerializer.Deserialize<CandidateImageEditProvenance>(
                        json,
                        JsonSerializerOptions.Default))
            .IsRequired(false);
        entity.HasMany(candidate => candidate.ReviewResults)
            .WithOne()
            .HasForeignKey(review => review.CandidateImageId)
            .OnDelete(DeleteBehavior.Cascade);
        entity.Navigation(candidate => candidate.ReviewResults).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
