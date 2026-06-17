namespace ContentDeliveryStudio.Core.Projects;

public sealed record BatchRequeueCandidate(
    Guid SeriesItemId,
    Guid CandidateImageId,
    Guid PromptVersionId,
    string ItemTitle,
    StructuredReviewOutput Review,
    PromptRepairSuggestion RepairSuggestion);

public sealed record BatchRequeuePlan(
    IReadOnlyList<BatchRequeueGroup> Groups)
{
    public int CandidateCount => Groups.Sum(group => group.Candidates.Count);

    public static BatchRequeuePlan Create(IReadOnlyList<BatchRequeueCandidate> candidates)
    {
        var rows = candidates
            .Where(candidate => candidate.Review.NeedsRepair || candidate.RepairSuggestion.HasRepair)
            .Select(candidate => new BatchRequeueRow(
                candidate.SeriesItemId,
                candidate.CandidateImageId,
                candidate.PromptVersionId,
                candidate.ItemTitle,
                SelectPrimaryReason(candidate),
                candidate.RepairSuggestion.Severity,
                candidate.RepairSuggestion.SuggestedPromptText))
            .ToArray();

        return new BatchRequeuePlan(
            rows
                .GroupBy(row => row.PrimaryReason)
                .OrderBy(group => group.Key.ToString(), StringComparer.OrdinalIgnoreCase)
                .Select(group => new BatchRequeueGroup(group.Key, group.ToArray()))
                .ToArray());
    }

    private static RequeueReason SelectPrimaryReason(BatchRequeueCandidate candidate)
    {
        if (candidate.Review.HardFailures.Count > 0)
        {
            return RequeueReason.HardFailure;
        }

        if (candidate.Review.Decision is ReviewDecision.Fail)
        {
            return RequeueReason.FailedReview;
        }

        if (candidate.Review.Scores.Any(score => score.Score > 0 && score.Score < 3))
        {
            return RequeueReason.LowScore;
        }

        return RequeueReason.SuggestedFix;
    }
}

public sealed record BatchRequeueGroup(
    RequeueReason Reason,
    IReadOnlyList<BatchRequeueRow> Candidates);

public sealed record BatchRequeueRow(
    Guid SeriesItemId,
    Guid CandidateImageId,
    Guid PromptVersionId,
    string ItemTitle,
    RequeueReason PrimaryReason,
    RepairSeverity Severity,
    string SuggestedPromptText);

public enum RequeueReason
{
    HardFailure = 0,
    FailedReview = 1,
    LowScore = 2,
    SuggestedFix = 3,
}
