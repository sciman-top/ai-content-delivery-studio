using System.Collections;
using ContentDeliveryStudio.Core.Artifacts;
using ContentDeliveryStudio.Core.Sources;
using ContentDeliveryStudio.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;

namespace ContentDeliveryStudio.Tests;

public sealed class PersistenceModelContractTests
{
    [Fact]
    public void JsonConvertedCollections_HaveValueComparers()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite("Data Source=:memory:")
            .ConfigureWarnings(warnings => warnings.Throw(CoreEventId.CollectionWithoutComparer))
            .Options;

        using var db = new AppDbContext(options);

        var model = db.GetService<IDesignTimeModel>().Model;
        var missingComparers = model
            .GetEntityTypes()
            .SelectMany(entity => entity.GetProperties())
            .Where(property => property.GetTypeMapping().Converter is not null)
            .Where(property => property.ClrType != typeof(string)
                && typeof(IEnumerable).IsAssignableFrom(property.ClrType))
            .Where(property => property is not IConventionProperty conventionProperty
                || conventionProperty.GetValueComparerConfigurationSource() is null)
            .Select(property => $"{property.DeclaringType.ClrType.Name}.{property.Name}")
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

        Assert.True(
            missingComparers.Length == 0,
            $"JSON-converted collection properties require value comparers:{Environment.NewLine}{string.Join(Environment.NewLine, missingComparers)}");
    }

    [Fact]
    public void JsonValueComparer_DetectsInPlaceSourceCollectionMutation()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite("Data Source=:memory:")
            .Options;
        var timestamp = new DateTimeOffset(2026, 7, 28, 14, 0, 0, TimeSpan.Zero);
        var asset = SourceAsset.Create(
            Guid.NewGuid(),
            SourceAssetKind.Pdf,
            "source.pdf",
            "source.pdf",
            "application/pdf",
            128,
            "sha256",
            timestamp);

        using var db = new AppDbContext(options);
        var entry = db.Attach(asset);
        Assert.Equal(EntityState.Unchanged, entry.State);

        asset.AddExtractedContent(
            ExtractedContentKind.PlainText,
            "Net force causes acceleration.",
            "page 4",
            4,
            null,
            null,
            timestamp.AddMinutes(1));
        db.ChangeTracker.DetectChanges();

        Assert.True(entry.Property(nameof(SourceAsset.ExtractedContents)).IsModified);
    }

    [Fact]
    public void JsonValueComparer_UsesConsistentEqualityHashAndDeepSnapshot()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite("Data Source=:memory:")
            .Options;
        using var db = new AppDbContext(options);
        var property = db.GetService<IDesignTimeModel>().Model
            .FindEntityType(typeof(OutputArtifact))!
            .FindProperty(nameof(OutputArtifact.SourceAssetIds))!;
        var comparer = property.GetValueComparer()!;
        var original = new List<Guid> { Guid.NewGuid() };

        var snapshot = Assert.IsAssignableFrom<IReadOnlyList<Guid>>(comparer.Snapshot(original));

        Assert.NotSame(original, snapshot);
        Assert.True(comparer.Equals(original, snapshot));
        Assert.Equal(comparer.GetHashCode(original), comparer.GetHashCode(snapshot));

        original.Add(Guid.NewGuid());

        Assert.False(comparer.Equals(original, snapshot));
    }

    [Fact]
    public void JsonValueComparer_TreatsDictionaryInsertionOrderAsStructurallyEquivalent()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite("Data Source=:memory:")
            .Options;
        using var db = new AppDbContext(options);
        var property = db.GetService<IDesignTimeModel>().Model
            .FindEntityType(typeof(OutputArtifact))!
            .FindProperty(nameof(OutputArtifact.Metadata))!;
        var comparer = property.GetValueComparer()!;
        IReadOnlyDictionary<string, string> first = new Dictionary<string, string>
        {
            ["alpha"] = "1",
            ["beta"] = "2",
        };
        IReadOnlyDictionary<string, string> second = new Dictionary<string, string>
        {
            ["beta"] = "2",
            ["alpha"] = "1",
        };

        Assert.True(comparer.Equals(first, second));
        Assert.Equal(comparer.GetHashCode(first), comparer.GetHashCode(second));
    }
}
