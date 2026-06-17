using ContentDeliveryStudio.Application.Composition;
using ContentDeliveryStudio.Infrastructure.Composition;
using SkiaSharp;

namespace ContentDeliveryStudio.Tests;

public sealed class SkiaDeterministicTextComposerTests
{
    [Fact]
    public async Task ComposeAsync_WritesComposedImageAndLayoutReportForOverlay()
    {
        var rootDirectory = Path.Combine(Path.GetTempPath(), "ContentDeliveryStudio.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(rootDirectory);

        try
        {
            var backgroundPath = Path.Combine(rootDirectory, "background.png");
            var composedImagePath = Path.Combine(rootDirectory, "composed.png");
            var layoutReportPath = Path.Combine(rootDirectory, "layout-report.json");

            using (var backgroundBitmap = new SKBitmap(256, 128))
            using (var backgroundCanvas = new SKCanvas(backgroundBitmap))
            {
                backgroundCanvas.Clear(SKColors.White);
                using var backgroundImage = SKImage.FromBitmap(backgroundBitmap);
                using var encodedBackground = backgroundImage.Encode(SKEncodedImageFormat.Png, quality: 100);
                await File.WriteAllBytesAsync(backgroundPath, encodedBackground.ToArray(), CancellationToken.None);
            }

            var composer = new SkiaDeterministicTextComposer();
            var result = await composer.ComposeAsync(
                new DeterministicTextCompositionRequest(
                    backgroundPath,
                    composedImagePath,
                    layoutReportPath,
                    [
                        new DeterministicTextOverlay(
                            "Title",
                            X: 0,
                            Y: 0,
                            FontSize: 18,
                            HexColor: "#112233"),
                    ]),
                CancellationToken.None);

            Assert.True(File.Exists(composedImagePath));
            Assert.True(File.Exists(layoutReportPath));
            Assert.Equal(composedImagePath, result.ComposedImagePath);
            Assert.Equal(layoutReportPath, result.LayoutReportPath);
            Assert.Equal(1, result.OverlayCount);

            var reportJson = await File.ReadAllTextAsync(layoutReportPath, CancellationToken.None);
            Assert.Contains("\"overlayCount\": 1", reportJson);
            Assert.Contains("\"text\": \"Title\"", reportJson);
        }
        finally
        {
            if (Directory.Exists(rootDirectory))
            {
                Directory.Delete(rootDirectory, recursive: true);
            }
        }
    }

    [Fact]
    public async Task ComposeAsync_ReportsReadabilityWarningsForSmallOrOutOfBoundsText()
    {
        var rootDirectory = Path.Combine(Path.GetTempPath(), "ContentDeliveryStudio.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(rootDirectory);

        try
        {
            var backgroundPath = Path.Combine(rootDirectory, "background.png");
            var composedImagePath = Path.Combine(rootDirectory, "composed.png");
            var layoutReportPath = Path.Combine(rootDirectory, "layout-report.json");

            using (var backgroundBitmap = new SKBitmap(120, 60))
            using (var backgroundCanvas = new SKCanvas(backgroundBitmap))
            {
                backgroundCanvas.Clear(SKColors.White);
                using var backgroundImage = SKImage.FromBitmap(backgroundBitmap);
                using var encodedBackground = backgroundImage.Encode(SKEncodedImageFormat.Png, quality: 100);
                await File.WriteAllBytesAsync(backgroundPath, encodedBackground.ToArray(), CancellationToken.None);
            }

            var composer = new SkiaDeterministicTextComposer();
            var result = await composer.ComposeAsync(
                new DeterministicTextCompositionRequest(
                    backgroundPath,
                    composedImagePath,
                    layoutReportPath,
                    [
                        new DeterministicTextOverlay(
                            "Tiny and long label",
                            X: 90,
                            Y: 5,
                            FontSize: 8,
                            HexColor: "#112233"),
                    ]),
                CancellationToken.None);

            Assert.NotEmpty(result.Warnings);
            Assert.Contains(result.Warnings, warning => warning.Contains("font size", StringComparison.OrdinalIgnoreCase));
            Assert.Contains(result.Warnings, warning => warning.Contains("canvas", StringComparison.OrdinalIgnoreCase));

            var reportJson = await File.ReadAllTextAsync(layoutReportPath, CancellationToken.None);
            Assert.Contains("\"warnings\": [", reportJson);
            Assert.Contains("font size", reportJson, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("canvas", reportJson, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            if (Directory.Exists(rootDirectory))
            {
                Directory.Delete(rootDirectory, recursive: true);
            }
        }
    }
}
