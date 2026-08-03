using ContentDeliveryStudio.Core.Projects;
using ContentDeliveryStudio.Application.Projects;

namespace ContentDeliveryStudio.Application.Delivery;

public interface IDeliveryPackageWriter
{
    Task<DeliveryExportResult> WriteAsync(DeliveryExportRequest request, CancellationToken cancellationToken);
}

public sealed record DeliveryExportRequest(
    string ProjectName,
    string OutputDirectory,
    IReadOnlyList<DeliveryExportItem> Items)
{
    /// <summary>
    /// Creates a delivery request rooted in the categorized final-image layout.
    ///
    /// Candidates and review-prep artifacts must be supplied as item inputs, but
    /// the writer is the only component that promotes them into the final package.
    /// Keeping path construction here prevents specialized callers from rebuilding
    /// category paths by hand.
    /// </summary>
    public static DeliveryExportRequest CreateForFinalDelivery(
        string projectName,
        Guid projectId,
        FinalImageDeliveryCategory category,
        DateTimeOffset timestamp,
        IReadOnlyList<DeliveryExportItem> items,
        string? customRoot = null)
    {
        ArgumentNullException.ThrowIfNull(items);

        return new DeliveryExportRequest(
            projectName,
            LocalStudioDataPaths.ResolveFinalDeliveryPackageDirectory(
                category,
                projectId,
                timestamp,
                customRoot),
            items);
    }
}

public sealed record DeliveryExportItem(
    string ItemKey,
    string Title,
    string FinalImagePath,
    string MetadataPath,
    string PromptText,
    ReviewDecision ReviewDecision,
    bool HumanApproved,
    string? FinalReviewer = null,
    string? FinalApprovalNotes = null,
    DateTimeOffset? FinalApprovalDecidedAt = null,
    Guid? StyleGuideId = null,
    int? StyleGuideVersion = null,
    Guid? RecipeId = null,
    IReadOnlyList<Guid>? ReferenceImageSetIds = null,
    string? ExperimentSlug = null,
    IReadOnlyDictionary<string, string>? ExperimentParameters = null,
    Guid? GenerationTaskId = null,
    Guid? OutputArtifactId = null,
    IReadOnlyList<Guid>? SourceAssetIds = null,
    IReadOnlyList<Guid>? EvidenceAnchorIds = null,
    string? ArtifactRole = null,
    DeliveryBlueprintMetadata? Blueprint = null,
    IReadOnlyList<Guid>? OperatorRunIds = null,
    string? DeterministicCompositionReportPath = null,
    CandidateImageEditProvenance? EditProvenance = null);

public sealed record DeliveryBlueprintMetadata(
    Guid Id,
    string Key,
    string DisplayName,
    string Category,
    string SequenceMode,
    string ConsistencySummary,
    string VariationSummary);

public sealed record DeliveryExportResult(
    string PackageDirectory,
    string ManifestJsonPath,
    string ManifestCsvPath,
    string ReviewReportPath,
    IReadOnlyList<string> FinalImagePaths);
