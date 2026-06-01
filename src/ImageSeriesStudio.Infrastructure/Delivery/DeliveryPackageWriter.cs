using System.Text;
using System.Text.Json;
using ImageSeriesStudio.Application.Delivery;
using ImageSeriesStudio.Core.Projects;

namespace ImageSeriesStudio.Infrastructure.Delivery;

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

        Directory.CreateDirectory(imagesDirectory);
        Directory.CreateDirectory(promptsDirectory);
        Directory.CreateDirectory(metadataDirectory);

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
                item.StyleGuideId,
                item.StyleGuideVersion,
                item.RecipeId,
                item.ReferenceImageSetIds ?? [],
                item.ExperimentSlug,
                item.ExperimentParameters ?? new Dictionary<string, string>(),
                item.GenerationTaskId));
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
                        item.StyleGuideId,
                        item.StyleGuideVersion,
                        item.RecipeId,
                        item.ReferenceImageSetIds,
                        item.ExperimentSlug,
                        item.ExperimentParameters,
                        item.GenerationTaskId))
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
        builder.AppendLine("itemKey,title,imagePath,promptPath,metadataPath,reviewDecision,humanApproved,styleGuideId,styleGuideVersion,recipeId,referenceImageSetIds,experimentSlug,experimentParameters,generationTaskId");

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
                EscapeCsv(item.StyleGuideId?.ToString() ?? string.Empty),
                item.StyleGuideVersion?.ToString() ?? string.Empty,
                EscapeCsv(item.RecipeId?.ToString() ?? string.Empty),
                EscapeCsv(string.Join(';', item.ReferenceImageSetIds)),
                EscapeCsv(item.ExperimentSlug ?? string.Empty),
                EscapeCsv(FormatExperimentParameters(item.ExperimentParameters)),
                EscapeCsv(item.GenerationTaskId?.ToString() ?? string.Empty)));
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
            builder.AppendLine($"- {item.Title}: {item.ReviewDecision}, humanApproved={item.HumanApproved}");
        }

        return builder.ToString();
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
    Guid? StyleGuideId = null,
    int? StyleGuideVersion = null,
    Guid? RecipeId = null,
    IReadOnlyList<Guid>? ReferenceImageSetIds = null,
    string? ExperimentSlug = null,
    IReadOnlyDictionary<string, string>? ExperimentParameters = null,
    Guid? GenerationTaskId = null);

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
    Guid? StyleGuideId,
    int? StyleGuideVersion,
    Guid? RecipeId,
    IReadOnlyList<Guid> ReferenceImageSetIds,
    string? ExperimentSlug,
    IReadOnlyDictionary<string, string> ExperimentParameters,
    Guid? GenerationTaskId);
