using ContentDeliveryStudio.Core.Projects;

namespace ContentDeliveryStudio.Tests;

public sealed class FinalApprovalWorkflowTests
{
    [Fact]
    public void Decide_ApprovesCleanPassingReview()
    {
        var review = CreateReview(ReviewDecision.Pass);
        var decidedAt = DateTimeOffset.Parse("2026-06-01T12:00:00Z");

        var decision = FinalApprovalWorkflow.Decide(
            new FinalApprovalRequest(review, Approve: true, "Teacher", "Looks ready."),
            decidedAt);

        Assert.True(decision.HumanApproved);
        Assert.Equal("Teacher", decision.Reviewer);
        Assert.Equal("Looks ready.", decision.Notes);
        Assert.Equal(decidedAt, decision.DecidedAt);
        Assert.True(decision.ReviewResult.HumanApproved);
        Assert.Equal(review.CandidateImageId, decision.ReviewResult.CandidateImageId);
        Assert.Equal("Teacher", decision.ReviewResult.FinalReviewer);
        Assert.Equal("Looks ready.", decision.ReviewResult.FinalApprovalNotes);
        Assert.Equal(decidedAt, decision.ReviewResult.FinalApprovalDecidedAt);
    }

    [Fact]
    public void Decide_RejectsFailedReviewWithNotes()
    {
        var review = CreateReview(ReviewDecision.Fail);

        var decision = FinalApprovalWorkflow.Decide(
            new FinalApprovalRequest(review, Approve: false, "Teacher", "Prompt mismatch."),
            DateTimeOffset.UtcNow);

        Assert.False(decision.HumanApproved);
        Assert.False(decision.ReviewResult.HumanApproved);
        Assert.Equal("Prompt mismatch.", decision.Notes);
        Assert.Equal("Teacher", decision.ReviewResult.FinalReviewer);
        Assert.Equal("Prompt mismatch.", decision.ReviewResult.FinalApprovalNotes);
        Assert.NotNull(decision.ReviewResult.FinalApprovalDecidedAt);
    }

    [Fact]
    public void Decide_BlocksApprovalForFailedOrRepairNeededReview()
    {
        var failed = CreateReview(ReviewDecision.Fail);
        var repairNeeded = CreateReview(ReviewDecision.Pass) with { SuggestedFix = "Improve composition." };

        Assert.Throws<InvalidOperationException>(() =>
            FinalApprovalWorkflow.Decide(
                new FinalApprovalRequest(failed, Approve: true, "Teacher", ""),
                DateTimeOffset.UtcNow));
        Assert.Throws<InvalidOperationException>(() =>
            FinalApprovalWorkflow.Decide(
                new FinalApprovalRequest(repairNeeded, Approve: true, "Teacher", ""),
                DateTimeOffset.UtcNow));
    }

    [Fact]
    public void Decide_RequiresReviewerAndRejectionNotes()
    {
        var review = CreateReview(ReviewDecision.Pass);

        Assert.Throws<ArgumentException>(() =>
            FinalApprovalWorkflow.Decide(
                new FinalApprovalRequest(review, Approve: true, " ", ""),
                DateTimeOffset.UtcNow));
        Assert.Throws<InvalidOperationException>(() =>
            FinalApprovalWorkflow.Decide(
                new FinalApprovalRequest(review, Approve: false, "Teacher", " "),
                DateTimeOffset.UtcNow));
    }

    private static StructuredReviewOutput CreateReview(ReviewDecision decision)
    {
        return new StructuredReviewOutput(
            Guid.NewGuid(),
            decision,
            [new StructuredReviewScore("match", "Match prompt.", 3, decision is ReviewDecision.Pass ? 5 : 1)],
            [],
            "Review comments.",
            null);
    }
}
