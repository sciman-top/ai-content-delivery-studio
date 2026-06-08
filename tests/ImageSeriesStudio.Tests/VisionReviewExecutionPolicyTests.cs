using ImageSeriesStudio.Core.Providers;

namespace ImageSeriesStudio.Tests;

public sealed class VisionReviewExecutionPolicyTests
{
    [Fact]
    public void Defaults_UseStatelessBoundedCompactReviewPrep()
    {
        Assert.Equal(6, VisionReviewExecutionPolicy.DefaultBatchItemLimit);
        Assert.Equal(4, VisionReviewExecutionPolicy.DefaultHighRiskBatchItemLimit);
        Assert.Equal(280, VisionReviewExecutionPolicy.DefaultCompactSummaryCharacters);
        Assert.False(VisionReviewExecutionPolicy.StoreResponsesByDefault);
        Assert.False(VisionReviewExecutionPolicy.AllowPreviousResponseIdByDefault);
    }

    [Fact]
    public void CreateMinimalLocalContract_TrimsLongPromptIntoCompactSummary()
    {
        var promptText = new string('A', VisionReviewExecutionPolicy.DefaultCompactSummaryCharacters + 50);

        var contract = VisionReviewExecutionPolicy.CreateMinimalLocalContract("Opening frame", promptText);

        Assert.True(contract.Summary.Length <= VisionReviewExecutionPolicy.DefaultCompactSummaryCharacters);
        var promptEvidence = Assert.Single(contract.EvidenceSelections, selection => selection.Role == "prompt-summary");
        Assert.NotNull(promptEvidence.Summary);
        Assert.True(promptEvidence.Summary!.Length <= VisionReviewExecutionPolicy.DefaultCompactSummaryCharacters);
    }
}
