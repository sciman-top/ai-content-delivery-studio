using ContentDeliveryStudio.App.ViewModels;
using ContentDeliveryStudio.Core.ScientificFigures;

namespace ContentDeliveryStudio.Tests;

public sealed class ScientificFigureSpecViewModelTests
{
    [Fact]
    public void Build_ProjectsEveryElementRelationAndExactProvenance()
    {
        var fixture = CreateFixture();

        var viewModel = new ScientificFigureSpecViewModel(fixture.Workflow, []);

        Assert.Equal(fixture.Workflow.Specification.Elements.Count, viewModel.Elements.Count);
        Assert.Equal(fixture.Workflow.Specification.Relations.Count, viewModel.Relations.Count);
        var force = Assert.Single(viewModel.Elements, item => item.ItemId == "element-force");
        Assert.Equal("claim-newton-second-law", force.ClaimId);
        Assert.Equal("block-dynamics", force.SourceBlockId);
        Assert.Equal("Net force causes acceleration for constant mass.", force.ExactQuote);
        Assert.Equal(4, force.PageNumber);
        Assert.Equal("2.1 Dynamics", force.Section);
        var relation = Assert.Single(viewModel.Relations);
        Assert.Equal("scientific_convention:causal-arrow", relation.ConventionId);
        Assert.Equal("A directed arrow represents the stated causal relation.", relation.ConventionStatement);
        Assert.Equal(1, viewModel.UnderstandingVersion);
        Assert.Equal(1, viewModel.SpecificationVersion);
        Assert.True(viewModel.IsDomainEligible);
        Assert.True(viewModel.CanApproveGateOne);
    }

    [Fact]
    public void ProposalDiffs_RequireExplicitDecisionAndAcceptedDiffBlocksCurrentVersionApproval()
    {
        var fixture = CreateFixture();
        var proposal = new ScientificFigureSpecProposalDiff(
            Guid.NewGuid(),
            ScientificFigureSpecTargetKind.Element,
            "element-force",
            ScientificFigureSpecField.ScientificMeaning,
            "Net force acting on the object.",
            "Net external force acting on the object.",
            "Clarify the system boundary.");
        var viewModel = new ScientificFigureSpecViewModel(fixture.Workflow, [proposal]);

        Assert.False(viewModel.CanApproveGateOne);
        Assert.Equal(ScientificProposalDecision.Pending, viewModel.Proposals[0].Decision);

        viewModel.SelectedProposal = viewModel.Proposals[0];
        viewModel.AcceptProposalCommand.Execute(null);

        Assert.Equal(ScientificProposalDecision.Accepted, viewModel.Proposals[0].Decision);
        Assert.False(viewModel.CanApproveGateOne);
        Assert.Contains("accepted-proposal-requires-spec-revision", viewModel.GateOneBlockingReasons);

        viewModel.RejectProposalCommand.Execute(null);

        Assert.Equal(ScientificProposalDecision.Rejected, viewModel.Proposals[0].Decision);
        Assert.True(viewModel.CanApproveGateOne);
    }

    [Fact]
    public void ProposalDiffs_RejectUnknownTargetsAndStaleCurrentValues()
    {
        var fixture = CreateFixture();
        var unknownTarget = new ScientificFigureSpecProposalDiff(
            Guid.NewGuid(),
            ScientificFigureSpecTargetKind.Element,
            "element-missing",
            ScientificFigureSpecField.ScientificMeaning,
            "missing",
            "replacement",
            "Invalid target probe.");
        var staleValue = new ScientificFigureSpecProposalDiff(
            Guid.NewGuid(),
            ScientificFigureSpecTargetKind.Element,
            "element-force",
            ScientificFigureSpecField.ScientificMeaning,
            "Stale scientific meaning.",
            "replacement",
            "Stale diff probe.");

        Assert.Throws<ArgumentException>(() =>
            new ScientificFigureSpecViewModel(fixture.Workflow, [unknownTarget]));
        Assert.Throws<ArgumentException>(() =>
            new ScientificFigureSpecViewModel(fixture.Workflow, [staleValue]));
    }

