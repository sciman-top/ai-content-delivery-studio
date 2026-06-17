using ContentDeliveryStudio.Core.Projects;
using ContentDeliveryStudio.Core.Sources;
using ContentDeliveryStudio.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ContentDeliveryStudio.Tests;

public sealed class SourceAssetPersistenceTests
{
    [Fact]
    public async Task EfProjectRepository_PersistsSourceAssetsWithExtractedContentAndEvidenceAnchors()
    {
        var databaseDirectory = Path.Combine(Path.GetTempPath(), "ContentDeliveryStudio.Tests", Guid.NewGuid().ToString("N"));
        var databasePath = Path.Combine(databaseDirectory, "source-assets.sqlite");
        Directory.CreateDirectory(databaseDirectory);

        try
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite($"Data Source={databasePath};Pooling=False")
                .Options;

            var timestamp = DateTimeOffset.Parse("2026-06-03T09:00:00Z");
            Guid projectId;
            Guid sourceAssetId;
            Guid extractedContentId;

            await using (var db = new AppDbContext(options))
            {
                await db.Database.EnsureCreatedAsync();
                var project = ImageProject.Create("Source asset project", timestamp);
                var asset = SourceAsset.Create(
                    project.Id,
                    SourceAssetKind.Docx,
                    "brief.docx",
                    @"C:\work\brief.docx",
                    "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                    4096,
                    "sha256-demo",
                    timestamp.AddMinutes(1));
                var extracted = asset.AddExtractedContent(
                    ExtractedContentKind.PlainText,
                    "A reusable workflow should keep evidence traceable.",
                    "page 1",
                    pageNumber: 1,
                    startOffset: 0,
                    endOffset: 52,
                    timestamp.AddMinutes(2));
                asset.AddEvidenceAnchor(
                    extracted.Id,
                    "workflow evidence",
                    "keep evidence traceable",
                    "page 1",
                    timestamp.AddMinutes(3));

                project.AddSourceAsset(asset, timestamp.AddMinutes(4));
                projectId = project.Id;
                sourceAssetId = asset.Id;
                extractedContentId = extracted.Id;

                var repository = new EfProjectRepository(db);
                await repository.SaveAsync(project, CancellationToken.None);
            }

            await using (var db = new AppDbContext(options))
            {
                var repository = new EfProjectRepository(db);
                var loaded = await repository.LoadAsync(projectId, CancellationToken.None);

                var loadedAsset = Assert.Single(loaded!.SourceAssets);
                var loadedContent = Assert.Single(loadedAsset.ExtractedContents);
                var loadedAnchor = Assert.Single(loadedAsset.EvidenceAnchors);

                Assert.Equal(sourceAssetId, loadedAsset.Id);
                Assert.Equal(projectId, loadedAsset.ProjectId);
                Assert.Equal(SourceAssetKind.Docx, loadedAsset.Kind);
                Assert.Equal("brief.docx", loadedAsset.DisplayName);
                Assert.Equal(@"C:\work\brief.docx", loadedAsset.OriginalPath);
                Assert.Equal("sha256-demo", loadedAsset.Sha256);
                Assert.Equal(extractedContentId, loadedContent.Id);
                Assert.Equal("A reusable workflow should keep evidence traceable.", loadedContent.Text);
                Assert.Equal(loadedContent.Id, loadedAnchor.ExtractedContentId);
                Assert.Equal("workflow evidence", loadedAnchor.Label);
                Assert.Equal(timestamp.AddMinutes(4), loaded.UpdatedAt);
            }
        }
        finally
        {
            if (Directory.Exists(databaseDirectory))
            {
                Directory.Delete(databaseDirectory, recursive: true);
            }
        }
    }
}
