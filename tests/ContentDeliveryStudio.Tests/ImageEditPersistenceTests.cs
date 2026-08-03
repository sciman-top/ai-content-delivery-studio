using System.Data;
using ContentDeliveryStudio.Application.Projects;
using ContentDeliveryStudio.Core.Projects;
using ContentDeliveryStudio.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ContentDeliveryStudio.Tests;

public sealed class ImageEditPersistenceTests
{
    [Fact]
    public async Task NewSchema_RoundTripsEditedCandidateWithNullableTaskAndPathSafeProvenance()
    {
        var databasePath = CreateDatabasePath();
        var options = CreateOptions(databasePath);
        var timestamp = DateTimeOffset.Parse("2026-08-03T09:00:00Z");
        var project = ImageProject.Create("Edit persistence", timestamp);
        var profile = project.AddProviderProfile("Source provider", ProviderKind.Fake, timestamp);
        var series = project.AddSeries("Series", "Edit persistence", timestamp);
        var item = series.AddItem("Panel", "Panel", timestamp);
        var prompt = item.AddPromptVersion(
            "Original prompt",
            new GenerationSettings(1024, 1024, "high", "png"),
            profile.Id,
            timestamp);
        var sourceId = Guid.NewGuid();
        var receiptId = Guid.NewGuid();
        var edited = item.AddCandidateImage(
            new CandidateImage(
                Guid.NewGuid(),
                item.Id,
                prompt.Id,
                generationTaskId: null,
                profile.Id,
                CandidateImageStatus.ReviewPending,
                "workspace/edited/candidate.png",
                "workspace/edited/candidate.json",
                timestamp,
                new CandidateImageEditProvenance(
                    Guid.NewGuid(),
                    sourceId,
                    new string('a', 64),
                    new string('b', 64),
                    new string('c', 64),
                    "openai-image-edit",
                    "images/edits",
                    "gpt-image-2",
                    [new CandidateImageEditReferenceProvenance(sourceId, "Subject", new string('a', 64))],
                    receiptId,
                    new string('d', 64),
                    timestamp)),
            timestamp);

        try
        {
            await using (var create = new AppDbContext(options))
            {
                await AppDatabaseInitializer.InitializeAsync(create, CancellationToken.None);
                await new EfProjectRepository(create).SaveAsync(project, CancellationToken.None);
            }

            await using (var load = new AppDbContext(options))
            {
                var loaded = await new EfProjectRepository(load).LoadAsync(project.Id, CancellationToken.None);
                var loadedCandidate = Assert.Single(Assert.Single(Assert.Single(loaded!.Series).Items).CandidateImages);

                Assert.Equal(edited.Id, loadedCandidate.Id);
                Assert.Null(loadedCandidate.GenerationTaskId);
                Assert.Equal(sourceId, loadedCandidate.EditProvenance?.SourceCandidateImageId);
                Assert.Equal(receiptId, loadedCandidate.EditProvenance?.ApprovalReceiptId);
                Assert.Equal("openai-image-edit", loadedCandidate.EditProvenance?.ProviderId);
                Assert.Equal("Subject", Assert.Single(loadedCandidate.EditProvenance!.References).Role);
            }
        }
        finally
        {
            File.Delete(databasePath);
        }
    }

    [Fact]
    public async Task Initializer_AddsNullableEditProvenanceToLegacyCandidateSchemaIdempotently()
    {
        var databasePath = CreateDatabasePath();
        var options = CreateOptions(databasePath);

        try
        {
            await using (var legacy = new AppDbContext(options))
            {
                await legacy.Database.EnsureCreatedAsync();
                await legacy.Database.OpenConnectionAsync();
                await legacy.Database.ExecuteSqlRawAsync("PRAGMA foreign_keys=OFF;");
                await legacy.Database.ExecuteSqlRawAsync("DROP TABLE \"CandidateImages\";");
                await legacy.Database.ExecuteSqlRawAsync(
                    """
                    CREATE TABLE "CandidateImages" (
                        "Id" TEXT NOT NULL CONSTRAINT "PK_CandidateImages" PRIMARY KEY,
                        "SeriesItemId" TEXT NOT NULL,
                        "PromptVersionId" TEXT NOT NULL,
                        "GenerationTaskId" TEXT NOT NULL,
                        "ProviderProfileId" TEXT NOT NULL,
                        "Status" INTEGER NOT NULL,
                        "AssetPath" TEXT NOT NULL,
                        "MetadataPath" TEXT NOT NULL,
                        "CreatedAt" TEXT NOT NULL,
                        CONSTRAINT "FK_CandidateImages_SeriesItems_SeriesItemId"
                            FOREIGN KEY ("SeriesItemId") REFERENCES "SeriesItems" ("Id") ON DELETE CASCADE
                    );
                    """);
                await legacy.Database.ExecuteSqlRawAsync(
                    "CREATE INDEX \"IX_CandidateImages_SeriesItemId\" ON \"CandidateImages\" (\"SeriesItemId\");");
                await legacy.Database.ExecuteSqlRawAsync("PRAGMA foreign_keys=ON;");
            }

            await using (var upgrade = new AppDbContext(options))
            {
                await AppDatabaseInitializer.InitializeAsync(upgrade, CancellationToken.None);
                await AppDatabaseInitializer.InitializeAsync(upgrade, CancellationToken.None);
                var columns = await ReadCandidateColumnsAsync(upgrade);
                var editProvenance = Assert.Single(columns, column => column.Name == "EditProvenance");
                Assert.False(editProvenance.NotNull);
                var generationTaskId = Assert.Single(columns, column => column.Name == "GenerationTaskId");
                Assert.False(generationTaskId.NotNull);
            }
        }
        finally
        {
            File.Delete(databasePath);
        }
    }

    private static DbContextOptions<AppDbContext> CreateOptions(string databasePath)
    {
        return new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite($"Data Source={databasePath};Pooling=False")
            .Options;
    }

    private static string CreateDatabasePath()
    {
        return Path.Combine(
            Path.GetTempPath(),
            "ContentDeliveryStudio.Tests",
            $"image-edit-{Guid.NewGuid():N}.sqlite");
    }

    private static async Task<IReadOnlyList<ColumnInfo>> ReadCandidateColumnsAsync(AppDbContext dbContext)
    {
        var connection = dbContext.Database.GetDbConnection();
        if (connection.State is not ConnectionState.Open)
        {
            await connection.OpenAsync();
        }

        var columns = new List<ColumnInfo>();
        await using var command = connection.CreateCommand();
        command.CommandText = "PRAGMA table_info('CandidateImages');";
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            columns.Add(new ColumnInfo(reader.GetString(1), reader.GetInt32(3) != 0));
        }

        return columns;
    }

    private sealed record ColumnInfo(string Name, bool NotNull);
}
