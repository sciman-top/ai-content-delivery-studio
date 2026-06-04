using System.Text.Json;
using ImageSeriesStudio.Core.Artifacts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ImageSeriesStudio.Infrastructure.Persistence.Configurations;

internal sealed class ArtifactPackageConfiguration : IEntityTypeConfiguration<ArtifactPackage>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public void Configure(EntityTypeBuilder<ArtifactPackage> entity)
    {
        entity.HasKey(package => package.Id);
        entity.Property(package => package.ProjectId);
        entity.Property(package => package.Name).IsRequired();
        entity.Property(package => package.OutputDirectory).IsRequired();
        entity.Property(package => package.CreatedAt);
        entity.Property(package => package.Manifest)
            .HasConversion(manifest => SerializeArtifactManifest(manifest), json => DeserializeArtifactManifest(json));
    }

    private static string SerializeArtifactManifest(ArtifactManifest manifest)
    {
        return JsonSerializer.Serialize(manifest, JsonOptions);
    }

    private static ArtifactManifest DeserializeArtifactManifest(string json)
    {
        return JsonSerializer.Deserialize<ArtifactManifest>(json, JsonOptions)
            ?? throw new InvalidOperationException("Artifact manifest JSON could not be deserialized.");
    }
}
