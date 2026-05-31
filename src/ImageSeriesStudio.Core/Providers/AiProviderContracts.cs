using ImageSeriesStudio.Core.Projects;

namespace ImageSeriesStudio.Core.Providers;

public interface ITextPlanningProvider
{
    IProviderCapabilities Capabilities { get; }

    Task<SeriesPlanResult> CreatePlanAsync(PlanningRequest request, CancellationToken cancellationToken);
}

public interface IImageGenerationProvider
{
    IProviderCapabilities Capabilities { get; }

    Task<ImageGenerationResult> GenerateImageAsync(ImageGenerationRequest request, CancellationToken cancellationToken);
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
}

public sealed record ProviderCapabilities(
    string ProviderId,
    string DisplayName,
    IReadOnlyList<string> ModelIds,
    bool SupportsTextPlanning,
    bool SupportsImageGeneration,
    bool SupportsVisionReview,
    bool SupportsImageEditing,
    bool SupportsStreaming) : IProviderCapabilities;

public sealed record PlanningRequest(
    string Goal,
    string Audience,
    int ItemCount,
    string StyleBrief = "");

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
    string OutputFileName = "");

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
