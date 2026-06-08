using ImageSeriesStudio.Application.ToolAdapters;
using ImageSeriesStudio.Core.Operators;

namespace ImageSeriesStudio.Tests;

public sealed class LocalToolRegistryTests
{
    [Fact]
    public void LocalToolRegistry_CreatesBuiltInDeterministicToolDescriptors()
    {
        var registry = LocalToolRegistry.CreateBuiltIn();

        Assert.Equal(8, registry.Tools.Count);
        AssertTool(
            registry.GetRequired("document-extraction"),
            ToolAdapterKind.LocalLibrary,
            OperatorRiskLevel.Low,
            dryRunSupported: true);
        AssertTool(
            registry.GetRequired("document-conversion"),
            ToolAdapterKind.LocalCli,
            OperatorRiskLevel.Medium,
            dryRunSupported: true);
        AssertTool(
            registry.GetRequired("ocr-extraction"),
            ToolAdapterKind.LocalCli,
            OperatorRiskLevel.Low,
            dryRunSupported: true);
        AssertTool(
            registry.GetRequired("imagemagick-processing"),
            ToolAdapterKind.LocalCli,
            OperatorRiskLevel.Medium,
            dryRunSupported: true);
        AssertTool(
            registry.GetRequired("ffmpeg-processing"),
            ToolAdapterKind.LocalCli,
            OperatorRiskLevel.Medium,
            dryRunSupported: true);
        AssertTool(
            registry.GetRequired("deterministic-text-composition"),
            ToolAdapterKind.LocalLibrary,
            OperatorRiskLevel.Medium,
            dryRunSupported: true);
        AssertTool(
            registry.GetRequired("artifact-validation"),
            ToolAdapterKind.LocalLibrary,
            OperatorRiskLevel.Low,
            dryRunSupported: true);
        AssertTool(
            registry.GetRequired("openai-launch-preflight"),
            ToolAdapterKind.LocalLibrary,
            OperatorRiskLevel.Low,
            dryRunSupported: true);
    }

    [Fact]
    public void LocalToolRegistry_RejectsDuplicateToolIds()
    {
        var descriptor = ToolAdapterDescriptor.Create(
            "artifact-validation",
            "Artifact Validation",
            ToolAdapterKind.LocalLibrary,
            OperatorRiskLevel.Low,
            dryRunSupported: true,
            inputNames: ["manifestPath"],
            outputNames: ["validationReport"],
            sideEffects: ["Reads artifacts and writes validation report."],
            defaultTimeout: TimeSpan.FromSeconds(30),
            cleanupPath: null);

        Assert.Throws<ArgumentException>(() => LocalToolRegistry.Create([descriptor, descriptor]));
    }

    private static void AssertTool(
        ToolAdapterDescriptor descriptor,
        ToolAdapterKind kind,
        OperatorRiskLevel riskLevel,
        bool dryRunSupported)
    {
        Assert.Equal(kind, descriptor.Kind);
        Assert.Equal(riskLevel, descriptor.RiskLevel);
        Assert.Equal(dryRunSupported, descriptor.DryRunSupported);
        Assert.NotEmpty(descriptor.InputNames);
        Assert.NotEmpty(descriptor.OutputNames);
        Assert.NotEmpty(descriptor.SideEffects);
        Assert.True(descriptor.DefaultTimeout > TimeSpan.Zero);
    }
}
