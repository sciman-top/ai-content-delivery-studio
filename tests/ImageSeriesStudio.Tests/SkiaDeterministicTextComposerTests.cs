using ImageSeriesStudio.Application.Composition;
using ImageSeriesStudio.Infrastructure.Composition;
using SkiaSharp;

namespace ImageSeriesStudio.Tests;

public sealed class SkiaDeterministicTextComposerTests
{
    [Fact]
    public async Task ComposeAsync_WritesComposedImageAndLayoutReportForOverlay()
    {
        var rootDirectory = Path.Combine(Path.GetTempPath(), "ImageSeriesStudio.Tests", Guid.NewGuid().ToString("N"));
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
    public async Task ComposeAsync_ReportsOverlayKindsForLabelsFormulasLegendsAndCallouts()
    {
        var rootDirectory = Path.Combine(Path.GetTempPath(), "ImageSeriesStudio.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(rootDirectory);

        try
        {
            var backgroundPath = Path.Combine(rootDirectory, "background.png");
            var composedImagePath = Path.Combine(rootDirectory, "composed.png");
            var layoutReportPath = Path.Combine(rootDirectory, "layout-report.json");
            await WriteBackgroundAsync(backgroundPath);

            var composer = new SkiaDeterministicTextComposer();
            var result = await composer.ComposeAsync(
                new DeterministicTextCompositionRequest(
                    backgroundPath,
                    composedImagePath,
                    layoutReportPath,
                    [
                        new DeterministicTextOverlay("Velocity", 12, 8, 18, "#111111", DeterministicTextOverlayKind.Label),
                        new DeterministicTextOverlay("v = s / t", 12, 36, 18, "#222222", DeterministicTextOverlayKind.Formula),
                        new DeterministicTextOverlay("Blue = fast", 12, 64, 18, "#333333", DeterministicTextOverlayKind.Legend),
                        new DeterministicTextOverlay("Check the slope", 12, 92, 18, "#444444", DeterministicTextOverlayKind.Callout),
                    ]),
                CancellationToken.None);

            Assert.Equal(4, result.OverlayCount);

            var reportJson = await File.ReadAllTextAsync(layoutReportPath, CancellationToken.None);
            Assert.Contains("\"kind\": \"label\"", reportJson);
            Assert.Contains("\"kind\": \"formula\"", reportJson);
            Assert.Contains("\"kind\": \"legend\"", reportJson);
            Assert.Contains("\"kind\": \"callout\"", reportJson);
        }
        finally
        {
            if (Directory.Exists(rootDirectory))
            {
                Directory.Delete(rootDirectory, recursive: true);
            }
        }
    }

    private static async Task WriteBackgroundAsync(string backgroundPath)
    {
        using var backgroundBitmap = new SKBitmap(256, 128);
        using var backgroundCanvas = new SKCanvas(backgroundBitmap);
        backgroundCanvas.Clear(SKColors.White);
        using var backgroundImage = SKImage.FromBitmap(backgroundBitmap);
        using var encodedBackground = backgroundImage.Encode(SKEncodedImageFormat.Png, quality: 100);
        await File.WriteAllBytesAsync(backgroundPath, encodedBackground.ToArray(), CancellationToken.None);
    }
}
