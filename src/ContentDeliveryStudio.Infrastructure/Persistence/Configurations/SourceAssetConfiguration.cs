using System.Text.Json;
using ContentDeliveryStudio.Core.Sources;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ContentDeliveryStudio.Infrastructure.Persistence.Configurations;

internal sealed class SourceAssetConfiguration : IEntityTypeConfiguration<SourceAsset>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public void Configure(EntityTypeBuilder<SourceAsset> entity)
    {
        entity.HasKey(asset => asset.Id);
        entity.Property(asset => asset.ProjectId);
        entity.Property(asset => asset.Kind);
        entity.Property(asset => asset.DisplayName).IsRequired();
        entity.Property(asset => asset.OriginalPath);
        entity.Property(asset => asset.MimeType);
        entity.Property(asset => asset.SizeBytes);
        entity.Property(asset => asset.Sha256);
        entity.Property(asset => asset.CreatedAt);
        entity.Property(asset => asset.UpdatedAt);
        entity.Property(asset => asset.ExtractedContents)
            .HasConversion(contents => SerializeExtractedContents(contents), json => DeserializeExtractedContents(json));
        entity.Property(asset => asset.EvidenceAnchors)
            .HasConversion(anchors => SerializeEvidenceAnchors(anchors), json => DeserializeEvidenceAnchors(json));
    }

    private static string SerializeExtractedContents(IReadOnlyCollection<ExtractedContent> contents)
    {
        return JsonSerializer.Serialize(contents, JsonOptions);
    }

    private static IReadOnlyCollection<ExtractedContent> DeserializeExtractedContents(string json)
    {
        return JsonSerializer.Deserialize<List<ExtractedContent>>(json, JsonOptions) ?? [];
    }

    private static string SerializeEvidenceAnchors(IReadOnlyCollection<EvidenceAnchor> anchors)
    {
        return JsonSerializer.Serialize(anchors, JsonOptions);
    }

    private static IReadOnlyCollection<EvidenceAnchor> DeserializeEvidenceAnchors(string json)
    {
        return JsonSerializer.Deserialize<List<EvidenceAnchor>>(json, JsonOptions) ?? [];
    }
}
