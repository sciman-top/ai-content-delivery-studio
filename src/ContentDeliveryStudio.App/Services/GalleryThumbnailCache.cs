using System.IO;
using System.Security.Cryptography;
using System.Text;
using ContentDeliveryStudio.Application.Projects;
using SkiaSharp;

namespace ContentDeliveryStudio.App.Services;

public static class GalleryThumbnailCache
{
    private const int MaxThumbnailWidth = 112;
    private const int MaxThumbnailHeight = 84;
    private static readonly object CacheLock = new();

    public static string GetOrCreate(string assetPath)
    {
        if (string.IsNullOrWhiteSpace(assetPath))
        {
            throw new ArgumentException("Asset path cannot be empty.", nameof(assetPath));
        }

        var normalizedAssetPath = Path.GetFullPath(assetPath.Trim());
        if (!File.Exists(normalizedAssetPath))
        {
            throw new FileNotFoundException("Gallery asset was not found.", normalizedAssetPath);
        }

        var cacheDirectory = Path.Combine(
            LocalStudioDataPaths.ResolveStudioRoot(),
            "cache",
            "gallery-thumbnails");
        Directory.CreateDirectory(cacheDirectory);

        var fileInfo = new FileInfo(normalizedAssetPath);
        var cacheKey = BuildCacheKey(normalizedAssetPath, fileInfo.Length, fileInfo.LastWriteTimeUtc.Ticks);
        var thumbnailPath = Path.Combine(cacheDirectory, $"{cacheKey}.png");
        if (File.Exists(thumbnailPath))
        {
            return thumbnailPath;
        }

        lock (CacheLock)
        {
            if (File.Exists(thumbnailPath))
            {
                return thumbnailPath;
            }

            using var sourceStream = File.OpenRead(normalizedAssetPath);
            using var sourceBitmap = SKBitmap.Decode(sourceStream)
                ?? throw new InvalidOperationException($"Gallery asset could not be decoded: {normalizedAssetPath}");
            using var thumbnailBitmap = CreateThumbnailBitmap(sourceBitmap);
            using var thumbnailImage = SKImage.FromBitmap(thumbnailBitmap);
            using var encoded = thumbnailImage.Encode(SKEncodedImageFormat.Png, quality: 100);
            File.WriteAllBytes(thumbnailPath, encoded.ToArray());

            return thumbnailPath;
        }
    }

    private static string BuildCacheKey(string assetPath, long length, long lastWriteTicks)
    {
        var input = $"{assetPath}|{length}|{lastWriteTicks}";
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static SKBitmap CreateThumbnailBitmap(SKBitmap source)
    {
        var maxDimension = Math.Max(source.Width, source.Height);
        if (maxDimension <= Math.Max(MaxThumbnailWidth, MaxThumbnailHeight))
        {
            return source.Copy();
        }

        var scale = Math.Min((double)MaxThumbnailWidth / source.Width, (double)MaxThumbnailHeight / source.Height);
        var resizedWidth = Math.Max(1, (int)Math.Round(source.Width * scale));
        var resizedHeight = Math.Max(1, (int)Math.Round(source.Height * scale));
        return source.Resize(
            new SKImageInfo(resizedWidth, resizedHeight),
            new SKSamplingOptions(SKFilterMode.Linear))
            ?? throw new InvalidOperationException("Could not resize image for gallery thumbnail cache.");
    }
}
