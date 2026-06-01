using ImageSeriesStudio.Core.Projects;

namespace ImageSeriesStudio.Tests;

public sealed class PromptRepairSuggestionTests
{
    [Fact]
    public void FromReview_ReturnsNoRepairWhenReviewPassesCleanly()
    {
        var review = new StructuredReviewOutput(
            Guid.NewGuid(),
            ReviewDecision.Pass,
            [new StructuredReviewScore("match", "Match prompt.", 3, 5)],
            [],
            "Looks good.",
            null);

        var suggestion = PromptRepairSuggestion.FromReview("Original prompt.", review);

        Assert.False(suggestion.HasRepair);
        Assert.Equal(RepairSeverity.None, suggestion.Severity);
        Assert.Empty(suggestion.Reasons);
        Assert.Empty(suggestion.Actions);
        Assert.Equal("Original prompt.", suggestion.SuggestedPromptText);
    }

    [Fact]
    public void FromReview_UsesSuggestedFixForMinorRepair()
    {
        var review = new StructuredReviewOutput(
            Guid.NewGuid(),
            ReviewDecision.Pass,
            [new StructuredReviewScore("composition", "Clear composition.", 2, 2)],
            [],
            "Needs a cleaner focal point.",
            "Move the main subject to the center.");

        var suggestion = PromptRepairSuggestion.FromReview("Original prompt.", review);

        Assert.True(suggestion.HasRepair);
        Assert.Equal(RepairSeverity.Minor, suggestion.Severity);
        Assert.Contains(suggestion.Reasons, reason => reason.Contains("composition=2", StringComparison.Ordinal));
        Assert.Contains("Move the main subject to the center.", suggestion.Actions);
        Assert.Contains("Revision notes:", suggestion.SuggestedPromptText);
    }

    [Fact]
    public void FromReview_UsesMajorSeverityForFailedReview()
    {
        var review = new StructuredReviewOutput(
            Guid.NewGuid(),
            ReviewDecision.Fail,
            [new StructuredReviewScore("match", "Match prompt.", 3, 1)],
            [],
            "Does not match the prompt.",
            null);

        var suggestion = PromptRepairSuggestion.FromReview("Original prompt.", review);

        Assert.Equal(RepairSeverity.Major, suggestion.Severity);
        Assert.Contains(suggestion.Reasons, reason => reason.Contains("AI review decision", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(suggestion.Actions, action => action.Contains("Regenerate", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void FromReview_UsesRegenerateSeverityForHardFailures()
    {
        var review = new StructuredReviewOutput(
            Guid.NewGuid(),
            ReviewDecision.Fail,
            [],
            ["unreadable-text"],
            "Text cannot be read.",
            "Reserve a blank area for deterministic text composition.");

        var suggestion = PromptRepairSuggestion.FromReview("Original prompt.", review);

        Assert.Equal(RepairSeverity.Regenerate, suggestion.Severity);
        Assert.Contains(suggestion.Reasons, reason => reason.Contains("unreadable-text", StringComparison.Ordinal));
        Assert.Contains(suggestion.Actions, action => action.Contains("hard failure", StringComparison.OrdinalIgnoreCase));
    }
}
