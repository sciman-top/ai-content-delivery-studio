using ContentDeliveryStudio.Application.Composition;

namespace ContentDeliveryStudio.Tests;

public sealed class CompositionReadabilityCheckServiceTests
{
    [Fact]
    public void Check_FlagsReadabilityLabelAndCalloutFindings()
    {
        var service = new CompositionReadabilityCheckService();

        var result = service.Check(
            new CompositionReadabilityCheckRequest(
                CanvasWidth: 320,
                CanvasHeight: 180,
                Overlays:
                [
                    new CompositionOverlayLayout(
                        DeterministicTextOverlayKind.Label,
                        "This label is much too long for quick classroom scanning",
                        X: 16,
                        Y: 12,
                        FontSize: 10,
                        MeasuredWidth: 260,
                        MeasuredHeight: 12),
                    new CompositionOverlayLayout(
                        DeterministicTextOverlayKind.Callout,
                        " ",
                        X: 24,
                        Y: 48,
                        FontSize: 18,
                        MeasuredWidth: 0,
                        MeasuredHeight: 0),
                    new CompositionOverlayLayout(
                        DeterministicTextOverlayKind.Legend,
                        "Legend",
                        X: 300,
                        Y: 168,
                        FontSize: 18,
                        MeasuredWidth: 80,
                        MeasuredHeight: 20),
                ]));

        Assert.False(result.Passed);
        Assert.Contains(result.Findings, finding => finding.Code == "text-too-small");
        Assert.Contains(result.Findings, finding => finding.Code == "label-too-long");
        Assert.Contains(result.Findings, finding => finding.Code == "empty-callout");
        Assert.Contains(result.Findings, finding => finding.Code == "overlay-overflows-canvas");
    }
}
