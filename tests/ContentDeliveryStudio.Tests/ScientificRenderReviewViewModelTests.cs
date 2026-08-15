using ContentDeliveryStudio.App.ViewModels;
using ContentDeliveryStudio.Application.ScientificFigures;
using ContentDeliveryStudio.Core.ScientificFigures;
using System.Security.Cryptography;
using System.Text;

namespace ContentDeliveryStudio.Tests;

public sealed class ScientificRenderReviewViewModelTests
{
    [Fact]
    public void SelectingSvgItem_TracesSpecificationClaimAndExactSourceEvidence()
    {
        var fixture = CreateFixture();
        var viewModel = fixture.CreateViewModel();

        viewModel.SelectSvgItem("element-force");

        Assert.Equal("element-force", viewModel.SelectedSvgItem!.SpecificationItemId);
        Assert.Equal("claim-newton-second-law", viewModel.SelectedProvenance!.ClaimId);
        Assert.Equal("block-dynamics", viewModel.SelectedProvenance.SourceBlockId);
        Assert.Equal("Net force causes acceleration for constant mass.", viewModel.SelectedProvenance.ExactQuote);
        Assert.Equal(4, viewModel.SelectedProvenance.PageNumber);
        Assert.Equal("2.1 Dynamics", viewModel.SelectedProvenance.Section);
        Assert.Contains("<svg", viewModel.SvgDocument, StringComparison.Ordinal);
    }

    [Fact]
    public void Findings_RemainSeparatedAndContractFailuresStayHard()
    {
        var fixture = CreateFixture();
        var viewModel = fixture.CreateViewModel();

        Assert.Single(viewModel.ContractFindings);
        Assert.True(viewModel.ContractFindings[0].IsHardFailure);
        Assert.Single(viewModel.SemanticFindings);
        Assert.Single(viewModel.VisualFindings);
        Assert.False(viewModel.CanProceedToGateTwo);
        Assert.Equal(0.99, viewModel.ContractAdvisoryScore);
    }

    [Fact]
    public void AutomaticRepair_OnlyRunsPresentationAuthorizedActionAndRecordsHistory()
    {
        var fixture = CreateFixture();
        ScientificRepairAction? requested = null;
        var viewModel = fixture.CreateViewModel(action => requested = action);
        var scientific = Assert.Single(viewModel.RepairActions, item =>
            item.Layer == ScientificRepairLayer.FigureSpecification);
        var presentation = Assert.Single(viewModel.RepairActions, item =>
            item.Layer == ScientificRepairLayer.LayoutStyle);

        viewModel.SelectedRepairAction = scientific;
        Assert.False(viewModel.RunAutomaticRepairCommand.CanExecute(null));

        viewModel.SelectedRepairAction = presentation;
        Assert.True(viewModel.RunAutomaticRepairCommand.CanExecute(null));
        viewModel.RunAutomaticRepairCommand.Execute(null);

        Assert.Equal(presentation.FindingCode, requested!.FindingCode);
        Assert.Equal(1, viewModel.CompletedAutomaticAttempts);
        var history = Assert.Single(viewModel.RepairHistory);
        Assert.Equal(ScientificRepairExecutionMode.Automatic, history.ExecutionMode);
        Assert.Equal(ScientificRepairLayer.LayoutStyle, history.Layer);
    }

    [Fact]
    public void ExistingAutomaticRepairHistory_RestoresThreeRoundLimit()
    {
        var fixture = CreateFixture();
        var history = Enumerable.Range(1, 3).Select(attempt =>
            new ScientificRepairHistoryEntry(
                attempt,
                "crowded-layout",
                "element-acceleration",
                ScientificRepairLayer.LayoutStyle,
                ScientificRepairExecutionMode.Automatic,
                DateTimeOffset.UtcNow)).ToArray();
        var viewModel = new ScientificRenderReviewViewModel(
            fixture.Understanding,
            fixture.Specification,
            fixture.Plan,
            fixture.Svg,
            fixture.ContractReport,
            fixture.MachineDecision,
            fixture.RepairPlan,
            history,
            _ => { });
        viewModel.SelectedRepairAction = Assert.Single(viewModel.RepairActions, item =>
            item.Layer == ScientificRepairLayer.LayoutStyle);

        Assert.Equal(3, viewModel.CompletedAutomaticAttempts);
        Assert.False(viewModel.RunAutomaticRepairCommand.CanExecute(null));
    }

