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
    bool HumanApproved);

public sealed record DeliveryExportResult(
    string PackageDirectory,
    string ManifestJsonPath,
    string ManifestCsvPath,
    string ReviewReportPath,
    IReadOnlyList<string> FinalImagePaths);
