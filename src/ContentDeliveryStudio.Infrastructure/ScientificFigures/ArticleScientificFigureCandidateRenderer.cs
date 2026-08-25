using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Xml.Linq;
using ContentDeliveryStudio.Application.ScientificFigures;
using ContentDeliveryStudio.Core.ScientificFigures;
using SkiaSharp;
using FormulaPiece = ContentDeliveryStudio.Infrastructure.ScientificFigures.ScientificMathLayout.FormulaPiece;
using MathRun = ContentDeliveryStudio.Infrastructure.ScientificFigures.ScientificMathLayout.MathRun;

namespace ContentDeliveryStudio.Infrastructure.ScientificFigures;

public sealed partial class ArticleScientificFigureCandidateRenderer
    : IArticleScientificFigureCandidateRenderer
{
    private static readonly XNamespace Svg = "http://www.w3.org/2000/svg";
    private const string Ink = "#172033";
    private const string Muted = "#64748B";
    private const string Blue = "#2563EB";
    private const string Magenta = "#C026D3";
    private const string Green = "#087E8B";
    private const string Amber = "#B45309";
    private const string Panel = "#F8FAFC";
    private static readonly ScientificMathLayout MathLayout = new();

    private static readonly IReadOnlyDictionary<ArticleScientificFigureCandidateKind, Action<XElement>>
        KindRenderers = new Dictionary<ArticleScientificFigureCandidateKind, Action<XElement>>
        {
        [ArticleScientificFigureCandidateKind.Mechanism] = RenderSecondaryImaging,
        [ArticleScientificFigureCandidateKind.LensEquationGraph] = RenderLensEquationGraph,
        [ArticleScientificFigureCandidateKind.ExperimentalComparison] = RenderScreenRetinaComparison,
        [ArticleScientificFigureCandidateKind.Comparison] = RenderObservationPositionComparison,
        [ArticleScientificFigureCandidateKind.CorrectiveLensControl] = RenderCorrectiveLensControl,
        [ArticleScientificFigureCandidateKind.ThermalFrontMechanism] = RenderThermalFront,
        [ArticleScientificFigureCandidateKind.ThermalBasinException] = RenderThermalBasin,
        [ArticleScientificFigureCandidateKind.ThermalConductivityComparison] = RenderThermalConductivity,
        [ArticleScientificFigureCandidateKind.ThermalTransferModes] = RenderThermalTransferModes,
        [ArticleScientificFigureCandidateKind.ThermalHumidityClothing] = RenderThermalHumidityClothing,
        [ArticleScientificFigureCandidateKind.ThermalDryWetHeat] = RenderThermalDryWetHeat,
        [ArticleScientificFigureCandidateKind.GravityTerminology] = RenderGravityTerminology,
        [ArticleScientificFigureCandidateKind.GravityOrbitFreeFall] = RenderGravityOrbitFreeFall,
        [ArticleScientificFigureCandidateKind.GravityElevatorFreeFall] = RenderGravityElevatorFreeFall,
        [ArticleScientificFigureCandidateKind.GravitySurfaceRotation] = RenderGravitySurfaceRotation,
        [ArticleScientificFigureCandidateKind.GravityCaseComparison] = RenderGravityCaseComparison,
        [ArticleScientificFigureCandidateKind.GravityReferenceFrames] = RenderGravityReferenceFrames,
        [ArticleScientificFigureCandidateKind.ThermistorCircuitDivider] = RenderThermistorCircuitDivider,
        [ArticleScientificFigureCandidateKind.ThermistorCurvature] = RenderThermistorCurvature,
        [ArticleScientificFigureCandidateKind.ThermistorError] = RenderThermistorError,
        [ArticleScientificFigureCandidateKind.ThermistorSpecialValues] = RenderThermistorSpecialValues,
        [ArticleScientificFigureCandidateKind.ArchimedesDefinition] = RenderArchimedesDefinition,
        [ArticleScientificFigureCandidateKind.ArchimedesWaterModel] = RenderArchimedesWaterModel,
        [ArticleScientificFigureCandidateKind.ArchimedesBottomContact] = RenderArchimedesBottomContact,
        [ArticleScientificFigureCandidateKind.ArchimedesDepthDependence] = RenderArchimedesDepthDependence,
        [ArticleScientificFigureCandidateKind.ArchimedesTopContact] = RenderArchimedesTopContact,
        [ArticleScientificFigureCandidateKind.ArchimedesPier] = RenderArchimedesPier,
        [ArticleScientificFigureCandidateKind.ArchimedesPressureCaveat] = RenderArchimedesPressureCaveat,
        [ArticleScientificFigureCandidateKind.BernoulliFanEnergy] = RenderBernoulliFanEnergy,
        [ArticleScientificFigureCandidateKind.BernoulliFanZones] = RenderBernoulliFanZones,
        [ArticleScientificFigureCandidateKind.BernoulliStreamlineBoundary] = RenderBernoulliStreamlineBoundary,
        [ArticleScientificFigureCandidateKind.PinholeGeometry] = RenderPinholeGeometry,
        [ArticleScientificFigureCandidateKind.PinholeFocusPlane] = RenderPinholeFocusPlane,
        [ArticleScientificFigureCandidateKind.PinholeObservation] = RenderPinholeObservation,
        [ArticleScientificFigureCandidateKind.SuperconductingEnergy] = RenderSuperconductingEnergy,
        [ArticleScientificFigureCandidateKind.SuperconductingPersistentCurrent] = RenderSuperconductingPersistentCurrent,
        [ArticleScientificFigureCandidateKind.SuperconductingExcitation] = RenderSuperconductingExcitation,
        [ArticleScientificFigureCandidateKind.MeterTransientResponse] = RenderMeterTransientResponse,
        [ArticleScientificFigureCandidateKind.MeterTrialDecision] = RenderMeterTrialDecision,
        [ArticleScientificFigureCandidateKind.MeterProtectionLayers] = RenderMeterProtectionLayers,
        [ArticleScientificFigureCandidateKind.BoilingPreBubbleCollapse] = RenderBoilingPreBubbleCollapse,
        [ArticleScientificFigureCandidateKind.BoilingBubbleGrowth] = RenderBoilingBubbleGrowth,
        [ArticleScientificFigureCandidateKind.BoilingPressureScale] = RenderBoilingPressureScale,
        [ArticleScientificFigureCandidateKind.GalileanAfocalPath] = RenderGalileanAfocalPath,
        [ArticleScientificFigureCandidateKind.GalileanVirtualObjectRegimes] = RenderGalileanVirtualObjectRegimes,
        [ArticleScientificFigureCandidateKind.GalileanAngularMagnification] = RenderGalileanAngularMagnification,
        [ArticleScientificFigureCandidateKind.DryIceWaterMechanism] = RenderDryIceWaterMechanism,
        [ArticleScientificFigureCandidateKind.DryIceHeatTransferComparison] = RenderDryIceHeatTransferComparison,
        [ArticleScientificFigureCandidateKind.DryIceIsolationVerification] = RenderDryIceIsolationVerification,
        [ArticleScientificFigureCandidateKind.LeverRockContact] = RenderLeverRockContact,
        [ArticleScientificFigureCandidateKind.LeverSeesawFriction] = RenderLeverSeesawFriction,
        [ArticleScientificFigureCandidateKind.LeverTwoForceMember] = RenderLeverTwoForceMember,
        [ArticleScientificFigureCandidateKind.RestIntervalDefinition] = RenderRestIntervalDefinition,
        [ArticleScientificFigureCandidateKind.RestZeroVelocityTurningPoint] = RenderRestZeroVelocityTurningPoint,
        [ArticleScientificFigureCandidateKind.RestStateComparison] = RenderRestStateComparison,
        };

    public ScientificSvgArtifact Render(
        ArticleScientificFigureCandidate candidate,
        int presentationAttempt)
    {
        ArgumentNullException.ThrowIfNull(candidate);
        if (presentationAttempt is < 1 or > 3)
        {
            throw new ArgumentOutOfRangeException(nameof(presentationAttempt));
        }

        if (candidate.Kind == ArticleScientificFigureCandidateKind.SourceEvidenceBoard)
        {
            throw new InvalidOperationException(
                "Source evidence boards must retain source pixels and cannot use the vector candidate renderer.");
        }

        var specificationId = StableGuid(candidate.CandidateId);
        var planId = $"article-candidate:{candidate.CandidateId}:presentation-{presentationAttempt}";
        var root = CreateRoot(candidate, planId, specificationId);
        var layer = new XElement(
            Svg + "g",
            new XAttribute("id", "layer-scientific-content"),
            new XAttribute("data-layer-id", "scientific-content"),
            new XAttribute("data-scientific", "true"));
        var group = new XElement(
            Svg + "g",
            new XAttribute("id", $"render-{candidate.CandidateId}"),
            new XAttribute("data-spec-id", candidate.CandidateId),
            new XAttribute("data-element-kind", FigureElementKind.Entity),
            new XAttribute("data-layer-id", "scientific-content"),
            new XAttribute("data-critical", "true"),
            new XAttribute("data-authoritative", "false"),
            new XAttribute("data-provenance-kind", ScientificProvenanceKind.ClaimEvidence));
        group.Add(Rect(24, 24, 1152, 752, "#FFFFFF", "#CBD5E1", 2));
        group.Add(Text(candidate.Title, 64, 74, 30, Ink));

        if (!KindRenderers.TryGetValue(candidate.Kind, out var renderKind))
        {
            throw new ArgumentOutOfRangeException(
                nameof(candidate),
                candidate.Kind,
                "Unsupported article candidate kind.");
        }

        renderKind(group);

        layer.Add(group);
        root.Add(layer);
        var document = new XDocument(new XDeclaration("1.0", "utf-8", null), root);
        var svg = document.ToString(SaveOptions.DisableFormatting);
        var sha256 = Hash(Encoding.UTF8.GetBytes(svg));
        return new ScientificSvgArtifact(planId, specificationId, 1, svg, sha256);
    }

    private static XElement CreateRoot(
        ArticleScientificFigureCandidate candidate,
        string planId,
        Guid specificationId) =>
        new(
            Svg + "svg",
            new XAttribute("xmlns", Svg.NamespaceName),
            new XAttribute("width", 1200),
            new XAttribute("height", 800),
            new XAttribute("viewBox", "0 0 1200 800"),
            new XAttribute("role", "img"),
            new XAttribute("aria-labelledby", "svg-title svg-description"),
            new XAttribute("data-plan-id", planId),
            new XAttribute("data-specification-id", specificationId.ToString("D")),
            new XAttribute("data-specification-version", 1),
            new XElement(Svg + "title", new XAttribute("id", "svg-title"), candidate.Title),
            new XElement(Svg + "desc", new XAttribute("id", "svg-description"), candidate.CentralMessage),
            new XElement(Svg + "metadata", $"candidate={candidate.CandidateId};gate1=pending;format=svg"),
            new XElement(
                Svg + "defs",
                new XElement(
                    Svg + "marker",
                    new XAttribute("id", "arrowhead"),
                    new XAttribute("markerWidth", 10),
                    new XAttribute("markerHeight", 7),
                    new XAttribute("refX", 9),
                    new XAttribute("refY", 3.5),
                    new XAttribute("orient", "auto-start-reverse"),
                    new XAttribute("markerUnits", "strokeWidth"),
                    new XElement(
                        Svg + "path",
                        new XAttribute("d", "M 0 0 L 10 3.5 L 0 7 z"),
                        new XAttribute("fill", Ink)))));

    /// <summary>
    /// A readable low-voltage apparatus anchor prevents the meter figures from
    /// degrading into ungrounded process cards or a curve with no operation.
    /// The two named connections are part of the semantic contract, not a
    /// decorative circuit motif.
    /// </summary>
    private static XElement DrawArticleCircleElement(double centerX, double centerY, double radius, string color, double width, string role)
    {
        var circle = new XElement(Svg + "g");
        const int segments = 24;
        for (var index = 0; index < segments; index++)
        {
            var start = 2 * Math.PI * index / segments;
            var end = 2 * Math.PI * (index + 1) / segments;
            circle.Add(ArticleLine(
                centerX + radius * Math.Cos(start),
                centerY + radius * Math.Sin(start),
                centerX + radius * Math.Cos(end),
                centerY + radius * Math.Sin(end),
                color,
                width,
                role));
        }
        return circle;
    }

    private static XElement DrawObserverEye(double centerX, double centerY, string role)
    {
        var eye = new XElement(Svg + "g");
        var outline = new[]
        {
            (centerX - 24, centerY), (centerX - 12, centerY - 11),
            (centerX, centerY - 15), (centerX + 12, centerY - 11),
            (centerX + 24, centerY), (centerX + 12, centerY + 11),
            (centerX, centerY + 15), (centerX - 12, centerY + 11),
            (centerX - 24, centerY),
        };
        for (var index = 0; index < outline.Length - 1; index++)
        {
            eye.Add(ArticleLine(
                outline[index].Item1,
                outline[index].Item2,
                outline[index + 1].Item1,
                outline[index + 1].Item2,
                Green,
                3,
                role));
        }
        eye.Add(DrawArticleCircleElement(centerX, centerY, 5, Ink, 2, role));
        return eye;
    }

    private static void DrawLens(
        XElement group,
        double x,
        double top,
        double bottom,
        string label,
        string color)
    {
        group.Add(Line(x, top, x, bottom, color, 5));
        group.Add(Line(x, top, x - 12, top + 18, color, 3));
        group.Add(Line(x, top, x + 12, top + 18, color, 3));
        group.Add(Line(x, bottom, x - 12, bottom - 18, color, 3));
        group.Add(Line(x, bottom, x + 12, bottom - 18, color, 3));
        group.Add(Text(label, x, bottom + 30, 16, color, "middle"));
    }

    private static void DrawConcaveLens(
        XElement group,
        double x,
        double top,
        double bottom,
        string label,
        string color)
    {
        group.Add(Line(x, top, x, bottom, color, 5));
        group.Add(Line(x, top + 18, x - 12, top, color, 3));
        group.Add(Line(x, top + 18, x + 12, top, color, 3));
        group.Add(Line(x, bottom - 18, x - 12, bottom, color, 3));
        group.Add(Line(x, bottom - 18, x + 12, bottom, color, 3));
        group.Add(Text(label, x, bottom + 30, 15, color, "middle"));
    }

    private static void DrawIncidentRay(
        XElement group,
        double startX,
        double startY,
        double imageX,
        double imageY,
        double lensX,
        string color)
    {
        if (lensX <= imageX)
        {
            var fraction = (lensX - startX) / (imageX - startX);
            var lensY = startY + ((imageY - startY) * fraction);
            group.Add(Line(startX, startY, lensX, lensY, color, 3, arrow: true));
            return;
        }

        var slope = (imageY - startY) / (imageX - startX);
        var extendedY = imageY + (slope * (lensX - imageX));
        group.Add(Line(startX, startY, imageX, imageY, color, 3, arrow: true));
        group.Add(Line(imageX, imageY, lensX, extendedY, color, 3, arrow: true));
    }

    private static void PlotCurve(
        XElement group,
        Func<double, double> function,
        double start,
        double end,
        int steps,
        double left,
        double bottom,
        double scale,
        string color)
    {
        var previousX = start;
        var previousY = function(start);
        for (var step = 1; step <= steps; step++)
        {
            var x = start + ((end - start) * step / steps);
            var y = function(x);
            group.Add(Line(
                left + (previousX * scale),
                bottom - (previousY * scale),
                left + (x * scale),
                bottom - (y * scale),
                color,
                3));
            previousX = x;
            previousY = y;
        }
    }

    private static void DrawArrow(
        XElement group,
        double x1,
        double y1,
        double x2,
        double y2,
        string color,
        double width) =>
        group.Add(Line(x1, y1, x2, y2, color, width, arrow: true));

    private static void DrawCircle(
        XElement group,
        double centerX,
        double centerY,
        double radius,
        string color,
        double width)
    {
        const int segments = 48;
        for (var index = 0; index < segments; index++)
        {
            var start = 2 * Math.PI * index / segments;
            var end = 2 * Math.PI * (index + 1) / segments;
            group.Add(Line(
                centerX + radius * Math.Cos(start),
                centerY + radius * Math.Sin(start),
                centerX + radius * Math.Cos(end),
                centerY + radius * Math.Sin(end),
                color,
                width));
        }
    }

    private static void DrawArticleCircle(
        XElement group,
        double centerX,
        double centerY,
        double radius,
        string color,
        double width,
        string role)
    {
        const int segments = 48;
        for (var index = 0; index < segments; index++)
        {
            var start = 2 * Math.PI * index / segments;
            var end = 2 * Math.PI * (index + 1) / segments;
            group.Add(ArticleLine(
                centerX + radius * Math.Cos(start),
                centerY + radius * Math.Sin(start),
                centerX + radius * Math.Cos(end),
                centerY + radius * Math.Sin(end),
                color,
                width,
                role));
        }
    }

    private static XElement Rect(
        double x,
        double y,
        double width,
        double height,
        string fill,
        string stroke,
        double strokeWidth) =>
        new(
            Svg + "rect",
            new XAttribute("x", Number(x)),
            new XAttribute("y", Number(y)),
            new XAttribute("width", Number(width)),
            new XAttribute("height", Number(height)),
            new XAttribute("rx", 4),
            new XAttribute("fill", fill),
            new XAttribute("stroke", stroke),
            new XAttribute("stroke-width", Number(strokeWidth)));

    private static XElement ArticleRect(
        double x,
        double y,
        double width,
        double height,
        string fill,
        string stroke,
        double strokeWidth,
        string role)
    {
        var rect = Rect(x, y, width, height, fill, stroke, strokeWidth);
        rect.SetAttributeValue("data-article-role", role);
        rect.SetAttributeValue("data-element-graphic", "true");
        return rect;
    }

    private static XElement Line(
        double x1,
        double y1,
        double x2,
        double y2,
        string color,
        double width,
        bool arrow = false) =>
        new(
            Svg + "path",
            new XAttribute("d", $"M {Number(x1)} {Number(y1)} L {Number(x2)} {Number(y2)}"),
            new XAttribute("fill", "none"),
            new XAttribute("stroke", color),
            new XAttribute("stroke-width", Number(width)),
            new XAttribute("data-element-graphic", "true"),
            arrow ? new XAttribute("marker-end", "url(#arrowhead)") : null);

    private static XElement ThermalLine(
        double x1,
        double y1,
        double x2,
        double y2,
        string color,
        double width,
        string role,
        bool arrow = false)
    {
        var line = Line(x1, y1, x2, y2, color, width, arrow);
        line.SetAttributeValue("data-thermal-role", role);
        return line;
    }

    private static XElement GravityLine(
        double x1,
        double y1,
        double x2,
        double y2,
        string color,
        double width,
        string role,
        bool arrow = false)
    {
        var line = Line(x1, y1, x2, y2, color, width, arrow);
        line.SetAttributeValue("data-gravity-role", role);
        return line;
    }

    private static XElement ArticleLine(
        double x1,
        double y1,
        double x2,
        double y2,
        string color,
        double width,
        string role,
        bool arrow = false)
    {
        var line = Line(x1, y1, x2, y2, color, width, arrow);
        line.SetAttributeValue("data-article-role", role);
        return line;
    }

    private static XElement DashedArticleLine(
        double x1,
        double y1,
        double x2,
        double y2,
        string color,
        double width,
        string role)
    {
        var line = ArticleLine(x1, y1, x2, y2, color, width, role);
        line.SetAttributeValue("stroke-dasharray", "7 6");
        return line;
    }

    private static XElement DashedGravityLine(
        double x1,
        double y1,
        double x2,
        double y2,
        string color,
        double width,
        string role)
    {
        var line = GravityLine(x1, y1, x2, y2, color, width, role);
        line.SetAttributeValue("stroke-dasharray", "7 6");
        return line;
    }

    private static XElement MathText(
        string tex,
        double x,
        double y,
        int fontSize,
        string color,
        string anchor,
        params MathRun[] runs)
    {
        return MathLayout.Render(
            new ScientificMathLayout.FormulaSpec(
                tex,
                new ScientificMathLayout.MathExpression(
                    [new ScientificMathLayout.MathTextNode(runs)])),
            x,
            y,
            fontSize,
            color,
            anchor);
    }

    private static XElement FractionFormula(
        string tex,
        double x,
        double y,
        int fontSize,
        string color,
        string anchor,
        params FormulaPiece[] pieces)
    {
        var nodes = pieces.Select(piece => piece.Denominator is null
            ? (ScientificMathLayout.MathNode)new ScientificMathLayout.MathTextNode(
                [ScientificMathLayout.MathRun.Normal(piece.Text!)])
            : new ScientificMathLayout.MathFractionNode(piece.Text!, piece.Denominator))
            .ToArray();
        return MathLayout.Render(
            new ScientificMathLayout.FormulaSpec(
                tex,
                new ScientificMathLayout.MathExpression(nodes)),
            x,
            y,
            fontSize,
            color,
            anchor);
    }

    private static XElement Text(
        string value,
        double x,
        double y,
        int fontSize,
        string color,
        string anchor = "start")
    {
        if (ScientificMathLayout.LooksLikeUnparsedFormula(value))
        {
            throw new InvalidOperationException(
                $"Visible ordinary text contains an unparsed formula fragment: '{value}'.");
        }

        var bounds = MeasureTextBounds(value, x, y, fontSize, anchor);

        return new(
            Svg + "text",
            new XAttribute("x", Number(x)),
            new XAttribute("y", Number(y)),
            new XAttribute("text-anchor", anchor),
            new XAttribute("font-family", "Microsoft YaHei"),
            new XAttribute("font-size", fontSize),
            new XAttribute("fill", color),
            new XAttribute("data-text-bounds", FormatBounds(bounds)),
            new XAttribute("data-content-kind", FigureElementKind.Entity),
            value);
    }

    private static SKRect MeasureTextBounds(
        string value,
        double x,
        double baseline,
        int fontSize,
        string anchor)
    {
        using var paint = new SKPaint { IsAntialias = true };
        using var typeface = ResolveTextTypeface(value);
        using var font = new SKFont(typeface, fontSize);
        var advance = font.MeasureText(value, out var glyphBounds, paint);
        var origin = anchor switch
        {
            "middle" => x - advance / 2,
            "end" => x - advance,
            "start" => x,
            _ => throw new InvalidOperationException($"Unsupported SVG text anchor: '{anchor}'."),
        };
        return new SKRect(
            (float)(origin + glyphBounds.Left),
            (float)(baseline + glyphBounds.Top),
            (float)(origin + glyphBounds.Right),
            (float)(baseline + glyphBounds.Bottom));
    }

    private static SKTypeface ResolveTextTypeface(string value)
    {
        const string family = "Microsoft YaHei";
        var primary = SKTypeface.FromFamilyName(family);
        if (primary.ContainsGlyphs(value))
        {
            return primary;
        }

        primary.Dispose();
        var manager = SKFontManager.Default;
        foreach (var character in value.Where(character => !char.IsWhiteSpace(character)))
        {
            var fallback = manager.MatchCharacter(family, character)
                ?? manager.MatchCharacter(character);
            if (fallback is not null && fallback.ContainsGlyphs(value))
            {
                return fallback;
            }

            fallback?.Dispose();
        }

        throw new InvalidOperationException(
            $"No installed typeface can measure the approved SVG text: '{value}'.");
    }

    private static string FormatBounds(SKRect bounds) =>
        string.Join(",", Number(bounds.Left), Number(bounds.Top), Number(bounds.Width), Number(bounds.Height));

    private static Guid StableGuid(string value)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        return new Guid(bytes[..16]);
    }

    private static string Hash(byte[] bytes) =>
        $"sha256:{Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant()}";

    private static string Number(double value) =>
        value.ToString("0.###", CultureInfo.InvariantCulture);

}
