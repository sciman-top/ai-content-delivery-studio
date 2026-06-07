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
    int OverlayCount);
