using ImageSeriesStudio.Core.Providers;

namespace ImageSeriesStudio.Core.Projects;

public sealed record StructuredReviewOutput(
    Guid CandidateImageId,
    ReviewDecision Decision,
    IReadOnlyList<StructuredReviewScore> Scores,
    IReadOnlyList<string> HardFailures,
    string Comments,
    string? SuggestedFix)
{
    public bool HasHardFailures => HardFailures.Count > 0;

    public bool NeedsRepair =>
        Decision is ReviewDecision.Fail
        || HasHardFailures
        || Scores.Any(score => score.Score > 0 && score.Score < 3)
        || !string.IsNullOrWhiteSpace(SuggestedFix);

    public ReviewResult ToReviewResult(DateTimeOffset createdAt, bool humanApproved = false)
    {
        return new ReviewResult(
            Guid.NewGuid(),
            CandidateImageId,
            Decision,
            Scores.ToDictionary(score => score.Name, score => score.Score),
            HardFailures,
            Comments,
            SuggestedFix,
            humanApproved,
            createdAt);
    }

    public static StructuredReviewOutput FromProviderResult(
        VisionReviewResult result,
        ReviewRubric rubric)
    {
        var scoreRows = rubric.Dimensions
            .Select(dimension => new StructuredReviewScore(
                dimension.Name,
                dimension.Requirement,
                dimension.Weight,
                result.Scores.TryGetValue(dimension.Name, out var score) ? score : 0))
            .ToArray();

        return new StructuredReviewOutput(
            result.CandidateImageId,
            result.Decision,
            scoreRows,
            result.HardFailures,
            result.Comments,
            result.SuggestedFix);
    }
}

public sealed record StructuredReviewScore(
    string Name,
    string Requirement,
    int Weight,
    int Score);
