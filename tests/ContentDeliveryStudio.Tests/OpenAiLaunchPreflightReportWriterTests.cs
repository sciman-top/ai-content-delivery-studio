using System.Text.Json;
using ContentDeliveryStudio.Infrastructure.OpenAI;

namespace ContentDeliveryStudio.Tests;

public sealed class OpenAiLaunchPreflightReportWriterTests
{
    [Fact]
    public async Task WriteAsync_WritesJsonAndMarkdownSummary()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), $"openai-launch-preflight-{Guid.NewGuid():N}");

        try
        {
            var report = new OpenAiLaunchPreflightReport(
                ["Image provider total concurrency must equal API key count multiplied by concurrency per key (20)."],
                new OpenAiOperationPreflight(OpenAiProviderOperation.TextPlanning, "TEXT_PROVIDER", true, []),
                new OpenAiOperationPreflight(OpenAiProviderOperation.VisionReview, "TEXT_PROVIDER", true, []),
                new OpenAiOperationPreflight(
                    OpenAiProviderOperation.ImageGeneration,
                    "IMAGE_PROVIDER",
                    false,
                    ["OpenAI API key was not found in the configured secret store."]),
                new OpenAiSmokeTestDecision(
                    CanRunRealApiSmoke: false,
                    IsDryRun: true,
                    OpenAiSmokeTestGate.DefaultOptInEnvironmentVariable,
                    ["Real OpenAI API smoke is not opted in."]),
                new OpenAiSmokeTestDecision(
                    CanRunRealApiSmoke: false,
                    IsDryRun: true,
                    OpenAiSmokeTestGate.DefaultOptInEnvironmentVariable,
                    ["OpenAI API key was not found in the configured secret store."]),
                CanRunLiveV1SampleSeries: false,
                ["Real OpenAI API smoke is not opted in.", "OpenAI API key was not found in the configured secret store."]);

            var writer = new OpenAiLaunchPreflightReportWriter();
            var result = await writer.WriteAsync(tempRoot, report, CancellationToken.None);

            Assert.True(File.Exists(result.JsonPath));
            Assert.True(File.Exists(result.MarkdownPath));

            using var jsonStream = File.OpenRead(result.JsonPath);
            using var json = await JsonDocument.ParseAsync(jsonStream, cancellationToken: CancellationToken.None);
            Assert.False(json.RootElement.GetProperty("canRunLiveV1SampleSeries").GetBoolean());
            Assert.Equal("TEXT_PROVIDER", json.RootElement.GetProperty("textPlanning").GetProperty("providerPrefix").GetString());
            Assert.Equal(
                OpenAiSmokeTestGate.DefaultOptInEnvironmentVariable,
                json.RootElement.GetProperty("textSmoke").GetProperty("optInEnvironmentVariable").GetString());

            var markdown = await File.ReadAllTextAsync(result.MarkdownPath, CancellationToken.None);
            Assert.Contains("# OpenAI Launch Preflight", markdown);
            Assert.Contains("CanRunLiveV1SampleSeries: False", markdown);
            Assert.Contains("## Operations", markdown);
            Assert.Contains("IMAGE_PROVIDER", markdown);
            Assert.Contains("## Smoke Gates", markdown);
            Assert.Contains(OpenAiSmokeTestGate.DefaultOptInEnvironmentVariable, markdown);
            Assert.Contains("## Blocking Reasons", markdown);
        }
        finally
        {
            if (Directory.Exists(tempRoot))
            {
                Directory.Delete(tempRoot, recursive: true);
            }
        }
    }
}
