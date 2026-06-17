using System.Text.Json;
using ContentDeliveryStudio.Core.Artifacts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ContentDeliveryStudio.Infrastructure.Persistence.Configurations;

internal sealed class OutputArtifactConfiguration : IEntityTypeConfiguration<OutputArtifact>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public void Configure(EntityTypeBuilder<OutputArtifact> entity)
    {
        entity.HasKey(artifact => artifact.Id);
        entity.Property(artifact => artifact.ProjectId);
        entity.Property(artifact => artifact.Kind);
        entity.Property(artifact => artifact.Status);
        entity.Property(artifact => artifact.DisplayName).IsRequired();
        entity.Property(artifact => artifact.RelativePath).IsRequired();
        entity.Property(artifact => artifact.MimeType).IsRequired();
        entity.Property(artifact => artifact.Role).IsRequired();
        entity.Property(artifact => artifact.CreatedAt);
        entity.Property(artifact => artifact.UpdatedAt);
        entity.Property(artifact => artifact.SourceAssetIds)
            .HasConversion(values => SerializeGuidList(values), json => DeserializeGuidList(json));
        entity.Property(artifact => artifact.EvidenceAnchorIds)
            .HasConversion(values => SerializeGuidList(values), json => DeserializeGuidList(json));
        entity.Property(artifact => artifact.Metadata)
            .HasConversion(values => SerializeStringDictionary(values), json => DeserializeStringDictionary(json));
    }

    private static string SerializeGuidList(IReadOnlyList<Guid> values)
    {
        return JsonSerializer.Serialize(values, JsonOptions);
    }

    private static IReadOnlyList<Guid> DeserializeGuidList(string json)
    {
        return JsonSerializer.Deserialize<List<Guid>>(json, JsonOptions) ?? [];
    }

    private static string SerializeStringDictionary(IReadOnlyDictionary<string, string> values)
    {
        return JsonSerializer.Serialize(values, JsonOptions);
    }

    private static IReadOnlyDictionary<string, string> DeserializeStringDictionary(string json)
    {
        return JsonSerializer.Deserialize<Dictionary<string, string>>(json, JsonOptions) ?? new Dictionary<string, string>();
    }
}
