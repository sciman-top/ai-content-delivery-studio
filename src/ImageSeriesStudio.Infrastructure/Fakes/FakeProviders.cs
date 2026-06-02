using System.Text.Json;
using ImageSeriesStudio.Core.Documents;
using ImageSeriesStudio.Core.Projects;
using ImageSeriesStudio.Core.Providers;
using ImageSeriesStudio.Core.Styles;

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

    public Task<BriefPlanningResult> CreatePromptDirectionsAsync(
        BriefPlanningRequest request,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var count = Math.Clamp(request.DirectionCount, 1, 4);
        var templates = new[]
        {
            new
            {
                Key = "conservative",
                Name = "Conservative faithful",
                Use = "Safest match to the brief.",
                Strength = "High requirement match.",
                Risk = "Less visually dramatic.",
            },
            new
            {
                Key = "visual-impact",
                Name = "Visual impact",
                Use = "Stronger composition and contrast.",
                Strength = "More engaging first impression.",
                Risk = "May need closer factual review.",
            },
            new
            {
                Key = "minimal-clean",
                Name = "Minimal clean",
                Use = "Low visual noise and easy review.",
                Strength = "Good for text composition.",
                Risk = "May feel plain.",
            },
            new
            {
                Key = "experimental",
                Name = "Experimental alternate",
                Use = "Explores a less obvious direction.",
                Strength = "Can reveal a stronger style.",
                Risk = "Higher mismatch risk.",
            },
        };

        var directions = templates
            .Take(count)
            .Select(template => new PromptDirectionDraft(
                template.Key,
                template.Name,
                template.Use,
                $"Create {request.Goal} for {request.Audience}. Style: {request.StyleIntent}. Include: {string.Join(", ", request.MustInclude)}.",
                $"Avoid: {string.Join(", ", request.MustAvoid)}.",
                template.Strength,
                template.Risk,
                CreateRecommendation(request, template.Key)))
            .ToArray();

        return Task.FromResult(new BriefPlanningResult(
            directions,
            ["Use draft generation before final quality."],
            ["Confirm whether final text should be composed in app."],
            "fake-text-brief"));
    }

    private static PromptDirectionRecommendation CreateRecommendation(
        BriefPlanningRequest request,
        string directionKey)
    {
        var presetId = SelectPresetId(request.Goal);
        var preset = ImageTypePresetCatalog.GetById(presetId);
        var textPolicy = request.TextPolicy is ImageTextPolicy.ImageModelOnly
            ? preset.TextPolicy
            : request.TextPolicy;
        var (width, height) = GetDefaultSize(preset.DefaultAspectRatio);

        return PromptDirectionRecommendation.Create(
            preset.Id,
            textPolicy,
            request.StyleIntent,
            preset.DefaultAspectRatio,
            width,
            height,
            preset.DefaultQualityBand,
            preset.DefaultOutputFormat,
            preset.DefaultBackgroundMode,
            preset.ReviewRubricTemplateId,
            draftCount: 2,
            finalCount: 1,
            $"Fake recommendation selected {preset.DisplayName} for {request.Goal}. Direction: {directionKey}.",
            confidence: directionKey.Equals("experimental", StringComparison.OrdinalIgnoreCase) ? 0.68 : 0.84,
            ["fake provider warning: verify real provider capabilities before generation"],
            [$"Refine style intent before final generation: {request.StyleIntent}".Trim()]);
    }

    private static string SelectPresetId(string goal)
    {
        if (goal.Contains("poster", StringComparison.OrdinalIgnoreCase))
        {
            return ImageTypePresetCatalog.EducationalPoster;
        }

        if (goal.Contains("cover", StringComparison.OrdinalIgnoreCase))
        {
            return ImageTypePresetCatalog.ArticleCover;
        }

        if (goal.Contains("diagram", StringComparison.OrdinalIgnoreCase)
            || goal.Contains("concept", StringComparison.OrdinalIgnoreCase))
        {
            return ImageTypePresetCatalog.ConceptDiagram;
        }

        if (goal.Contains("social", StringComparison.OrdinalIgnoreCase)
            || goal.Contains("square", StringComparison.OrdinalIgnoreCase))
        {
            return ImageTypePresetCatalog.SocialSquare;
        }

        return ImageTypePresetCatalog.ArticleInlineIllustration;
    }

    private static (int Width, int Height) GetDefaultSize(AspectRatio aspectRatio)
    {
        if (aspectRatio.WidthUnits == aspectRatio.HeightUnits)
        {
            return (1024, 1024);
        }

        return aspectRatio.WidthUnits > aspectRatio.HeightUnits
            ? (1536, 1024)
            : (1024, 1280);
    }

    public Task<DocumentIllustrationPlanningResult> CreateDocumentIllustrationPlanAsync(
        DocumentIllustrationPlanningRequest request,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var timestamp = DateTimeOffset.UtcNow;
        var brief = DocumentBrief.Create(
            Guid.NewGuid(),
            DocumentSourceKind.Paste,
            $"{request.Title.Trim()}.txt",
            request.Title,
            request.DocumentFamily,
            request.Audience,
            request.Sections,
            request.KeyClaims,
            request.Sections.Select(section => $"Illustrate {section}").ToArray(),
            request.KnownConstraints,
            request.StrictnessLevel,
            timestamp);

        var evidence = request.KeyClaims.Count == 0 ? [request.SourceText] : request.KeyClaims;
        var target = request.StrictnessLevel is IllustrationStrictnessLevel.ScholarlyDraft
            ? IllustrationTarget.Create(
                brief.Id,
                "Graphical abstract schematic",
                "document:summary",
                IllustrationPurpose.GraphicalAbstract,
                ["summarize the central concept as a schematic"],
                ["do not imply real experimental evidence"],
                evidence,
                ImageTypePresetCatalog.GraphicalAbstract,
                ReviewRubricTemplateCatalog.ScholarlySchematic,
                ImageTextPolicy.DeterministicPostRender,
                ["scholarly draft mode"],
                timestamp)
            : IllustrationTarget.Create(
                brief.Id,
                "Concept illustration",
                "document:introduction",
                IllustrationPurpose.ConceptDiagram,
                ["explain the central idea visually"],
                request.KnownConstraints,
                evidence,
                ImageTypePresetCatalog.ConceptDiagram,
                ReviewRubricTemplateCatalog.EducationalAccuracy,
                ImageTextPolicy.DeterministicPostRender,
                ["fake provider"],
                timestamp);

        var plan = IllustrationPlan.Create(
            brief.ProjectId,
            brief.Id,
            $"Fake document illustration plan for {request.Title.Trim()}.",
            [target],
            ["Central concept covered."],
            request.KnownConstraints,
            timestamp);

        return Task.FromResult(new DocumentIllustrationPlanningResult(brief, plan, "fake-document-plan"));
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
