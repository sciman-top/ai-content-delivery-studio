using ContentDeliveryStudio.Core.Projects;

namespace ContentDeliveryStudio.Tests;

public sealed class PromptDiffTests
{
    [Fact]
    public void Compare_ReturnsUnchangedLinesWhenPromptsMatch()
    {
        var diff = PromptDiff.Compare("line 1\nline 2", "line 1\r\nline 2");

        Assert.False(diff.HasChanges);
        Assert.All(diff.Lines, line => Assert.Equal(PromptDiffLineKind.Unchanged, line.Kind));
    }

    [Fact]
    public void Compare_DetectsAddedRemovedAndModifiedLines()
    {
        var diff = PromptDiff.Compare(
            "line 1\nold line\nremoved line",
            "line 1\nnew line\nadded replacement\nextra line");

        Assert.True(diff.HasChanges);
        Assert.Equal(PromptDiffLineKind.Unchanged, diff.Lines[0].Kind);
        Assert.Equal(PromptDiffLineKind.Modified, diff.Lines[1].Kind);
        Assert.Equal("old line", diff.Lines[1].OriginalText);
        Assert.Equal("new line", diff.Lines[1].RevisedText);
        Assert.Equal(PromptDiffLineKind.Modified, diff.Lines[2].Kind);
        Assert.Equal(PromptDiffLineKind.Added, diff.Lines[3].Kind);
        Assert.Equal("extra line", diff.Lines[3].RevisedText);
    }

    [Fact]
    public void Compare_DetectsRemovedTrailingLines()
    {
        var diff = PromptDiff.Compare("line 1\nline 2\nline 3", "line 1");

        Assert.Equal(PromptDiffLineKind.Removed, diff.Lines[1].Kind);
        Assert.Equal(PromptDiffLineKind.Removed, diff.Lines[2].Kind);
    }

    [Fact]
    public void Compare_WorksWithRepairSuggestionOutput()
    {
        var review = new StructuredReviewOutput(
            Guid.NewGuid(),
            ReviewDecision.Fail,
            [],
            [],
            "Failed.",
            "Make the subject larger.");
        var repair = PromptRepairSuggestion.FromReview("Original prompt.", review);

        var diff = PromptDiff.Compare("Original prompt.", repair.SuggestedPromptText);

        Assert.True(diff.HasChanges);
        Assert.Contains(diff.Lines, line => line.Kind is PromptDiffLineKind.Added);
    }
}
