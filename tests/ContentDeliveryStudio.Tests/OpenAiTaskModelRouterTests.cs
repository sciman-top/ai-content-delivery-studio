using ContentDeliveryStudio.Core.Documents;
using ContentDeliveryStudio.Core.Projects;
using ContentDeliveryStudio.Core.Providers;
using ContentDeliveryStudio.Infrastructure.OpenAI;

namespace ContentDeliveryStudio.Tests;

public sealed class OpenAiTaskModelRouterTests
{
    private static readonly OpenAiProviderOptions Auto = new()
    {
        TextRoutingMode = OpenAiTextRoutingMode.Auto,
    };

    [Theory]
    [InlineData(2, 100, TextProviderModelPresets.TerraHigh)]
    [InlineData(OpenAiTaskModelRouter.LargeSeriesItemCount, 100, TextProviderModelPresets.TerraXHigh)]
    [InlineData(OpenAiTaskModelRouter.ComplexSeriesItemCount, 100, TextProviderModelPresets.SolMedium)]
    [InlineData(2, OpenAiTaskModelRouter.LargeSeriesInputCharacters, TextProviderModelPresets.TerraXHigh)]
    [InlineData(2, OpenAiTaskModelRouter.ComplexSeriesInputCharacters, TextProviderModelPresets.SolMedium)]
    public void Planning_UsesDeterministicScaleThresholds(
        int itemCount,
        int inputCharacters,
        string expectedPreset)
    {
        var request = new PlanningRequest(
            new string('g', Math.Max(1, inputCharacters - 2)),
            "a",
            itemCount);

        Assert.Equal(expectedPreset, OpenAiTaskModelRouter.ForPlanning(Auto, request).Preset);
    }

    [Fact]
    public void DocumentPlanning_UsesScholarlyAndComplexEducationalSignals()
    {
        var scholarly = Document(DocumentFamily.ScholarlyDraft, IllustrationStrictnessLevel.ScholarlyDraft);
        var complexEducational = Document(
            DocumentFamily.Educational,
            IllustrationStrictnessLevel.Educational,
            sourceText: new string('s', OpenAiTaskModelRouter.ComplexDocumentInputWeight));
        var editorial = Document(DocumentFamily.Editorial, IllustrationStrictnessLevel.Editorial);

        Assert.Equal(
            TextProviderModelPresets.SolMedium,
            OpenAiTaskModelRouter.ForDocumentPlanning(Auto, scholarly).Preset);
        Assert.Equal(
            TextProviderModelPresets.TerraXHigh,
            OpenAiTaskModelRouter.ForDocumentPlanning(Auto, complexEducational).Preset);
        Assert.Equal(
            TextProviderModelPresets.TerraHigh,
            OpenAiTaskModelRouter.ForDocumentPlanning(Auto, editorial).Preset);
    }

    [Fact]
    public void ScientificTasks_UseQualityFirstRoutes()
    {
        var fixture = ScientificReviewTestFixture.Create();
        var understanding = new ContentDeliveryStudio.Application.ScientificFigures.ScientificUnderstandingChunkRequest(
            ScientificUnderstandingProviderTests.Extraction(("block", "Evidence.")),
            "Understand evidence.",
            0,
            1,
            ScientificUnderstandingProviderTests.Extraction(("block", "Evidence.")).Blocks);

        Assert.Equal(
            TextProviderModelPresets.TerraXHigh,
            OpenAiTaskModelRouter.ForScientificUnderstanding(Auto, understanding).Preset);
        Assert.Equal(
            TextProviderModelPresets.SolMedium,
            OpenAiTaskModelRouter.ForScientificSemanticReview(Auto, fixture.SemanticRequest).Preset);
        Assert.Equal(
            TextProviderModelPresets.SolXHigh,
            OpenAiTaskModelRouter.ForScientificVisualReview(Auto, fixture.VisualRequest).Preset);
    }

    [Theory]
    [InlineData(1, 0, TextProviderModelPresets.TerraHigh)]
    [InlineData(3, 2, TextProviderModelPresets.TerraXHigh)]
    [InlineData(5, 3, TextProviderModelPresets.SolMedium)]
    public void VisionReview_UsesRubricAndEvidenceSignalCount(
        int dimensions,
        int evidenceSelections,
        string expectedPreset)
    {
        var rubric = new ReviewRubric(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "review",
            Enumerable.Range(0, dimensions)
                .Select(index => new ReviewRubricDimension($"dimension-{index}", "requirement", 1))
                .ToArray(),
            DateTimeOffset.UtcNow);
        var request = new VisionReviewRequest(
            Guid.NewGuid(),
            "candidate.png",
            rubric,
            "prompt",
            new ReviewPrepArtifactContract(
                "summary",
                EvidenceSelections: Enumerable.Range(0, evidenceSelections)
                    .Select(index => new ReviewPrepEvidenceSelection($"role-{index}", "local", null))
                    .ToArray()));

        Assert.Equal(expectedPreset, OpenAiTaskModelRouter.ForVisionReview(Auto, request).Preset);
    }

    [Fact]
    public void FixedMode_AlwaysUsesConfiguredModelAndEffort()
    {
        var options = new OpenAiProviderOptions
        {
            TextRoutingMode = OpenAiTextRoutingMode.Fixed,
            TextPlanningModel = "custom-model",
            VisionReviewModel = "custom-vision-model",
            ReasoningEffort = "low",
        };

        var planning = OpenAiTaskModelRouter.ForPlanning(options, new PlanningRequest("goal", "audience", 99));
        var vision = OpenAiTaskModelRouter.ForVisionReview(
            options,
            new VisionReviewRequest(
                Guid.NewGuid(),
                "candidate.png",
                new ReviewRubric(Guid.NewGuid(), Guid.NewGuid(), "review", [], DateTimeOffset.UtcNow),
                "prompt"));

        Assert.Equal(("custom-model", "low"), (planning.Model, planning.ReasoningEffort));
        Assert.Equal(("custom-vision-model", "low"), (vision.Model, vision.ReasoningEffort));
    }

    private static DocumentIllustrationPlanningRequest Document(
        DocumentFamily family,
        IllustrationStrictnessLevel strictness,
        string sourceText = "source") =>
        new("title", sourceText, "audience", family, strictness, ["section"], ["claim"], ["constraint"]);
}
