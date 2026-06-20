using System.Diagnostics;
using System.Text.Json;
using ContentDeliveryStudio.Application.Delivery;
using ContentDeliveryStudio.App.Services;
using ContentDeliveryStudio.App.ViewModels;
using ContentDeliveryStudio.Core.Projects;
using ContentDeliveryStudio.Infrastructure.Delivery;
using ContentDeliveryStudio.Infrastructure.Import;
using SkiaSharp;

namespace ContentDeliveryStudio.Tests;

public sealed class LargeGalleryPerformanceBenchmarkTests
{
    [Fact]
    public async Task GalleryThumbnailBenchmark_RecordsRepeatableLocalMetrics()
    {
        using var localStudioRoot = LocalStudioDataPathScope.Create();
        var benchmarkRoot = Path.Combine(localStudioRoot.RootPath, "benchmarks", "large-gallery");
        Directory.CreateDirectory(benchmarkRoot);

        var sourceDirectory = Path.Combine(benchmarkRoot, "sources");
        Directory.CreateDirectory(sourceDirectory);
        var metadataPath = Path.Combine(sourceDirectory, "candidate.json");
        File.WriteAllText(metadataPath, """{"providerId":"fake-image"}""");

        var baseImagePath = Path.Combine(sourceDirectory, "base.png");
        WritePng(baseImagePath, width: 240, height: 160);

        var sourcePaths = Enumerable.Range(0, 1000)
            .Select(index =>
            {
                var path = Path.Combine(sourceDirectory, $"candidate-{index:0000}.png");
                File.Copy(baseImagePath, path, overwrite: true);
                File.WriteAllText(Path.ChangeExtension(path, ".json"), """{"providerId":"fake-image"}""");
                return path;
            })
            .ToArray();

        var populationStopwatch = Stopwatch.StartNew();
        var rows = sourcePaths
            .Select((assetPath, index) => new GalleryRowViewModel(
                Guid.NewGuid(),
                Guid.NewGuid(),
                $"Candidate {index:0000}",
                assetPath,
                metadataPath,
                $"Prompt {index:0000}"))
            .ToArray();
        populationStopwatch.Stop();
        var peakManagedBytes = GC.GetTotalMemory(forceFullCollection: false);

        var warmupStopwatch = Stopwatch.StartNew();
        foreach (var assetPath in sourcePaths)
        {
            var thumbnailPath = GalleryThumbnailCache.GetOrCreate(assetPath);
            Assert.True(File.Exists(thumbnailPath));
            peakManagedBytes = Math.Max(peakManagedBytes, GC.GetTotalMemory(forceFullCollection: false));
        }
        warmupStopwatch.Stop();
        peakManagedBytes = Math.Max(peakManagedBytes, GC.GetTotalMemory(forceFullCollection: false));

        var revisitStopwatch = Stopwatch.StartNew();
        foreach (var assetPath in sourcePaths)
        {
            var thumbnailPath = GalleryThumbnailCache.GetOrCreate(assetPath);
            Assert.True(File.Exists(thumbnailPath));
        }
        revisitStopwatch.Stop();
        peakManagedBytes = Math.Max(peakManagedBytes, GC.GetTotalMemory(forceFullCollection: false));

        var deliveryRoot = Path.Combine(benchmarkRoot, "delivery");
        var exportItems = sourcePaths
            .Select((assetPath, index) => new DeliveryPackageItem(
                $"candidate-{index:0000}",
                $"Candidate {index:0000}",
                assetPath,
                metadataPath,
                $"Prompt {index:0000}",
                ReviewDecision.Pass,
                HumanApproved: true))
            .ToArray();

        var exportStopwatch = Stopwatch.StartNew();
        var exportResult = await new DeliveryPackageWriter().WriteAsync(
            new DeliveryPackageRequest("Large gallery benchmark", deliveryRoot, exportItems),
            CancellationToken.None);
        exportStopwatch.Stop();
        peakManagedBytes = Math.Max(peakManagedBytes, GC.GetTotalMemory(forceFullCollection: false));

        Assert.Equal(1000, exportResult.FinalImagePaths.Count);
        Assert.True(File.Exists(exportResult.ManifestJsonPath));

        var finalizedManifestRoot = Path.Combine(benchmarkRoot, "outputs", "finalized-by-content");
        Directory.CreateDirectory(finalizedManifestRoot);
        var finalizedManifestPath = Path.Combine(finalizedManifestRoot, "finalized-manifest.csv");
        const int importRowLimit = 250;
        await File.WriteAllTextAsync(
            finalizedManifestPath,
            BuildFinalizedManifestCsv(benchmarkRoot, sourcePaths),
            CancellationToken.None);

        var importStopwatch = Stopwatch.StartNew();
        var importedRows = await new PhysicsPosterImportService().ImportFinalizedDeliveryAsync(
            benchmarkRoot,
            maxRows: importRowLimit,
            DateTimeOffset.UtcNow,
            CancellationToken.None);
        importStopwatch.Stop();
        peakManagedBytes = Math.Max(peakManagedBytes, GC.GetTotalMemory(forceFullCollection: false));

        var report = new GalleryBenchmarkReport(
            rows.Length,
            populationStopwatch.ElapsedMilliseconds,
            warmupStopwatch.ElapsedMilliseconds,
            revisitStopwatch.ElapsedMilliseconds,
            exportStopwatch.ElapsedMilliseconds,
            importStopwatch.ElapsedMilliseconds,
            importedRows.Count,
            importRowLimit,
            peakManagedBytes,
            benchmarkRoot);

        var reportPath = Path.Combine(benchmarkRoot, "large-gallery-benchmark.json");
        File.WriteAllText(reportPath, JsonSerializer.Serialize(report, new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            WriteIndented = true,
        }));

