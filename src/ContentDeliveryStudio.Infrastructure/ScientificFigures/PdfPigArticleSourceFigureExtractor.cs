using System.Security.Cryptography;
using ContentDeliveryStudio.Application.ScientificFigures;
using ContentDeliveryStudio.Infrastructure.Sources;
using SkiaSharp;
using UglyToad.PdfPig;

namespace ContentDeliveryStudio.Infrastructure.ScientificFigures;

public sealed class PdfPigArticleSourceFigureExtractor : IArticleSourceFigureExtractor
{
    private readonly SourceIngestionBudget _budget;

    public PdfPigArticleSourceFigureExtractor()
        : this(SourceIngestionBudget.Default)
    {
    }

    internal PdfPigArticleSourceFigureExtractor(SourceIngestionBudget budget)
    {
        _budget = budget ?? throw new ArgumentNullException(nameof(budget));
    }

    public ArticleSourceFigureAudit Extract(string sourcePdfPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourcePdfPath);
        if (!File.Exists(sourcePdfPath))
        {
            throw new FileNotFoundException("Article PDF was not found.", sourcePdfPath);
        }

        _budget.ValidateSourceFile(sourcePdfPath);

        var assets = new List<ArticleSourceFigureAsset>();
        var extractedFigureCount = 0;
        long extractedFigureBytes = 0;
        using var document = PdfDocument.Open(sourcePdfPath);
        _budget.ValidatePdfPageCount(document.NumberOfPages);
        foreach (var page in document.GetPages())
        {
            var imageIndex = 0;
            foreach (var image in page.GetImages())
            {
                imageIndex++;
                if (image.WidthInSamples < 200
                    || image.HeightInSamples < 150)
                {
                    continue;
                }

                _budget.ValidateFigureCandidate(
                    image.WidthInSamples,
                    image.HeightInSamples,
                    image.RawMemory.Length);
                if (!TryGetSourcePng(image, out var pngBytes) || pngBytes.Length == 0)
                {
                    continue;
                }

                (extractedFigureCount, extractedFigureBytes) = _budget.AddExtractedFigure(
                    extractedFigureCount,
                    extractedFigureBytes,
                    pngBytes.Length);

                var bounds = image.BoundingBox;
                assets.Add(new ArticleSourceFigureAsset(
                    $"page-{page.Number}-image-{imageIndex}",
                    page.Number,
                    imageIndex,
                    image.WidthInSamples,
                    image.HeightInSamples,
                    bounds.Left,
                    bounds.Bottom,
                    bounds.Width,
                    bounds.Height,
                    Hash(pngBytes),
                    pngBytes));
            }
        }

        if (assets.Count == 0)
        {
            throw new InvalidOperationException("Article PDF contains no extractable source figures.");
        }

        return new ArticleSourceFigureAudit(
            HashFile(sourcePdfPath),
            document.NumberOfPages,
            Array.AsReadOnly(assets.ToArray()));
    }

    private static string Hash(byte[] bytes) =>
        $"sha256:{Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant()}";

    private static string HashFile(string path)
    {
        using var stream = File.OpenRead(path);
        return $"sha256:{Convert.ToHexString(SHA256.HashData(stream)).ToLowerInvariant()}";
    }

    private static bool TryGetSourcePng(
        UglyToad.PdfPig.Content.IPdfImage source,
        out byte[] pngBytes)
    {
        if (source.TryGetPng(out var converted) && converted.Length > 0)
        {
            pngBytes = converted;
            return true;
        }

        using var bitmap = SKBitmap.Decode(source.RawBytes.ToArray());
        if (bitmap is null)
        {
            pngBytes = [];
            return false;
        }

        using var image = SKImage.FromBitmap(bitmap);
        using var encoded = image.Encode(SKEncodedImageFormat.Png, 100);
        if (encoded is null)
        {
            pngBytes = [];
            return false;
        }

        pngBytes = encoded.ToArray();
        return pngBytes.Length > 0;
    }
}

