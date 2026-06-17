using System.Text.Json;
using ContentDeliveryStudio.Application.ToolAdapters;
using ContentDeliveryStudio.Infrastructure.Composition;
using ContentDeliveryStudio.Infrastructure.ToolAdapters;
using SkiaSharp;

namespace ContentDeliveryStudio.Tests;

public sealed class DeterministicTextCompositionToolAdapterTests
{
    [Fact]
    public async Task RunAsync_ComposesImageFromLabelSpec()
    {
        var rootDirectory = Path.Combine(Path.GetTempPath(), "ContentDeliveryStudio.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(rootDirectory);

        try
        {
            var backgroundPath = Path.Combine(rootDirectory, "background.png");
            var labelSpecPath = Path.Combine(rootDirectory, "labels.json");
            var composedImagePath = Path.Combine(rootDirectory, "outputs", "composed.png");
            var layoutReportPath = Path.Combine(rootDirectory, "outputs", "layout-report.json");

            using (var backgroundBitmap = new SKBitmap(320, 180))
            using (var backgroundCanvas = new SKCanvas(backgroundBitmap))
            {
                backgroundCanvas.Clear(SKColors.White);
                using var backgroundImage = SKImage.FromBitmap(backgroundBitmap);
                using var encodedBackground = backgroundImage.Encode(SKEncodedImageFormat.Png, quality: 100);
                await File.WriteAllBytesAsync(backgroundPath, encodedBackground.ToArray(), CancellationToken.None);
            }

            await File.WriteAllTextAsync(
                labelSpecPath,
                JsonSerializer.Serialize(new
                {
                    composedImagePath,
                    layoutReportPath,
                    overlays = new[]
                    {
                        new
                        {
                            text = "Series Title",
                            x = 16,
                            y = 20,
                            fontSize = 24,
                            hexColor = "#224466",
                        },
                    },
                }),
                CancellationToken.None);

            var adapter = new DeterministicTextCompositionToolAdapter(new SkiaDeterministicTextComposer());
            var request = ToolAdapterRunRequest.Create(
                adapter.Descriptor,
                dryRun: false,
                inputs: new Dictionary<string, string>
                {
                    ["backgroundPath"] = backgroundPath,
                    ["labelSpecPath"] = labelSpecPath,
                },
                DateTimeOffset.Parse("2026-06-07T08:30:00Z"));

            var result = await adapter.RunAsync(request, CancellationToken.None);

            Assert.Equal("deterministic-text-composition", result.ToolAdapterId);
            Assert.False(result.DryRun);
            Assert.Equal(composedImagePath, result.Outputs["composedImagePath"]);
            Assert.Equal(layoutReportPath, result.Outputs["layoutReportPath"]);
            Assert.True(File.Exists(composedImagePath));
            Assert.True(File.Exists(layoutReportPath));
            Assert.Contains("overlay", result.Summary, StringComparison.OrdinalIgnoreCase);

            var reportJson = await File.ReadAllTextAsync(layoutReportPath, CancellationToken.None);
            Assert.Contains("\"overlayCount\": 1", reportJson);
            Assert.Contains("\"text\": \"Series Title\"", reportJson);
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
    public async Task RunAsync_DryRunReturnsPlannedOutputsWithoutWritingFiles()
    {
        var rootDirectory = Path.Combine(Path.GetTempPath(), "ContentDeliveryStudio.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(rootDirectory);

        try
        {
            var backgroundPath = Path.Combine(rootDirectory, "background.png");
            var labelSpecPath = Path.Combine(rootDirectory, "labels.json");
            var composedImagePath = Path.Combine(rootDirectory, "outputs", "composed.png");
            var layoutReportPath = Path.Combine(rootDirectory, "outputs", "layout-report.json");

            using (var backgroundBitmap = new SKBitmap(320, 180))
            using (var backgroundCanvas = new SKCanvas(backgroundBitmap))
            {
                backgroundCanvas.Clear(SKColors.White);
                using var backgroundImage = SKImage.FromBitmap(backgroundBitmap);
                using var encodedBackground = backgroundImage.Encode(SKEncodedImageFormat.Png, quality: 100);
                await File.WriteAllBytesAsync(backgroundPath, encodedBackground.ToArray(), CancellationToken.None);
            }

            await File.WriteAllTextAsync(
                labelSpecPath,
                JsonSerializer.Serialize(new
                {
                    composedImagePath,
                    layoutReportPath,
                    overlays = new[]
                    {
                        new
                        {
                            text = "Dry Run Label",
                            x = 12,
                            y = 18,
                            fontSize = 20,
                            hexColor = "#335577",
                        },
                    },
                }),
                CancellationToken.None);

            var adapter = new DeterministicTextCompositionToolAdapter(new SkiaDeterministicTextComposer());
            var request = ToolAdapterRunRequest.Create(
                adapter.Descriptor,
                dryRun: true,
                inputs: new Dictionary<string, string>
                {
                    ["backgroundPath"] = backgroundPath,
                    ["labelSpecPath"] = labelSpecPath,
                },
                DateTimeOffset.Parse("2026-06-07T08:32:00Z"));

            var result = await adapter.RunAsync(request, CancellationToken.None);

            Assert.True(result.DryRun);
            Assert.Equal(composedImagePath, result.Outputs["composedImagePath"]);
            Assert.Equal(layoutReportPath, result.Outputs["layoutReportPath"]);
            Assert.False(File.Exists(composedImagePath));
            Assert.False(File.Exists(layoutReportPath));
            Assert.Contains("dry-run", result.Summary, StringComparison.OrdinalIgnoreCase);
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
    public async Task RunAsync_ForwardsReadabilityWarningsFromCompositionReport()
    {
        var rootDirectory = Path.Combine(Path.GetTempPath(), "ContentDeliveryStudio.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(rootDirectory);

        try
        {
            var backgroundPath = Path.Combine(rootDirectory, "background.png");
            var labelSpecPath = Path.Combine(rootDirectory, "labels.json");
            var composedImagePath = Path.Combine(rootDirectory, "outputs", "composed.png");
            var layoutReportPath = Path.Combine(rootDirectory, "outputs", "layout-report.json");

            using (var backgroundBitmap = new SKBitmap(120, 60))
            using (var backgroundCanvas = new SKCanvas(backgroundBitmap))
            {
                backgroundCanvas.Clear(SKColors.White);
                using var backgroundImage = SKImage.FromBitmap(backgroundBitmap);
                using var encodedBackground = backgroundImage.Encode(SKEncodedImageFormat.Png, quality: 100);
                await File.WriteAllBytesAsync(backgroundPath, encodedBackground.ToArray(), CancellationToken.None);
            }

            await File.WriteAllTextAsync(
                labelSpecPath,
                JsonSerializer.Serialize(new
                {
                    composedImagePath,
                    layoutReportPath,
                    overlays = new[]
                    {
                        new
                        {
                            text = "Tiny and long label",
                            x = 90,
                            y = 5,
                            fontSize = 8,
                            hexColor = "#224466",
                        },
                    },
                }),
                CancellationToken.None);

            var adapter = new DeterministicTextCompositionToolAdapter(new SkiaDeterministicTextComposer());
            var request = ToolAdapterRunRequest.Create(
                adapter.Descriptor,
                dryRun: false,
                inputs: new Dictionary<string, string>
                {
                    ["backgroundPath"] = backgroundPath,
                    ["labelSpecPath"] = labelSpecPath,
                },
                DateTimeOffset.Parse("2026-06-07T08:34:00Z"));

            var result = await adapter.RunAsync(request, CancellationToken.None);

            Assert.NotEmpty(result.Warnings);
            Assert.Contains(result.Warnings, warning => warning.Contains("font size", StringComparison.OrdinalIgnoreCase));
            Assert.Contains(result.Warnings, warning => warning.Contains("canvas", StringComparison.OrdinalIgnoreCase));
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
