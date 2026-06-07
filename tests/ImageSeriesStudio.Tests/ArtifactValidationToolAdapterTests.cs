using System.Text.Json;
using ImageSeriesStudio.Application.Diagnostics;
using ImageSeriesStudio.Application.ToolAdapters;
using ImageSeriesStudio.Core.Operators;
using ImageSeriesStudio.Core.Projects;
using ImageSeriesStudio.Infrastructure.Delivery;
using ImageSeriesStudio.Infrastructure.Diagnostics;
using ImageSeriesStudio.Infrastructure.ToolAdapters;

namespace ImageSeriesStudio.Tests;

public sealed class ArtifactValidationToolAdapterTests
{
    [Fact]
    public async Task ArtifactValidationToolAdapter_WritesValidationReportIntoValidationSubfolder()
    {
        var rootDirectory = Path.Combine(Path.GetTempPath(), "ImageSeriesStudio.Tests", Guid.NewGuid().ToString("N"));
        var sourceDirectory = Path.Combine(rootDirectory, "source");
        var packageDirectory = Path.Combine(rootDirectory, "delivery");
        Directory.CreateDirectory(sourceDirectory);

        try
        {
            var delivery = await CreateDeliveryPackageAsync(sourceDirectory, packageDirectory);
            var descriptor = LocalToolRegistry.CreateBuiltIn().GetRequired("artifact-validation");
            var adapter = new ArtifactValidationToolAdapter();

            var result = await adapter.RunAsync(
                ToolAdapterRunRequest.Create(
                    descriptor,
                    dryRun: true,
                    new Dictionary<string, string> { ["manifestPath"] = delivery.ManifestJsonPath },
                    DateTimeOffset.Parse("2026-06-07T11:00:00Z")),
                CancellationToken.None);

            var reportPath = result.Outputs["validationReportPath"];
            Assert.True(File.Exists(reportPath));
            Assert.Contains($"{Path.DirectorySeparatorChar}validation{Path.DirectorySeparatorChar}", reportPath);
            Assert.Contains("No rollback needed", result.Summary, StringComparison.OrdinalIgnoreCase);

            await using var reportStream = File.OpenRead(reportPath);
            using var report = await JsonDocument.ParseAsync(reportStream, cancellationToken: CancellationToken.None);
            Assert.True(report.RootElement.GetProperty("isValid").GetBoolean());
            Assert.True(report.RootElement.GetProperty("dryRun").GetBoolean());
            Assert.Equal(1, report.RootElement.GetProperty("itemCount").GetInt32());
            Assert.Equal(0, report.RootElement.GetProperty("missingPaths").GetArrayLength());
            Assert.Contains("No rollback needed", report.RootElement.GetProperty("rollbackNote").GetString(), StringComparison.OrdinalIgnoreCase);
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
    public async Task ArtifactValidationToolAdapter_ReportsMissingArtifactsWithoutFailingAuditRun()
    {
        var rootDirectory = Path.Combine(Path.GetTempPath(), "ImageSeriesStudio.Tests", Guid.NewGuid().ToString("N"));
        var packageDirectory = Path.Combine(rootDirectory, "delivery");
        Directory.CreateDirectory(packageDirectory);

        try
        {
            var manifestPath = Path.Combine(packageDirectory, "manifest.json");
            await File.WriteAllTextAsync(
                manifestPath,
                """
                {
                  "projectName": "Missing artifact demo",
                  "items": [
                    {
                      "id": "cover",
                      "title": "Cover",
                      "imagePath": "images/missing.png",
                      "promptPath": "prompts/missing.txt"
                    }
                  ]
                }
                """,
                CancellationToken.None);
            var descriptor = LocalToolRegistry.CreateBuiltIn().GetRequired("artifact-validation");
            var adapter = new ArtifactValidationToolAdapter();

            var result = await adapter.RunAsync(
                ToolAdapterRunRequest.Create(
                    descriptor,
                    dryRun: true,
                    new Dictionary<string, string> { ["manifestPath"] = manifestPath },
                    DateTimeOffset.Parse("2026-06-07T11:02:00Z")),
                CancellationToken.None);

            var reportPath = result.Outputs["validationReportPath"];
            Assert.True(File.Exists(reportPath));
            Assert.Contains("missing artifact path", result.Summary, StringComparison.OrdinalIgnoreCase);
            Assert.Contains(result.Warnings, warning => warning.Contains("Missing artifact path count: 2", StringComparison.OrdinalIgnoreCase));

            await using var reportStream = File.OpenRead(reportPath);
            using var report = await JsonDocument.ParseAsync(reportStream, cancellationToken: CancellationToken.None);
            Assert.False(report.RootElement.GetProperty("isValid").GetBoolean());
            Assert.Equal(2, report.RootElement.GetProperty("missingPaths").GetArrayLength());
            Assert.Contains("No rollback needed", report.RootElement.GetProperty("rollbackNote").GetString(), StringComparison.OrdinalIgnoreCase);
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
    public async Task ArtifactValidationToolAdapter_RunsThroughLowRiskAutoRepairService_WithAuditEvidence()
    {
        var rootDirectory = Path.Combine(Path.GetTempPath(), "ImageSeriesStudio.Tests", Guid.NewGuid().ToString("N"));
        var sourceDirectory = Path.Combine(rootDirectory, "source");
        var packageDirectory = Path.Combine(rootDirectory, "delivery");
        var diagnosticsDirectory = Path.Combine(rootDirectory, "diagnostics");
        Directory.CreateDirectory(sourceDirectory);

        try
        {
            var delivery = await CreateDeliveryPackageAsync(sourceDirectory, packageDirectory);
            var descriptor = LocalToolRegistry.CreateBuiltIn().GetRequired("artifact-validation");
            var action = OperatorAction.CreateDraft(
                Guid.NewGuid(),
                repairPlanStepOrder: 1,
                toolAdapterId: descriptor.Id,
                displayName: descriptor.DisplayName,
                descriptor.RiskLevel,
                descriptor.DryRunSupported,
                inputs: new Dictionary<string, string> { ["manifestPath"] = delivery.ManifestJsonPath },
                expectedOutputs: descriptor.OutputNames,
                sideEffects: descriptor.SideEffects,
                timeout: descriptor.DefaultTimeout,
                cleanupPath: descriptor.CleanupPath,
                DateTimeOffset.Parse("2026-06-07T11:05:00Z"));
            var service = new LowRiskAutoRepairService([new ArtifactValidationToolAdapter()]);

            var repairResult = await service.RunAsync(
                action,
                dryRun: true,
                startedAt: DateTimeOffset.Parse("2026-06-07T11:06:00Z"),
                CancellationToken.None);

            var diagnostics = await new DiagnosticsPackageWriter().WriteAsync(
                new DiagnosticsExportRequest(
                    diagnosticsDirectory,
                    new DiagnosticsApplicationSnapshot(
                        "AI Content Delivery Studio",
                        "0.1.0-test",
                        "Debug",
                        DateTimeOffset.Parse("2026-06-07T11:07:00Z")),
                    DiagnosticsPackageWriter.CaptureMachineSnapshot(),
                    [],
                    [],
                    [],
                    OperatorRuns:
                    [
                        OperatorAuditSnapshot.FromRun(action, repairResult.Run),
                    ]),
                CancellationToken.None);

            Assert.Equal(OperatorRunStatus.Succeeded, repairResult.Run.Status);
            Assert.True(File.Exists(repairResult.AdapterResult.Outputs["validationReportPath"]));
            Assert.True(File.Exists(diagnostics.JsonPath));
            Assert.True(File.Exists(diagnostics.MarkdownPath));

            var markdown = await File.ReadAllTextAsync(diagnostics.MarkdownPath, CancellationToken.None);
            Assert.Contains("artifact-validation", markdown);
            Assert.Contains("No rollback needed", markdown, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            if (Directory.Exists(rootDirectory))
            {
                Directory.Delete(rootDirectory, recursive: true);
            }
        }
    }

    private static async Task<DeliveryPackageResult> CreateDeliveryPackageAsync(
        string sourceDirectory,
        string packageDirectory)
    {
        var approvedImage = Path.Combine(sourceDirectory, "approved.png");
        var approvedMetadata = Path.Combine(sourceDirectory, "approved.json");
        await File.WriteAllBytesAsync(approvedImage, [1, 2, 3], CancellationToken.None);
        await File.WriteAllTextAsync(approvedMetadata, """{"providerId":"fake-image"}""", CancellationToken.None);

        return await new DeliveryPackageWriter().WriteAsync(
            new DeliveryPackageRequest(
                "Operator validation demo",
                packageDirectory,
                [
                    new DeliveryPackageItem(
                        "cover",
                        "Cover",
                        approvedImage,
                        approvedMetadata,
                        "A validated delivery candidate.",
                        ReviewDecision.Pass,
                        HumanApproved: true,
                        HumanReviewer: "Teacher",
                        HumanApprovalNotes: "Ready for validation.",
                        HumanApprovalDecidedAt: DateTimeOffset.Parse("2026-06-07T10:59:00Z")),
                ]),
            CancellationToken.None);
    }
}
