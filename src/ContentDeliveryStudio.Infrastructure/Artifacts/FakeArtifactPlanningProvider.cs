using System.Text;
using ContentDeliveryStudio.Application.Artifacts;
using ContentDeliveryStudio.Core.Artifacts;

namespace ContentDeliveryStudio.Infrastructure.Artifacts;

public sealed class FakeArtifactPlanningProvider : IArtifactPlanningProvider
{
    public ArtifactPlanningProviderCapabilities Capabilities { get; } = new(
        "fake-artifact-planning",
        "Fake Artifact Planning Provider");

    public Task<ArtifactPlanningProviderResult> PlanAsync(
        ArtifactPlanningProviderRequest request,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var kinds = NormalizeKinds(request.Request.RequestedKinds);
        var title = EnsureText(request.Request.BriefTitle, "artifact");
        var slug = Slugify(title);
        var outputDirectory = NormalizeOutputDirectory(request.Request.OutputDirectory);
        var drafts = kinds
            .Select(kind => CreateDraft(kind, title, slug, outputDirectory, request))
            .ToArray();

        return Task.FromResult(new ArtifactPlanningProviderResult(
            drafts,
            ["Fake planner creates planned artifact records only; it does not render files."],
            "fake-artifact-planning"));
    }

    private static IReadOnlyList<OutputArtifactKind> NormalizeKinds(IReadOnlyList<OutputArtifactKind> kinds)
    {
        ArgumentNullException.ThrowIfNull(kinds);

        var normalized = kinds.Count == 0
            ? [OutputArtifactKind.Markdown, OutputArtifactKind.ReviewReport]
            : kinds.Distinct().ToArray();

        if (normalized.Any(kind => !Enum.IsDefined(typeof(OutputArtifactKind), kind)))
        {
            throw new ArgumentOutOfRangeException(nameof(kinds), "Requested artifact kind is not supported.");
        }

        return normalized;
    }

    private static ArtifactPlanDraft CreateDraft(
        OutputArtifactKind kind,
        string title,
        string slug,
        string outputDirectory,
        ArtifactPlanningProviderRequest request)
    {
        var (suffix, extension, mimeType, role) = GetArtifactDefaults(kind);
        var relativePath = $"{outputDirectory}/{slug}{suffix}{extension}";
        var metadata = new Dictionary<string, string>
        {
            ["briefTitle"] = title,
            ["providerId"] = "fake-artifact-planning",
            ["briefText"] = EnsureText(request.Request.BriefText, "No brief text supplied."),
        };

        return new ArtifactPlanDraft(
            kind,
            $"{title} {suffix.Trim('-')}".Trim(),
            relativePath,
            mimeType,
            role,
            request.SourceAssetIds,
            request.Request.EvidenceAnchorIds,
            metadata);
    }

    private static (string Suffix, string Extension, string MimeType, string Role) GetArtifactDefaults(OutputArtifactKind kind)
    {
        return kind switch
        {
            OutputArtifactKind.Image => ("-image", ".png", "image/png", "image-series-item"),
            OutputArtifactKind.Pdf => ("", ".pdf", "application/pdf", "final-report"),
            OutputArtifactKind.Docx => ("", ".docx", "application/vnd.openxmlformats-officedocument.wordprocessingml.document", "editable-document"),
            OutputArtifactKind.Markdown => ("", ".md", "text/markdown", "source-grounded-markdown"),
            OutputArtifactKind.ReviewReport => ("-review-report", ".md", "text/markdown", "review-evidence"),
            OutputArtifactKind.Manifest => ("-manifest", ".json", "application/json", "manifest"),
            OutputArtifactKind.Archive => ("", ".zip", "application/zip", "delivery-archive"),
            OutputArtifactKind.SlideDeck => ("", ".pptx", "application/vnd.openxmlformats-officedocument.presentationml.presentation", "slide-deck"),
            OutputArtifactKind.Spreadsheet => ("", ".xlsx", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "spreadsheet"),
            OutputArtifactKind.Text => ("", ".txt", "text/plain", "plain-text"),
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Artifact kind is not supported."),
        };
    }

    private static string EnsureText(string value, string fallback)
    {
        return string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
    }

    private static string NormalizeOutputDirectory(string outputDirectory)
    {
        var normalized = EnsureText(outputDirectory, "delivery").Replace('\\', '/').Trim('/');
        return normalized.Length == 0 ? "delivery" : normalized;
    }

    private static string Slugify(string value)
    {
        var builder = new StringBuilder();
        foreach (var character in value.Trim().ToLowerInvariant())
        {
            if (char.IsLetterOrDigit(character))
            {
                builder.Append(character);
            }
            else if (builder.Length > 0 && builder[^1] != '-')
            {
                builder.Append('-');
            }
        }

        return builder.ToString().Trim('-') is { Length: > 0 } slug ? slug : "artifact";
    }
}
