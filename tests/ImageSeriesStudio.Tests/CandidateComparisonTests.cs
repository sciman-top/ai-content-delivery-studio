using ImageSeriesStudio.Core.Projects;

namespace ImageSeriesStudio.Tests;

public sealed class CandidateComparisonTests
{
    [Fact]
    public void Compare_RanksPassingCandidateWithBestWeightedScoreFirst()
    {
        var first = CreateInput("A", ReviewDecision.Pass, hardFailures: [], suggestedFix: null, ("match", 3, 4), ("quality", 1, 5));
        var second = CreateInput("B", ReviewDecision.Pass, hardFailures: [], suggestedFix: null, ("match", 3, 5), ("quality", 1, 5));

        var comparison = CandidateComparison.Compare([first, second]);

        Assert.Equal(second.CandidateImageId, comparison.BestCandidate!.CandidateImageId);
        Assert.Equal(5, comparison.BestCandidate.WeightedScore);
    }

    [Fact]
    public void Compare_PenalizesCandidatesThatNeedRepair()
    {
        var cleanPass = CreateInput("Clean", ReviewDecision.Pass, hardFailures: [], suggestedFix: null, ("match", 1, 4));
        var suggestedFix = CreateInput("Fix", ReviewDecision.Pass, hardFailures: [], "Improve composition.", ("match", 1, 5));
        var failed = CreateInput("Fail", ReviewDecision.Fail, hardFailures: [], suggestedFix: null, ("match", 1, 5));

        var rows = CandidateComparison.Compare([failed, suggestedFix, cleanPass]).Rows;

        Assert.Equal(cleanPass.CandidateImageId, rows[0].CandidateImageId);
        Assert.Equal(suggestedFix.CandidateImageId, rows[1].CandidateImageId);
        Assert.Equal(failed.CandidateImageId, rows[2].CandidateImageId);
    }

    [Fact]
    public void Compare_UsesHardFailureCountBeforeScore()
    {
        var fewerHardFailures = CreateInput("One failure", ReviewDecision.Fail, ["bad-text"], null, ("match", 1, 1));
        var moreHardFailures = CreateInput("Two failures", ReviewDecision.Fail, ["bad-text", "unsafe"], null, ("match", 1, 5));

        var rows = CandidateComparison.Compare([moreHardFailures, fewerHardFailures]).Rows;

        Assert.Equal(fewerHardFailures.CandidateImageId, rows[0].CandidateImageId);
        Assert.Equal(1, rows[0].HardFailureCount);
    }

    [Fact]
    public void Compare_ReturnsNullBestCandidateForEmptyList()
    {
        var comparison = CandidateComparison.Compare([]);

        Assert.Empty(comparison.Rows);
        Assert.Null(comparison.BestCandidate);
    }

    private static CandidateComparisonInput CreateInput(
        string title,
        ReviewDecision decision,
        IReadOnlyList<string> hardFailures,
        string? suggestedFix,
        params (string Name, int Weight, int Score)[] scores)
    {
        var candidateId = Guid.NewGuid();
        return new CandidateComparisonInput(
            candidateId,
            title,
            $"{title}.png",
            new StructuredReviewOutput(
                candidateId,
                decision,
                scores
                    .Select(score => new StructuredReviewScore(score.Name, $"{score.Name} requirement", score.Weight, score.Score))
                    .ToArray(),
                hardFailures,
                $"{title} comments",
                suggestedFix));
    }
}
