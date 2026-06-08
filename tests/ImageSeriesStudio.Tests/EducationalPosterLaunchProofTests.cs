using System.Text.Json;
using ImageSeriesStudio.Application.Delivery;
using ImageSeriesStudio.Application.ToolAdapters;
using ImageSeriesStudio.Core.Projects;
using ImageSeriesStudio.Infrastructure.Composition;
using ImageSeriesStudio.Infrastructure.Delivery;
using ImageSeriesStudio.Infrastructure.ToolAdapters;
using SkiaSharp;

namespace ImageSeriesStudio.Tests;

public sealed class EducationalPosterLaunchProofTests
{
    [Fact]
    public async Task EducationalPosterProofPath_ExportsCompositionProvenanceAndApprovalEvidence()
    {
        var rootDirectory = Path.Combine(Path.GetTempPath(), "ImageSeriesStudio.Tests", Guid.NewGuid().ToString("N"));
        var sourceDirectory = Path.Combine(rootDirectory, "source");
        var packageDirectory = Path.Combine(rootDirectory, "delivery");
        Directory.CreateDirectory(sourceDirectory);

        try
        {
            var backgroundPath = Path.Combine(sourceDirectory, "background.png");
            var labelSpecPath = Path.Combine(sourceDirectory, "labels.json");
            var composedImagePath = Path.Combine(sourceDirectory, "poster-final.png");
            var layoutReportPath = Path.Combine(sourceDirectory, "poster-layout-report.json");
            var metadataPath = Path.Combine(sourceDirectory, "poster-final.json");

            using (var backgroundBitmap = new SKBitmap(640, 360))
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
                            text = "Archimedes Principle",
                            x = 24,
                            y = 28,
                            fontSize = 28,
                            hexColor = "#224466",
                        },
                        new
                        {
                            text = "Buoyant force equals displaced fluid weight.",
                            x = 24,
                            y = 84,
                            fontSize = 18,
                            hexColor = "#335577",
                        },
                    },
                }),
                CancellationToken.None);

            var compositionAdapter = new DeterministicTextCompositionToolAdapter(new SkiaDeterministicTextComposer());
            var compositionResult = await compositionAdapter.RunAsync(
                ToolAdapterRunRequest.Create(
                    compositionAdapter.Descriptor,
                    dryRun: false,
                    inputs: new Dictionary<string, string>
                    {
                        ["backgroundPath"] = backgroundPath,
                        ["labelSpecPath"] = labelSpecPath,
                    },
                    DateTimeOffset.Parse("2026-06-09T10:00:00Z")),
                CancellationToken.None);

            await File.WriteAllTextAsync(
                metadataPath,
                JsonSerializer.Serialize(new
                {
                    providerId = "deterministic-text-composition",
                    textPolicy = "DeterministicPostRender",
                    composedImagePath,
                    deterministicComposition = new
                    {
                        layoutReportPath,
                        overlayCount = 2,
                    },
                }),
                CancellationToken.None);

            var writer = new DeliveryPackageWriter();
            var export = await writer.WriteAsync(
                new DeliveryExportRequest(
                    "Educational poster proof",
                    packageDirectory,
                    [
                        new DeliveryExportItem(
                            "poster-proof",
                            "Archimedes poster",
                            compositionResult.Outputs["composedImagePath"],
                            metadataPath,
                            "Create a classroom-safe background with deterministic labels reserved for the final educational poster.",
                            ReviewDecision.Pass,
                            HumanApproved: true,
                            FinalReviewer: "Teacher",
                            FinalApprovalNotes: "Ready for classroom delivery.",
                            FinalApprovalDecidedAt: DateTimeOffset.Parse("2026-06-09T10:05:00Z"),
                            ArtifactRole: "educational-poster",
                            DeterministicCompositionReportPath: compositionResult.Outputs["layoutReportPath"]),
                    ]),
                CancellationToken.None);

            var copiedReportPath = Path.Combine(export.PackageDirectory, "composition", "poster-proof.json");
            var copiedMetadataPath = Path.Combine(export.PackageDirectory, "metadata", "poster-proof.json");

            Assert.True(File.Exists(export.ManifestJsonPath));
            Assert.True(File.Exists(export.ReviewReportPath));
            Assert.True(File.Exists(copiedReportPath));
            Assert.True(File.Exists(copiedMetadataPath));

            using var manifestStream = File.OpenRead(export.ManifestJsonPath);
            using var manifest = await JsonDocument.ParseAsync(manifestStream, cancellationToken: CancellationToken.None);
            var item = manifest.RootElement.GetProperty("items")[0];
            Assert.Equal("composition/poster-proof.json", item.GetProperty("deterministicCompositionReportPath").GetString());
            Assert.Equal("Teacher", item.GetProperty("finalReviewer").GetString());
            Assert.Equal("Ready for classroom delivery.", item.GetProperty("finalApprovalNotes").GetString());

            var copiedReportJson = await File.ReadAllTextAsync(copiedReportPath, CancellationToken.None);
            Assert.Contains("\"overlayCount\": 2", copiedReportJson);
            Assert.Contains("Archimedes Principle", copiedReportJson);

            var copiedMetadataJson = await File.ReadAllTextAsync(copiedMetadataPath, CancellationToken.None);
            Assert.Contains("\"textPolicy\":\"DeterministicPostRender\"", copiedMetadataJson);
            Assert.Contains("\"layoutReportPath\"", copiedMetadataJson);

            var reviewReport = await File.ReadAllTextAsync(export.ReviewReportPath, CancellationToken.None);
            Assert.Contains("reviewer=Teacher", reviewReport);
            Assert.Contains("notes=Ready for classroom delivery.", reviewReport);
            Assert.Contains("compositionReport=composition/poster-proof.json", reviewReport);
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
