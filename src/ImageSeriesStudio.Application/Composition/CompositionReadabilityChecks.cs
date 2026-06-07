namespace ImageSeriesStudio.Application.Composition;

public sealed class CompositionReadabilityCheckService
{
    private const float MinimumReadableFontSize = 14;
    private const int MaximumLabelCharacters = 48;

    public CompositionReadabilityCheckResult Check(CompositionReadabilityCheckRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.CanvasWidth <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(request.CanvasWidth), request.CanvasWidth, "Canvas width must be greater than zero.");
        }

        if (request.CanvasHeight <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(request.CanvasHeight), request.CanvasHeight, "Canvas height must be greater than zero.");
        }

        var overlays = request.Overlays ?? throw new ArgumentNullException(nameof(request.Overlays));
        var findings = new List<CompositionReadabilityFinding>();
        foreach (var overlay in overlays)
        {
            if (overlay.FontSize < MinimumReadableFontSize)
            {
                findings.Add(CreateFinding("text-too-small", overlay, $"Font size {overlay.FontSize} is below {MinimumReadableFontSize}."));
            }

            if (overlay.Kind is DeterministicTextOverlayKind.Label
                && overlay.Text.Trim().Length > MaximumLabelCharacters)
            {
                findings.Add(CreateFinding("label-too-long", overlay, "Label text is too long for fast scanning."));
            }

            if (overlay.Kind is DeterministicTextOverlayKind.Callout
                && string.IsNullOrWhiteSpace(overlay.Text))
            {
                findings.Add(CreateFinding("empty-callout", overlay, "Callout text cannot be empty."));
            }

            if (OverlayOverflowsCanvas(overlay, request.CanvasWidth, request.CanvasHeight))
            {
                findings.Add(CreateFinding("overlay-overflows-canvas", overlay, "Overlay extends outside the composition canvas."));
            }
        }

        return new CompositionReadabilityCheckResult(findings.Count == 0, findings);
    }

    private static bool OverlayOverflowsCanvas(
        CompositionOverlayLayout overlay,
        int canvasWidth,
        int canvasHeight)
    {
        return overlay.X < 0
            || overlay.Y < 0
            || overlay.X + overlay.MeasuredWidth > canvasWidth
            || overlay.Y + overlay.MeasuredHeight > canvasHeight;
    }

    private static CompositionReadabilityFinding CreateFinding(
        string code,
        CompositionOverlayLayout overlay,
        string message)
    {
        return new CompositionReadabilityFinding(code, overlay.Kind, overlay.Text.Trim(), message);
    }
}

public sealed record CompositionReadabilityCheckRequest(
    int CanvasWidth,
    int CanvasHeight,
    IReadOnlyList<CompositionOverlayLayout> Overlays);

public sealed record CompositionOverlayLayout(
    DeterministicTextOverlayKind Kind,
    string Text,
    float X,
    float Y,
    float FontSize,
    float MeasuredWidth,
    float MeasuredHeight);

public sealed record CompositionReadabilityCheckResult(
    bool Passed,
    IReadOnlyList<CompositionReadabilityFinding> Findings);

public sealed record CompositionReadabilityFinding(
    string Code,
    DeterministicTextOverlayKind Kind,
    string Text,
    string Message);
