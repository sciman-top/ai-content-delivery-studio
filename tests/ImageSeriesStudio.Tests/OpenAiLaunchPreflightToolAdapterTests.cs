using System.Text.Json;
using ImageSeriesStudio.Application.ToolAdapters;
using ImageSeriesStudio.Infrastructure.ToolAdapters;

namespace ImageSeriesStudio.Tests;

public sealed class OpenAiLaunchPreflightToolAdapterTests
{
    [Fact]
    public async Task RunAsync_WritesOpenAiLaunchPreflightReports()
    {
        var rootDirectory = Path.Combine(Path.GetTempPath(), "ImageSeriesStudio.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(rootDirectory);

        try
        {
            var envPath = Path.Combine(rootDirectory, ".env");
            await File.WriteAllLinesAsync(
                envPath,
                [
                    "TEXT_PROVIDER_BASE_URL=https://text.example/v1",
                    "TEXT_PROVIDER_API_KEY=sk-text",
                    "TEXT_PROVIDER_MODEL=gpt-5.5",
                    "IMAGE_PROVIDER_BASE_URL=https://image.example/v1",
                    "IMAGE_PROVIDER_MODEL=gpt-image-2",
                    "IMAGE_PROVIDER_API_KEY_1=sk-image-1",
                ]);

            var adapter = new OpenAiLaunchPreflightToolAdapter();
            var request = ToolAdapterRunRequest.Create(
                adapter.Descriptor,
                dryRun: false,
                inputs: new Dictionary<string, string>
                {
                    ["envPath"] = envPath,
                },
                DateTimeOffset.Parse("2026-06-09T16:00:00Z"));

            var result = await adapter.RunAsync(request, CancellationToken.None);

            var expectedJsonPath = Path.Combine(rootDirectory, "diagnostics", "openai-launch-preflight.json");
            var expectedMarkdownPath = Path.Combine(rootDirectory, "diagnostics", "openai-launch-preflight.md");
            Assert.Equal(expectedJsonPath, result.Outputs["preflightJsonPath"]);
            Assert.Equal(expectedMarkdownPath, result.Outputs["preflightMarkdownPath"]);
            Assert.True(File.Exists(expectedJsonPath));
            Assert.True(File.Exists(expectedMarkdownPath));

            using var reportStream = File.OpenRead(expectedJsonPath);
            using var reportJson = await JsonDocument.ParseAsync(reportStream, cancellationToken: CancellationToken.None);
            Assert.False(reportJson.RootElement.GetProperty("canRunLiveV1SampleSeries").GetBoolean());
            Assert.Equal(
                "IMAGE_SERIES_STUDIO_OPENAI_REAL_API_SMOKE",
                reportJson.RootElement.GetProperty("textSmoke").GetProperty("optInEnvironmentVariable").GetString());

            var markdown = await File.ReadAllTextAsync(expectedMarkdownPath, CancellationToken.None);
            Assert.Contains("# OpenAI Launch Preflight", markdown);
            Assert.Contains("CanRunLiveV1SampleSeries: False", markdown);
            Assert.Contains("not opted in", markdown, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            if (Directory.Exists(rootDirectory))
            {
                Directory.Delete(rootDirectory, recursive: true);
            }
        }
    }

    [Fact]
    public async Task RunAsync_DryRunPlansPreflightReportsWithoutWritingThem()
    {
        var rootDirectory = Path.Combine(Path.GetTempPath(), "ImageSeriesStudio.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(rootDirectory);

        try
        {
            var envPath = Path.Combine(rootDirectory, ".env");
            await File.WriteAllLinesAsync(
                envPath,
                [
                    "TEXT_PROVIDER_BASE_URL=https://text.example/v1",
                    "TEXT_PROVIDER_API_KEY=sk-text",
                    "TEXT_PROVIDER_MODEL=gpt-5.5",
                    "IMAGE_PROVIDER_BASE_URL=https://image.example/v1",
                    "IMAGE_PROVIDER_MODEL=gpt-image-2",
                    "IMAGE_PROVIDER_API_KEY_1=sk-image-1",
                ]);

            var adapter = new OpenAiLaunchPreflightToolAdapter();
            var request = ToolAdapterRunRequest.Create(
                adapter.Descriptor,
                dryRun: true,
                inputs: new Dictionary<string, string>
                {
                    ["envPath"] = envPath,
                },
                DateTimeOffset.Parse("2026-06-09T16:05:00Z"));

            var result = await adapter.RunAsync(request, CancellationToken.None);

            var expectedJsonPath = Path.Combine(rootDirectory, "diagnostics", "openai-launch-preflight.json");
            var expectedMarkdownPath = Path.Combine(rootDirectory, "diagnostics", "openai-launch-preflight.md");
            Assert.Equal(expectedJsonPath, result.Outputs["preflightJsonPath"]);
            Assert.Equal(expectedMarkdownPath, result.Outputs["preflightMarkdownPath"]);
            Assert.False(File.Exists(expectedJsonPath));
            Assert.False(File.Exists(expectedMarkdownPath));
            Assert.Contains("dry-run", result.Summary, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            if (Directory.Exists(rootDirectory))
            {
                Directory.Delete(rootDirectory, recursive: true);
            }
        }
    }
}
