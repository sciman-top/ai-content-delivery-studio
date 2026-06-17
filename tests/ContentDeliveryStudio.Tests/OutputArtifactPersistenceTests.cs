using ContentDeliveryStudio.Core.Artifacts;
using ContentDeliveryStudio.Core.Projects;
using ContentDeliveryStudio.Core.Sources;
using ContentDeliveryStudio.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ContentDeliveryStudio.Tests;

public sealed class OutputArtifactPersistenceTests
{
    [Fact]
    public async Task EfProjectRepository_PersistsOutputArtifactsAndArtifactPackages()
    {
        var databaseDirectory = Path.Combine(Path.GetTempPath(), "ContentDeliveryStudio.Tests", Guid.NewGuid().ToString("N"));
        var databasePath = Path.Combine(databaseDirectory, "output-artifacts.sqlite");
        Directory.CreateDirectory(databaseDirectory);

        try
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite($"Data Source={databasePath};Pooling=False")
                .Options;

            var timestamp = DateTimeOffset.Parse("2026-06-03T10:00:00Z");
            Guid projectId;
            Guid sourceAssetId;
            Guid evidenceAnchorId;
            Guid artifactId;
            Guid packageId;

            await using (var db = new AppDbContext(options))
            {
                await db.Database.EnsureCreatedAsync();
                var project = ImageProject.Create("Artifact persistence project", timestamp);
                var source = SourceAsset.Create(
                    project.Id,
                    SourceAssetKind.Markdown,
                    "source.md",
                    "source.md",
                    "text/markdown",
                    512,
                    "sha256-source",
                    timestamp.AddMinutes(1));
                var content = source.AddExtractedContent(
                    ExtractedContentKind.Markdown,
                    "All artifact claims should remain traceable.",
                    "section 1",
                    pageNumber: null,
                    startOffset: 0,
                    endOffset: 44,
                    timestamp.AddMinutes(2));
                var anchor = source.AddEvidenceAnchor(
                    content.Id,
                    "traceability",
                    "claims should remain traceable",
                    "section 1",
                    timestamp.AddMinutes(3));
                var artifact = OutputArtifact.Create(
                    project.Id,
                    OutputArtifactKind.Markdown,
                    "Traceable summary",
                    "delivery/summary.md",
                    "text/markdown",
                    "summary",
                    [source.Id],
                    [anchor.Id],
                    new Dictionary<string, string> { ["language"] = "en-US" },
                    timestamp.AddMinutes(4));
                var package = ArtifactPackage.Create(
                    project.Id,
                    "Markdown delivery",
                    "delivery",
                    version: 1,
                    [artifact],
                    timestamp.AddMinutes(5));

                project.AddSourceAsset(source, timestamp.AddMinutes(6));
                project.AddOutputArtifact(artifact, timestamp.AddMinutes(7));
                project.AddArtifactPackage(package, timestamp.AddMinutes(8));
                projectId = project.Id;
                sourceAssetId = source.Id;
                evidenceAnchorId = anchor.Id;
                artifactId = artifact.Id;
                packageId = package.Id;

                var repository = new EfProjectRepository(db);
                await repository.SaveAsync(project, CancellationToken.None);
            }

            await using (var db = new AppDbContext(options))
            {
                var repository = new EfProjectRepository(db);
                var loaded = await repository.LoadAsync(projectId, CancellationToken.None);

                var loadedArtifact = Assert.Single(loaded!.OutputArtifacts);
                var loadedPackage = Assert.Single(loaded.ArtifactPackages);
                var manifestItem = Assert.Single(loadedPackage.Manifest.Items);

                Assert.Equal(artifactId, loadedArtifact.Id);
                Assert.Equal(OutputArtifactKind.Markdown, loadedArtifact.Kind);
                Assert.Equal("Traceable summary", loadedArtifact.DisplayName);
                Assert.Equal([sourceAssetId], loadedArtifact.SourceAssetIds);
                Assert.Equal([evidenceAnchorId], loadedArtifact.EvidenceAnchorIds);
                Assert.Equal("en-US", loadedArtifact.Metadata["language"]);
                Assert.Equal(packageId, loadedPackage.Id);
                Assert.Equal(1, loadedPackage.Manifest.Version);
                Assert.Equal(loadedArtifact.Id, manifestItem.OutputArtifactId);
                Assert.Equal(loadedArtifact.RelativePath, manifestItem.RelativePath);
                Assert.Equal(timestamp.AddMinutes(8), loaded.UpdatedAt);
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
