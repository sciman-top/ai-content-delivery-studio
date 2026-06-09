using System.Text.Json;
using ImageSeriesStudio.Application.ToolAdapters;
using ImageSeriesStudio.Core.Operators;
using ImageSeriesStudio.Infrastructure.OpenAI;
using ImageSeriesStudio.Infrastructure.ToolAdapters;

namespace ImageSeriesStudio.Tests;

public sealed class OpenAiLaunchPreflightToolAdapterTests
{
    [Fact]
    public async Task LowRiskAutoRepairService_RunsOpenAiLaunchPreflightAdapterAndWritesDiagnosticsReport()
    {
        var rootDirectory = Path.Combine(Path.GetTempPath(), "ImageSeriesStudio.Tests", Guid.NewGuid().ToString("N"));
        var previousOptIn = Environment.GetEnvironmentVariable(OpenAiSmokeTestGate.DefaultOptInEnvironmentVariable);
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

            Environment.SetEnvironmentVariable(OpenAiSmokeTestGate.DefaultOptInEnvironmentVariable, "1");

            var adapter = new OpenAiLaunchPreflightToolAdapter();
            var service = new LowRiskAutoRepairService([adapter]);
            var action = OperatorAction.CreateDraft(
                Guid.NewGuid(),
                repairPlanStepOrder: 1,
                toolAdapterId: "openai-launch-preflight",
                displayName: "OpenAI launch preflight",
                OperatorRiskLevel.Low,
                dryRunSupported: true,
                inputs: new Dictionary<string, string> { ["envPath"] = envPath },
                expectedOutputs: ["preflight report"],
                sideEffects: ["Reads provider configuration and secret readiness, writes an OpenAI launch preflight report."],
                timeout: TimeSpan.FromSeconds(30),
                cleanupPath: null,
                DateTimeOffset.Parse("2026-06-09T16:10:00Z"));

            var result = await service.RunAsync(
                action,
                dryRun: false,
                startedAt: DateTimeOffset.Parse("2026-06-09T16:11:00Z"),
                CancellationToken.None);

            var expectedJsonPath = Path.Combine(rootDirectory, "diagnostics", "openai-launch-preflight.json");
            var expectedMarkdownPath = Path.Combine(rootDirectory, "diagnostics", "openai-launch-preflight.md");

            Assert.Equal(OperatorRunStatus.Succeeded, result.Run.Status);
            Assert.Equal("openai-launch-preflight", result.Run.ToolAdapterId);
            Assert.Equal(expectedJsonPath, result.AdapterResult.Outputs["preflightJsonPath"]);
            Assert.Equal(expectedMarkdownPath, result.AdapterResult.Outputs["preflightMarkdownPath"]);
            Assert.True(File.Exists(expectedJsonPath));
            Assert.True(File.Exists(expectedMarkdownPath));

            using var reportStream = File.OpenRead(expectedJsonPath);
            using var reportJson = await JsonDocument.ParseAsync(reportStream, cancellationToken: CancellationToken.None);
            Assert.True(reportJson.RootElement.GetProperty("canRunLiveV1SampleSeries").GetBoolean());
            Assert.Empty(reportJson.RootElement.GetProperty("blockingReasons").EnumerateArray());
        }
        finally
        {
            Environment.SetEnvironmentVariable(OpenAiSmokeTestGate.DefaultOptInEnvironmentVariable, previousOptIn);

            if (Directory.Exists(rootDirectory))
            {
                Directory.Delete(rootDirectory, recursive: true);
            }
        }
    }

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
