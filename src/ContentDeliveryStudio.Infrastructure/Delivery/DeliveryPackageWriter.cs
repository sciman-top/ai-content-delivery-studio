using System.Text;
using System.Text.Json;
using ContentDeliveryStudio.Application.Delivery;
using ContentDeliveryStudio.Core.Projects;

namespace ContentDeliveryStudio.Infrastructure.Delivery;

public sealed class DeliveryPackageWriter : IDeliveryPackageWriter
{
    private static readonly JsonSerializerOptions ManifestJsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
    };

    public async Task<DeliveryPackageResult> WriteAsync(
        DeliveryPackageRequest request,
        CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(request.OutputDirectory);

        var imagesDirectory = Path.Combine(request.OutputDirectory, "images");
        var promptsDirectory = Path.Combine(request.OutputDirectory, "prompts");
        var metadataDirectory = Path.Combine(request.OutputDirectory, "metadata");
        var compositionDirectory = Path.Combine(request.OutputDirectory, "composition");

        Directory.CreateDirectory(imagesDirectory);
        Directory.CreateDirectory(promptsDirectory);
        Directory.CreateDirectory(metadataDirectory);
        Directory.CreateDirectory(compositionDirectory);

        var approvedItems = request.Items
            .Where(item => item.ReviewDecision is ReviewDecision.Pass && item.HumanApproved)
            .ToArray();

        var manifestItems = new List<DeliveryManifestItem>();
        var finalImagePaths = new List<string>();

        foreach (var item in approvedItems)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var itemKey = NormalizeKey(item.ItemKey);
            var imageExtension = Path.GetExtension(item.FinalImagePath);
            if (string.IsNullOrWhiteSpace(imageExtension))
            {
                imageExtension = ".png";
            }

            var imagePath = Path.Combine(imagesDirectory, $"{itemKey}{imageExtension}");
            var promptPath = Path.Combine(promptsDirectory, $"{itemKey}.txt");
            var metadataPath = Path.Combine(metadataDirectory, $"{itemKey}.json");
            var deterministicCompositionReportPath = CopyDeterministicCompositionReport(
                item,
                compositionDirectory,
                request.OutputDirectory,
                itemKey);

            File.Copy(item.FinalImagePath, imagePath, overwrite: true);
            await File.WriteAllTextAsync(promptPath, item.PromptText, cancellationToken);

            if (File.Exists(item.MetadataPath))
            {
                File.Copy(item.MetadataPath, metadataPath, overwrite: true);
            }

            finalImagePaths.Add(imagePath);
            manifestItems.Add(new DeliveryManifestItem(
                itemKey,
                item.Title,
                ToRelativePath(request.OutputDirectory, imagePath),
                ToRelativePath(request.OutputDirectory, promptPath),
                File.Exists(metadataPath) ? ToRelativePath(request.OutputDirectory, metadataPath) : null,
                item.ReviewDecision,
                item.HumanApproved,
                item.FinalReviewer,
                item.FinalApprovalNotes,
                item.FinalApprovalDecidedAt,
                deterministicCompositionReportPath,
                item.StyleGuideId,
                item.StyleGuideVersion,
                item.RecipeId,
                item.ReferenceImageSetIds ?? [],
                item.ExperimentSlug,
                item.ExperimentParameters ?? new Dictionary<string, string>(),
                item.GenerationTaskId,
                item.OutputArtifactId,
                item.SourceAssetIds ?? [],
                item.EvidenceAnchorIds ?? [],
                item.ArtifactRole,
                item.Blueprint,
                item.OperatorRunIds ?? []));
        }

        var manifestPath = Path.Combine(request.OutputDirectory, "manifest.json");
        var csvPath = Path.Combine(request.OutputDirectory, "manifest.csv");
        var reviewReportPath = Path.Combine(request.OutputDirectory, "review-report.md");

        var manifest = new DeliveryManifest(
            request.ProjectName,
            DateTimeOffset.UtcNow,
            manifestItems);

        await File.WriteAllTextAsync(
            manifestPath,
            JsonSerializer.Serialize(manifest, ManifestJsonOptions),
            cancellationToken);
        await File.WriteAllTextAsync(csvPath, WriteManifestCsv(manifestItems), cancellationToken);
        await File.WriteAllTextAsync(reviewReportPath, WriteReviewReport(request.ProjectName, manifestItems), cancellationToken);

        return new DeliveryPackageResult(
            request.OutputDirectory,
            manifestPath,
            csvPath,
            reviewReportPath,
            finalImagePaths);
    }

    public async Task<DeliveryExportResult> WriteAsync(
        DeliveryExportRequest request,
        CancellationToken cancellationToken)
    {
        var result = await WriteAsync(
            new DeliveryPackageRequest(
                request.ProjectName,
                request.OutputDirectory,
                request.Items
                    .Select(item => new DeliveryPackageItem(
                        item.ItemKey,
                        item.Title,
                        item.FinalImagePath,
                        item.MetadataPath,
                        item.PromptText,
                        item.ReviewDecision,
                        item.HumanApproved,
                        item.FinalReviewer,
                        item.FinalApprovalNotes,
                        item.FinalApprovalDecidedAt,
                        item.StyleGuideId,
                        item.StyleGuideVersion,
                        item.RecipeId,
                        item.ReferenceImageSetIds,
                        item.ExperimentSlug,
                        item.ExperimentParameters,
                        item.GenerationTaskId,
                        item.OutputArtifactId,
                        item.SourceAssetIds,
                        item.EvidenceAnchorIds,
                        item.ArtifactRole,
                        item.Blueprint,
                        item.OperatorRunIds,
                        item.DeterministicCompositionReportPath))
                    .ToArray()),
            cancellationToken);

        return new DeliveryExportResult(
            result.PackageDirectory,
            result.ManifestJsonPath,
            result.ManifestCsvPath,
            result.ReviewReportPath,
            result.FinalImagePaths);
    }

    private static string NormalizeKey(string value)
    {
        var builder = new StringBuilder();
        var invalidChars = Path.GetInvalidFileNameChars();

        foreach (var character in value.Trim())
        {
            builder.Append(invalidChars.Contains(character) || char.IsWhiteSpace(character) ? '-' : char.ToLowerInvariant(character));
        }

        return builder.Length == 0 ? "item" : builder.ToString();
    }

    private static string ToRelativePath(string rootDirectory, string path)
    {
        return Path.GetRelativePath(rootDirectory, path).Replace('\\', '/');
    }

    private static string WriteManifestCsv(IReadOnlyList<DeliveryManifestItem> items)
    {
        var builder = new StringBuilder();
        builder.AppendLine("itemKey,title,imagePath,promptPath,metadataPath,reviewDecision,humanApproved,finalReviewer,finalApprovalNotes,finalApprovalDecidedAt,deterministicCompositionReportPath,styleGuideId,styleGuideVersion,recipeId,referenceImageSetIds,experimentSlug,experimentParameters,generationTaskId,outputArtifactId,sourceAssetIds,evidenceAnchorIds,operatorRunIds,artifactRole,blueprintId,blueprintKey,blueprintDisplayName,blueprintCategory,blueprintSequenceMode,blueprintConsistencySummary,blueprintVariationSummary");

        foreach (var item in items)
        {
            builder.AppendLine(string.Join(
                ',',
                EscapeCsv(item.ItemKey),
                EscapeCsv(item.Title),
                EscapeCsv(item.ImagePath),
                EscapeCsv(item.PromptPath),
                EscapeCsv(item.MetadataPath ?? string.Empty),
                EscapeCsv(item.ReviewDecision.ToString()),
                item.HumanApproved ? "true" : "false",
                EscapeCsv(item.FinalReviewer ?? string.Empty),
                EscapeCsv(item.FinalApprovalNotes ?? string.Empty),
                EscapeCsv(item.FinalApprovalDecidedAt?.ToString("O") ?? string.Empty),
                EscapeCsv(item.DeterministicCompositionReportPath ?? string.Empty),
                EscapeCsv(item.StyleGuideId?.ToString() ?? string.Empty),
                item.StyleGuideVersion?.ToString() ?? string.Empty,
                EscapeCsv(item.RecipeId?.ToString() ?? string.Empty),
                EscapeCsv(string.Join(';', item.ReferenceImageSetIds)),
                EscapeCsv(item.ExperimentSlug ?? string.Empty),
                EscapeCsv(FormatExperimentParameters(item.ExperimentParameters)),
                EscapeCsv(item.GenerationTaskId?.ToString() ?? string.Empty),
                EscapeCsv(item.OutputArtifactId?.ToString() ?? string.Empty),
                EscapeCsv(string.Join(';', item.SourceAssetIds)),
                EscapeCsv(string.Join(';', item.EvidenceAnchorIds)),
                EscapeCsv(string.Join(';', item.OperatorRunIds)),
                EscapeCsv(item.ArtifactRole ?? string.Empty),
                EscapeCsv(item.Blueprint?.Id.ToString() ?? string.Empty),
                EscapeCsv(item.Blueprint?.Key ?? string.Empty),
                EscapeCsv(item.Blueprint?.DisplayName ?? string.Empty),
                EscapeCsv(item.Blueprint?.Category ?? string.Empty),
                EscapeCsv(item.Blueprint?.SequenceMode ?? string.Empty),
                EscapeCsv(item.Blueprint?.ConsistencySummary ?? string.Empty),
                EscapeCsv(item.Blueprint?.VariationSummary ?? string.Empty)));
        }

        return builder.ToString();
    }

    private static string WriteReviewReport(string projectName, IReadOnlyList<DeliveryManifestItem> items)
    {
        var builder = new StringBuilder();
        builder.AppendLine($"# Review Report: {projectName}");
        builder.AppendLine();

        foreach (var item in items)
        {
            builder.AppendLine(
                $"- {item.Title}: {item.ReviewDecision}, humanApproved={item.HumanApproved}, reviewer={item.FinalReviewer ?? string.Empty}, notes={item.FinalApprovalNotes ?? string.Empty}, compositionReport={item.DeterministicCompositionReportPath ?? string.Empty}");
        }

        return builder.ToString();
    }

    private static string? CopyDeterministicCompositionReport(
        DeliveryPackageItem item,
        string compositionDirectory,
        string outputDirectory,
        string itemKey)
    {
        if (string.IsNullOrWhiteSpace(item.DeterministicCompositionReportPath))
        {
            return null;
        }

        if (!File.Exists(item.DeterministicCompositionReportPath))
        {
            throw new InvalidOperationException(
                $"Deterministic composition report was not found: {item.DeterministicCompositionReportPath}");
        }

        var extension = Path.GetExtension(item.DeterministicCompositionReportPath);
        if (string.IsNullOrWhiteSpace(extension))
        {
            extension = ".json";
        }

        var reportPath = Path.Combine(compositionDirectory, $"{itemKey}{extension}");
        File.Copy(item.DeterministicCompositionReportPath, reportPath, overwrite: true);
        return ToRelativePath(outputDirectory, reportPath);
    }

    private static string FormatExperimentParameters(IReadOnlyDictionary<string, string> parameters)
    {
        return string.Join(';', parameters.Select(parameter => $"{parameter.Key}={parameter.Value}"));
    }

    private static string EscapeCsv(string value)
    {
        if (!value.Contains(',') && !value.Contains('"') && !value.Contains('\n'))
        {
            return value;
        }

        return $"\"{value.Replace("\"", "\"\"")}\"";
    }
}

