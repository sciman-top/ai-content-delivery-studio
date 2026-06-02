using System.Text.Json;
using ImageSeriesStudio.Core.Documents;
using ImageSeriesStudio.Core.Projects;
using ImageSeriesStudio.Core.Providers;
using ImageSeriesStudio.Infrastructure.Fakes;

namespace ImageSeriesStudio.Tests;

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
        var outputDirectory = Path.Combine(Path.GetTempPath(), "ImageSeriesStudio.Tests", Guid.NewGuid().ToString("N"));
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
                "A clean blue poster background"),
            CancellationToken.None);

        Assert.Equal(candidateId, result.CandidateImageId);
        Assert.Equal(ReviewDecision.Fail, result.Decision);
        Assert.Contains("fake-review-failed", result.HardFailures);
    }
}
