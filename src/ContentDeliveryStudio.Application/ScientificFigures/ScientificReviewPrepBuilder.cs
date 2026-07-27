using System.Globalization;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
using ContentDeliveryStudio.Core.Providers;
using ContentDeliveryStudio.Core.ScientificFigures;

namespace ContentDeliveryStudio.Application.ScientificFigures;

public interface IScientificReviewImageCropper
{
    byte[] CropPng(
        byte[] sourcePng,
        int sourceWidth,
        int sourceHeight,
        ScientificPixelRegion region);
}

public sealed record ScientificPixelRegion(
    int X,
    int Y,
    int Width,
    int Height);

public sealed record ScientificReviewManifestEvidence(
    string ClaimId,
    string SourceBlockId,
    int PageNumber,
    ClaimEvidenceRole Role);

public sealed record ScientificSvgStructureRow(
    string ResponsibleItemId,
    ScientificVisualRegionKind Kind,
    bool IsCritical,
    string Summary,
    ScientificPixelRegion Region);

public sealed record ScientificReviewManifest(
    Guid SpecificationId,
    int SpecificationVersion,
    string PlanId,
    string SvgSha256,
    string FullResolutionFormat,
    string FullResolutionSha256,
    int FullResolutionWidth,
    int FullResolutionHeight,
    IReadOnlyList<ScientificReviewManifestEvidence> EvidenceSelections,
    IReadOnlyList<ScientificSvgStructureRow> StructureRows,
    IReadOnlyList<string> CropIds);

public sealed record ScientificReviewPrepBundle(
    ScientificReviewManifest Manifest,
    ScientificSemanticReviewRequest SemanticRequest,
    ScientificVisualReviewRequest VisualRequest);

public sealed class ScientificReviewPrepBuilder
{
    private static readonly XNamespace Svg = "http://www.w3.org/2000/svg";
    private static readonly Regex LinePath = new(
        @"^M\s+(-?\d+(?:\.\d+)?)\s+(-?\d+(?:\.\d+)?)\s+L\s+(-?\d+(?:\.\d+)?)\s+(-?\d+(?:\.\d+)?)$",
        RegexOptions.CultureInvariant | RegexOptions.Compiled);
    private static readonly Regex PrivatePath = new(
        @"(?i)(?:[a-z]:\\[^\s""']+|/(?:users|home)/[^\s""']+)",
        RegexOptions.CultureInvariant | RegexOptions.Compiled);

    private readonly IScientificReviewImageCropper _cropper;

    public ScientificReviewPrepBuilder(IScientificReviewImageCropper cropper)
    {
        _cropper = cropper ?? throw new ArgumentNullException(nameof(cropper));
    }

