namespace ContentDeliveryStudio.Application.Composition;

public sealed class PostRenderTextCompositionService
{
    private readonly IDeterministicTextComposer _composer;

    public PostRenderTextCompositionService(IDeterministicTextComposer composer)
    {
        _composer = composer ?? throw new ArgumentNullException(nameof(composer));
    }

    public Task<DeterministicTextCompositionResult> ComposeEducationalVisualAsync(
        PostRenderTextCompositionRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var backgroundPath = RequireText(request.BackgroundPath, nameof(request.BackgroundPath));
        var outputDirectory = RequireText(request.OutputDirectory, nameof(request.OutputDirectory));
        var artifactName = CreateArtifactSlug(request.ArtifactName);
        var overlays = request.Overlays ?? throw new ArgumentNullException(nameof(request.Overlays));
        if (overlays.Count == 0)
        {
            throw new ArgumentException("At least one deterministic text overlay is required.", nameof(request));
        }

        Directory.CreateDirectory(outputDirectory);

        return _composer.ComposeAsync(
            new DeterministicTextCompositionRequest(
                backgroundPath,
                Path.Combine(outputDirectory, $"{artifactName}.composed.png"),
                Path.Combine(outputDirectory, $"{artifactName}.layout-report.json"),
                overlays),
            cancellationToken);
    }

    private static string CreateArtifactSlug(string value)
    {
        var text = RequireText(value, nameof(value)).Trim().ToLowerInvariant();
        var characters = text.Select(character =>
            char.IsAsciiLetterOrDigit(character)
                ? character
                : '-').ToArray();
        var slug = new string(characters).Trim('-');

        while (slug.Contains("--", StringComparison.Ordinal))
        {
            slug = slug.Replace("--", "-", StringComparison.Ordinal);
        }

        return slug.Length == 0
            ? "composed-visual"
            : slug;
    }

    private static string RequireText(string value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Value cannot be empty.", parameterName);
        }

        return value.Trim();
    }
}

public interface IDeterministicTextComposer
{
    Task<DeterministicTextCompositionResult> ComposeAsync(
        DeterministicTextCompositionRequest request,
        CancellationToken cancellationToken);
}

public sealed record PostRenderTextCompositionRequest(
    string BackgroundPath,
    string OutputDirectory,
    string ArtifactName,
    IReadOnlyList<DeterministicTextOverlay> Overlays);

public sealed record DeterministicTextCompositionRequest(
    string BackgroundPath,
    string ComposedImagePath,
    string LayoutReportPath,
    IReadOnlyList<DeterministicTextOverlay> Overlays);

public sealed record DeterministicTextOverlay(
    string Text,
    float X,
    float Y,
    float FontSize,
    string HexColor,
    DeterministicTextOverlayKind Kind = DeterministicTextOverlayKind.Label);

public enum DeterministicTextOverlayKind
{
    Label = 0,
    Formula = 1,
    Legend = 2,
    Callout = 3,
}

public sealed record DeterministicTextCompositionResult(
    string ComposedImagePath,
    string LayoutReportPath,
    int OverlayCount,
    IReadOnlyList<string> Warnings);
