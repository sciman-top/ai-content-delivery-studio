using System.Globalization;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using SkiaSharp;

namespace ContentDeliveryStudio.Infrastructure.ScientificFigures;

/// <summary>
/// The single math seam used by article figures.  Callers provide one FormulaSpec;
/// the measured layout is then used for SVG, accessibility text, and diagnostics.
/// </summary>
internal sealed class ScientificMathLayout
{
    private static readonly XNamespace Svg = "http://www.w3.org/2000/svg";
    private static readonly string[] MathFamilies =
    [
        "Cambria Math",
        "STIX Two Math",
        "Times New Roman",
        "Segoe UI",
    ];
    private const double CanvasWidth = 1200;
    private const double CanvasHeight = 800;
    private const int MinimumReadableFontSize = 10;
    private const string MathCodePattern = @"\\(?:frac|mathbf|mathrm)|\b[A-Za-z]_[A-Za-z]";

    public XElement Render(
        FormulaSpec spec,
        double x,
        double baseline,
        int fontSize,
        string color,
        string anchor) =>
        Layout(spec, x, baseline, fontSize, color, anchor).Element;

    internal ScientificMathLayoutResult Layout(
        FormulaSpec spec,
        double x,
        double baseline,
        int fontSize,
        string color,
        string anchor)
    {
        ArgumentNullException.ThrowIfNull(spec);
        if (fontSize < MinimumReadableFontSize)
        {
            throw new InvalidOperationException(
                $"Formula '{spec.TeX}' uses font size {fontSize}, below the minimum readable size {MinimumReadableFontSize}.");
        }

        var measured = Measure(spec, fontSize, x, baseline, anchor);
        if (measured.Bounds.Left < 0 || measured.Bounds.Top < 0
            || measured.Bounds.Right > CanvasWidth || measured.Bounds.Bottom > CanvasHeight)
        {
            throw new InvalidOperationException(
                $"Formula '{spec.TeX}' is outside the SVG canvas: {FormatBounds(measured.Bounds)}.");
        }

        var group = new XElement(
            Svg + "g",
            new XAttribute("data-math-tex", spec.TeX),
            new XAttribute("aria-label", spec.TeX),
            new XAttribute("data-math-bounds", FormatBounds(measured.Bounds)),
            new XAttribute("data-math-diagnostics", string.Join(";", measured.Diagnostics)));

        foreach (var item in measured.Items)
        {
            if (item is MeasuredRun run)
            {
                group.Add(CreateText(run.Run, run.X, run.Baseline, run.FontSize, color));
                continue;
            }

            var fraction = (MeasuredFraction)item;
            group.Add(CreateText(MathRun.ItalicRun(fraction.Numerator), fraction.CenterX, fraction.NumeratorBaseline, fraction.FontSize, color, "middle"));
            group.Add(new XElement(
                Svg + "path",
                new XAttribute("d", $"M {Number(fraction.Left + 2)} {Number(fraction.LineY)} L {Number(fraction.Right - 2)} {Number(fraction.LineY)}"),
                new XAttribute("fill", "none"),
                new XAttribute("stroke", color),
                new XAttribute("stroke-width", "1.4"),
                new XAttribute("data-element-graphic", "true")));
            group.Add(CreateText(MathRun.ItalicRun(fraction.Denominator), fraction.CenterX, fraction.DenominatorBaseline, fraction.FontSize, color, "middle"));
        }

        return new ScientificMathLayoutResult(
            group,
            new ScientificMathBounds(
                measured.Bounds.Left,
                measured.Bounds.Top,
                measured.Bounds.Width,
                measured.Bounds.Height),
            measured.Diagnostics);
    }

    internal static bool LooksLikeUnparsedFormula(string value) =>
        Regex.IsMatch(value, MathCodePattern, RegexOptions.CultureInvariant);

