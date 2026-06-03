using ImageSeriesStudio.Core.Projects;

namespace ImageSeriesStudio.Application.Delivery;

public interface IDeliveryPackageWriter
{
    Task<DeliveryExportResult> WriteAsync(DeliveryExportRequest request, CancellationToken cancellationToken);
}

public sealed record DeliveryExportRequest(
    string ProjectName,
    string OutputDirectory,
    IReadOnlyList<DeliveryExportItem> Items);

public sealed record DeliveryExportItem(
    string ItemKey,
    string Title,
    string FinalImagePath,
    string MetadataPath,
    string PromptText,
    ReviewDecision ReviewDecision,
    bool HumanApproved,
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
    IReadOnlyList<Guid>? OperatorRunIds = null);

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
