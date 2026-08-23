using System.Text.Json;
using ContentDeliveryStudio.Core.Providers;

namespace ContentDeliveryStudio.Application.Projects;

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
        var directory = LocalStudioDataPaths.ResolveWorkspaceProjectDirectory("review-prep", projectId);

        Directory.CreateDirectory(directory);
        // The second-precision timestamp plus title collides when two
        // same-titled candidates are prepared within one second; the unique
        // suffix keeps both manifests, and the temp+move landing keeps a
        // crash from leaving a truncated review-prep artifact behind.
        var fileName =
            $"{DateTimeOffset.UtcNow:yyyyMMddHHmmss}-{SanitizeFileName(itemTitle)}-{Guid.NewGuid().ToString("N")[..8]}-review-prep.json";
        var manifestPath = Path.Combine(directory, fileName);

        var manifest = new ReviewPrepArtifactManifest(
            itemTitle,
            assetPath,
            metadataPath,
            promptText,
            CreateEvidenceSelections(assetPath, metadataPath, promptText),
            DateTimeOffset.UtcNow);

        var payload = JsonSerializer.Serialize(manifest, JsonOptions);
        var temporaryPath = manifestPath + $".{Guid.NewGuid():N}.tmp";
        try
        {
            await File.WriteAllTextAsync(temporaryPath, payload, cancellationToken);
            File.Move(temporaryPath, manifestPath, overwrite: true);
        }
        finally
        {
            if (File.Exists(temporaryPath))
            {
                File.Delete(temporaryPath);
            }
        }

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