    private static MeasuredLayout Measure(
        FormulaSpec spec,
        int fontSize,
        double x,
        double baseline,
        string anchor)
    {
        var widths = spec.Expression.Nodes.Select(node => node.MeasureWidth(fontSize)).ToArray();
        var totalWidth = widths.Sum();
        var cursor = anchor switch
        {
            "middle" => x - (totalWidth / 2),
            "end" => x - totalWidth,
            _ => x,
        };
        var items = new List<MeasuredItem>();
        var bounds = SKRect.Empty;
        SKRect? previousRunBounds = null;
        for (var index = 0; index < spec.Expression.Nodes.Count; index++)
        {
            var node = spec.Expression.Nodes[index];
            var width = widths[index];
            switch (node)
            {
                case MathTextNode textNode:
                    foreach (var run in textNode.Runs)
                    {
                        var scriptScale = run.Script == MathScript.Normal ? 1d : 0.68d;
                        var runFontSize = Math.Max(MinimumReadableFontSize, (int)Math.Round(fontSize * scriptScale));
                        var runBaseline = run.Script switch
                        {
                            MathScript.Subscript => baseline + (fontSize * 0.30),
                            MathScript.Superscript => baseline - (fontSize * 0.48),
                            _ => baseline,
                        };
                        var runWidth = MeasureWidth(run.Value, runFontSize);
                        var runBounds = MeasureBounds(run.Value, cursor, runBaseline, runFontSize, run.Script);
                        if (run.Script != MathScript.Normal
                            && previousRunBounds is { } previous
                            && runBounds.Left < previous.Right - 3)
                        {
                            throw new InvalidOperationException(
                                $"Formula '{spec.TeX}' has overlapping subscript or superscript bounds.");
                        }

                        bounds = Union(bounds, runBounds);
                        items.Add(new MeasuredRun(run, cursor, runBaseline, runFontSize));
                        previousRunBounds = runBounds;
                        cursor += runWidth;
                    }

                    break;
                case MathFractionNode fractionNode:
                    var fractionFontSize = Math.Max(MinimumReadableFontSize, (int)Math.Round(fontSize * 0.72));
                    var numeratorWidth = MeasureWidth(fractionNode.Numerator, fractionFontSize);
                    var denominatorWidth = MeasureWidth(fractionNode.Denominator, fractionFontSize);
                    var fractionWidth = Math.Max(width - 8, Math.Max(numeratorWidth, denominatorWidth) + 8);
                    var centerX = cursor + (fractionWidth / 2);
                    var numeratorBaseline = baseline - 4;
                    var denominatorBaseline = baseline + 18;
                    var numeratorBounds = MeasureBounds(fractionNode.Numerator, centerX, numeratorBaseline, fractionFontSize, MathScript.Normal, "middle");
                    var denominatorBounds = MeasureBounds(fractionNode.Denominator, centerX, denominatorBaseline, fractionFontSize, MathScript.Normal, "middle");
                    var lineY = baseline + 2;
                    if (numeratorBounds.Bottom >= lineY - 0.5
                        || denominatorBounds.Top <= lineY + 0.5
                        || numeratorBounds.Bottom >= denominatorBounds.Top)
                    {
                        throw new InvalidOperationException(
                            $"Formula '{spec.TeX}' has overlapping fraction numerator and denominator.");
                    }

                    bounds = Union(bounds, numeratorBounds);
                    bounds = Union(bounds, denominatorBounds);
                    bounds = Union(bounds, new SKRect((float)(cursor + 2), (float)(baseline + 1), (float)(cursor + fractionWidth - 2), (float)(baseline + 3)));
                    items.Add(new MeasuredFraction(
                        fractionNode.Numerator,
                        fractionNode.Denominator,
                        centerX,
                        numeratorBaseline,
                        denominatorBaseline,
                        lineY,
                        cursor,
                        cursor + fractionWidth,
                        fractionFontSize));
                    break;
                default:
                    throw new InvalidOperationException($"Unsupported math node in '{spec.TeX}'.");
            }

            if (node is MathFractionNode)
            {
                cursor += width;
                previousRunBounds = null;
            }
        }

        return new MeasuredLayout(bounds, items, ["skia-font-measurement", "ast-single-source"]);
    }

    private static double MeasureWidth(string value, int fontSize)
    {
        using var paint = new SKPaint { IsAntialias = true };
        using var typeface = ResolveTypeface(value);
        using var font = new SKFont(typeface, fontSize);
        return Math.Max(4, font.MeasureText(value, paint));
    }

    private static SKRect MeasureBounds(
        string value,
        double x,
        double baseline,
        int fontSize,
        MathScript script,
        string anchor = "start")
    {
        using var paint = new SKPaint { IsAntialias = true };
        using var typeface = ResolveTypeface(value);
        using var font = new SKFont(typeface, fontSize);
        font.MeasureText(value, out var measured, paint);
        var left = x + measured.Left;
        if (anchor == "middle")
        {
            left -= measured.Width / 2;
        }
        else if (anchor == "end")
        {
            left -= measured.Width;
        }

        return new SKRect(
            (float)left,
            (float)(baseline + measured.Top),
            (float)(left + measured.Width),
            (float)(baseline + measured.Bottom));
    }