        Console.WriteLine(
            $"large-gallery-benchmark rows={report.RowCount} populationMs={report.RowPopulationMs} " +
            $"warmupMs={report.ThumbnailWarmupMs} revisitMs={report.CachedRevisitMs} " +
            $"exportMs={report.DeliveryExportMs} importMs={report.RowLimitedImportMs} " +
            $"importedRows={report.RowLimitedImportCount}/{report.RowLimitedImportLimit} " +
            $"peakManagedMB={report.PeakManagedBytes / 1024d / 1024d:F2} report={reportPath}");

        Assert.Equal(1000, rows.Length);
        Assert.Equal(importRowLimit, importedRows.Count);
        Assert.True(File.Exists(reportPath));
    }

    private static void WritePng(string path, int width, int height)
    {
        using var bitmap = new SKBitmap(width, height);
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(SKColors.SteelBlue);
        using var image = SKImage.FromBitmap(bitmap);
        using var encoded = image.Encode(SKEncodedImageFormat.Png, quality: 100);
        File.WriteAllBytes(path, encoded.ToArray());
    }

    private static string BuildFinalizedManifestCsv(string benchmarkRoot, IReadOnlyList<string> sourcePaths)
    {
        var builder = new System.Text.StringBuilder();
        builder.AppendLine("series,id,title_cn,content_dir,item_dir,prompt_source,prompt_snapshot,final_count,alternate_count,final_images,alternate_images,metadata_files,status,warnings");

        foreach (var assetPath in sourcePaths)
        {
            var itemKey = Path.GetFileNameWithoutExtension(assetPath);
            var metadataPath = Path.ChangeExtension(assetPath, ".json");
            builder.AppendLine(string.Join(
                ',',
                EscapeCsv("gallery"),
                EscapeCsv(itemKey),
                EscapeCsv(itemKey),
                EscapeCsv("sources"),
                EscapeCsv(itemKey),
                EscapeCsv($"prompts/{itemKey}.md"),
                EscapeCsv($"prompts/{itemKey}.md"),
                "1",
                "0",
                EscapeCsv(ToRelativePath(benchmarkRoot, assetPath)),
                string.Empty,
                EscapeCsv(ToRelativePath(benchmarkRoot, metadataPath)),
                EscapeCsv("ok"),
                string.Empty));
        }

        return builder.ToString();
    }

    private static string ToRelativePath(string rootDirectory, string path)
    {
        return Path.GetRelativePath(rootDirectory, path).Replace('\\', '/');
    }

    private static string EscapeCsv(string value)
    {
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n') || value.Contains('\r'))
        {
            return $"\"{value.Replace("\"", "\"\"")}\"";
        }

        return value;
    }

    private sealed record GalleryBenchmarkReport(
        int RowCount,
        long RowPopulationMs,
        long ThumbnailWarmupMs,
        long CachedRevisitMs,
        long DeliveryExportMs,
        long RowLimitedImportMs,
        int RowLimitedImportCount,
        int RowLimitedImportLimit,
        long PeakManagedBytes,
        string BenchmarkRoot);
}
