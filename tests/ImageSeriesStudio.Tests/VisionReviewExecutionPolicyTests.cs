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
        Assert.Equal(384, VisionReviewExecutionPolicy.DefaultCompactReviewImageMaxDimension);
        Assert.Equal(70, VisionReviewExecutionPolicy.DefaultCompactReviewImageJpegQuality);
        Assert.False(VisionReviewExecutionPolicy.StoreResponsesByDefault);
        Assert.False(VisionReviewExecutionPolicy.AllowPreviousResponseIdByDefault);
    }

    [Fact]
    public void CreateOperatorDescriptor_UsesStatelessLocalDirectDefaultsForRemoteReview()
    {
        var descriptor = VisionReviewExecutionPolicy.CreateOperatorDescriptor("openai-vision");

        Assert.Equal("local-direct-stateless", descriptor.ExecutionMode);
        Assert.Equal(6, descriptor.BatchItemLimit);
        Assert.Equal(4, descriptor.HighRiskBatchItemLimit);
        Assert.Equal(280, descriptor.CompactSummaryCharacterLimit);
        Assert.False(descriptor.UsesStoredResponses);
        Assert.False(descriptor.AllowsPreviousResponseId);
        Assert.True(descriptor.RequiresCompactLocalArtifacts);
    }

    [Fact]
    public void CreateOperatorDescriptor_SkipsCompactArtifactRequirementForFakeReview()
    {
        var descriptor = VisionReviewExecutionPolicy.CreateOperatorDescriptor("fake-vision");

        Assert.Equal("fake-local-review", descriptor.ExecutionMode);
        Assert.False(descriptor.RequiresCompactLocalArtifacts);
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
