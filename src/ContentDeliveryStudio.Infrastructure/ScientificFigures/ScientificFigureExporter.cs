using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
using ContentDeliveryStudio.Application.ScientificFigures;
using SkiaSharp;

namespace ContentDeliveryStudio.Infrastructure.ScientificFigures;

public sealed class ScientificFigureExporter : IScientificFigureExporter
{
    public const string ExporterIdentity = "content-delivery-studio.skia-scientific-exporter";
    public const string ExporterVersion = "1.0";

    private const int MaximumDimension = 16_384;
    private const long MaximumPixels = 100_000_000;

    private static readonly XNamespace Svg = "http://www.w3.org/2000/svg";
    private static readonly HashSet<string> SupportedElements =
    [
        "svg", "title", "desc", "metadata", "defs", "marker", "g", "rect", "path", "text",
    ];
    private static readonly Regex LinePathPattern = new(
        @"^M\s+(-?\d+(?:\.\d+)?)\s+(-?\d+(?:\.\d+)?)\s+L\s+(-?\d+(?:\.\d+)?)\s+(-?\d+(?:\.\d+)?)$",
        RegexOptions.CultureInvariant | RegexOptions.Compiled);

    public ScientificFigureExportBundle Export(ScientificFigureExportRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.SourceSvg);
        if (request.Width <= 0
            || request.Height <= 0
            || request.Width > MaximumDimension
            || request.Height > MaximumDimension
            || (long)request.Width * request.Height > MaximumPixels)
        {
            throw new ArgumentOutOfRangeException(
                nameof(request),
                "Export dimensions must be positive and remain within the bounded render budget.");
        }

        var actualSvgHash = Hash(Encoding.UTF8.GetBytes(request.SourceSvg.Svg));
        if (!HashEquals(actualSvgHash, request.SourceSvg.Sha256)
            || !HashEquals(actualSvgHash, request.ApprovedSvgSha256))
        {
            throw new InvalidOperationException(
                "The source SVG does not match the approved SVG hash.");
        }

        var document = ParseAuthoritySvg(request.SourceSvg);
        var semantics = BuildSemantics(document);
        var semanticHash = Hash(JsonSerializer.SerializeToUtf8Bytes(semantics));
        var pngBytes = RenderPng(document, request.Width, request.Height);
        var pdfBytes = RenderPdf(
            document,
            request.Width,
            request.Height,
            semantics,
            actualSvgHash,
            request.SourceSvg);
        var artifacts = new[]
        {
            CreateArtifact("png", "image/png", pngBytes, actualSvgHash, semanticHash),
            CreateArtifact("pdf", "application/pdf", pdfBytes, actualSvgHash, semanticHash),
        };

