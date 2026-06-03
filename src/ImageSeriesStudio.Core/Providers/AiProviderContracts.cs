using ImageSeriesStudio.Core.Documents;
using ImageSeriesStudio.Core.Projects;
using ImageSeriesStudio.Core.Styles;

namespace ImageSeriesStudio.Core.Providers;

public interface ITextPlanningProvider
{
    IProviderCapabilities Capabilities { get; }

    Task<SeriesPlanResult> CreatePlanAsync(PlanningRequest request, CancellationToken cancellationToken);

    Task<BriefPlanningResult> CreatePromptDirectionsAsync(
        BriefPlanningRequest request,
        CancellationToken cancellationToken);

    Task<BlueprintPlanningResult> CreateDesignBlueprintsAsync(
        BlueprintPlanningRequest request,
        CancellationToken cancellationToken);

    Task<DocumentIllustrationPlanningResult> CreateDocumentIllustrationPlanAsync(
        DocumentIllustrationPlanningRequest request,
        CancellationToken cancellationToken);
}

public interface IImageGenerationProvider
{
    IProviderCapabilities Capabilities { get; }

    Task<ImageGenerationResult> GenerateImageAsync(ImageGenerationRequest request, CancellationToken cancellationToken);
}

public interface IImageEditProvider
{
    IProviderCapabilities Capabilities { get; }

    Task<ImageGenerationResult> EditImageAsync(ImageEditRequest request, CancellationToken cancellationToken);
}

public interface IVisionReviewProvider
{
    IProviderCapabilities Capabilities { get; }

    Task<VisionReviewResult> ReviewAsync(VisionReviewRequest request, CancellationToken cancellationToken);
}

public interface IProviderCapabilities
{
    string ProviderId { get; }

    string DisplayName { get; }

    IReadOnlyList<string> ModelIds { get; }

    bool SupportsTextPlanning { get; }

    bool SupportsImageGeneration { get; }

    bool SupportsVisionReview { get; }

    bool SupportsImageEditing { get; }

    bool SupportsStreaming { get; }

    IReadOnlyList<ImageOutputSize> SupportedSizes { get; }

    IReadOnlyList<string> SupportedQualities { get; }

    IReadOnlyList<string> SupportedOutputFormats { get; }

    IReadOnlyList<string> SupportedBackgroundModes { get; }

    bool SupportsReferenceImages { get; }

    IReadOnlyList<ProviderCostHint> CostHints { get; }
}

public sealed class ProviderCapabilities : IProviderCapabilities
{
    public ProviderCapabilities(
        string providerId,
        string displayName,
        IReadOnlyList<string> modelIds,
        bool SupportsTextPlanning,
        bool SupportsImageGeneration,
        bool SupportsVisionReview,
        bool SupportsImageEditing,
        bool SupportsStreaming,
        IReadOnlyList<ImageOutputSize>? supportedSizes = null,
        IReadOnlyList<string>? supportedQualities = null,
        IReadOnlyList<string>? supportedOutputFormats = null,
        IReadOnlyList<string>? supportedBackgroundModes = null,
        bool supportsReferenceImages = false,
        IReadOnlyList<ProviderCostHint>? costHints = null)
    {
        ProviderId = providerId;
        DisplayName = displayName;
        ModelIds = modelIds;
        this.SupportsTextPlanning = SupportsTextPlanning;
        this.SupportsImageGeneration = SupportsImageGeneration;
        this.SupportsVisionReview = SupportsVisionReview;
        this.SupportsImageEditing = SupportsImageEditing;
        this.SupportsStreaming = SupportsStreaming;
        SupportedSizes = supportedSizes ?? [];
        SupportedQualities = supportedQualities ?? [];
        SupportedOutputFormats = supportedOutputFormats ?? [];
        SupportedBackgroundModes = supportedBackgroundModes ?? [];
        SupportsReferenceImages = supportsReferenceImages;
        CostHints = costHints ?? [];
    }

    public string ProviderId { get; }

    public string DisplayName { get; }

    public IReadOnlyList<string> ModelIds { get; }

    public bool SupportsTextPlanning { get; }

    public bool SupportsImageGeneration { get; }

    public bool SupportsVisionReview { get; }

    public bool SupportsImageEditing { get; }

    public bool SupportsStreaming { get; }

    public IReadOnlyList<ImageOutputSize> SupportedSizes { get; }

    public IReadOnlyList<string> SupportedQualities { get; }