    [Fact]
    public void RepairHistoryWithAttemptGap_IsRejected()
    {
        var fixture = CreateFixture();
        var history = new ScientificRepairHistoryEntry(
            2,
            "crowded-layout",
            "element-acceleration",
            ScientificRepairLayer.LayoutStyle,
            ScientificRepairExecutionMode.Automatic,
            DateTimeOffset.UtcNow);

        Assert.Throws<ArgumentException>(() => new ScientificRenderReviewViewModel(
            fixture.Understanding,
            fixture.Specification,
            fixture.Plan,
            fixture.Svg,
            fixture.ContractReport,
            fixture.MachineDecision,
            fixture.RepairPlan,
            [history],
            _ => { }));
    }

    [Fact]
    public void AuthorityMismatch_IsRejectedBeforePreview()
    {
        var fixture = CreateFixture();
        var mismatched = fixture.Svg with { SpecificationVersion = 99 };

        Assert.Throws<ArgumentException>(() => new ScientificRenderReviewViewModel(
            fixture.Understanding,
            fixture.Specification,
            fixture.Plan,
            mismatched,
            fixture.ContractReport,
            fixture.MachineDecision,
            fixture.RepairPlan,
            []));
    }

    [Fact]
    public void UnsafeSvgMarkup_IsRejectedEvenWhenHashMatches()
    {
        var fixture = CreateFixture();
        const string malicious = "<svg xmlns=\"http://www.w3.org/2000/svg\"><script>alert(1)</script></svg>";
        var hash = $"sha256:{Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(malicious))).ToLowerInvariant()}";
        var unsafeSvg = fixture.Svg with { Svg = malicious, Sha256 = hash };

        Assert.Throws<ArgumentException>(() => new ScientificRenderReviewViewModel(
            fixture.Understanding,
            fixture.Specification,
            fixture.Plan,
            unsafeSvg,
            fixture.ContractReport,
            fixture.MachineDecision,
            fixture.RepairPlan,
            []));
    }

    [Fact]
    public void ZoomCommands_StayWithinBoundedPreviewRange()
    {
        var viewModel = CreateFixture().CreateViewModel();

        for (var index = 0; index < 20; index++)
        {
            viewModel.ZoomInCommand.Execute(null);
        }
        Assert.Equal(2, viewModel.ZoomScale);

        for (var index = 0; index < 30; index++)
        {
            viewModel.ZoomOutCommand.Execute(null);
        }
        Assert.Equal(0.5, viewModel.ZoomScale);
    }

    private static RenderReviewFixture CreateFixture()
    {
        var source = ScientificContractReviewFixture.Create(advisoryScore: 0.99);
        var contract = ScientificContractReviewReport.Create(
            0.99,
            [new ScientificContractFinding(
                "formula-drift",
                ScientificContractInvariant.ExactScientificContent,
                "element-formula",
                "The rendered formula differs from the approved exact content.",
                ScientificContractRepairLayer.FigureSpecification)]);
        var decision = new ScientificMachineReviewDecision(
            [
                new ScientificReviewBlocker(
                    ScientificReviewLayer.Semantic,
                    "meaning-mismatch",
                    "element-force",
                    "The rendered meaning is inconsistent with the approved claim.",
                    ScientificProviderFindingKind.ScientificMismatch),
                new ScientificReviewBlocker(
                    ScientificReviewLayer.Visual,
                    "crowded-layout",
                    "element-acceleration",
                    "The label overlaps the relation arrow.",
                    ScientificProviderFindingKind.VisualDefect),
            ]);
        var repairPlan = new ScientificRepairApplicationService().CreatePlan(contract, decision);
        return new RenderReviewFixture(
            source.Understanding,
            source.Specification,
            source.Plan,
            source.Svg,
            contract,
            decision,
            repairPlan);
    }

    private sealed record RenderReviewFixture(
        ScientificDocumentUnderstanding Understanding,
        ScientificFigureSpec Specification,
        SvgRenderPlan Plan,
        ScientificSvgArtifact Svg,
        ScientificContractReviewReport ContractReport,
        ScientificMachineReviewDecision MachineDecision,
        ScientificRepairPlan RepairPlan)
    {
        public ScientificRenderReviewViewModel CreateViewModel(
            Action<ScientificRepairAction>? automaticRepairRequested = null)
        {
            return new ScientificRenderReviewViewModel(
                Understanding,
                Specification,
                Plan,
                Svg,
                ContractReport,
                MachineDecision,
                RepairPlan,
                [],
                automaticRepairRequested);
        }
    }
}
