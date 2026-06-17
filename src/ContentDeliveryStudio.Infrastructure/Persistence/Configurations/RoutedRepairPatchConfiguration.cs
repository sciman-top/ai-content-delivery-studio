using System.Text.Json;
using ContentDeliveryStudio.Core.Projects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ContentDeliveryStudio.Infrastructure.Persistence.Configurations;

internal sealed class RoutedRepairPatchConfiguration : IEntityTypeConfiguration<RoutedRepairPatch>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public void Configure(EntityTypeBuilder<RoutedRepairPatch> entity)
    {
        entity.HasKey(patch => patch.Id);
        entity.Property(patch => patch.ProjectId);
        entity.Property(patch => patch.Items)
            .HasConversion(items => SerializeItems(items), json => DeserializeItems(json));
    }

    private static string SerializeItems(IReadOnlyList<RoutedRepairPatchItem> items)
    {
        return JsonSerializer.Serialize(items, JsonOptions);
    }

    private static IReadOnlyList<RoutedRepairPatchItem> DeserializeItems(string json)
    {
        return JsonSerializer.Deserialize<List<RoutedRepairPatchItem>>(json, JsonOptions) ?? [];
    }
}
