using System.Text.Json;
using ImageSeriesStudio.Application.ToolAdapters;
using ImageSeriesStudio.Core.Operators;
using ImageSeriesStudio.Infrastructure.ToolAdapters;

namespace ImageSeriesStudio.Tests;

public sealed class ArtifactValidationToolAdapterTests
{
    [Fact]
    public async Task LowRiskAutoRepairService_RunsArtifactValidationAdapterAndWritesDiagnosticsReport()
    {
        var rootDirectory = Path.Combine(Path.GetTempPath(), "ImageSeriesStudio.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(rootDirectory);

        try
        {
            var deliveryDirectory = Path.Combine(rootDirectory, "delivery");
            Directory.CreateDirectory(deliveryDirectory);

            var manifestPath = Path.Combine(deliveryDirectory, "manifest.json");
            await File.WriteAllTextAsync(
                manifestPath,
                JsonSerializer.Serialize(new
                {
                    projectName = "Artifact validation demo",
                    items = new[]
                    {
                        new
                        {
                            itemKey = "item-01",
                            title = "Validated item",
                            imagePath = "images/item-01.png",
                            promptPath = "prompts/item-01.txt",
                            reviewDecision = "Pass",
                            humanApproved = true,
                        },
                    },
                }),
                CancellationToken.None);

            var service = new LowRiskAutoRepairService([new ArtifactValidationToolAdapter()]);
            var action = OperatorAction.CreateDraft(
                Guid.NewGuid(),
                repairPlanStepOrder: 1,
                toolAdapterId: "artifact-validation",
                displayName: "Validate artifact manifest",
                OperatorRiskLevel.Low,
                dryRunSupported: true,
                inputs: new Dictionary<string, string> { ["manifestPath"] = manifestPath },
                expectedOutputs: ["validation report"],
                sideEffects: ["Reads delivery artifacts and writes a validation report."],
                timeout: TimeSpan.FromSeconds(30),
                cleanupPath: null,
                DateTimeOffset.Parse("2026-06-07T09:00:00Z"));

            var result = await service.RunAsync(
                action,
                dryRun: false,
                startedAt: DateTimeOffset.Parse("2026-06-07T09:01:00Z"),
                CancellationToken.None);

            var expectedReportPath = Path.Combine(rootDirectory, "diagnostics", "manifest.validation.json");
            Assert.Equal(OperatorRunStatus.Succeeded, result.Run.Status);
            Assert.Equal("artifact-validation", result.Run.ToolAdapterId);
            Assert.Equal(expectedReportPath, result.AdapterResult.Outputs["validationReportPath"]);
            Assert.True(File.Exists(expectedReportPath));
            Assert.Contains("validation succeeded", result.AdapterResult.Summary, StringComparison.OrdinalIgnoreCase);

            var reportJson = await File.ReadAllTextAsync(expectedReportPath, CancellationToken.None);
            Assert.Contains("\"isValid\": true", reportJson);
            Assert.Contains("\"itemCount\": 1", reportJson);
            Assert.Contains("Delete the generated diagnostics report", reportJson);
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
    public async Task LowRiskAutoRepairService_DryRunPlansArtifactValidationReportWithoutWritingIt()
    {
        var rootDirectory = Path.Combine(Path.GetTempPath(), "ImageSeriesStudio.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(rootDirectory);

        try
        {
            var deliveryDirectory = Path.Combine(rootDirectory, "delivery");
            Directory.CreateDirectory(deliveryDirectory);

            var manifestPath = Path.Combine(deliveryDirectory, "manifest.json");
            await File.WriteAllTextAsync(
                manifestPath,
                JsonSerializer.Serialize(new
                {
                    projectName = "Artifact validation demo",
                    items = new[]
                    {
                        new
                        {
                            itemKey = "item-01",
                            title = "Validated item",
                            imagePath = "images/item-01.png",
                            promptPath = "prompts/item-01.txt",
                        },
                    },
                }),
                CancellationToken.None);

            var service = new LowRiskAutoRepairService([new ArtifactValidationToolAdapter()]);
            var action = OperatorAction.CreateDraft(
                Guid.NewGuid(),
                repairPlanStepOrder: 1,
                toolAdapterId: "artifact-validation",
                displayName: "Validate artifact manifest",
                OperatorRiskLevel.Low,
                dryRunSupported: true,
                inputs: new Dictionary<string, string> { ["manifestPath"] = manifestPath },
                expectedOutputs: ["validation report"],
                sideEffects: ["Reads delivery artifacts and writes a validation report."],
                timeout: TimeSpan.FromSeconds(30),
                cleanupPath: null,
                DateTimeOffset.Parse("2026-06-07T09:05:00Z"));

            var result = await service.RunAsync(
                action,
                dryRun: true,
                startedAt: DateTimeOffset.Parse("2026-06-07T09:06:00Z"),
                CancellationToken.None);

            var expectedReportPath = Path.Combine(rootDirectory, "diagnostics", "manifest.validation.json");
            Assert.True(result.Run.DryRun);
            Assert.True(result.AdapterResult.DryRun);
            Assert.Equal(expectedReportPath, result.AdapterResult.Outputs["validationReportPath"]);
            Assert.False(File.Exists(expectedReportPath));
            Assert.Contains("dry-run", result.AdapterResult.Summary, StringComparison.OrdinalIgnoreCase);
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
