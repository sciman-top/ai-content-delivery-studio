using System.Text.Json;
using ContentDeliveryStudio.Core.Projects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ContentDeliveryStudio.Infrastructure.Persistence.Configurations;

internal sealed class ReviewResultConfiguration : IEntityTypeConfiguration<ReviewResult>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public void Configure(EntityTypeBuilder<ReviewResult> entity)
    {
        entity.HasKey(review => review.Id);
        // One current, updatable review result per candidate image; legacy
        // databases are deduplicated to this invariant by the schema
        // version-2 upgrade step in AppDatabaseInitializer.
        entity.HasIndex(review => review.CandidateImageId).IsUnique();
        entity.Property(review => review.Scores)
            .HasConversion(
                scores => SerializeScores(scores),
                json => DeserializeScores(json),
                new JsonValueComparer<IReadOnlyDictionary<string, int>>(SerializeScores, DeserializeScores));
        entity.Property(review => review.HardFailures)
            .HasConversion(
                failures => SerializeHardFailures(failures),
                json => DeserializeHardFailures(json),
                new JsonValueComparer<IReadOnlyList<string>>(SerializeHardFailures, DeserializeHardFailures));
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
