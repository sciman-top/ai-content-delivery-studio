using System.IO;
using ImageSeriesStudio.Application.Delivery;
using ImageSeriesStudio.Application.Projects;
using ImageSeriesStudio.Core.Projects;

namespace ImageSeriesStudio.App.ViewModels;

public sealed class DeliveryWorkflowCoordinator
{
    private readonly ProjectApplicationService _projectService;

    public DeliveryWorkflowCoordinator(ProjectApplicationService projectService)
    {
        _projectService = projectService;
    }

    public async Task<DeliveryWorkflowResult> ExportDeliveryAsync(
        Guid projectId,
        string projectName,
        IReadOnlyList<GalleryRowViewModel> galleryRows,
        IReadOnlyList<ReviewRowViewModel> reviewRows,
        IReadOnlyList<DesignBlueprintRowViewModel> designBlueprintRows,
        Guid? activeCreativeBriefId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(galleryRows);
        ArgumentNullException.ThrowIfNull(reviewRows);
        ArgumentNullException.ThrowIfNull(designBlueprintRows);

        var reviewByCandidate = reviewRows.ToDictionary(row => row.CandidateImageId);
        var outputDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "ImageSeriesStudio",
            "deliveries",
            projectId.ToString("N"),
            DateTimeOffset.UtcNow.ToString("yyyyMMdd-HHmmss"));
        var blueprint = ResolvePromotedDeliveryBlueprint(designBlueprintRows, activeCreativeBriefId);
        var items = galleryRows
            .Where(row => reviewByCandidate.TryGetValue(row.CandidateImageId, out var review)
                && review.HumanApproved
                && Enum.TryParse<ReviewDecision>(review.Decision, out var decision)
                && decision is ReviewDecision.Pass)
            .Select((row, index) => new DeliveryExportItem(
                $"{index + 1:000}-{row.ItemTitle}",
                row.ItemTitle,
                row.AssetPath,
                row.MetadataPath,
                row.PromptText,
                ReviewDecision.Pass,
                HumanApproved: true,
                FinalReviewer: reviewByCandidate[row.CandidateImageId].FinalReviewer,
                FinalApprovalNotes: reviewByCandidate[row.CandidateImageId].FinalApprovalNotes,
                FinalApprovalDecidedAt: reviewByCandidate[row.CandidateImageId].FinalApprovalDecidedAt,
                Blueprint: blueprint))
            .ToArray();

        var result = await _projectService.ExportDeliveryPackageAsync(
            new DeliveryExportRequest(
                projectName,
                outputDirectory,
                items),
            cancellationToken);

        return new DeliveryWorkflowResult(
            result,
            [
                new DeliveryRowViewModel(
                    result.PackageDirectory,
                    result.ManifestJsonPath,
                    result.ManifestCsvPath,
                    result.ReviewReportPath,
                    result.FinalImagePaths.Count.ToString()),
            ]);
    }

    public static DeliveryBlueprintMetadata? ResolvePromotedDeliveryBlueprint(
        IReadOnlyList<DesignBlueprintRowViewModel> designBlueprintRows,
        Guid? activeCreativeBriefId)
    {
        ArgumentNullException.ThrowIfNull(designBlueprintRows);

        var row = designBlueprintRows.FirstOrDefault(blueprint =>
                blueprint.IsPromoted
                && (activeCreativeBriefId is null || blueprint.CreativeBriefId == activeCreativeBriefId))
            ?? designBlueprintRows.FirstOrDefault(blueprint => blueprint.IsPromoted);

        return row is null
            ? null
            : new DeliveryBlueprintMetadata(
                row.BlueprintId,
                row.Key,
                row.DisplayName,
                row.Category,
                row.SequenceMode,
                row.ConsistencySummary,
                row.VariationSummary);
    }
}

public sealed record DeliveryWorkflowResult(
    DeliveryExportResult ExportResult,
    IReadOnlyList<DeliveryRowViewModel> DeliveryRows);
