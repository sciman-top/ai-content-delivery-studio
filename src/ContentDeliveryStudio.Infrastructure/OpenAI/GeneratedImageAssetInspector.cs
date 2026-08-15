using ContentDeliveryStudio.Core.Projects;
using SkiaSharp;

namespace ContentDeliveryStudio.Infrastructure.OpenAI;

internal static class GeneratedImageAssetInspector
{
    public static GeneratedImageAssetInfo Inspect(
        byte[] imageBytes,
        GenerationSettings requestedSettings,
        string requestedFormat)
    {
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
}

internal sealed record GeneratedImageAssetInfo(string Format, int Width, int Height)
{
    public string Size => $"{Width}x{Height}";
}
