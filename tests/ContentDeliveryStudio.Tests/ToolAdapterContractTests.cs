using ContentDeliveryStudio.Application.ToolAdapters;
using ContentDeliveryStudio.Core.Operators;

namespace ContentDeliveryStudio.Tests;

public sealed class ToolAdapterContractTests
{
    [Fact]
    public void ToolAdapterDescriptor_CapturesRiskDryRunIoSideEffectsTimeoutApprovalAndCleanup()
    {
        var descriptor = ToolAdapterDescriptor.Create(
            "deterministic-text-composer",
            "Deterministic Text Composer",
            ToolAdapterKind.LocalLibrary,
            OperatorRiskLevel.Medium,
            dryRunSupported: true,
            inputNames: ["backgroundPath", "labelSpecPath"],
            outputNames: ["composedImagePath", "layoutReportPath"],
            sideEffects: ["Writes composed image and layout report under workspace outputs."],
            defaultTimeout: TimeSpan.FromMinutes(2),
            cleanupPath: "workspace/outputs/.composition-tmp");

        Assert.Equal("deterministic-text-composer", descriptor.Id);
        Assert.Equal("Deterministic Text Composer", descriptor.DisplayName);
        Assert.Equal(ToolAdapterKind.LocalLibrary, descriptor.Kind);
        Assert.Equal(OperatorRiskLevel.Medium, descriptor.RiskLevel);
        Assert.True(descriptor.DryRunSupported);
        Assert.True(descriptor.RequiresApproval);
        Assert.Equal(["backgroundPath", "labelSpecPath"], descriptor.InputNames);
        Assert.Equal(["composedImagePath", "layoutReportPath"], descriptor.OutputNames);
        Assert.Equal(["Writes composed image and layout report under workspace outputs."], descriptor.SideEffects);
        Assert.Equal(TimeSpan.FromMinutes(2), descriptor.DefaultTimeout);
        Assert.Equal("workspace/outputs/.composition-tmp", descriptor.CleanupPath);
    }

    [Fact]
    public void ToolAdapterDescriptor_RejectsUnknownKindMissingIoAndUnsafeTimeout()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            ToolAdapterDescriptor.Create(
                "bad-kind",
                "Bad Kind",
                (ToolAdapterKind)999,
                OperatorRiskLevel.Low,
                dryRunSupported: true,
                inputNames: ["input"],
                outputNames: ["output"],
                sideEffects: ["reads only"],
                defaultTimeout: TimeSpan.FromSeconds(30),
                cleanupPath: null));
        Assert.Throws<ArgumentException>(() =>
            ToolAdapterDescriptor.Create(
                "missing-io",
                "Missing IO",
                ToolAdapterKind.LocalCli,
                OperatorRiskLevel.Low,
                dryRunSupported: true,
                inputNames: [],
                outputNames: ["output"],
                sideEffects: ["reads only"],
                defaultTimeout: TimeSpan.FromSeconds(30),
                cleanupPath: null));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            ToolAdapterDescriptor.Create(
                "bad-timeout",
                "Bad Timeout",
                ToolAdapterKind.LocalCli,
                OperatorRiskLevel.Low,
                dryRunSupported: true,
                inputNames: ["input"],
                outputNames: ["output"],
                sideEffects: ["reads only"],
                defaultTimeout: TimeSpan.Zero,
                cleanupPath: null));
    }

    [Fact]
    public void ToolAdapterDescriptor_AcceptsPlannedAdapterBoundaryKinds()
    {
        var kinds = new[]
        {
            ToolAdapterKind.Sdk,
            ToolAdapterKind.LocalCli,
            ToolAdapterKind.LocalLibrary,
            ToolAdapterKind.BrowserAutomation,
            ToolAdapterKind.WindowsDesktopAutomation,
            ToolAdapterKind.ComputerUse,
        };

        foreach (var kind in kinds)
        {
            var descriptor = ToolAdapterDescriptor.Create(
                $"adapter-{kind}",
                $"{kind} Adapter",
                kind,
                OperatorRiskLevel.Low,
                dryRunSupported: true,
                inputNames: ["input"],
                outputNames: ["output"],
                sideEffects: ["Records adapter boundary metadata only."],
                defaultTimeout: TimeSpan.FromSeconds(30),
                cleanupPath: null);

            Assert.Equal(kind, descriptor.Kind);
        }
    }

    [Fact]
    public void ToolAdapterRunRequest_RejectsDryRunWhenAdapterDoesNotSupportIt()
    {
        var descriptor = ToolAdapterDescriptor.Create(
            "artifact-validator",
            "Artifact Validator",
            ToolAdapterKind.LocalCli,
            OperatorRiskLevel.Low,
            dryRunSupported: false,
            inputNames: ["manifestPath"],
            outputNames: ["validationReport"],
            sideEffects: ["Reads manifest and writes validation report."],
            defaultTimeout: TimeSpan.FromSeconds(30),
            cleanupPath: null);

        Assert.Throws<InvalidOperationException>(() =>
            ToolAdapterRunRequest.Create(
                descriptor,
                dryRun: true,
                inputs: new Dictionary<string, string> { ["manifestPath"] = "delivery/manifest.json" },
                DateTimeOffset.Parse("2026-06-03T17:30:00Z")));
    }
}
