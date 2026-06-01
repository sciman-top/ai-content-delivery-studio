using ImageSeriesStudio.Core.Experiments;

namespace ImageSeriesStudio.Core.Projects;

public sealed record CandidateComparisonInput(
    Guid CandidateImageId,
    string Title,
    string AssetPath,
    StructuredReviewOutput Review,
    ParameterGridVariant? ExperimentVariant = null);

public sealed record CandidateComparison(
    IReadOnlyList<CandidateComparisonRow> Rows)
{
    public CandidateComparisonRow? BestCandidate => Rows.FirstOrDefault();

    public static CandidateComparison Compare(IReadOnlyList<CandidateComparisonInput> candidates)
    {
        return new CandidateComparison(
            candidates
                .Select(candidate => new CandidateComparisonRow(
                    candidate.CandidateImageId,
                    candidate.Title,
                    candidate.AssetPath,
                    candidate.Review.Decision,
                    CalculateWeightedScore(candidate.Review),
                    candidate.Review.HardFailures.Count,
                    candidate.Review.NeedsRepair,
                    candidate.ExperimentVariant?.Slug,
                    candidate.ExperimentVariant?.ParameterValues ?? new Dictionary<string, string>(),
                    candidate.ExperimentVariant?.GenerationTaskId,
                    candidate.ExperimentVariant?.Recipe?.Id))
                .OrderBy(row => row.NeedsRepair)
                .ThenByDescending(row => row.Decision is ReviewDecision.Pass)
                .ThenBy(row => row.HardFailureCount)
                .ThenByDescending(row => row.WeightedScore)
                .ThenBy(row => row.Title, StringComparer.OrdinalIgnoreCase)
                .ToArray());
    }

    private static double CalculateWeightedScore(StructuredReviewOutput review)
    {
        var totalWeight = review.Scores.Sum(score => score.Weight);
        if (totalWeight <= 0)
        {
            return 0;
        }

        return review.Scores.Sum(score => score.Score * score.Weight) / (double)totalWeight;
    }
}

public sealed record CandidateComparisonRow(
    Guid CandidateImageId,
    string Title,
    string AssetPath,
    ReviewDecision Decision,
    double WeightedScore,
    int HardFailureCount,
    bool NeedsRepair,
    string? ExperimentSlug,
    IReadOnlyDictionary<string, string> ExperimentParameters,
    Guid? GenerationTaskId,
    Guid? RecipeId);
