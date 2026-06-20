using ContentDeliveryStudio.App.Services;
using SkiaSharp;

namespace ContentDeliveryStudio.Tests;

public sealed class GalleryThumbnailCacheTests
{
    [Fact]
    public void GetOrCreate_GeneratesCachedThumbnailAndReusesIt()
    {
        using var localStudioRoot = LocalStudioDataPathScope.Create();
        var sourceDirectory = Path.Combine(localStudioRoot.RootPath, "source-images");
        Directory.CreateDirectory(sourceDirectory);
        var sourcePath = Path.Combine(sourceDirectory, "candidate.png");
        WritePng(sourcePath, width: 640, height: 360);

        var firstThumbnailPath = GalleryThumbnailCache.GetOrCreate(sourcePath);
        var firstWriteTime = File.GetLastWriteTimeUtc(firstThumbnailPath);
        var secondThumbnailPath = GalleryThumbnailCache.GetOrCreate(sourcePath);

        Assert.Equal(firstThumbnailPath, secondThumbnailPath);
        Assert.True(File.Exists(firstThumbnailPath));
        Assert.Contains(Path.Combine("cache", "gallery-thumbnails"), firstThumbnailPath);
        Assert.Equal(firstWriteTime, File.GetLastWriteTimeUtc(secondThumbnailPath));

        using var thumbnail = SKBitmap.Decode(firstThumbnailPath);
        Assert.NotNull(thumbnail);
        Assert.True(thumbnail.Width <= 112);
        Assert.True(thumbnail.Height <= 84);
    }

    [Fact]
    public void GetOrCreate_UsesSourceMetadataToInvalidateStaleCacheEntries()
    {
        using var localStudioRoot = LocalStudioDataPathScope.Create();
        var sourceDirectory = Path.Combine(localStudioRoot.RootPath, "source-images");
        Directory.CreateDirectory(sourceDirectory);
        var sourcePath = Path.Combine(sourceDirectory, "candidate.png");
        WritePng(sourcePath, width: 640, height: 360);

        var firstThumbnailPath = GalleryThumbnailCache.GetOrCreate(sourcePath);
        var firstCacheFileName = Path.GetFileName(firstThumbnailPath);

        WritePng(sourcePath, width: 640, height: 360);
        File.SetLastWriteTimeUtc(sourcePath, DateTimeOffset.UtcNow.AddMinutes(2).UtcDateTime);

        var secondThumbnailPath = GalleryThumbnailCache.GetOrCreate(sourcePath);
        var secondCacheFileName = Path.GetFileName(secondThumbnailPath);

        Assert.NotEqual(firstCacheFileName, secondCacheFileName);
        Assert.True(File.Exists(firstThumbnailPath));
        Assert.True(File.Exists(secondThumbnailPath));
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
}
