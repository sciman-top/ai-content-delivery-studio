using System.Globalization;
using System.Text.Json;
using ImageSeriesStudio.Application.Composition;
using SkiaSharp;

namespace ImageSeriesStudio.Infrastructure.Composition;

public sealed class SkiaDeterministicTextComposer : IDeterministicTextComposer
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
    };

    public async Task<DeterministicTextCompositionResult> ComposeAsync(
        DeterministicTextCompositionRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var backgroundPath = RequirePath(request.BackgroundPath, nameof(request.BackgroundPath));
        var composedImagePath = RequirePath(request.ComposedImagePath, nameof(request.ComposedImagePath));
        var layoutReportPath = RequirePath(request.LayoutReportPath, nameof(request.LayoutReportPath));
        var overlays = request.Overlays ?? throw new ArgumentNullException(nameof(request.Overlays));
        if (overlays.Count == 0)
        {
            throw new ArgumentException("At least one overlay is required.", nameof(request));
        }

        if (!File.Exists(backgroundPath))
        {
            throw new FileNotFoundException("Background image was not found.", backgroundPath);
        }

        var backgroundBytes = await File.ReadAllBytesAsync(backgroundPath, cancellationToken);
        using var backgroundBitmap = SKBitmap.Decode(backgroundBytes)
            ?? throw new InvalidOperationException($"Background image could not be decoded: {backgroundPath}");
        using var composedBitmap = backgroundBitmap.Copy();
        using var canvas = new SKCanvas(composedBitmap);

        var layoutEntries = new List<DeterministicTextOverlayReport>(overlays.Count);
        var warnings = new List<string>();
        foreach (var overlay in overlays)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var text = RequireText(overlay.Text, nameof(overlay.Text));
            var fontSize = overlay.FontSize > 0
                ? overlay.FontSize
                : throw new ArgumentOutOfRangeException(nameof(overlay.FontSize), overlay.FontSize, "Font size must be greater than zero.");

            using var paint = new SKPaint
            {
                Color = ParseColor(overlay.HexColor),
                IsAntialias = true,
            };
            using var font = new SKFont
            {
                Size = fontSize,
            };

            var bounds = new SKRect();
            font.MeasureText(text, out bounds, paint);
            var baselineY = overlay.Y + fontSize;
            canvas.DrawText(text, overlay.X, baselineY, SKTextAlign.Left, font, paint);

            var overlayReport = new DeterministicTextOverlayReport(
                text,
                overlay.X,
                overlay.Y,
                baselineY,
                fontSize,
                overlay.HexColor.Trim(),
                bounds.Width,
                bounds.Height);
            layoutEntries.Add(overlayReport);
            warnings.AddRange(EvaluateWarnings(overlayReport, composedBitmap.Width, composedBitmap.Height));
        }

        EnsureParentDirectory(composedImagePath);
        EnsureParentDirectory(layoutReportPath);

        using var composedImage = SKImage.FromBitmap(composedBitmap);
        using var encoded = composedImage.Encode(SKEncodedImageFormat.Png, quality: 100);
        await File.WriteAllBytesAsync(composedImagePath, encoded.ToArray(), cancellationToken);

        var layoutReport = new DeterministicTextCompositionReport(
            backgroundPath,
            composedImagePath,
            composedBitmap.Width,
            composedBitmap.Height,
            layoutEntries,
            warnings.Distinct(StringComparer.Ordinal).ToArray());
        await File.WriteAllTextAsync(
            layoutReportPath,
            JsonSerializer.Serialize(layoutReport, JsonOptions),
            cancellationToken);

        return new DeterministicTextCompositionResult(
            composedImagePath,
            layoutReportPath,
            layoutEntries.Count,
            warnings.Distinct(StringComparer.Ordinal).ToArray());
    }

    private static string RequirePath(string value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Path cannot be empty.", parameterName);
        }

        return value.Trim();
    }

    private static string RequireText(string value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Text cannot be empty.", parameterName);
        }

        return value.Trim();
    }

    private static void EnsureParentDirectory(string path)
    {
        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }
    }

    private static SKColor ParseColor(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Overlay color cannot be empty.", nameof(value));
        }

        var normalized = value.Trim().TrimStart('#');
        if (normalized.Length != 6 ||
            !uint.TryParse(normalized, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var rgb))
        {
            throw new ArgumentException("Overlay color must use #RRGGBB format.", nameof(value));
        }

        return new SKColor(
            (byte)((rgb >> 16) & 0xFF),
            (byte)((rgb >> 8) & 0xFF),
            (byte)(rgb & 0xFF));
    }

    private static IReadOnlyList<string> EvaluateWarnings(
        DeterministicTextOverlayReport overlay,
        int canvasWidth,
        int canvasHeight)
    {
        var warnings = new List<string>();
        if (overlay.FontSize < 12)
        {
            warnings.Add($"Overlay '{overlay.Text}' uses font size {overlay.FontSize}, which may be unreadable.");
        }

        var extendsOutsideCanvas =
            overlay.X < 0
            || overlay.Y < 0
            || overlay.X + overlay.MeasuredWidth > canvasWidth
            || overlay.Y + overlay.MeasuredHeight > canvasHeight;
        if (extendsOutsideCanvas)
        {
            warnings.Add($"Overlay '{overlay.Text}' extends beyond canvas bounds and may clip labels or callouts.");
        }

        return warnings;
    }
}

internal sealed record DeterministicTextCompositionReport(
    string BackgroundPath,
    string ComposedImagePath,
    int Width,
    int Height,
    IReadOnlyList<DeterministicTextOverlayReport> Overlays,
    IReadOnlyList<string> Warnings)
{
    public int OverlayCount => Overlays.Count;
}

internal sealed record DeterministicTextOverlayReport(
    string Text,
    float X,
    float Y,
    float BaselineY,
    float FontSize,
    string HexColor,
    float MeasuredWidth,
    float MeasuredHeight);