    public ScientificReviewPrepBundle Build(
        ScientificDocumentUnderstanding understanding,
        ScientificFigureSpec specification,
        SvgRenderPlan renderPlan,
        ScientificSvgArtifact svgArtifact,
        ScientificFigureExportBundle exports)
    {
        ArgumentNullException.ThrowIfNull(svgArtifact);
        ArgumentNullException.ThrowIfNull(exports);
        var semanticRequest = ScientificSemanticReviewRequest.Create(
            understanding,
            specification,
            renderPlan);
        ValidateAuthority(renderPlan, svgArtifact, exports);
        var png = exports.Artifacts.SingleOrDefault(item =>
            string.Equals(item.Format, "png", StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidOperationException(
                "Scientific review requires exactly one full-resolution PNG export.");
        ScientificReviewExecutionPolicy.ValidateFullResolutionArtifact(
            exports.Width,
            exports.Height,
            png.Bytes.Length);

        var document = ParseSvg(svgArtifact.Svg);
        var rows = BuildStructureRows(
            document,
            renderPlan,
            exports.Width,
            exports.Height);
        var requiredIds = renderPlan.Elements
            .Where(item => item.IsCritical)
            .Select(item => item.SourceSpecificationItemId)
            .Concat(renderPlan.Connections
                .Where(item => item.IsCritical)
                .Select(item => item.SourceSpecificationItemId))
            .ToHashSet(StringComparer.Ordinal);
        var rowIds = rows.Select(item => item.ResponsibleItemId)
            .ToHashSet(StringComparer.Ordinal);
        if (!requiredIds.SetEquals(rowIds))
        {
            var missing = requiredIds.Except(rowIds, StringComparer.Ordinal);
            throw new InvalidOperationException(
                $"Scientific review structure is missing critical items: {string.Join(", ", missing)}.");
        }

        ScientificReviewExecutionPolicy.ValidateCropPlan(rows.Count);
        var crops = rows.Select(row =>
        {
            var cropId = $"crop-{row.ResponsibleItemId}";
            var bytes = _cropper.CropPng(
                png.Bytes,
                exports.Width,
                exports.Height,
                row.Region);
            ScientificReviewExecutionPolicy.ValidateCropBytes(cropId, bytes.Length);
            return new ScientificVisualRegionCrop(
                cropId,
                row.Kind,
                row.ResponsibleItemId,
                row.Region.X,
                row.Region.Y,
                row.Region.Width,
                row.Region.Height,
                "image/png",
                bytes);
        }).ToArray();
        var fullResolution = new ScientificFullResolutionImage(
            png.Format,
            png.MimeType,
            png.Bytes,
            png.Sha256,
            exports.Width,
            exports.Height,
            exports.Width,
            exports.Height);
        var visualRequest = ScientificVisualReviewRequest.Create(
            fullResolution,
            crops);
        var evidence = semanticRequest.ApprovedClaims.SelectMany(claim =>
            claim.Evidence.Select(item => new ScientificReviewManifestEvidence(
                claim.ClaimId,
                item.SourceBlockId,
                item.Location.PageNumber,
                item.Role)))
            .Distinct()
            .ToArray();
        var manifest = new ScientificReviewManifest(
            specification.SpecificationId,
            specification.Version,
            renderPlan.PlanId,
            svgArtifact.Sha256,
            png.Format,
            png.Sha256,
            exports.Width,
            exports.Height,
            Array.AsReadOnly(evidence),
            Array.AsReadOnly(rows.ToArray()),
            Array.AsReadOnly(crops.Select(item => item.CropId).ToArray()));
        return new ScientificReviewPrepBundle(
            manifest,
            semanticRequest,
            visualRequest);
    }

    private static void ValidateAuthority(
        SvgRenderPlan plan,
        ScientificSvgArtifact svg,
        ScientificFigureExportBundle exports)
    {
        var actualSvgHash = Hash(System.Text.Encoding.UTF8.GetBytes(svg.Svg));
        if (svg.PlanId != plan.PlanId
            || svg.SpecificationId != plan.SpecificationId
            || svg.SpecificationVersion != plan.SpecificationVersion
            || !HashEquals(actualSvgHash, svg.Sha256)
            || !HashEquals(svg.Sha256, exports.SourceSvgSha256))
        {
            throw new InvalidOperationException(
                "Scientific review preparation authority identities do not match.");
        }

        foreach (var artifact in exports.Artifacts)
        {
            if (!HashEquals(Hash(artifact.Bytes), artifact.Sha256)
                || !HashEquals(artifact.SourceSvgSha256, svg.Sha256)
                || !HashEquals(artifact.SemanticSha256, exports.SemanticSha256))
            {
                throw new InvalidOperationException(
                    $"Scientific review export binding is invalid: {artifact.Format}.");
            }
        }
    }

    private static XDocument ParseSvg(string svg)
    {
        var settings = new XmlReaderSettings
        {
            DtdProcessing = DtdProcessing.Prohibit,
            XmlResolver = null,
        };
        using var textReader = new StringReader(svg);
        using var reader = XmlReader.Create(textReader, settings);
        return XDocument.Load(reader, LoadOptions.None);
    }

    private static IReadOnlyList<ScientificSvgStructureRow> BuildStructureRows(
        XDocument document,
        SvgRenderPlan plan,
        int width,
        int height)
    {
        var root = document.Root
            ?? throw new InvalidOperationException("Scientific SVG root is missing.");
        var rows = new List<ScientificSvgStructureRow>();
        foreach (var element in plan.Elements.Where(item => item.IsCritical))
        {
            var groups = root.Descendants(Svg + "g").Where(item =>
                string.Equals(
                    (string?)item.Attribute("data-spec-id"),
                    element.SourceSpecificationItemId,
                    StringComparison.Ordinal)).ToArray();
            if (groups.Length != 1)
            {
                continue;
            }

            var rectangle = groups[0].Element(Svg + "rect");
            if (rectangle is null)
            {
                continue;
            }

            rows.Add(new ScientificSvgStructureRow(
                element.SourceSpecificationItemId,
                RegionKind(element.Kind),
                IsCritical: true,
                Redact($"{element.Kind}: {element.ScientificMeaning} {element.ExactContent}"),
                ClampRegion(
                    Number(rectangle, "x"),
                    Number(rectangle, "y"),
                    Number(rectangle, "width"),
                    Number(rectangle, "height"),
                    width,
                    height,
                    padding: 12)));
        }

        foreach (var relation in plan.Connections.Where(item => item.IsCritical))
        {
            var paths = root.Descendants(Svg + "path").Where(item =>
                string.Equals(
                    (string?)item.Attribute("data-spec-id"),
                    relation.SourceSpecificationItemId,
                    StringComparison.Ordinal)).ToArray();
            if (paths.Length != 1)
            {
                continue;
            }

            var match = LinePath.Match((string?)paths[0].Attribute("d") ?? string.Empty);
            if (!match.Success)
            {
                continue;
            }

            var x1 = Parse(match.Groups[1].Value);
            var y1 = Parse(match.Groups[2].Value);
            var x2 = Parse(match.Groups[3].Value);
            var y2 = Parse(match.Groups[4].Value);
            var labels = paths[0].Parent?.Elements(Svg + "text")
                .Where(item => item.Attribute("data-relation-label") is not null)
                .ToArray() ?? [];
            var labelX = labels.Length == 0 ? (x1 + x2) / 2 : Number(labels[0], "x");
            var labelY = labels.Length == 0
                ? (y1 + y2) / 2
                : labels.Average(item => Number(item, "y"));
            var labelLines = labels.Select(item => item.Value).ToArray();
            var maximumLineLength = labelLines.Length == 0
                ? relation.Label?.Length ?? 0
                : labelLines.Max(item => item.Length);
            var labelHalfWidth = Math.Clamp(maximumLineLength * 4.5, 36, 180);
            var labelHalfHeight = Math.Max(14, labelLines.Length * 10);
            var left = Math.Min(Math.Min(x1, x2), labelX - labelHalfWidth);
            var top = Math.Min(Math.Min(y1, y2), labelY - labelHalfHeight);
            var right = Math.Max(Math.Max(x1, x2), labelX + labelHalfWidth);
            var bottom = Math.Max(Math.Max(y1, y2), labelY + labelHalfHeight);
            rows.Add(new ScientificSvgStructureRow(
                relation.SourceSpecificationItemId,
                ScientificVisualRegionKind.Relation,
                IsCritical: true,
                Redact($"{relation.Kind} {relation.Direction}: {relation.Label}"),
                ClampRegion(
                    left,
                    top,
                    right - left,
                    bottom - top,
                    width,
                    height,
                    padding: 12)));
        }

        return rows;
    }

    private static ScientificVisualRegionKind RegionKind(FigureElementKind kind)
    {
        return kind switch
        {
            FigureElementKind.Formula => ScientificVisualRegionKind.Formula,
            FigureElementKind.Legend => ScientificVisualRegionKind.Legend,
            _ => ScientificVisualRegionKind.Element,
        };
    }

    private static ScientificPixelRegion ClampRegion(
        double x,
        double y,
        double width,
        double height,
        int canvasWidth,
        int canvasHeight,
        int padding)
    {
        var left = Math.Max(0, (int)Math.Floor(x) - padding);
        var top = Math.Max(0, (int)Math.Floor(y) - padding);
        var right = Math.Min(canvasWidth, (int)Math.Ceiling(x + Math.Max(1, width)) + padding);
        var bottom = Math.Min(canvasHeight, (int)Math.Ceiling(y + Math.Max(1, height)) + padding);
        return new ScientificPixelRegion(
            left,
            top,
            Math.Max(1, right - left),
            Math.Max(1, bottom - top));
    }

    private static double Number(XElement element, string attribute)
    {
        return Parse((string?)element.Attribute(attribute)
            ?? throw new InvalidOperationException($"SVG attribute is missing: {attribute}."));
    }

    private static double Parse(string value)
    {
        if (!double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed)
            || !double.IsFinite(parsed))
        {
            throw new InvalidOperationException($"SVG number is invalid: {value}.");
        }

        return parsed;
    }

    private static string Redact(string value)
    {
        return PrivatePath.Replace(value.Trim(), "[redacted-local-path]");
    }

    private static string Hash(byte[] bytes)
    {
        return $"sha256:{Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant()}";
    }

    private static bool HashEquals(string first, string second)
    {
        return string.Equals(first, second, StringComparison.OrdinalIgnoreCase);
    }
}
