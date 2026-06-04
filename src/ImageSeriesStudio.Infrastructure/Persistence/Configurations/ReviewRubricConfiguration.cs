using System.Text.Json;
using ImageSeriesStudio.Core.Projects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ImageSeriesStudio.Infrastructure.Persistence.Configurations;

internal sealed class ReviewRubricConfiguration : IEntityTypeConfiguration<ReviewRubric>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public void Configure(EntityTypeBuilder<ReviewRubric> entity)
    {
        entity.HasKey(rubric => rubric.Id);
        entity.Property(rubric => rubric.Name).IsRequired();
        entity.Property(rubric => rubric.Dimensions)
            .HasConversion(dimensions => SerializeDimensions(dimensions), json => DeserializeDimensions(json));
    }

    private static string SerializeDimensions(IReadOnlyList<ReviewRubricDimension> dimensions)
    {
        return JsonSerializer.Serialize(dimensions, JsonOptions);
    }

    private static IReadOnlyList<ReviewRubricDimension> DeserializeDimensions(string json)
    {
        return JsonSerializer.Deserialize<List<ReviewRubricDimension>>(json, JsonOptions) ?? [];
    }
}
