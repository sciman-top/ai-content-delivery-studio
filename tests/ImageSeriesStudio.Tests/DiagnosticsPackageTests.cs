using ImageSeriesStudio.Application.Diagnostics;
using ImageSeriesStudio.Core.Projects;
using ImageSeriesStudio.Infrastructure.Diagnostics;

namespace ImageSeriesStudio.Tests;

public sealed class DiagnosticsPackageTests
{
    [Fact]
    public async Task DiagnosticsPackageWriter_WritesRedactedDiagnostics()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), $"image-series-diagnostics-{Guid.NewGuid():N}");

        try
        {
            var project = ImageProject.Create("Diagnostics demo", DateTimeOffset.Parse("2026-06-01T08:00:00Z"));
            var provider = project.AddProviderProfile("Fake provider", ProviderKind.Fake, DateTimeOffset.Parse("2026-06-01T08:01:00Z"));
            var series = project.AddSeries("Series", "Brief", DateTimeOffset.Parse("2026-06-01T08:02:00Z"));
            var item = series.AddItem("Item", "Item brief", DateTimeOffset.Parse("2026-06-01T08:03:00Z"));
            item.AddPromptVersion(
                "Prompt",
                new GenerationSettings(1024, 1024, "standard", "png"),
                provider.Id,
                DateTimeOffset.Parse("2026-06-01T08:04:00Z"));

            var writer = new DiagnosticsPackageWriter();
            var result = await writer.WriteAsync(
                new DiagnosticsExportRequest(
                    tempRoot,
                    new DiagnosticsApplicationSnapshot(
                        "AI Image Series Studio",
                        "0.1.0-test",
                        "Debug",
                        DateTimeOffset.Parse("2026-06-01T08:05:00Z")),
                    new DiagnosticsMachineSnapshot(
                        "Test OS",
                        ".NET test",
                        "X64",
                        "zh-CN",
                        "zh-CN"),
                    [DiagnosticsProjectSnapshot.FromProject(project)],
                    [
                        new DiagnosticsProviderSnapshot(
                            "fake-image",
                            "Fake image",
                            "Fake",
                            ["fake-image-v1"],
                            ["image-generation"],
                            RealApiEnabled: false,
                            DryRunOnly: true),
                    ],
                    [new DiagnosticsSecretSnapshot("OPENAI_API_KEY", IsConfigured: true)]),
                CancellationToken.None);

            var json = await File.ReadAllTextAsync(result.JsonPath);
            var markdown = await File.ReadAllTextAsync(result.MarkdownPath);

            Assert.True(File.Exists(result.JsonPath));
            Assert.True(File.Exists(result.MarkdownPath));
            Assert.Contains("Diagnostics demo", json);
            Assert.Contains("\"isConfigured\": true", json);
            Assert.DoesNotContain("test-openai-key", json);
            Assert.DoesNotContain("test-openai-key", markdown);
            Assert.Contains("configured=True", markdown);
        }
        finally
        {
            if (Directory.Exists(tempRoot))
            {
                Directory.Delete(tempRoot, recursive: true);
            }
        }
    }

    [Fact]
    public void DiagnosticsProjectSnapshot_FromProject_CountsNestedState()
    {
        var timestamp = DateTimeOffset.Parse("2026-06-01T08:00:00Z");
        var project = ImageProject.Create("Snapshot demo", timestamp);
        var provider = project.AddProviderProfile("Fake provider", ProviderKind.Fake, timestamp.AddMinutes(1));
        var series = project.AddSeries("Series", "Brief", timestamp.AddMinutes(2));
        var item = series.AddItem("Item", "Brief", timestamp.AddMinutes(3));
        item.AddPromptVersion(
            "Prompt",
            new GenerationSettings(1024, 1024, "standard", "png"),
            provider.Id,
            timestamp.AddMinutes(4));

        var snapshot = DiagnosticsProjectSnapshot.FromProject(project);

        Assert.Equal(1, snapshot.SeriesCount);
        Assert.Equal(1, snapshot.ItemCount);
        Assert.Equal(1, snapshot.PromptVersionCount);
        Assert.Equal(1, snapshot.ProviderProfileCount);
        Assert.Equal(timestamp.AddMinutes(4), snapshot.UpdatedAt);
    }
}