public sealed class SkiaArticleSourceEvidenceBoardRenderer : IArticleSourceEvidenceBoardRenderer
{
    private const int Width = 1600;
    private const int Columns = 3;
    private const int CardHeight = 280;
    private const int Gap = 28;
    private const int StartY = 24;
    private const int BottomPadding = 24;

    public ArticleSourceEvidenceBoard Render(ArticleSourceFigureAudit audit)
    {
        ArgumentNullException.ThrowIfNull(audit);
        var selected = audit.Assets
            .DistinctBy(asset => asset.Sha256, StringComparer.OrdinalIgnoreCase)
            .Take(8)
            .ToArray();
        if (selected.Length == 0)
        {
            var diagnostics = string.Join(", ", audit.Assets.Select(asset =>
            {
                return $"{asset.AssetId}@p{asset.PageNumber}:{asset.PixelWidth}x{asset.PixelHeight}";
            }));
            throw new InvalidOperationException(
                $"Source PDF contains no source evidence assets. Evaluated: {diagnostics}");
        }

        var rows = (selected.Length + Columns - 1) / Columns;
        var height = StartY
            + (rows * CardHeight)
            + (Math.Max(0, rows - 1) * Gap)
            + BottomPadding;

        using var surface = SKSurface.Create(new SKImageInfo(
            Width,
            height,
            SKColorType.Rgba8888,
            SKAlphaType.Premul))
            ?? throw new InvalidOperationException("Could not create evidence-board surface.");
        var canvas = surface.Canvas;
        canvas.Clear(SKColors.White);

        const int cardWidth = 480;
        const int startX = 52;
        for (var index = 0; index < selected.Length; index++)
        {
            var row = index / Columns;
            var column = index % Columns;
            var left = startX + (column * (cardWidth + Gap));
            var top = StartY + (row * (CardHeight + Gap));
            DrawCard(canvas, selected[index], left, top, cardWidth, CardHeight);
        }

        canvas.Flush();
        using var image = surface.Snapshot();
        using var encoded = image.Encode(SKEncodedImageFormat.Png, 100)
            ?? throw new InvalidOperationException("Could not encode source evidence board.");
        var bytes = encoded.ToArray();
        return new ArticleSourceEvidenceBoard(
            bytes,
            ArticleScientificFigureSetService.Hash(bytes),
            Width,
            height,
            selected.Select(item => item.AssetId).ToArray());
    }


    private static void DrawCard(
        SKCanvas canvas,
        ArticleSourceFigureAsset asset,
        int left,
        int top,
        int width,
        int height)
    {
        using var border = new SKPaint
        {
            Color = new SKColor(203, 213, 225),
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 2,
            IsAntialias = true,
        };
        using var fill = new SKPaint
        {
            Color = new SKColor(248, 250, 252),
            Style = SKPaintStyle.Fill,
            IsAntialias = true,
        };
        var card = new SKRect(left, top, left + width, top + height);
        canvas.DrawRoundRect(card, 6, 6, fill);
        canvas.DrawRoundRect(card, 6, 6, border);

        using var bitmap = SKBitmap.Decode(asset.PngBytes)
            ?? throw new InvalidOperationException($"Could not decode source asset {asset.AssetId}.");
        var imageArea = new SKRect(left + 12, top + 12, left + width - 12, top + height - 12);
        var scale = Math.Min(imageArea.Width / bitmap.Width, imageArea.Height / bitmap.Height);
        var targetWidth = bitmap.Width * scale;
        var targetHeight = bitmap.Height * scale;
        var target = new SKRect(
            imageArea.MidX - (targetWidth / 2),
            imageArea.MidY - (targetHeight / 2),
            imageArea.MidX + (targetWidth / 2),
            imageArea.MidY + (targetHeight / 2));
        using var imagePaint = new SKPaint { IsAntialias = true };
        canvas.DrawBitmap(bitmap, target, imagePaint);
    }
}