        return new ScientificFigureExportBundle(
            actualSvgHash,
            ExporterIdentity,
            ExporterVersion,
            request.Width,
            request.Height,
            semantics,
            semanticHash,
            artifacts);
    }

    private static XDocument ParseAuthoritySvg(ScientificSvgArtifact source)
    {
        var settings = new XmlReaderSettings
        {
            DtdProcessing = DtdProcessing.Prohibit,
            XmlResolver = null,
        };
        using var textReader = new StringReader(source.Svg);
        using var xmlReader = XmlReader.Create(textReader, settings);
        var document = XDocument.Load(xmlReader, LoadOptions.None);
        var root = document.Root
            ?? throw new InvalidOperationException("The approved SVG has no root element.");
        if (root.Name != Svg + "svg")
        {
            throw new InvalidOperationException("The approved artifact is not an SVG document.");
        }

        var unsupported = root.DescendantsAndSelf()
            .FirstOrDefault(element =>
                element.Name.Namespace != Svg
                || !SupportedElements.Contains(element.Name.LocalName));
        if (unsupported is not null)
        {
            throw new InvalidOperationException(
                $"The approved SVG contains an unsupported element: {unsupported.Name.LocalName}.");
        }

        if (root.DescendantsAndSelf().Attributes().Any(attribute =>
                attribute.Name.LocalName is "href" or "src"))
        {
            throw new InvalidOperationException("The approved SVG cannot reference external content.");
        }

        RequireRootMetadata(root, "data-plan-id", source.PlanId);
        RequireRootMetadata(root, "data-specification-id", source.SpecificationId.ToString("D"));
        RequireRootMetadata(
            root,
            "data-specification-version",
            source.SpecificationVersion.ToString(CultureInfo.InvariantCulture));
        ValidateVisibleAuthority(root);
        return document;
    }

    private static void ValidateVisibleAuthority(XElement root)
    {
        var unboundText = root.Descendants(Svg + "text").FirstOrDefault(element =>
            element.Attribute("data-content-kind") is null
            && element.Parent?.Attribute("data-connection-group") is null);
        if (unboundText is not null)
        {
            throw new InvalidOperationException(
                "The approved SVG contains unbound visible text.");
        }

        var unboundRectangle = root.Descendants(Svg + "rect").FirstOrDefault(element =>
            !element.Ancestors(Svg + "g")
                .Any(group => group.Attribute("data-element-kind") is not null));
        if (unboundRectangle is not null)
        {
            throw new InvalidOperationException(
                "The approved SVG contains geometry without specification authority.");
        }

        var unboundPath = root.Descendants(Svg + "path").FirstOrDefault(element =>
            !element.Ancestors(Svg + "defs").Any()
            && element.Attribute("data-relation-kind") is null);
        if (unboundPath is not null)
        {
            throw new InvalidOperationException(
                "The approved SVG contains a path without relation authority.");
        }
    }

    private static void RequireRootMetadata(XElement root, string attributeName, string expected)
    {
        var actual = (string?)root.Attribute(attributeName);
        if (!string.Equals(actual, expected, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"SVG provenance metadata does not match the artifact: {attributeName}.");
        }
    }

    private static ScientificExportSemantics BuildSemantics(XDocument document)
    {
        var root = document.Root!;
        var elements = root.Descendants(Svg + "g")
            .Where(element => element.Attribute("data-element-kind") is not null)
            .Select(element => new ScientificExportElementFixture(
                RequiredAttribute(element, "id"),
                RequiredAttribute(element, "data-spec-id"),
                RequiredAttribute(element, "data-element-kind"),
                Boolean(element, "data-authoritative"),
                (string?)element.Attribute("data-provenance-kind")))
            .ToArray();
        var texts = root.Descendants(Svg + "text")
            .Where(element => element.Attribute("data-content-kind") is not null)
            .Select(element =>
            {
                var owner = element.Ancestors(Svg + "g")
                    .First(group => group.Attribute("data-element-kind") is not null);
                return new ScientificExportTextFixture(
                    RequiredAttribute(owner, "id"),
                    RequiredAttribute(owner, "data-spec-id"),
                    RequiredAttribute(element, "data-content-kind"),
                    element.Value);
            })
            .ToArray();
        var relations = root.Descendants(Svg + "path")
            .Where(element => element.Attribute("data-relation-kind") is not null)
            .Select(element => new ScientificExportRelationFixture(
                RequiredAttribute(element, "id"),
                RequiredAttribute(element, "data-spec-id"),
                RequiredAttribute(element, "data-direction"),
                element.Parent?.Element(Svg + "text")?.Value,
                RequiredAttribute(element, "data-provenance-kind")))
            .ToArray();

        return new ScientificExportSemantics(
            root.Element(Svg + "title")?.Value
                ?? throw new InvalidOperationException("SVG accessibility title is required."),
            root.Element(Svg + "desc")?.Value
                ?? throw new InvalidOperationException("SVG accessibility description is required."),
            elements,
            texts,
            relations);
    }

    private static byte[] RenderPng(XDocument document, int width, int height)
    {
        using var surface = SKSurface.Create(new SKImageInfo(
            width,
            height,
            SKColorType.Rgba8888,
            SKAlphaType.Premul))
            ?? throw new InvalidOperationException("Could not create the PNG render surface.");
        DrawDocument(surface.Canvas, document, width, height);
        using var image = surface.Snapshot();
        using var encoded = image.Encode(SKEncodedImageFormat.Png, quality: 100)
            ?? throw new InvalidOperationException("Could not encode the scientific PNG.");
        return encoded.ToArray();
    }

    private static byte[] RenderPdf(
        XDocument document,
        int width,
        int height,
        ScientificExportSemantics semantics,
        string sourceSvgHash,
        ScientificSvgArtifact source)
    {
        using var stream = new MemoryStream();
        var metadata = new SKDocumentPdfMetadata
        {
            Title = semantics.AccessibilityTitle,
            Subject = sourceSvgHash,
            Author = "AI Content Delivery Studio",
            Creator = ExporterIdentity,
            Producer = $"{ExporterIdentity}/{ExporterVersion}",
            Keywords = $"plan:{source.PlanId};specification:{source.SpecificationId:D};version:{source.SpecificationVersion}",
        };
        using var pdf = SKDocument.CreatePdf(stream, metadata)
            ?? throw new InvalidOperationException("Could not create the scientific PDF.");
        var canvas = pdf.BeginPage(width, height);
        DrawDocument(canvas, document, width, height);
        pdf.EndPage();
        pdf.Close();
        return stream.ToArray();
    }

    private static void DrawDocument(SKCanvas canvas, XDocument document, int width, int height)
    {
        var root = document.Root!;
        var sourceWidth = Number(root, "width");
        var sourceHeight = Number(root, "height");
        if (sourceWidth <= 0 || sourceHeight <= 0)
        {
            throw new InvalidOperationException("SVG source dimensions must be positive.");
        }
        canvas.Clear(SKColors.White);
        canvas.Save();
        canvas.Scale(width / sourceWidth, height / sourceHeight);
        foreach (var element in root.Descendants()
                     .Where(element => !element.Ancestors(Svg + "defs").Any()))
        {
            if (element.Name == Svg + "rect")
            {
                DrawRectangle(canvas, element);
            }
            else if (element.Name == Svg + "path")
            {
                DrawLine(canvas, element);
            }
            else if (element.Name == Svg + "text")
            {
                DrawText(canvas, element);
            }
        }

        canvas.Restore();
        canvas.Flush();
    }

    private static void DrawRectangle(SKCanvas canvas, XElement element)
    {
        var opacity = OptionalNumber(element, "fill-opacity", 1);
        if (opacity is < 0 or > 1)
        {
            throw new InvalidOperationException("SVG fill opacity must be between zero and one.");
        }
        using var fill = new SKPaint
        {
            Color = Color(element, "fill", SKColors.Transparent).WithAlpha((byte)(255 * opacity)),
            IsAntialias = true,
            Style = SKPaintStyle.Fill,
        };
        var rect = new SKRect(
            Number(element, "x"),
            Number(element, "y"),
            Number(element, "x") + Number(element, "width"),
            Number(element, "y") + Number(element, "height"));
        var radius = OptionalNumber(element, "rx", 0);
        if (fill.Color.Alpha > 0)
        {
            canvas.DrawRoundRect(rect, radius, radius, fill);
        }

        var strokeColor = Color(element, "stroke", SKColors.Transparent);
        if (strokeColor.Alpha > 0)
        {
            using var stroke = new SKPaint
            {
                Color = strokeColor,
                IsAntialias = true,
                Style = SKPaintStyle.Stroke,
                StrokeWidth = OptionalNumber(element, "stroke-width", 1),
            };
            canvas.DrawRoundRect(rect, radius, radius, stroke);
        }
    }

    private static void DrawLine(SKCanvas canvas, XElement element)
    {
        var match = LinePathPattern.Match(RequiredAttribute(element, "d"));
        if (!match.Success)
        {
            throw new InvalidOperationException("The SVG contains an unsupported path command.");
        }

        var start = new SKPoint(Parse(match.Groups[1].Value), Parse(match.Groups[2].Value));
        var end = new SKPoint(Parse(match.Groups[3].Value), Parse(match.Groups[4].Value));
        var color = Color(element, "stroke", SKColors.Black);
        using var paint = new SKPaint
        {
            Color = color,
            IsAntialias = true,
            Style = SKPaintStyle.Stroke,
            StrokeWidth = OptionalNumber(element, "stroke-width", 1),
        };
        canvas.DrawLine(start, end, paint);
        if (element.Attribute("marker-start") is not null)
        {
            DrawArrowhead(canvas, end, start, color);
        }

        if (element.Attribute("marker-end") is not null)
        {
            DrawArrowhead(canvas, start, end, color);
        }
    }

    private static void DrawArrowhead(SKCanvas canvas, SKPoint from, SKPoint tip, SKColor color)
    {
        var dx = tip.X - from.X;
        var dy = tip.Y - from.Y;
        var length = MathF.Sqrt((dx * dx) + (dy * dy));
        if (length <= 0)
        {
            return;
        }

        var ux = dx / length;
        var uy = dy / length;
        var basePoint = new SKPoint(tip.X - (ux * 10), tip.Y - (uy * 10));
        var normal = new SKPoint(-uy * 5, ux * 5);
        using var path = new SKPath();
        path.MoveTo(tip);
        path.LineTo(basePoint.X + normal.X, basePoint.Y + normal.Y);
        path.LineTo(basePoint.X - normal.X, basePoint.Y - normal.Y);
        path.Close();
        using var paint = new SKPaint
        {
            Color = color,
            IsAntialias = true,
            Style = SKPaintStyle.Fill,
        };
        canvas.DrawPath(path, paint);
    }

    private static void DrawText(SKCanvas canvas, XElement element)
    {
        using var paint = new SKPaint
        {
            Color = Color(element, "fill", SKColors.Black),
            IsAntialias = true,
        };
        using var typeface = SKTypeface.FromFamilyName(
            (string?)element.Attribute("font-family") ?? "Segoe UI");
        using var font = new SKFont(typeface, OptionalNumber(element, "font-size", 16));
        var alignment = string.Equals(
            (string?)element.Attribute("text-anchor"),
            "middle",
            StringComparison.Ordinal)
            ? SKTextAlign.Center
            : SKTextAlign.Left;
        canvas.DrawText(
            element.Value,
            Number(element, "x"),
            Number(element, "y"),
            alignment,
            font,
            paint);
    }

    private static ScientificFigureExportArtifact CreateArtifact(
        string format,
        string mimeType,
        byte[] bytes,
        string sourceSvgHash,
        string semanticHash)
    {
        return new ScientificFigureExportArtifact(
            format,
            mimeType,
            bytes,
            Hash(bytes),
            sourceSvgHash,
            semanticHash);
    }

    private static string RequiredAttribute(XElement element, string name)
    {
        return (string?)element.Attribute(name)
            ?? throw new InvalidOperationException($"SVG attribute is required: {name}.");
    }

    private static float Number(XElement element, string attributeName)
    {
        return Parse(RequiredAttribute(element, attributeName));
    }

    private static float OptionalNumber(XElement element, string attributeName, float fallback)
    {
        var value = (string?)element.Attribute(attributeName);
        return value is null ? fallback : Parse(value);
    }

    private static float Parse(string value)
    {
        if (!float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed)
            || !float.IsFinite(parsed))
        {
            throw new InvalidOperationException($"SVG numeric value is invalid: {value}.");
        }

        return parsed;
    }

    private static bool Boolean(XElement element, string attributeName)
    {
        var value = RequiredAttribute(element, attributeName);
        return value switch
        {
            "true" => true,
            "false" => false,
            _ => throw new InvalidOperationException(
                $"SVG boolean value is invalid: {attributeName}={value}."),
        };
    }

    private static SKColor Color(XElement element, string attributeName, SKColor fallback)
    {
        var value = (string?)element.Attribute(attributeName);
        if (value is null)
        {
            return fallback;
        }

        if (string.Equals(value, "none", StringComparison.OrdinalIgnoreCase))
        {
            return SKColors.Transparent;
        }

        if (value.Length != 7
            || value[0] != '#'
            || !uint.TryParse(value.AsSpan(1), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var rgb))
        {
            throw new InvalidOperationException($"SVG color is unsupported: {value}.");
        }

        return new SKColor(
            (byte)((rgb >> 16) & 0xFF),
            (byte)((rgb >> 8) & 0xFF),
            (byte)(rgb & 0xFF));
    }

    private static string Hash(byte[] bytes)
    {
        return $"sha256:{Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant()}";
    }

    private static bool HashEquals(string first, string second)
    {
        return !string.IsNullOrWhiteSpace(second)
            && string.Equals(first, second.Trim(), StringComparison.OrdinalIgnoreCase);
    }
}