    public IReadOnlyList<string> SupportedOutputFormats { get; }

    public IReadOnlyList<string> SupportedBackgroundModes { get; }

    public bool SupportsReferenceImages { get; }

    public IReadOnlyList<ProviderCostHint> CostHints { get; }
}

public sealed record ImageOutputSize(int Width, int Height);

public sealed record ProviderCostHint(string Name, string CostBand);

public sealed record PlanningRequest(
    string Goal,
    string Audience,
    int ItemCount,
    string StyleBrief = "");

public sealed record BriefPlanningRequest(
    string Goal,
    string Audience,
    string StyleIntent,
    IReadOnlyList<string> MustInclude,
    IReadOnlyList<string> MustAvoid,
    int DirectionCount = 3,
    ImageTextPolicy TextPolicy = ImageTextPolicy.Hybrid);

public sealed record BlueprintPlanningRequest(
    string Goal,
    string Audience,
    string StyleIntent,
    IReadOnlyList<string> MustInclude,
    IReadOnlyList<string> MustAvoid,
    ImageTextPolicy TextPolicy = ImageTextPolicy.Hybrid,
    int CandidateCount = 3);

public sealed record BriefPlanningResult(
    IReadOnlyList<PromptDirectionDraft> Directions,
    IReadOnlyList<string> Assumptions,
    IReadOnlyList<string> ClarifyingQuestions,
    string ProviderTraceId);

public sealed record BlueprintPlanningResult(
    IReadOnlyList<DesignBlueprintDraft> Blueprints,
    IReadOnlyList<string> Assumptions,
    IReadOnlyList<string> ClarifyingQuestions,
    string ProviderTraceId);

public sealed record PromptDirectionDraft(
    string Key,
    string Name,
    string IntendedUse,
    string PromptText,
    string NegativePrompt,
    string Strength,
    string Risk,
    PromptDirectionRecommendation? Recommendation = null);

public sealed record DesignBlueprintDraft(
    string Key,
    string DisplayName,
    string Category,
    string Summary,
    string IntendedUse,
    int MinimumRecommendedItemCount,
    int MaximumRecommendedItemCount,
    bool SupportsPanelSequence,
    ImageTextPolicy DefaultTextPolicy,
    string DefaultReviewRubricTemplateId,
    IReadOnlyList<string> ConsistencyRules,
    IReadOnlyList<string> VariationRules,
    IReadOnlyList<string> RiskNotes);

public sealed record DocumentIllustrationPlanningRequest(
    string Title,
    string SourceText,
    string Audience,
    DocumentFamily DocumentFamily,
    IllustrationStrictnessLevel StrictnessLevel,
    IReadOnlyList<string> Sections,
    IReadOnlyList<string> KeyClaims,
    IReadOnlyList<string> KnownConstraints);

public sealed record DocumentIllustrationPlanningResult(
    DocumentBrief Brief,
    IllustrationPlan Plan,
    string ProviderTraceId);

public sealed record SeriesPlanResult(
    string Summary,
    IReadOnlyList<SeriesPlanItem> Items,
    string ProviderTraceId);

public sealed record SeriesPlanItem(
    string Title,
    string Brief,
    string PromptDraft);

public sealed record ImageGenerationRequest(
    Guid SeriesItemId,
    Guid PromptVersionId,
    string PromptText,
    GenerationSettings Settings,
    string OutputDirectory,
    string OutputFileName = "",
    GenerationRecipe? Recipe = null);

public sealed record ImageEditRequest(
    Guid SeriesItemId,
    Guid SourceCandidateImageId,
    string SourceImagePath,
    string? MaskImagePath,
    string PromptText,
    GenerationSettings Settings,
    string OutputDirectory,
    string OutputFileName = "",
    GenerationRecipe? Recipe = null)
{
    public bool IsMaskEdit => !string.IsNullOrWhiteSpace(MaskImagePath);
}

public sealed record ImageGenerationResult(
    Guid CandidateImageId,
    string AssetPath,
    string MetadataPath,
    string ProviderTraceId,
    DateTimeOffset GeneratedAt);

public sealed record VisionReviewRequest(
    Guid CandidateImageId,
    string AssetPath,
    ReviewRubric Rubric,
    string PromptText);

public sealed record VisionReviewResult(
    Guid CandidateImageId,
    ReviewDecision Decision,
    IReadOnlyDictionary<string, int> Scores,
    IReadOnlyList<string> HardFailures,
    string Comments,
    string? SuggestedFix);
