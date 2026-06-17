using System.Text.Json;
using ContentDeliveryStudio.Core.Documents;
using ContentDeliveryStudio.Core.Projects;
using ContentDeliveryStudio.Core.Providers;
using ContentDeliveryStudio.Core.Styles;
using ContentDeliveryStudio.Infrastructure.Fakes;

namespace ContentDeliveryStudio.Tests;

public sealed class FakeProviderTests
{
    [Fact]
    public async Task FakeTextPlanningProvider_ReturnsDeterministicSeriesPlan()
    {
        var provider = new FakeTextPlanningProvider();

        var result = await provider.CreatePlanAsync(
            new PlanningRequest("course poster set", "physics teachers", 2, "clean editorial style"),
            CancellationToken.None);

        Assert.Equal("fake-text", provider.Capabilities.ProviderId);
        Assert.Contains("course poster set", result.Summary);
        Assert.Collection(
            result.Items,
            item => Assert.Equal("Image 01", item.Title),
            item => Assert.Equal("Image 02", item.Title));
    }

    [Fact]
    public async Task FakeTextPlanningProvider_CreatesPromptDirectionsForBrief()
    {
        var provider = new FakeTextPlanningProvider();

        var result = await provider.CreatePromptDirectionsAsync(
            new BriefPlanningRequest(
                "article illustration",
                "teachers",
                "clean editorial",
                ["accurate subject"],
                ["unreadable text"],
                DirectionCount: 3),
            CancellationToken.None);

        Assert.Equal("fake-text-brief", result.ProviderTraceId);
        Assert.Equal(3, result.Directions.Count);
        Assert.Contains(result.Assumptions, assumption => assumption.Contains("draft", StringComparison.OrdinalIgnoreCase));
        Assert.Equal("conservative", result.Directions[0].Key);
        Assert.Contains("article illustration", result.Directions[0].PromptText);
        var recommendation = result.Directions[0].Recommendation;

        Assert.NotNull(recommendation);
        Assert.Equal(ImageTypePresetCatalog.ArticleInlineIllustration, recommendation.ImageTypePresetId);
        Assert.Equal(ImageTextPolicy.Hybrid, recommendation.TextPolicy);
        Assert.Equal(new AspectRatio(16, 9), recommendation.AspectRatio);
        Assert.Equal(1536, recommendation.Width);
        Assert.Equal(1024, recommendation.Height);
        Assert.Equal("draft", recommendation.QualityBand);
        Assert.Equal("png", recommendation.OutputFormat);
        Assert.Equal(ReviewRubricTemplateCatalog.EditorialIllustration, recommendation.ReviewRubricTemplateId);
        Assert.InRange(recommendation.Confidence, 0.65, 1);
        Assert.Contains("article", recommendation.RecommendationReason, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(recommendation.CapabilityWarnings, value => value.Contains("fake", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(recommendation.NonExecutableSuggestions, value => value.Contains("style", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task FakeTextPlanningProvider_CreatesDesignBlueprintsForBrief()
    {
        var provider = new FakeTextPlanningProvider();

        var result = await provider.CreateDesignBlueprintsAsync(
            new BlueprintPlanningRequest(
                "panel story sequence",
                "students",
                "clear visual storytelling",
                ["same main character"],
                ["wall of unreadable text"],
                ImageTextPolicy.DeterministicPostRender,
                CandidateCount: 3),
            CancellationToken.None);

        Assert.Equal("fake-text-blueprint", result.ProviderTraceId);
        Assert.Equal(3, result.Blueprints.Count);
        Assert.Contains(result.Assumptions, assumption => assumption.Contains("compare", StringComparison.OrdinalIgnoreCase));
        Assert.Equal("panel-narrative-sequence", result.Blueprints[0].Key);
        Assert.True(result.Blueprints[0].SupportsPanelSequence);
        Assert.Equal(ImageTextPolicy.DeterministicPostRender, result.Blueprints[0].DefaultTextPolicy);
        Assert.Contains(result.Blueprints[0].ConsistencyRules, value => value.Contains("same main character", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(result.Blueprints[0].RiskNotes, value => value.Contains("unreadable text", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task FakeTextPlanningProvider_CreatesDocumentIllustrationPlan()
    {
        var provider = new FakeTextPlanningProvider();

        var result = await provider.CreateDocumentIllustrationPlanAsync(
            new DocumentIllustrationPlanningRequest(
                "Quantum teaching note",
                "Teachers need an intuitive explanation of superposition.",
                "teachers",
                DocumentFamily.Educational,
                IllustrationStrictnessLevel.Educational,
                ["Introduction", "Classroom analogy"],
                ["Superposition needs a visual analogy."],
                ["avoid fake lab data"]),
            CancellationToken.None);

        Assert.Equal("fake-document-plan", result.ProviderTraceId);
        Assert.Equal(DocumentFamily.Educational, result.Brief.DocumentFamily);
        Assert.NotEmpty(result.Plan.Targets);
        Assert.All(result.Plan.Targets, target => Assert.NotEmpty(target.SourceEvidence));
        Assert.Contains(result.Plan.Targets, target => target.Purpose == IllustrationPurpose.ConceptDiagram);
    }

    [Fact]
    public async Task FakeImageGenerationProvider_WritesPlaceholderImageAndSidecarMetadata()
    {
        var outputDirectory = Path.Combine(Path.GetTempPath(), "ContentDeliveryStudio.Tests", Guid.NewGuid().ToString("N"));
        var provider = new FakeImageGenerationProvider();
        var seriesItemId = Guid.NewGuid();
        var promptVersionId = Guid.NewGuid();

        try
        {
            var result = await provider.GenerateImageAsync(
                new ImageGenerationRequest(
                    seriesItemId,
                    promptVersionId,
                    "A clean blue poster background",
                    new GenerationSettings(1024, 1024, "standard", "png", 42),
                    outputDirectory,
                    "cover.png"),
                CancellationToken.None);

            Assert.True(File.Exists(result.AssetPath));
            Assert.True(File.Exists(result.MetadataPath));
            Assert.EndsWith(".png", result.AssetPath, StringComparison.OrdinalIgnoreCase);
            Assert.EndsWith(".json", result.MetadataPath, StringComparison.OrdinalIgnoreCase);

            using var metadataStream = File.OpenRead(result.MetadataPath);
            using var metadata = await JsonDocument.ParseAsync(metadataStream, cancellationToken: CancellationToken.None);

            Assert.Equal("fake-image", metadata.RootElement.GetProperty("providerId").GetString());
            Assert.Equal(seriesItemId, metadata.RootElement.GetProperty("seriesItemId").GetGuid());
            Assert.Equal(promptVersionId, metadata.RootElement.GetProperty("promptVersionId").GetGuid());
            Assert.Equal(1024, metadata.RootElement.GetProperty("settings").GetProperty("width").GetInt32());
        }
        finally
        {
            if (Directory.Exists(outputDirectory))
            {
                Directory.Delete(outputDirectory, recursive: true);
            }
        }
    }

    [Fact]
    public async Task FakeImageGenerationProvider_WritesMaskEditOutputAndMetadata()
    {
        var rootDirectory = Path.Combine(Path.GetTempPath(), "ContentDeliveryStudio.Tests", Guid.NewGuid().ToString("N"));
        var outputDirectory = Path.Combine(rootDirectory, "edited");
        var sourcePath = Path.Combine(rootDirectory, "source.png");
        var maskPath = Path.Combine(rootDirectory, "mask.png");
        var provider = new FakeImageGenerationProvider();
        var seriesItemId = Guid.NewGuid();
        var candidateId = Guid.NewGuid();
        Directory.CreateDirectory(rootDirectory);
        await File.WriteAllBytesAsync(sourcePath, [1, 2, 3], CancellationToken.None);
        await File.WriteAllBytesAsync(maskPath, [4, 5, 6], CancellationToken.None);

        try
        {
            var result = await provider.EditImageAsync(
                new ImageEditRequest(
                    seriesItemId,
                    candidateId,
                    sourcePath,
                    maskPath,
                    "Replace the label area with a cleaner background.",
                    new GenerationSettings(1024, 1024, "standard", "png"),
                    outputDirectory,
                    "edited.png"),
                CancellationToken.None);

            Assert.True(File.Exists(result.AssetPath));
            Assert.True(File.Exists(result.MetadataPath));
            Assert.Equal("fake-image-edit", result.ProviderTraceId);

            using var metadataStream = File.OpenRead(result.MetadataPath);
            using var metadata = await JsonDocument.ParseAsync(metadataStream, cancellationToken: CancellationToken.None);

            Assert.Equal("mask-edit", metadata.RootElement.GetProperty("editMode").GetString());
            Assert.Equal(candidateId, metadata.RootElement.GetProperty("sourceCandidateId").GetGuid());
            Assert.Equal(sourcePath, metadata.RootElement.GetProperty("sourceImagePath").GetString());
            Assert.Equal(maskPath, metadata.RootElement.GetProperty("maskImagePath").GetString());
        }
        finally
        {
            if (Directory.Exists(rootDirectory))
            {
                Directory.Delete(rootDirectory, recursive: true);
            }
        }
    }

    [Fact]
    public async Task FakeVisionReviewProvider_ReturnsConfiguredDecision()
    {
        var provider = new FakeVisionReviewProvider(defaultPasses: false);
        var candidateId = Guid.NewGuid();

        var result = await provider.ReviewAsync(
            new VisionReviewRequest(
                candidateId,
                "fake-output.png",
                new ReviewRubric(
                    Guid.NewGuid(),
                    Guid.NewGuid(),
                "Default rubric",
                [new ReviewRubricDimension("subject", "Required subject is visible", 1)],
                DateTimeOffset.UtcNow),
                "A clean blue poster background",
                new ReviewPrepArtifactContract("Local fake review prep.")),
            CancellationToken.None);

        Assert.Equal(candidateId, result.CandidateImageId);
        Assert.Equal(ReviewDecision.Fail, result.Decision);
        Assert.Contains("fake-review-failed", result.HardFailures);
    }
}
