using System.Text.Json;
using ContentDeliveryStudio.Core.Documents;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ContentDeliveryStudio.Infrastructure.Persistence.Configurations;

internal sealed class IllustrationPlanConfiguration : IEntityTypeConfiguration<IllustrationPlan>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public void Configure(EntityTypeBuilder<IllustrationPlan> entity)
    {
        entity.HasKey(plan => plan.Id);
        entity.Property(plan => plan.ProjectId);
        entity.Property(plan => plan.DocumentBriefId);
        entity.Property(plan => plan.Summary).IsRequired();
        entity.Property(plan => plan.CreatedAt);
        entity.Property(plan => plan.UpdatedAt);
        entity.Ignore(plan => plan.ApprovedTargets);
        entity.HasOne<DocumentBrief>()
            .WithMany()
            .HasForeignKey(plan => plan.DocumentBriefId)
            .OnDelete(DeleteBehavior.Restrict);
        entity.Property(plan => plan.Targets)
            .HasConversion(targets => SerializeTargets(targets), json => DeserializeTargets(json));
        entity.Property(plan => plan.CoverageNotes)
            .HasConversion(values => SerializeStringList(values), json => DeserializeStringList(json));
        entity.Property(plan => plan.RiskNotes)
            .HasConversion(values => SerializeStringList(values), json => DeserializeStringList(json));
    }

    private static string SerializeTargets(IReadOnlyList<IllustrationTarget> targets)
    {
        return JsonSerializer.Serialize(targets, JsonOptions);
    }

    private static IReadOnlyList<IllustrationTarget> DeserializeTargets(string json)
    {
        return JsonSerializer.Deserialize<List<IllustrationTarget>>(json, JsonOptions) ?? [];
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
