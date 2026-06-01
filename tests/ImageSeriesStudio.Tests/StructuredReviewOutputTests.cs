using ImageSeriesStudio.Core.Projects;
using ImageSeriesStudio.Core.Providers;

namespace ImageSeriesStudio.Tests;

public sealed class StructuredReviewOutputTests
{
    [Fact]
    public void FromProviderResult_MapsRubricDimensionsAndMissingScores()
    {
        var candidateId = Guid.NewGuid();
        var rubric = new ReviewRubric(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "General",
            [
                new ReviewRubricDimension("match", "Match prompt.", 3),
                new ReviewRubricDimension("composition", "Clear composition.", 2),
            ],
            DateTimeOffset.UtcNow);

        var output = StructuredReviewOutput.FromProviderResult(
            new VisionReviewResult(
                candidateId,
                ReviewDecision.Pass,
                new Dictionary<string, int> { ["match"] = 5 },
                [],
                "Looks good.",
                SuggestedFix: null),
            rubric);

        Assert.Equal(candidateId, output.CandidateImageId);
        Assert.Equal(ReviewDecision.Pass, output.Decision);
        Assert.False(output.NeedsRepair);
        Assert.Equal(2, output.Scores.Count);
        Assert.Equal(5, output.Scores.Single(score => score.Name == "match").Score);
        Assert.Equal(0, output.Scores.Single(score => score.Name == "composition").Score);
        Assert.Equal("Clear composition.", output.Scores.Single(score => score.Name == "composition").Requirement);
    }

    [Fact]
    public void NeedsRepair_IsTrueForFailHardFailuresOrSuggestedFix()
    {
        var failed = new StructuredReviewOutput(
            Guid.NewGuid(),
            ReviewDecision.Fail,
            [],
            [],
            "Failed.",
            null);
        var hardFailure = failed with { Decision = ReviewDecision.Pass, HardFailures = ["bad-text"] };
        var suggestedFix = failed with { Decision = ReviewDecision.Pass, SuggestedFix = "Revise text area." };

        Assert.True(failed.NeedsRepair);
        Assert.True(hardFailure.NeedsRepair);
        Assert.True(suggestedFix.NeedsRepair);
    }

    [Fact]
    public void ToReviewResult_CreatesPersistableReviewResult()
    {
        var candidateId = Guid.NewGuid();
        var createdAt = DateTimeOffset.Parse("2026-06-01T11:00:00Z");
        var output = new StructuredReviewOutput(
            candidateId,
            ReviewDecision.Pass,
            [new StructuredReviewScore("match", "Match prompt.", 3, 5)],
            [],
            "Approved by AI review.",
            null);

        var review = output.ToReviewResult(createdAt, humanApproved: true);

        Assert.Equal(candidateId, review.CandidateImageId);
        Assert.Equal(ReviewDecision.Pass, review.Decision);
        Assert.Equal(5, review.Scores["match"]);
        Assert.True(review.HumanApproved);
        Assert.Equal(createdAt, review.CreatedAt);
    }
}
