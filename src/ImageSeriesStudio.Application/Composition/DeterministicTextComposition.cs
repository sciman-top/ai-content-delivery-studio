namespace ImageSeriesStudio.Application.Composition;

public interface IDeterministicTextComposer
{
    Task<DeterministicTextCompositionResult> ComposeAsync(
        DeterministicTextCompositionRequest request,
        CancellationToken cancellationToken);
}

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
    string HexColor);

public sealed record DeterministicTextCompositionResult(
    string ComposedImagePath,
    string LayoutReportPath,
    int OverlayCount,
    IReadOnlyList<string> Warnings);
