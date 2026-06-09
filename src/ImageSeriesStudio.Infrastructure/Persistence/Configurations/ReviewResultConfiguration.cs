using System.Text.Json;
using ImageSeriesStudio.Core.Projects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ImageSeriesStudio.Infrastructure.Persistence.Configurations;

internal sealed class ReviewResultConfiguration : IEntityTypeConfiguration<ReviewResult>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public void Configure(EntityTypeBuilder<ReviewResult> entity)
    {
        entity.HasKey(review => review.Id);
        entity.Property(review => review.Scores)
            .HasConversion(scores => SerializeScores(scores), json => DeserializeScores(json));
        entity.Property(review => review.HardFailures)
            .HasConversion(failures => SerializeHardFailures(failures), json => DeserializeHardFailures(json));
    }

    private static string SerializeScores(IReadOnlyDictionary<string, int> scores)
    {
        return JsonSerializer.Serialize(scores, JsonOptions);
    }

    private static IReadOnlyDictionary<string, int> DeserializeScores(string json)
    {
        return JsonSerializer.Deserialize<Dictionary<string, int>>(json, JsonOptions) ?? new Dictionary<string, int>();
    }

    private static string SerializeHardFailures(IReadOnlyList<string> failures)
    {
        return JsonSerializer.Serialize(failures, JsonOptions);
    }

    private static IReadOnlyList<string> DeserializeHardFailures(string json)
    {
        return JsonSerializer.Deserialize<List<string>>(json, JsonOptions) ?? [];
    }
}
