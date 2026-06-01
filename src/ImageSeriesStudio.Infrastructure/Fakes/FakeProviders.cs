using System.Text.Json;
using ImageSeriesStudio.Core.Projects;
using ImageSeriesStudio.Core.Providers;

namespace ImageSeriesStudio.Infrastructure.Fakes;

public sealed class FakeTextPlanningProvider : ITextPlanningProvider
{
    public IProviderCapabilities Capabilities { get; } = new ProviderCapabilities(
        "fake-text",
        "Fake Text Planning Provider",
        ["fake-plan-v1"],
        SupportsTextPlanning: true,
        SupportsImageGeneration: false,
        SupportsVisionReview: false,
        SupportsImageEditing: false,
        SupportsStreaming: false);

    public Task<SeriesPlanResult> CreatePlanAsync(PlanningRequest request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var count = Math.Max(1, request.ItemCount);
        var items = Enumerable.Range(1, count)
            .Select(index => new SeriesPlanItem(
                $"Image {index:00}",
                $"For {request.Audience}: {request.Goal}",
                $"Create image {index:00} for {request.Goal}. Style: {request.StyleBrief}".Trim()))
            .ToArray();

        var result = new SeriesPlanResult(
            $"Fake plan for {request.Goal} with {count} item(s).",
            items,
            "fake-text-plan");

        return Task.FromResult(result);
    }
}

public sealed class FakeImageGenerationProvider : IImageGenerationProvider
{
    private static readonly JsonSerializerOptions MetadataJsonOptions = new() { WriteIndented = true };

    private static readonly byte[] PlaceholderPng = Convert.FromBase64String(
        "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+/p9sAAAAASUVORK5CYII=");

    public IProviderCapabilities Capabilities { get; } = new ProviderCapabilities(
        "fake-image",
        "Fake Image Generation Provider",
        ["fake-image-v1"],
        SupportsTextPlanning: false,
        SupportsImageGeneration: true,
        SupportsVisionReview: false,
        SupportsImageEditing: true,
        SupportsStreaming: false,
        supportedSizes: [new ImageOutputSize(512, 512), new ImageOutputSize(1024, 1024)],
        supportedQualities: ["draft", "standard"],
        supportedOutputFormats: ["png"],
        supportedBackgroundModes: ["auto", "opaque"],
        supportsReferenceImages: true,
        costHints: [new ProviderCostHint("fake-image-v1", "free")]);

    public async Task<ImageGenerationResult> GenerateImageAsync(
        ImageGenerationRequest request,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        Directory.CreateDirectory(request.OutputDirectory);

        var fileName = EnsurePngFileName(
            string.IsNullOrWhiteSpace(request.OutputFileName)
                ? $"{request.SeriesItemId:N}-{request.PromptVersionId:N}.png"
                : Path.GetFileName(request.OutputFileName));

        var assetPath = Path.Combine(request.OutputDirectory, fileName);
        var metadataPath = Path.ChangeExtension(assetPath, ".json");
        var generatedAt = DateTimeOffset.UtcNow;

        await File.WriteAllBytesAsync(assetPath, PlaceholderPng, cancellationToken);

        var metadata = new
        {
            providerId = Capabilities.ProviderId,
            seriesItemId = request.SeriesItemId,
            promptVersionId = request.PromptVersionId,
            promptText = request.PromptText,
            settings = new
            {
                width = request.Settings.Width,
                height = request.Settings.Height,
                quality = request.Settings.Quality,
                outputFormat = request.Settings.OutputFormat,
                seed = request.Settings.Seed,
            },
            generatedAt,
        };

        await File.WriteAllTextAsync(
            metadataPath,
            JsonSerializer.Serialize(metadata, MetadataJsonOptions),
            cancellationToken);

        return new ImageGenerationResult(
            Guid.NewGuid(),
            assetPath,
            metadataPath,
            "fake-image-generate",
            generatedAt);
    }

    private static string EnsurePngFileName(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            fileName = "fake-image.png";
        }

        return Path.GetExtension(fileName).Equals(".png", StringComparison.OrdinalIgnoreCase)
            ? fileName
            : $"{fileName}.png";
    }
}

public sealed class FakeVisionReviewProvider : IVisionReviewProvider
{
    private readonly bool _defaultPasses;

    public FakeVisionReviewProvider(bool defaultPasses = true)
    {
        _defaultPasses = defaultPasses;
    }

    public IProviderCapabilities Capabilities { get; } = new ProviderCapabilities(
        "fake-vision",
        "Fake Vision Review Provider",
        ["fake-vision-v1"],
        SupportsTextPlanning: false,
        SupportsImageGeneration: false,
        SupportsVisionReview: true,
        SupportsImageEditing: false,
        SupportsStreaming: false);

    public Task<VisionReviewResult> ReviewAsync(VisionReviewRequest request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var decision = _defaultPasses ? ReviewDecision.Pass : ReviewDecision.Fail;
        var hardFailures = _defaultPasses ? Array.Empty<string>() : ["fake-review-failed"];
        var scores = request.Rubric.Dimensions.ToDictionary(
            dimension => dimension.Name,
            _ => _defaultPasses ? 5 : 1);

        var result = new VisionReviewResult(
            request.CandidateImageId,
            decision,
            scores,
            hardFailures,
            _defaultPasses ? "Fake review passed." : "Fake review failed by configuration.",
            _defaultPasses ? null : "Revise the prompt or regenerate the candidate.");

        return Task.FromResult(result);
    }
}
