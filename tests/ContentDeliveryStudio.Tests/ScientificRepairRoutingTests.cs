using ContentDeliveryStudio.Application.ScientificFigures;
using ContentDeliveryStudio.Core.ScientificFigures;

namespace ContentDeliveryStudio.Tests;

public sealed class ScientificRepairRoutingTests
{
    [Theory]
    [InlineData(ScientificRepairLayer.Extraction, ScientificRepairExecutionMode.HumanRequired)]
    [InlineData(ScientificRepairLayer.ScientificUnderstanding, ScientificRepairExecutionMode.HumanRequired)]
    [InlineData(ScientificRepairLayer.FigureSpecification, ScientificRepairExecutionMode.HumanRequired)]
    [InlineData(ScientificRepairLayer.SvgRenderer, ScientificRepairExecutionMode.HumanRequired)]
    [InlineData(ScientificRepairLayer.LayoutStyle, ScientificRepairExecutionMode.Automatic)]
    [InlineData(ScientificRepairLayer.NonEvidentiaryAsset, ScientificRepairExecutionMode.Automatic)]
    [InlineData(ScientificRepairLayer.Exporter, ScientificRepairExecutionMode.HumanRequired)]
    public void RouteFinding_OnlyPresentationAndAssetLayersAreAutomatic(
        ScientificRepairLayer layer,
        ScientificRepairExecutionMode expectedMode)
    {
        var action = new ScientificRepairApplicationService().RouteFinding(
            "finding",
            "item",
            "evidence",
            layer);

        Assert.Equal(layer, action.Layer);
        Assert.Equal(expectedMode, action.ExecutionMode);
    }

    [Fact]
    public void CreatePlan_RoutesContractAndProviderFindingsToResponsibleLayers()
    {
        var contract = ScientificContractReviewReport.Create(
            advisoryScore: 1,
            [new ScientificContractFinding(
                "formula-drift",
                ScientificContractInvariant.ExactScientificContent,
                "element-formula",
                "Formula changed.",
                ScientificContractRepairLayer.FigureSpecification)]);
        var decision = new ScientificMachineReviewDecision(
            [
                new ScientificReviewBlocker(
                    ScientificReviewLayer.Visual,
                    "crowded-layout",
                    "element-force",
                    "Labels overlap.",
                    ScientificProviderFindingKind.VisualDefect),
                new ScientificReviewBlocker(
                    ScientificReviewLayer.Semantic,
                    "meaning-mismatch",
                    "claim-newton-second-law",
                    "Meaning differs.",
                    ScientificProviderFindingKind.ScientificMismatch),
            ]);

        var plan = new ScientificRepairApplicationService().CreatePlan(
            contract,
            decision);

        Assert.Contains(plan.Actions, item =>
            item.Layer == ScientificRepairLayer.FigureSpecification
            && item.ExecutionMode == ScientificRepairExecutionMode.HumanRequired);
        Assert.Contains(plan.Actions, item =>
            item.Layer == ScientificRepairLayer.LayoutStyle
            && item.ExecutionMode == ScientificRepairExecutionMode.Automatic);
        Assert.Contains(plan.Actions, item =>
            item.Layer == ScientificRepairLayer.ScientificUnderstanding
            && item.ExecutionMode == ScientificRepairExecutionMode.HumanRequired);
    }

    [Fact]
    public void ApplyScientificRevision_InvalidatesGateOneAndAllDownstreamArtifacts()
    {
        var fixture = ScientificContractReviewFixture.Create();
        var workflow = ScientificFigureWorkflow.Create(fixture.Specification)
            .ApproveGate1("reviewer", "Approved.", DateTimeOffset.UtcNow)
            .RecordDownstreamApproval(
                ScientificDownstreamApprovalStage.RenderPlan,
                "renderer",
                DateTimeOffset.UtcNow)
            .RecordDownstreamApproval(
                ScientificDownstreamApprovalStage.ScientificReview,
                "scientist",
                DateTimeOffset.UtcNow);

        var revised = new ScientificRepairApplicationService().ApplyScientificRevision(
            workflow,
            fixture.Understanding,
            "Revised scientific message.",
            fixture.Specification.Elements,
            fixture.Specification.Relations,
            fixture.Specification.Issues);

        Assert.Equal(fixture.Specification.Version + 1, revised.Specification.Version);
        Assert.Equal(ScientificFigureWorkflowState.FigureSpecDraft, revised.State);
        Assert.Null(revised.Gate1Approval);
        Assert.Empty(revised.DownstreamApprovals);
    }
}
