using ImageSeriesStudio.Core.Projects;

namespace ImageSeriesStudio.Tests;

public sealed class BatchRequeuePlanTests
{
    [Fact]
    public void Create_IgnoresCandidatesThatDoNotNeedRepair()
    {
        var clean = CreateCandidate("Clean", ReviewDecision.Pass, [], null, ("match", 5));

        var plan = BatchRequeuePlan.Create([clean]);

        Assert.Empty(plan.Groups);
        Assert.Equal(0, plan.CandidateCount);
    }

    [Fact]
    public void Create_GroupsCandidatesByPrimaryFailureReason()
    {
        var hardFailure = CreateCandidate("Hard", ReviewDecision.Fail, ["unreadable-text"], null, ("match", 1));
        var failed = CreateCandidate("Failed", ReviewDecision.Fail, [], null, ("match", 1));
        var lowScore = CreateCandidate("Low", ReviewDecision.Pass, [], null, ("match", 2));
        var suggestedFix = CreateCandidate("Fix", ReviewDecision.Pass, [], "Improve composition.", ("match", 5));

        var plan = BatchRequeuePlan.Create([suggestedFix, lowScore, failed, hardFailure]);

        Assert.Equal(4, plan.CandidateCount);
        Assert.Contains(plan.Groups, group => group.Reason is RequeueReason.HardFailure);
        Assert.Contains(plan.Groups, group => group.Reason is RequeueReason.FailedReview);
        Assert.Contains(plan.Groups, group => group.Reason is RequeueReason.LowScore);
        Assert.Contains(plan.Groups, group => group.Reason is RequeueReason.SuggestedFix);
    }

    [Fact]
    public void Create_CarriesPromptVersionAndSuggestedPromptText()
    {
        var candidate = CreateCandidate("Fix", ReviewDecision.Pass, [], "Improve composition.", ("match", 5));

        var row = Assert.Single(Assert.Single(BatchRequeuePlan.Create([candidate]).Groups).Candidates);

        Assert.Equal(candidate.SeriesItemId, row.SeriesItemId);
        Assert.Equal(candidate.CandidateImageId, row.CandidateImageId);
        Assert.Equal(candidate.PromptVersionId, row.PromptVersionId);
        Assert.Contains("Revision notes:", row.SuggestedPromptText);
        Assert.Equal(RepairSeverity.Minor, row.Severity);
    }

    private static BatchRequeueCandidate CreateCandidate(
        string title,
        ReviewDecision decision,
        IReadOnlyList<string> hardFailures,
        string? suggestedFix,
        params (string Name, int Score)[] scores)
    {
        var review = new StructuredReviewOutput(
            Guid.NewGuid(),
            decision,
            scores
                .Select(score => new StructuredReviewScore(score.Name, $"{score.Name} requirement", 1, score.Score))
                .ToArray(),
            hardFailures,
            $"{title} comments",
            suggestedFix);

        return new BatchRequeueCandidate(
            Guid.NewGuid(),
            review.CandidateImageId,
            Guid.NewGuid(),
            title,
            review,
            PromptRepairSuggestion.FromReview($"{title} prompt.", review));
    }
}
