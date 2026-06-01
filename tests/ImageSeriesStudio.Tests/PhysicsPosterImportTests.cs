using ImageSeriesStudio.Core.Projects;
using ImageSeriesStudio.Infrastructure.Import;

namespace ImageSeriesStudio.Tests;

public sealed class PhysicsPosterImportTests
{
    [Fact]
    public async Task ImportAsync_MapsMasterListRowsToProjectSeriesItemsAndPrompts()
    {
        var root = Path.Combine(Path.GetTempPath(), "ImageSeriesStudio.Tests", Guid.NewGuid().ToString("N"));
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
        var root = Path.Combine(Path.GetTempPath(), "ImageSeriesStudio.Tests", Guid.NewGuid().ToString("N"));
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
        var root = Path.Combine(Path.GetTempPath(), "ImageSeriesStudio.Tests", Guid.NewGuid().ToString("N"));
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
