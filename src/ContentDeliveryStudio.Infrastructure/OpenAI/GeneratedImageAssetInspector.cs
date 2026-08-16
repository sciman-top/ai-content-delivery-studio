using ContentDeliveryStudio.Core.Projects;
using SkiaSharp;

namespace ContentDeliveryStudio.Infrastructure.OpenAI;

internal static class GeneratedImageAssetInspector
{
    private const int MaximumGeneratedImageBytes = 50 * 1024 * 1024;
    private const int MaximumBase64Characters = ((MaximumGeneratedImageBytes + 2) / 3) * 4;

    public static byte[] DecodeBase64(string imageBase64)
    {
        if (string.IsNullOrWhiteSpace(imageBase64))
        {
            throw new InvalidOperationException("Generated image response did not contain base64 image data.");
        }

        if (imageBase64.Length > MaximumBase64Characters)
        {
            throw new InvalidOperationException("Generated image exceeded the bounded 50 MB output limit.");
        }

        byte[] imageBytes;
        try
        {
            imageBytes = Convert.FromBase64String(imageBase64);
        }
        catch (FormatException exception)
        {
            throw new InvalidOperationException("Generated image response contained invalid base64 image data.", exception);
        }

        EnsureBoundedSize(imageBytes);
        return imageBytes;
    }

    public static GeneratedImageAssetInfo Inspect(
        byte[] imageBytes,
        GenerationSettings requestedSettings,
        string requestedFormat)
    {
        ArgumentNullException.ThrowIfNull(imageBytes);
        EnsureBoundedSize(imageBytes);

        using var stream = new MemoryStream(imageBytes, writable: false);
        using var codec = SKCodec.Create(stream)
            ?? throw new InvalidOperationException("Generated image bytes could not be decoded.");

        var deliveredFormat = codec.EncodedFormat switch
        {
            SKEncodedImageFormat.Png => "png",
            SKEncodedImageFormat.Jpeg => "jpeg",
            SKEncodedImageFormat.Webp => "webp",
            _ => throw new InvalidOperationException(
                $"Decoded generated image format '{codec.EncodedFormat}' is not supported."),
        };

        if (!deliveredFormat.Equals(requestedFormat, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"Decoded generated image format '{deliveredFormat}' does not match requested format '{requestedFormat}'.");
        }

        var width = codec.Info.Width;
        var height = codec.Info.Height;
        if (requestedSettings.Width > 0
            && requestedSettings.Height > 0
            && (width != requestedSettings.Width || height != requestedSettings.Height))
        {
            throw new InvalidOperationException(
                $"Decoded generated image size {width}x{height} does not match requested size "
                + $"{requestedSettings.Width}x{requestedSettings.Height}.");
        }

        return new GeneratedImageAssetInfo(deliveredFormat, width, height);
    }

    private static void EnsureBoundedSize(byte[] imageBytes)
    {
        if (imageBytes.Length == 0 || imageBytes.Length > MaximumGeneratedImageBytes)
        {
            throw new InvalidOperationException("Generated image must be non-empty and no larger than 50 MB.");
        }
    }
}

internal sealed record GeneratedImageAssetInfo(string Format, int Width, int Height)
{
    public string Size => $"{Width}x{Height}";
}
