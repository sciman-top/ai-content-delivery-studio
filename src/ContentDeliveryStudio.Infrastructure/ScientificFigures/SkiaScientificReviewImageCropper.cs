using ContentDeliveryStudio.Application.ScientificFigures;
using SkiaSharp;

namespace ContentDeliveryStudio.Infrastructure.ScientificFigures;

public sealed class SkiaScientificReviewImageCropper : IScientificReviewImageCropper
{
    public byte[] CropPng(
        byte[] sourcePng,
        int sourceWidth,
        int sourceHeight,
        ScientificPixelRegion region)
    {
        ArgumentNullException.ThrowIfNull(sourcePng);
        ArgumentNullException.ThrowIfNull(region);
        using var source = SKBitmap.Decode(sourcePng)
            ?? throw new InvalidOperationException(
                "Scientific review source PNG could not be decoded.");
        if (source.Width != sourceWidth || source.Height != sourceHeight)
        {
            throw new InvalidOperationException(
                "Scientific review source PNG dimensions do not match export metadata.");
        }

        var sourceRectangle = new SKRectI(
            region.X,
            region.Y,
            checked(region.X + region.Width),
            checked(region.Y + region.Height));
        if (sourceRectangle.Left < 0
            || sourceRectangle.Top < 0
            || sourceRectangle.Right > source.Width
            || sourceRectangle.Bottom > source.Height
            || sourceRectangle.Width <= 0
            || sourceRectangle.Height <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(region),
                "Scientific review crop is outside the full-resolution output.");
        }

        using var cropped = new SKBitmap(
            sourceRectangle.Width,
            sourceRectangle.Height,
            source.ColorType,
            source.AlphaType);
        using (var canvas = new SKCanvas(cropped))
        {
            canvas.DrawBitmap(
                source,
                sourceRectangle,
                new SKRect(0, 0, cropped.Width, cropped.Height));
            canvas.Flush();
        }

        using var image = SKImage.FromBitmap(cropped);
        using var encoded = image.Encode(SKEncodedImageFormat.Png, quality: 100)
            ?? throw new InvalidOperationException(
                "Scientific review crop could not be encoded.");
        return encoded.ToArray();
    }
}