    private static SKTypeface ResolveTypeface(string text)
    {
        foreach (var family in MathFamilies)
        {
            var typeface = SKTypeface.FromFamilyName(family);
            if (typeface.ContainsGlyphs(text))
            {
                return typeface;
            }

            typeface.Dispose();
        }

        var manager = SKFontManager.Default;
        foreach (var character in text.Where(character => !char.IsWhiteSpace(character)))
        {
            var fallback = manager.MatchCharacter(character);
            if (fallback is not null && fallback.ContainsGlyphs(text))
            {
                return fallback;
            }

            fallback?.Dispose();
        }

        throw new InvalidOperationException($"No installed typeface can render formula text '{text}'.");
    }

    private static XElement CreateText(MathRun run, double x, double y, int fontSize, string color, string anchor = "start")
    {
        var element = new XElement(
            Svg + "text",
            new XAttribute("x", Number(x)),
            new XAttribute("y", Number(y)),
            new XAttribute("text-anchor", anchor),
            new XAttribute("font-family", "Cambria Math, STIX Two Math, Times New Roman"),
            new XAttribute("font-size", fontSize),
            new XAttribute("fill", color),
            run.Bold ? new XAttribute("font-weight", "700") : null,
            run.Italic ? new XAttribute("font-style", "italic") : null,
            new XAttribute("data-content-kind", "Entity"),
            run.Value);
        return element;
    }

    private static SKRect Union(SKRect first, SKRect second) =>
        first == SKRect.Empty
            ? second
            : SKRect.Union(first, second);

    private static string FormatBounds(SKRect bounds) =>
        string.Join(",", Number(bounds.Left), Number(bounds.Top), Number(bounds.Width), Number(bounds.Height));

    private static string Number(float value) => value.ToString("0.###", CultureInfo.InvariantCulture);
    private static string Number(double value) => value.ToString("0.###", CultureInfo.InvariantCulture);

    internal sealed record FormulaSpec(string TeX, MathExpression Expression);
    internal sealed record MathExpression(IReadOnlyList<MathNode> Nodes);
    internal abstract record MathNode
    {
        public abstract double MeasureWidth(int fontSize);
    }

    internal sealed record MathTextNode(IReadOnlyList<MathRun> Runs) : MathNode
    {
        public override double MeasureWidth(int fontSize) => Runs.Sum(run => run.MeasureWidth(fontSize));
    }

    internal sealed record MathFractionNode(string Numerator, string Denominator) : MathNode
    {
        public override double MeasureWidth(int fontSize) =>
            Math.Max(
                4,
                Math.Max(
                    ScientificMathLayout.MeasureWidth(Numerator, Math.Max(MinimumReadableFontSize, (int)Math.Round(fontSize * 0.72))),
                    ScientificMathLayout.MeasureWidth(Denominator, Math.Max(MinimumReadableFontSize, (int)Math.Round(fontSize * 0.72))))) + 8;
    }

    internal sealed record MathRun(string Value, MathScript Script, bool Bold, bool Italic)
    {
        public static MathRun Normal(string value) => new(value, MathScript.Normal, false, false);
        public static MathRun Vector(string value) => new(value, MathScript.Normal, true, true);
        public static MathRun ItalicRun(string value) => new(value, MathScript.Normal, false, true);
        public static MathRun Subscript(string value) => new(value, MathScript.Subscript, false, false);
        public static MathRun Superscript(string value) => new(value, MathScript.Superscript, false, false);

        public double MeasureWidth(int fontSize)
        {
            var scale = Script == MathScript.Normal ? 1d : 0.68d;
            return Math.Max(4, ScientificMathLayout.MeasureWidth(Value, Math.Max(MinimumReadableFontSize, (int)Math.Round(fontSize * scale))));
        }
    }

    internal sealed record FormulaPiece(string? Text, string? Denominator)
    {
        public static FormulaPiece Plain(string value) => new(value, null);
        public static FormulaPiece Fraction(string numerator, string denominator) => new(numerator, denominator);
    }

    internal enum MathScript
    {
        Normal,
        Subscript,
        Superscript,
    }

    private sealed record MeasuredLayout(SKRect Bounds, IReadOnlyList<MeasuredItem> Items, IReadOnlyList<string> Diagnostics);
    private abstract record MeasuredItem;
    private sealed record MeasuredRun(MathRun Run, double X, double Baseline, int FontSize) : MeasuredItem;
    private sealed record MeasuredFraction(
        string Numerator,
        string Denominator,
        double CenterX,
        double NumeratorBaseline,
        double DenominatorBaseline,
        double LineY,
        double Left,
        double Right,
        int FontSize) : MeasuredItem;

    internal sealed record ScientificMathLayoutResult(
        XElement Element,
        ScientificMathBounds Bounds,
        IReadOnlyList<string> Diagnostics);

    internal readonly record struct ScientificMathBounds(
        double Left,
        double Top,
        double Width,
        double Height)
    {
        public double Right => Left + Width;
        public double Bottom => Top + Height;
    }
}
