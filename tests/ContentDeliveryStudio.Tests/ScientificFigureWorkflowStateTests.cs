using ContentDeliveryStudio.Core.ScientificFigures;

namespace ContentDeliveryStudio.Tests;

public sealed class ScientificFigureWorkflowStateTests
{
    [Fact]
    public void ApproveGate1_FreezesReadySpecificationVersion()
    {
        var understanding = ScientificFigureTestFixture.ReadyUnderstanding();
        var spec = ReadySpec(understanding);
        var reviewedAt = DateTimeOffset.Parse("2026-07-26T12:00:00Z");

        var workflow = ScientificFigureWorkflow
            .Create(spec)
            .ApproveGate1("scientific-reviewer", "Claims and specification verified.", reviewedAt);

        Assert.Equal(ScientificFigureWorkflowState.Gate1Approved, workflow.State);
        Assert.Equal(spec.Version, workflow.Gate1Approval!.ApprovedSpecVersion);
        Assert.Equal(understanding.Version, workflow.Gate1Approval.ApprovedUnderstandingVersion);
        Assert.Equal("scientific-reviewer", workflow.Gate1Approval.Reviewer);
        Assert.Equal(reviewedAt, workflow.Gate1Approval.ReviewedAt);
    }

    [Fact]
    public void ApproveGate1_RejectsBlockedUnderstanding()
    {
        var ready = ScientificFigureTestFixture.ReadyUnderstanding();
        var blocked = ScientificDocumentUnderstanding.Create(
            Guid.NewGuid(),
            ReadyExtractionFrom(ready),
            ready.Objective,
            version: 2,
            ready.Terminology,
            ready.Claims,
            ready.Conflicts,
            []);
        var spec = ReadySpec(blocked);

        Assert.Equal(ScientificFigureSpecStatus.Blocked, spec.Status);
        Assert.Throws<InvalidOperationException>(() =>
            ScientificFigureWorkflow
                .Create(spec)
                .ApproveGate1(
                    "scientific-reviewer",
                    "Cannot approve missing coverage.",
                    DateTimeOffset.UtcNow));
    }

    [Fact]
    public void ApproveGate1_RejectsUnresolvedSpecificationIssue()
    {
        var understanding = ScientificFigureTestFixture.ReadyUnderstanding();
        var claim = Assert.Single(understanding.Claims);
        var evidence = Assert.Single(claim.SupportingEvidence);
        var issue = ScientificFigureIssue.Create(
            "issue-formula-uncertain",
            ScientificFigureIssueKind.Uncertainty,
            "Formula notation remains uncertain.",
            ScientificFigureIssueStatus.Unresolved);
        var spec = ScientificFigureTestFixture.CreateSpec(
            understanding,
            [ScientificFigureTestFixture.RequiredElement(claim, evidence)],
            [],
            [issue]);

        Assert.Throws<InvalidOperationException>(() =>
            ScientificFigureWorkflow
                .Create(spec)
                .ApproveGate1("reviewer", "Not ready.", DateTimeOffset.UtcNow));
    }

    [Fact]
    public void ReviseScientificContent_IncrementsVersionAndInvalidatesApprovals()
    {
        var understanding = ScientificFigureTestFixture.ReadyUnderstanding();
        var spec = ReadySpec(understanding);
        var approved = ScientificFigureWorkflow
            .Create(spec)
            .ApproveGate1("reviewer", "Approved.", DateTimeOffset.UtcNow)
            .RecordDownstreamApproval(
                ScientificDownstreamApprovalStage.RenderPlan,
                "render-reviewer",
                DateTimeOffset.UtcNow)
            .RecordDownstreamApproval(
                ScientificDownstreamApprovalStage.ScientificReview,
                "science-reviewer",
                DateTimeOffset.UtcNow);
        var claim = Assert.Single(understanding.Claims);
        var evidence = Assert.Single(claim.SupportingEvidence);

        var revised = approved.ReviseScientificContent(
            understanding,
            "Net force causes acceleration only for the stated constant-mass condition.",
            [ScientificFigureTestFixture.RequiredElement(claim, evidence)],
            [],
            []);

        Assert.Equal(spec.Version + 1, revised.Specification.Version);
        Assert.Equal(ScientificFigureWorkflowState.FigureSpecDraft, revised.State);
        Assert.Null(revised.Gate1Approval);
        Assert.Empty(revised.DownstreamApprovals);
    }

    [Fact]
    public void RecordDownstreamApproval_RequiresCurrentGate1Approval()
    {
        var workflow = ScientificFigureWorkflow.Create(
            ReadySpec(ScientificFigureTestFixture.ReadyUnderstanding()));

        Assert.Throws<InvalidOperationException>(() =>
            workflow.RecordDownstreamApproval(
                ScientificDownstreamApprovalStage.RenderPlan,
                "reviewer",
                DateTimeOffset.UtcNow));
    }

    [Fact]
    public void RecordDownstreamApproval_RejectsDuplicateStage()
    {
        var workflow = ScientificFigureWorkflow
            .Create(ReadySpec(ScientificFigureTestFixture.ReadyUnderstanding()))
            .ApproveGate1("reviewer", "Approved.", DateTimeOffset.UtcNow)
            .RecordDownstreamApproval(
                ScientificDownstreamApprovalStage.RenderPlan,
                "render-reviewer",
                DateTimeOffset.UtcNow);

        Assert.Throws<InvalidOperationException>(() =>
            workflow.RecordDownstreamApproval(
                ScientificDownstreamApprovalStage.RenderPlan,
                "second-reviewer",
                DateTimeOffset.UtcNow));
    }

    [Fact]
    public void RecordDownstreamApproval_RejectsOutOfOrderReview()
    {
        var workflow = ScientificFigureWorkflow
            .Create(ReadySpec(ScientificFigureTestFixture.ReadyUnderstanding()))
            .ApproveGate1("reviewer", "Approved.", DateTimeOffset.UtcNow);

        Assert.Throws<InvalidOperationException>(() =>
            workflow.RecordDownstreamApproval(
                ScientificDownstreamApprovalStage.ScientificReview,
                "science-reviewer",
                DateTimeOffset.UtcNow));
    }

    private static ScientificFigureSpec ReadySpec(
        ScientificDocumentUnderstanding understanding)
    {
        var claim = Assert.Single(understanding.Claims);
        var evidence = Assert.Single(claim.SupportingEvidence);
        return ScientificFigureTestFixture.CreateSpec(
            understanding,
            [ScientificFigureTestFixture.RequiredElement(claim, evidence)],
            [],
            []);
    }

    private static ScientificDocumentExtraction ReadyExtractionFrom(
        ScientificDocumentUnderstanding understanding)
    {
        var claim = Assert.Single(understanding.Claims);
        var evidence = Assert.Single(claim.SupportingEvidence);
        var block = ScientificSourceBlock.Create(
            evidence.SourceBlockId,
            ScientificSourceBlockKind.Paragraph,
            evidence.Location,
            evidence.QuotedText,
            isRequired: true,
            ScientificRecoveryStatus.NotRequired);
        return ScientificDocumentExtraction.Create(
            understanding.SourceAssetId,
            understanding.SourceSha256,
            ScientificExtractorIdentity.Create("fixture-extractor", "1.0"),
            ScientificExtractionQuality.Create(
                isScanned: false,
                ocrApplied: false,
                ScientificReadingOrderStatus.Reliable,
                ScientificRequiredContentStatus.Complete),
            [block],
            []);
    }
}
