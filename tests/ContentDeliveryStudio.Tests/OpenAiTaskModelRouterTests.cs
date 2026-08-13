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
    [InlineData(OpenAiTaskModelRouter.LargeSeriesItemCount, 100, TextProviderModelPresets.SolMedium)]
    [InlineData(OpenAiTaskModelRouter.ComplexSeriesItemCount, 100, TextProviderModelPresets.SolXHigh)]
    [InlineData(2, OpenAiTaskModelRouter.LargeSeriesInputCharacters, TextProviderModelPresets.SolMedium)]
    [InlineData(2, OpenAiTaskModelRouter.ComplexSeriesInputCharacters, TextProviderModelPresets.SolXHigh)]
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
            TextProviderModelPresets.SolXHigh,
            OpenAiTaskModelRouter.ForDocumentPlanning(Auto, scholarly).Preset);
        Assert.Equal(
            TextProviderModelPresets.SolXHigh,
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
            TextProviderModelPresets.SolXHigh,
            OpenAiTaskModelRouter.ForScientificUnderstanding(Auto, understanding).Preset);
        Assert.Equal(
            TextProviderModelPresets.SolXHigh,
            OpenAiTaskModelRouter.ForScientificSemanticReview(Auto, fixture.SemanticRequest).Preset);
        Assert.Equal(
            TextProviderModelPresets.SolXHigh,
            OpenAiTaskModelRouter.ForScientificVisualReview(Auto, fixture.VisualRequest).Preset);
    }

    [Fact]
    public void ComplexAndScientificRoutes_AlwaysUseSolXHigh()
    {
        var fixture = ScientificReviewTestFixture.Create();
        var understandingExtraction = ScientificUnderstandingProviderTests.Extraction(("block", "Evidence."));
        var routes = new[]
        {
            OpenAiTaskModelRouter.ForPlanning(
                Auto,
                new PlanningRequest("goal", "audience", OpenAiTaskModelRouter.ComplexSeriesItemCount)),
            OpenAiTaskModelRouter.ForDocumentPlanning(
                Auto,
                Document(DocumentFamily.ScholarlyDraft, IllustrationStrictnessLevel.ScholarlyDraft)),
            OpenAiTaskModelRouter.ForDocumentPlanning(
                Auto,
                Document(
                    DocumentFamily.Educational,
                    IllustrationStrictnessLevel.Educational,
                    new string('e', OpenAiTaskModelRouter.ComplexDocumentInputWeight))),
            OpenAiTaskModelRouter.ForScientificUnderstanding(
                Auto,
                new ContentDeliveryStudio.Application.ScientificFigures.ScientificUnderstandingChunkRequest(
                    understandingExtraction,
                    "Understand evidence.",
                    0,
                    1,
                    understandingExtraction.Blocks)),
            OpenAiTaskModelRouter.ForScientificSemanticReview(Auto, fixture.SemanticRequest),
            OpenAiTaskModelRouter.ForScientificVisualReview(Auto, fixture.VisualRequest),
            OpenAiTaskModelRouter.ForVisionReview(Auto, VisionReview(signals: OpenAiTaskModelRouter.ComplexVisionSignals)),
        };

        Assert.All(routes, route =>
        {
            Assert.Equal(TextProviderModelPresets.SolXHigh, route.Preset);
            Assert.Equal("gpt-5.6-sol", route.Model);
            Assert.Equal("xhigh", route.ReasoningEffort);
        });
    }

    [Theory]
    [InlineData(1, 0, TextProviderModelPresets.TerraHigh)]
    [InlineData(3, 2, TextProviderModelPresets.TerraXHigh)]
    [InlineData(5, 3, TextProviderModelPresets.SolXHigh)]
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

    private static VisionReviewRequest VisionReview(int signals)
    {
        var rubric = new ReviewRubric(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "review",
            Enumerable.Range(0, signals)
                .Select(index => new ReviewRubricDimension($"dimension-{index}", "requirement", 1))
                .ToArray(),
            DateTimeOffset.UtcNow);
        return new VisionReviewRequest(Guid.NewGuid(), "candidate.png", rubric, "prompt");
    }
}
