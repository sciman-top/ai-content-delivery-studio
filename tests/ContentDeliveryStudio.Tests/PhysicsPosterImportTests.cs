using ContentDeliveryStudio.Core.Projects;
using ContentDeliveryStudio.Infrastructure.Import;

namespace ContentDeliveryStudio.Tests;

public sealed class PhysicsPosterImportTests
{
    [Fact]
    public async Task ImportAsync_MapsMasterListRowsToProjectSeriesItemsAndPrompts()
    {
        var root = Path.Combine(Path.GetTempPath(), "ContentDeliveryStudio.Tests", Guid.NewGuid().ToString("N"));
        var promptDirectory = Path.Combine(root, "prompts", "person", "01_classical");
        Directory.CreateDirectory(promptDirectory);
        Directory.CreateDirectory(Path.Combine(root, "tables"));

        try
        {
            await File.WriteAllTextAsync(
                Path.Combine(root, "manifest.json"),
                """{"project":"physics","model_default":"gpt-image-2","production_size":"2560x1440"}""",
                CancellationToken.None);
            await File.WriteAllTextAsync(
                Path.Combine(promptDirectory, "001_archimedes.md"),
                "Archimedes prompt",
                CancellationToken.None);
            await File.WriteAllTextAsync(
                Path.Combine(promptDirectory, "002_kepler.md"),
                "Kepler prompt",
                CancellationToken.None);
            await File.WriteAllTextAsync(
                Path.Combine(root, "tables", "master-list.csv"),
                string.Join(
                    Environment.NewLine,
                    [
                        "id,domain_cn,domain_folder,title_cn,subtitle_cn,hook_cn,primary_contribution,scientific_caution,prompt_file",
                        "1,古典物理,01_classical,阿基米德,浮力,浴池灵感,浮力原理,避免神话化,prompts/person/01_classical/001_archimedes.md",
                        "2,古典物理,01_classical,开普勒,行星运动,椭圆轨道,行星运动三定律,避免画成圆形,prompts/person/01_classical/002_kepler.md",
                    ]),
                CancellationToken.None);

            var timestamp = DateTimeOffset.Parse("2026-06-01T12:00:00Z");
            var project = await new PhysicsPosterImportService().ImportAsync(root, maxItems: 2, timestamp, CancellationToken.None);

            var series = Assert.Single(project.Series);
            Assert.Equal("古典物理", series.Title);
            Assert.Equal(2, series.Items.Count);
            Assert.Single(project.ProviderProfiles);
            Assert.All(series.Items, item => Assert.Single(item.PromptVersions));
            Assert.Contains(series.Items, item => item.Title == "阿基米德");
            Assert.Contains(series.Items.SelectMany(item => item.PromptVersions), prompt => prompt.PromptText == "Kepler prompt");
            Assert.All(
                series.Items.SelectMany(item => item.PromptVersions),
                prompt =>
                {
                    Assert.Equal(2560, prompt.Settings.Width);
                    Assert.Equal(1440, prompt.Settings.Height);
                    Assert.Equal("high", prompt.Settings.Quality);
                    Assert.Equal("png", prompt.Settings.OutputFormat);
                });
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }

    [Fact]
    public async Task ImportAsync_DoesNotModifySourceFiles()
    {
        var root = Path.Combine(Path.GetTempPath(), "ContentDeliveryStudio.Tests", Guid.NewGuid().ToString("N"));
        var promptDirectory = Path.Combine(root, "prompts");
        Directory.CreateDirectory(promptDirectory);
        Directory.CreateDirectory(Path.Combine(root, "tables"));

        try
        {
            var manifestPath = Path.Combine(root, "manifest.json");
            var masterListPath = Path.Combine(root, "tables", "master-list.csv");
            var promptPath = Path.Combine(promptDirectory, "prompt.md");
            await File.WriteAllTextAsync(manifestPath, """{"production_size":"1024x1024"}""", CancellationToken.None);
            await File.WriteAllTextAsync(promptPath, "Prompt text", CancellationToken.None);
            await File.WriteAllTextAsync(
                masterListPath,
                string.Join(
                    Environment.NewLine,
                    [
                        "domain_cn,domain_folder,title_cn,prompt_file",
                        "Domain,domain,Title,prompts/prompt.md",
                    ]),
                CancellationToken.None);
            var before = SnapshotFiles(root);

            _ = await new PhysicsPosterImportService().ImportAsync(
                root,
                maxItems: 1,
                DateTimeOffset.UtcNow,
                CancellationToken.None);

            Assert.Equal(before, SnapshotFiles(root));
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }

    [Fact]
    public async Task ImportAsync_BlocksPromptPathsOutsideSourceRoot()
    {
        var root = Path.Combine(Path.GetTempPath(), "ContentDeliveryStudio.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(root, "tables"));

        try
        {
            await File.WriteAllTextAsync(Path.Combine(root, "manifest.json"), "{}", CancellationToken.None);
            await File.WriteAllTextAsync(
                Path.Combine(root, "tables", "master-list.csv"),
                string.Join(
                    Environment.NewLine,
                    [
                        "domain_cn,domain_folder,title_cn,prompt_file",
                        "Domain,domain,Title,../outside.md",
                    ]),
                CancellationToken.None);

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                new PhysicsPosterImportService().ImportAsync(
                    root,
                    maxItems: 1,
                    DateTimeOffset.UtcNow,
                    CancellationToken.None));
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }

    [Fact]
    public async Task ImportFinalizedDeliveryAsync_MapsManifestImagesToCandidatesAndReviews()
    {
        var root = Path.Combine(Path.GetTempPath(), "ContentDeliveryStudio.Tests", Guid.NewGuid().ToString("N"));
        var deliveryDirectory = Path.Combine(root, "outputs", "finalized-by-content", "person", "domain");
        Directory.CreateDirectory(deliveryDirectory);

        try
        {
            var finalImage = Path.Combine(deliveryDirectory, "001_archimedes_v01-最终.png");
            var finalMetadata = Path.ChangeExtension(finalImage, ".json");
            var alternateImage = Path.Combine(deliveryDirectory, "001_archimedes_v02-备选.png");
            var alternateMetadata = Path.ChangeExtension(alternateImage, ".json");
            await File.WriteAllBytesAsync(finalImage, [1, 2, 3], CancellationToken.None);
            await File.WriteAllTextAsync(finalMetadata, """{"kind":"final"}""", CancellationToken.None);
            await File.WriteAllBytesAsync(alternateImage, [4, 5, 6], CancellationToken.None);
            await File.WriteAllTextAsync(alternateMetadata, """{"kind":"alternate"}""", CancellationToken.None);
            await File.WriteAllTextAsync(
                Path.Combine(root, "outputs", "finalized-by-content", "finalized-manifest.csv"),
                string.Join(
                    Environment.NewLine,
                    [
                        "series,id,title_cn,content_dir,item_dir,prompt_source,prompt_snapshot,final_count,alternate_count,final_images,alternate_images,metadata_files,status,warnings",
                        "person,001,阿基米德,domain,item,prompt.md,prompt_snapshot.md,1,1,outputs/finalized-by-content/person/domain/001_archimedes_v01-最终.png,outputs/finalized-by-content/person/domain/001_archimedes_v02-备选.png,outputs/finalized-by-content/person/domain/001_archimedes_v01-最终.json;outputs/finalized-by-content/person/domain/001_archimedes_v02-备选.json,ok,",
                    ]),
                CancellationToken.None);

            var imported = await new PhysicsPosterImportService().ImportFinalizedDeliveryAsync(
                root,
                maxRows: 1,
                DateTimeOffset.Parse("2026-06-01T12:00:00Z"),
                CancellationToken.None);

            var item = Assert.Single(imported);
            Assert.Equal("001", item.SourceId);
            Assert.Equal("阿基米德", item.Title);
            Assert.Equal(2, item.Candidates.Count);
            Assert.Contains(item.Candidates, candidate => candidate.CandidateImage.Status is CandidateImageStatus.Final && candidate.ReviewResult.HumanApproved);
            Assert.Contains(item.Candidates, candidate => candidate.CandidateImage.Status is CandidateImageStatus.Alternate && !candidate.ReviewResult.HumanApproved);
            Assert.All(item.Candidates, candidate =>
            {
                Assert.True(File.Exists(candidate.CandidateImage.AssetPath));
                Assert.True(File.Exists(candidate.CandidateImage.MetadataPath));
                Assert.Equal(candidate.CandidateImage.Id, candidate.ReviewResult.CandidateImageId);
            });
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }

    [Fact]
    public async Task ImportFinalizedDeliveryAsync_BlocksImagePathsOutsideSourceRoot()
    {
        var root = Path.Combine(Path.GetTempPath(), "ContentDeliveryStudio.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(root, "outputs", "finalized-by-content"));

        try
        {
            await File.WriteAllTextAsync(
                Path.Combine(root, "outputs", "finalized-by-content", "finalized-manifest.csv"),
                string.Join(
                    Environment.NewLine,
                    [
                        "series,id,title_cn,final_images,alternate_images,metadata_files,status,warnings",
                        "person,001,Title,../outside.png,,,needs_attention,",
                    ]),
                CancellationToken.None);

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                new PhysicsPosterImportService().ImportFinalizedDeliveryAsync(
                    root,
                    maxRows: 1,
                    DateTimeOffset.UtcNow,
                    CancellationToken.None));
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }

    private static IReadOnlyDictionary<string, string> SnapshotFiles(string root)
    {
        return Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories)
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                path => Path.GetRelativePath(root, path),
                path => File.ReadAllText(path),
                StringComparer.OrdinalIgnoreCase);
    }
}