    [Fact]
    public void ApproveGateOne_FreezesExactUnderstandingAndSpecificationVersions()
    {
        var fixture = CreateFixture();
        var reviewedAt = new DateTimeOffset(2026, 7, 27, 1, 30, 0, TimeSpan.Zero);
        var viewModel = new ScientificFigureSpecViewModel(fixture.Workflow, [], () => reviewedAt)
        {
            GateOneReviewer = "human-reviewer",
            GateOneNotes = "All scientific elements, relations, and evidence were reviewed.",
        };

        viewModel.ApproveGateOneCommand.Execute(null);

        Assert.True(viewModel.IsGateOneApproved);
        Assert.False(viewModel.CanApproveGateOne);
        Assert.Equal(1, viewModel.FrozenUnderstandingVersion);
        Assert.Equal(1, viewModel.FrozenSpecificationVersion);
        Assert.Equal("human-reviewer", viewModel.GateOneReviewer);
        Assert.Equal(reviewedAt, viewModel.GateOneReviewedAt);
    }

    [Fact]
    public void BlockedSpecification_ExposesDomainReasonsAndDisablesGateOne()
    {
        var understanding = ScientificFigureTestFixture.ReadyUnderstanding();
        var claim = Assert.Single(understanding.Claims);
        var evidence = Assert.Single(claim.SupportingEvidence);
        var unresolved = ScientificFigureIssue.Create(
            "issue-boundary",
            ScientificFigureIssueKind.Uncertainty,
            "The system boundary remains uncertain.",
            ScientificFigureIssueStatus.Unresolved);
        var specification = ScientificFigureTestFixture.CreateSpec(
            understanding,
            [ScientificFigureTestFixture.RequiredElement(claim, evidence)],
            [],
            [unresolved]);

        var viewModel = new ScientificFigureSpecViewModel(
            ScientificFigureWorkflow.Create(specification),
            []);

        Assert.False(viewModel.IsDomainEligible);
        Assert.False(viewModel.CanApproveGateOne);
        Assert.Contains("unresolved-uncertainty:issue-boundary", viewModel.GateOneBlockingReasons);
    }

    [Fact]
    public void FigureSpecView_UsesStructuredControlsWithoutRawJsonEditor()
    {
        var xaml = ReadRepoFile("ScientificFigureSpecWorkspaceView.xaml");

        Assert.Contains("AutomationProperties.AutomationId=\"ScientificFigureElementList\"", xaml);
        Assert.Contains("AutomationProperties.AutomationId=\"ScientificFigureRelationList\"", xaml);
        Assert.Contains("AutomationProperties.AutomationId=\"ScientificFigureProposalDiffs\"", xaml);
        Assert.Contains("ScientificFigureSpec.AcceptProposalCommand", xaml);
        Assert.Contains("ScientificFigureSpec.RejectProposalCommand", xaml);
        Assert.Contains("ScientificFigureSpec.ApproveGateOneCommand", xaml);
        Assert.DoesNotContain("JSON", xaml, StringComparison.OrdinalIgnoreCase);
    }

    private static FigureSpecFixture CreateFixture()
    {
        var understanding = ScientificFigureTestFixture.ReadyUnderstanding();
        var claim = Assert.Single(understanding.Claims);
        var evidence = Assert.Single(claim.SupportingEvidence);
        var force = ScientificFigureTestFixture.RequiredElement(claim, evidence);
        var acceleration = FigureElementSpec.Create(
            "element-acceleration",
            "Acceleration of the object.",
            FigureElementKind.Entity,
            "Acceleration",
            "deterministic-node",
            FigureContentRequirement.Required,
            isCritical: true,
            ScientificFigureProvenance.FromEvidence(claim, evidence));
        var convention = ScientificFigureProvenance.FromConvention(
            "scientific_convention:causal-arrow",
            "A directed arrow represents the stated causal relation.");
        var relation = FigureRelationSpec.Create(
            "relation-force-acceleration",
            force.ElementId,
            acceleration.ElementId,
            FigureRelationKind.Causes,
            FigureRelationDirection.Directed,
            "causes",
            "Net force causes acceleration.",
            "Arrow points from force to acceleration.",
            FigureContentRequirement.Required,
            isCritical: true,
            convention);
        var specification = ScientificFigureTestFixture.CreateSpec(
            understanding,
            [force, acceleration],
            [relation],
            []);
        return new FigureSpecFixture(ScientificFigureWorkflow.Create(specification));
    }

    private static string ReadRepoFile(string fileName)
    {
        return File.ReadAllText(Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory, "..", "..", "..", "..", "..",
            "src", "ContentDeliveryStudio.App", "Views", fileName)));
    }

    private sealed record FigureSpecFixture(ScientificFigureWorkflow Workflow);
}
