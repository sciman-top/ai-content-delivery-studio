using System.Collections;
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
}
