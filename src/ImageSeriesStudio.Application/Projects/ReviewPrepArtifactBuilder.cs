using System.Text.Json;
using ImageSeriesStudio.Core.Providers;

namespace ImageSeriesStudio.Application.Projects;

public static class ReviewPrepArtifactBuilder
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
    };

    public static async Task<ReviewPrepArtifactContract> BuildAsync(
        Guid projectId,
        string itemTitle,
        string assetPath,
        string metadataPath,
        string promptText,
        CancellationToken cancellationToken)
    {
        var directory = LocalStudioDataPaths.ResolveProjectDirectory("review-prep", projectId);

        Directory.CreateDirectory(directory);
        var fileName = $"{DateTimeOffset.UtcNow:yyyyMMddHHmmss}-{SanitizeFileName(itemTitle)}-review-prep.json";
        var manifestPath = Path.Combine(directory, fileName);

        var manifest = new ReviewPrepArtifactManifest(
            itemTitle,
            assetPath,
            metadataPath,
            promptText,
            CreateEvidenceSelections(assetPath, metadataPath, promptText),
            DateTimeOffset.UtcNow);

        await File.WriteAllTextAsync(
            manifestPath,
            JsonSerializer.Serialize(manifest, JsonOptions),
            cancellationToken);

        return new ReviewPrepArtifactContract(
            Summary: VisionReviewExecutionPolicy.CreateCompactSummary(itemTitle, promptText),
            ManifestPath: manifestPath,
            ThumbnailGridPath: null,
            EvidenceSelections: manifest.EvidenceSelections);
    }

    private static IReadOnlyList<ReviewPrepEvidenceSelection> CreateEvidenceSelections(
        string assetPath,
        string metadataPath,
        string promptText)
    {
        return
        [
            new ReviewPrepEvidenceSelection(
                Role: "candidate-image",
                SourceKind: "generated-asset",
                LocalPath: assetPath,
                Summary: "Primary local candidate image selected for bounded remote review."),
            new ReviewPrepEvidenceSelection(
                Role: "candidate-metadata",
                SourceKind: "generation-metadata",
                LocalPath: metadataPath,
                Summary: "Local generation sidecar metadata kept as provenance evidence."),
            new ReviewPrepEvidenceSelection(
                Role: "prompt-summary",
                SourceKind: "prompt-text",
                LocalPath: null,
                Summary: VisionReviewExecutionPolicy.CreateCompactPromptSummary(promptText)),
        ];
    }

    private static string SanitizeFileName(string value)
    {
        var invalidChars = Path.GetInvalidFileNameChars();
        var sanitized = new string(value.Select(character => invalidChars.Contains(character) ? '-' : character).ToArray());
        return string.IsNullOrWhiteSpace(sanitized) ? "review-prep" : sanitized.Trim();
    }
}
