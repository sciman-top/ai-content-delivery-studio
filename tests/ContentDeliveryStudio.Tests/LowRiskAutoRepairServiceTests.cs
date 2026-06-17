using ContentDeliveryStudio.Application.ToolAdapters;
using ContentDeliveryStudio.Core.Operators;

namespace ContentDeliveryStudio.Tests;

public sealed class LowRiskAutoRepairServiceTests
{
    [Fact]
    public async Task LowRiskAutoRepairService_RunsReadyLowRiskActionThroughAdapterDryRun()
    {
        var descriptor = ToolAdapterDescriptor.Create(
            "artifact-validator",
            "Artifact Validator",
            ToolAdapterKind.LocalLibrary,
            OperatorRiskLevel.Low,
            dryRunSupported: true,
            inputNames: ["manifestPath"],
            outputNames: ["validationReportPath"],
            sideEffects: ["Reads manifest and writes validation report."],
            defaultTimeout: TimeSpan.FromSeconds(30),
            cleanupPath: null);
        var adapter = new FakeToolAdapter(descriptor);
        var service = new LowRiskAutoRepairService([adapter]);
        var action = OperatorAction.CreateDraft(
            Guid.NewGuid(),
            repairPlanStepOrder: 1,
            toolAdapterId: "artifact-validator",
            displayName: "Validate artifact manifest",
            OperatorRiskLevel.Low,
            dryRunSupported: true,
            inputs: new Dictionary<string, string> { ["manifestPath"] = "delivery/manifest.json" },
            expectedOutputs: ["validation report"],
            sideEffects: ["Reads manifest and writes validation report."],
            timeout: TimeSpan.FromSeconds(30),
            cleanupPath: null,
            DateTimeOffset.Parse("2026-06-03T18:30:00Z"));

        var result = await service.RunAsync(
            action,
            dryRun: true,
            startedAt: DateTimeOffset.Parse("2026-06-03T18:31:00Z"),
            CancellationToken.None);

        Assert.Equal(OperatorRunStatus.Succeeded, result.Run.Status);
        Assert.True(result.Run.DryRun);
        Assert.Equal("artifact-validator", result.Run.ToolAdapterId);
        Assert.Equal("delivery/manifest.validation.json", result.AdapterResult.Outputs["validationReportPath"]);
        Assert.Equal(1, adapter.RunCount);
    }

    [Fact]
    public async Task LowRiskAutoRepairService_RejectsMediumRiskActionsEvenWhenApproved()
    {
        var descriptor = ToolAdapterDescriptor.Create(
            "deterministic-text-composition",
            "Deterministic Text Composition",
            ToolAdapterKind.LocalLibrary,
            OperatorRiskLevel.Medium,
            dryRunSupported: true,
            inputNames: ["backgroundPath", "labelSpecPath"],
            outputNames: ["composedImagePath"],
            sideEffects: ["Writes composed image."],
            defaultTimeout: TimeSpan.FromMinutes(2),
            cleanupPath: "workspace/outputs/.composition-tmp");
        var action = OperatorAction.CreateDraft(
            Guid.NewGuid(),
            repairPlanStepOrder: 1,
            toolAdapterId: "deterministic-text-composition",
            displayName: "Compose deterministic labels",
            OperatorRiskLevel.Medium,
            dryRunSupported: true,
            inputs: new Dictionary<string, string>
            {
                ["backgroundPath"] = "background.png",
                ["labelSpecPath"] = "labels.json",
            },
            expectedOutputs: ["composed image"],
            sideEffects: ["Writes composed image."],
            timeout: TimeSpan.FromMinutes(2),
            cleanupPath: "workspace/outputs/.composition-tmp",
            DateTimeOffset.Parse("2026-06-03T18:30:00Z"))
            .Approve(
                "human-reviewer",
                "Approved, but not eligible for low-risk auto path.",
                DateTimeOffset.Parse("2026-06-03T18:31:00Z"));
        var service = new LowRiskAutoRepairService([new FakeToolAdapter(descriptor)]);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.RunAsync(
                action,
                dryRun: true,
                startedAt: DateTimeOffset.Parse("2026-06-03T18:32:00Z"),
                CancellationToken.None));
    }

    private sealed class FakeToolAdapter : IToolAdapter
    {
        public FakeToolAdapter(ToolAdapterDescriptor descriptor)
        {
            Descriptor = descriptor;
        }

        public ToolAdapterDescriptor Descriptor { get; }

        public int RunCount { get; private set; }

        public Task<ToolAdapterRunResult> RunAsync(
            ToolAdapterRunRequest request,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            RunCount++;

            return Task.FromResult(ToolAdapterRunResult.Create(
                Descriptor.Id,
                request.DryRun,
                new Dictionary<string, string> { ["validationReportPath"] = "delivery/manifest.validation.json" },
                [],
                "Fake validation succeeded."));
        }
    }
}
