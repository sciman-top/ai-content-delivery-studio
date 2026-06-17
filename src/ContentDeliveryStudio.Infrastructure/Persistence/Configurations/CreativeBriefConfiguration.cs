using System.Text.Json;
using ContentDeliveryStudio.Core.Projects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ContentDeliveryStudio.Infrastructure.Persistence.Configurations;

internal sealed class CreativeBriefConfiguration : IEntityTypeConfiguration<CreativeBrief>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public void Configure(EntityTypeBuilder<CreativeBrief> entity)
    {
        entity.HasKey(brief => brief.Id);
        entity.Property(brief => brief.Goal).IsRequired();
        entity.Property(brief => brief.Audience).IsRequired();
        entity.Property(brief => brief.StyleIntent).IsRequired();
        entity.Property(brief => brief.MustInclude)
            .HasConversion(values => SerializeStringList(values), json => DeserializeStringList(json));
        entity.Property(brief => brief.MustAvoid)
            .HasConversion(values => SerializeStringList(values), json => DeserializeStringList(json));
        entity.Property(brief => brief.PromptDirections)
            .HasConversion(directions => SerializePromptDirections(directions), json => DeserializePromptDirections(json));
        entity.Property(brief => brief.DesignBlueprints)
            .HasConversion(blueprints => SerializeDesignBlueprints(blueprints), json => DeserializeDesignBlueprints(json));
        entity.Property(brief => brief.RepairNotesJson);
    }

    private static string SerializeStringList(IReadOnlyList<string> values)
    {
        return JsonSerializer.Serialize(values, JsonOptions);
    }

    private static IReadOnlyList<string> DeserializeStringList(string json)
    {
        return JsonSerializer.Deserialize<List<string>>(json, JsonOptions) ?? [];
    }

    private static string SerializePromptDirections(IReadOnlyCollection<PromptDirection> directions)
    {
        return JsonSerializer.Serialize(directions, JsonOptions);
    }

    private static IReadOnlyCollection<PromptDirection> DeserializePromptDirections(string json)
    {
        return JsonSerializer.Deserialize<List<PromptDirection>>(json, JsonOptions) ?? [];
    }

    private static string SerializeDesignBlueprints(IReadOnlyCollection<DesignBlueprint> blueprints)
    {
        return JsonSerializer.Serialize(blueprints, JsonOptions);
    }

    private static IReadOnlyCollection<DesignBlueprint> DeserializeDesignBlueprints(string json)
    {
        return JsonSerializer.Deserialize<List<DesignBlueprint>>(json, JsonOptions) ?? [];
    }
}
