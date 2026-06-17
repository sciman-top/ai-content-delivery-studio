using System.Text.Json;
using ContentDeliveryStudio.Core.Documents;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ContentDeliveryStudio.Infrastructure.Persistence.Configurations;

internal sealed class DocumentBriefConfiguration : IEntityTypeConfiguration<DocumentBrief>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public void Configure(EntityTypeBuilder<DocumentBrief> entity)
    {
        entity.HasKey(brief => brief.Id);
        entity.Property(brief => brief.ProjectId);
        entity.Property(brief => brief.SourceKind);
        entity.Property(brief => brief.SourceDisplayName).IsRequired();
        entity.Property(brief => brief.Title).IsRequired();
        entity.Property(brief => brief.DocumentFamily);
        entity.Property(brief => brief.Audience).IsRequired();
        entity.Property(brief => brief.StrictnessLevel);
        entity.Property(brief => brief.CreatedAt);
        entity.Property(brief => brief.Sections)
            .HasConversion(values => SerializeStringList(values), json => DeserializeStringList(json));
        entity.Property(brief => brief.KeyClaims)
            .HasConversion(values => SerializeStringList(values), json => DeserializeStringList(json));
        entity.Property(brief => brief.VisualOpportunities)
            .HasConversion(values => SerializeStringList(values), json => DeserializeStringList(json));
        entity.Property(brief => brief.KnownConstraints)
            .HasConversion(values => SerializeStringList(values), json => DeserializeStringList(json));
    }

    private static string SerializeStringList(IReadOnlyList<string> values)
    {
        return JsonSerializer.Serialize(values, JsonOptions);
    }

    private static IReadOnlyList<string> DeserializeStringList(string json)
    {
        return JsonSerializer.Deserialize<List<string>>(json, JsonOptions) ?? [];
    }
}
