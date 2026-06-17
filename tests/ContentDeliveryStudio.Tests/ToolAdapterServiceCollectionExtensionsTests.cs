using System.Text.Json;
using ContentDeliveryStudio.Application.Composition;
using ContentDeliveryStudio.Application.ToolAdapters;
using ContentDeliveryStudio.Core.Operators;
using ContentDeliveryStudio.Infrastructure.Composition;
using ContentDeliveryStudio.Infrastructure.OpenAI;
using ContentDeliveryStudio.Infrastructure.ToolAdapters;
using Microsoft.Extensions.DependencyInjection;

namespace ContentDeliveryStudio.Tests;

public sealed class ToolAdapterServiceCollectionExtensionsTests
{
    [Fact]
    public async Task AddBuiltInLocalToolAdapters_RegistersPreflightPathForLowRiskExecution()
    {
        var rootDirectory = Path.Combine(Path.GetTempPath(), "ContentDeliveryStudio.Tests", Guid.NewGuid().ToString("N"));
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

            var services = new ServiceCollection();
            services.AddSingleton<IDeterministicTextComposer, SkiaDeterministicTextComposer>();
            services.AddBuiltInLocalToolAdapters();

            using var serviceProvider = services.BuildServiceProvider();
            var adapterIds = serviceProvider
                .GetServices<IToolAdapter>()
                .Select(adapter => adapter.Descriptor.Id)
                .OrderBy(id => id, StringComparer.Ordinal)
                .ToArray();

            Assert.Equal(
                ["artifact-validation", "deterministic-text-composition", "openai-launch-preflight"],
                adapterIds);

            var service = serviceProvider.GetRequiredService<LowRiskAutoRepairService>();
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
                DateTimeOffset.Parse("2026-06-09T16:20:00Z"));

            var result = await service.RunAsync(
                action,
                dryRun: false,
                startedAt: DateTimeOffset.Parse("2026-06-09T16:21:00Z"),
                CancellationToken.None);

            var expectedJsonPath = Path.Combine(rootDirectory, "diagnostics", "openai-launch-preflight.json");
            Assert.Equal(OperatorRunStatus.Succeeded, result.Run.Status);
            Assert.True(File.Exists(expectedJsonPath));

            using var reportStream = File.OpenRead(expectedJsonPath);
            using var reportJson = await JsonDocument.ParseAsync(reportStream, cancellationToken: CancellationToken.None);
            Assert.True(reportJson.RootElement.GetProperty("canRunLiveV1SampleSeries").GetBoolean());
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
}
