using System.Text;
using System.Text.Json;
using ContentDeliveryStudio.Application.Delivery;
using ContentDeliveryStudio.Core.Projects;
using ContentDeliveryStudio.Infrastructure.IO;

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
        var outputDirectory = NormalizeOutputDirectory(request.OutputDirectory);
        var stagingDirectory = CreateTemporarySiblingDirectory(outputDirectory, "staging");
        var backupDirectory = CreateTemporarySiblingDirectory(outputDirectory, "backup");

        try
        {
            var stagedPackage = await WritePackageContentsAsync(
                request,
                stagingDirectory,
                cancellationToken);
            PromoteStagedDirectory(stagingDirectory, outputDirectory, backupDirectory);

            return new DeliveryPackageResult(
                outputDirectory,
                Path.Combine(outputDirectory, "manifest.json"),
                Path.Combine(outputDirectory, "manifest.csv"),
                Path.Combine(outputDirectory, "review-report.md"),
                stagedPackage.FinalImageRelativePaths
                    .Select(relativePath => CombineRelativePath(outputDirectory, relativePath))
                    .ToArray());
        }
        finally
        {
            TryDeleteDirectory(stagingDirectory);
            TryDeleteDirectory(backupDirectory);
        }
    }

    private static async Task<StagedDeliveryPackage> WritePackageContentsAsync(
        DeliveryPackageRequest request,
        string packageDirectory,
        CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(packageDirectory);

        var imagesDirectory = Path.Combine(packageDirectory, "images");
        var promptsDirectory = Path.Combine(packageDirectory, "prompts");
        var metadataDirectory = Path.Combine(packageDirectory, "metadata");
        var compositionDirectory = Path.Combine(packageDirectory, "composition");

        Directory.CreateDirectory(imagesDirectory);
        Directory.CreateDirectory(promptsDirectory);
        Directory.CreateDirectory(metadataDirectory);
        Directory.CreateDirectory(compositionDirectory);

        var approvedItems = request.Items
            .Where(item => item.ReviewDecision is ReviewDecision.Pass && item.HumanApproved)
            .ToArray();

        var manifestItems = new List<DeliveryManifestItem>();
        var finalImageRelativePaths = new List<string>();
        var seenItemKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var item in approvedItems)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var itemKey = NormalizeKey(item.ItemKey);
            if (!seenItemKeys.Add(itemKey))
            {
                throw new InvalidOperationException(
                    $"Delivery package contains duplicate normalized item key '{itemKey}'.");
            }

            var imageExtension = Path.GetExtension(item.FinalImagePath);
            if (string.IsNullOrWhiteSpace(imageExtension))
            {
                imageExtension = ".png";
            }

            var imagePath = Path.Combine(imagesDirectory, $"{itemKey}{imageExtension}");
            var promptPath = Path.Combine(promptsDirectory, $"{itemKey}.txt");
            var metadataPath = Path.Combine(metadataDirectory, $"{itemKey}.json");
            var imageRelativePath = ToRelativePath(packageDirectory, imagePath);
            var promptRelativePath = ToRelativePath(packageDirectory, promptPath);
            var deterministicCompositionReportPath = await CopyDeterministicCompositionReportAsync(
                item,
                compositionDirectory,
                packageDirectory,
                itemKey,
                cancellationToken);

            await AtomicFileWriter.CopyFileAsync(item.FinalImagePath, imagePath, cancellationToken);
            await AtomicFileWriter.WriteAllTextAsync(promptPath, item.PromptText, cancellationToken);

            if (File.Exists(item.MetadataPath))
            {
                await AtomicFileWriter.CopyFileAsync(item.MetadataPath, metadataPath, cancellationToken);
            }

            finalImageRelativePaths.Add(imageRelativePath);
            manifestItems.Add(new DeliveryManifestItem(
                itemKey,
                item.Title,
                imageRelativePath,
                promptRelativePath,
                File.Exists(metadataPath) ? ToRelativePath(packageDirectory, metadataPath) : null,
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

        var manifestPath = Path.Combine(packageDirectory, "manifest.json");
        var csvPath = Path.Combine(packageDirectory, "manifest.csv");
        var reviewReportPath = Path.Combine(packageDirectory, "review-report.md");

        var manifest = new DeliveryManifest(
            request.ProjectName,
            DateTimeOffset.UtcNow,
            manifestItems);

        await AtomicFileWriter.WriteAllTextAsync(
            manifestPath,
            JsonSerializer.Serialize(manifest, ManifestJsonOptions),
            cancellationToken);
        await AtomicFileWriter.WriteAllTextAsync(csvPath, WriteManifestCsv(manifestItems), cancellationToken);
        await AtomicFileWriter.WriteAllTextAsync(
            reviewReportPath,
            WriteReviewReport(request.ProjectName, manifestItems),
            cancellationToken);

        return new StagedDeliveryPackage(finalImageRelativePaths);
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

    private static string CombineRelativePath(string rootDirectory, string relativePath)
    {
        return Path.Combine(
            rootDirectory,
            relativePath.Replace('/', Path.DirectorySeparatorChar));
    }

    private static string NormalizeOutputDirectory(string outputDirectory)
    {
        if (string.IsNullOrWhiteSpace(outputDirectory))
        {
            throw new ArgumentException("Output directory cannot be empty.", nameof(outputDirectory));
        }

        var normalizedOutputDirectory = Path.GetFullPath(outputDirectory.Trim());
        var parentDirectory = Path.GetDirectoryName(normalizedOutputDirectory)
            ?? throw new InvalidOperationException(
                $"Could not resolve the parent directory for '{outputDirectory}'.");
        Directory.CreateDirectory(parentDirectory);
        return normalizedOutputDirectory;
    }

    private static string CreateTemporarySiblingDirectory(string finalDirectory, string suffix)
    {
        var parentDirectory = Path.GetDirectoryName(finalDirectory)
            ?? throw new InvalidOperationException(
                $"Could not resolve the parent directory for '{finalDirectory}'.");
        var directoryName = Path.GetFileName(finalDirectory);
        return Path.Combine(parentDirectory, $".{directoryName}.{Guid.NewGuid():N}.{suffix}");
    }

    private static void PromoteStagedDirectory(
        string stagingDirectory,
        string finalDirectory,
        string backupDirectory)
    {
        if (!Directory.Exists(finalDirectory))
        {
            Directory.Move(stagingDirectory, finalDirectory);
            return;
        }

        Directory.Move(finalDirectory, backupDirectory);
        try
        {
            Directory.Move(stagingDirectory, finalDirectory);
            TryDeleteDirectory(backupDirectory);
        }
        catch
        {
            TryRestoreDirectory(finalDirectory, backupDirectory);
            throw;
        }
    }

    private static void TryRestoreDirectory(string finalDirectory, string backupDirectory)
    {
        try
        {
            if (!Directory.Exists(finalDirectory) && Directory.Exists(backupDirectory))
            {
                Directory.Move(backupDirectory, finalDirectory);
            }
        }
        catch
        {
            // Best-effort rollback only. The original failure should remain visible.
        }
    }

    private static void TryDeleteDirectory(string directoryPath)
    {
        try
        {
            if (Directory.Exists(directoryPath))
            {
                Directory.Delete(directoryPath, recursive: true);
            }
        }
        catch
        {
            // Best-effort cleanup only. The original failure should remain visible.
        }
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

    private static async Task<string?> CopyDeterministicCompositionReportAsync(
        DeliveryPackageItem item,
        string compositionDirectory,
        string outputDirectory,
        string itemKey,
        CancellationToken cancellationToken)
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
        await AtomicFileWriter.CopyFileAsync(item.DeterministicCompositionReportPath, reportPath, cancellationToken);
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

internal sealed record StagedDeliveryPackage(
    IReadOnlyList<string> FinalImageRelativePaths);
