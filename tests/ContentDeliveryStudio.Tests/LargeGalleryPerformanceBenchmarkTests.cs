using System.Diagnostics;
using System.Text.Json;
using ContentDeliveryStudio.Application.Delivery;
using ContentDeliveryStudio.App.Services;
using ContentDeliveryStudio.App.ViewModels;
using ContentDeliveryStudio.Core.Projects;
using ContentDeliveryStudio.Infrastructure.Delivery;
using SkiaSharp;

namespace ContentDeliveryStudio.Tests;

[Trait("Category", "ReleaseOnly")]
public sealed class LargeGalleryPerformanceBenchmarkTests
{
    private static readonly GalleryBenchmarkBudgets Budgets = new(
        RowPopulationMilliseconds: 1_000,
        ThumbnailWarmupMilliseconds: 30_000,
        CachedRevisitMilliseconds: 5_000,
        DeliveryExportMilliseconds: 30_000,
        PeakManagedBytes: 512L * 1024 * 1024);

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

        var report = new GalleryBenchmarkReport(
            rows.Length,
            populationStopwatch.ElapsedMilliseconds,
            warmupStopwatch.ElapsedMilliseconds,
            revisitStopwatch.ElapsedMilliseconds,
            exportStopwatch.ElapsedMilliseconds,
            peakManagedBytes,
            Budgets,
            benchmarkRoot);

        var reportPath = Path.Combine(benchmarkRoot, "large-gallery-benchmark.json");
        File.WriteAllText(reportPath, JsonSerializer.Serialize(report, new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            WriteIndented = true,
        }));

        Console.WriteLine(
            $"large-gallery-benchmark rows={report.RowCount} populationMs={report.RowPopulationMs} " +
            $"warmupMs={report.ThumbnailWarmupMs} revisitMs={report.CachedRevisitMs} " +
            $"exportMs={report.DeliveryExportMs} " +
            $"peakManagedMB={report.PeakManagedBytes / 1024d / 1024d:F2} report={reportPath}");

        Assert.Equal(1000, rows.Length);
        Assert.True(File.Exists(reportPath));
        AssertWithinBudget("row population", populationStopwatch.ElapsedMilliseconds, Budgets.RowPopulationMilliseconds);
        AssertWithinBudget("thumbnail warmup", warmupStopwatch.ElapsedMilliseconds, Budgets.ThumbnailWarmupMilliseconds);
        AssertWithinBudget("cached revisit", revisitStopwatch.ElapsedMilliseconds, Budgets.CachedRevisitMilliseconds);
        AssertWithinBudget("delivery export", exportStopwatch.ElapsedMilliseconds, Budgets.DeliveryExportMilliseconds);
        Assert.True(
            peakManagedBytes <= Budgets.PeakManagedBytes,
            $"Peak managed memory {peakManagedBytes} bytes exceeded budget {Budgets.PeakManagedBytes} bytes.");
        Assert.True(
            revisitStopwatch.ElapsedTicks * 4 <= warmupStopwatch.ElapsedTicks * 3,
            $"Cached thumbnail revisit ({revisitStopwatch.ElapsedMilliseconds} ms) was not at least 25% faster " +
            $"than initial warmup ({warmupStopwatch.ElapsedMilliseconds} ms).");
    }

    private static void AssertWithinBudget(string metric, long actualMilliseconds, long budgetMilliseconds)
    {
        Assert.True(
            actualMilliseconds <= budgetMilliseconds,
            $"Large-gallery {metric} took {actualMilliseconds} ms and exceeded the {budgetMilliseconds} ms budget.");
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

    private sealed record GalleryBenchmarkReport(
        int RowCount,
        long RowPopulationMs,
        long ThumbnailWarmupMs,
        long CachedRevisitMs,
        long DeliveryExportMs,
        long PeakManagedBytes,
        GalleryBenchmarkBudgets Budgets,
        string BenchmarkRoot);

    private sealed record GalleryBenchmarkBudgets(
        long RowPopulationMilliseconds,
        long ThumbnailWarmupMilliseconds,
        long CachedRevisitMilliseconds,
        long DeliveryExportMilliseconds,
        long PeakManagedBytes);
}