public sealed record DeliveryPackageRequest(
    string ProjectName,
    string OutputDirectory,
    IReadOnlyList<DeliveryPackageItem> Items);

public sealed record DeliveryPackageItem(
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
    string? DeterministicCompositionReportPath = null);

public sealed record DeliveryPackageResult(
    string PackageDirectory,
    string ManifestJsonPath,
    string ManifestCsvPath,
    string ReviewReportPath,
    IReadOnlyList<string> FinalImagePaths);

internal sealed record DeliveryManifest(
    string ProjectName,
    DateTimeOffset CreatedAt,
    IReadOnlyList<DeliveryManifestItem> Items);

internal sealed record DeliveryManifestItem(
    string ItemKey,
    string Title,
    string ImagePath,
    string PromptPath,
    string? MetadataPath,
    ReviewDecision ReviewDecision,
    bool HumanApproved,
    string? FinalReviewer,
    string? FinalApprovalNotes,
    DateTimeOffset? FinalApprovalDecidedAt,
    string? DeterministicCompositionReportPath,
    Guid? StyleGuideId,
    int? StyleGuideVersion,
    Guid? RecipeId,
    IReadOnlyList<Guid> ReferenceImageSetIds,
    string? ExperimentSlug,
    IReadOnlyDictionary<string, string> ExperimentParameters,
    Guid? GenerationTaskId,
    Guid? OutputArtifactId,
    IReadOnlyList<Guid> SourceAssetIds,
    IReadOnlyList<Guid> EvidenceAnchorIds,
    string? ArtifactRole,
    DeliveryBlueprintMetadata? Blueprint,
    IReadOnlyList<Guid> OperatorRunIds);
