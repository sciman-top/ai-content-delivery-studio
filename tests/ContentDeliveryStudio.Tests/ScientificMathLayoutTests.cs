using System.Globalization;
using System.Xml.Linq;
using ContentDeliveryStudio.Infrastructure.ScientificFigures;

namespace ContentDeliveryStudio.Tests;

public sealed class ScientificMathLayoutTests
{
    private static readonly XNamespace Svg = "http://www.w3.org/2000/svg";

    [Fact]
    public void Corpus_UsesOneAstForTexVisibleSvgAndAria()
    {
        var layout = new ScientificMathLayout();
        foreach (var spec in Corpus())
        {
            var result = layout.Layout(spec, 600, 300, 18, "#172033", "middle");
            var element = result.Element;

            Assert.Equal(spec.TeX, (string?)element.Attribute("data-math-tex"));
            Assert.Equal(spec.TeX, (string?)element.Attribute("aria-label"));
            Assert.Contains("skia-font-measurement", (string?)element.Attribute("data-math-diagnostics"));
            var bounds = ParseBounds((string)element.Attribute("data-math-bounds")!);
            Assert.Equal(bounds.Left, result.Bounds.Left, 3);
            Assert.Equal(bounds.Top, result.Bounds.Top, 3);
            Assert.Equal(bounds.Right - bounds.Left, result.Bounds.Width, 3);
            Assert.Equal(bounds.Bottom - bounds.Top, result.Bounds.Height, 3);
            Assert.InRange(result.Bounds.Left, 0, 1200);
            Assert.InRange(result.Bounds.Right, 0, 1200);
            Assert.InRange(result.Bounds.Top, 0, 800);
            Assert.InRange(result.Bounds.Bottom, 0, 800);
            Assert.NotEmpty(element.Descendants(Svg + "text"));
        }
    }

    [Fact]
    public void Layout_UsesFailClosedBoundsAndReadableFontGate()
    {
        var layout = new ScientificMathLayout();
        var spec = Corpus().Single(item => item.TeX == @"\mathbf{F}_{\mathrm{inertial}}");

        var tooSmall = Assert.Throws<InvalidOperationException>(() =>
            layout.Render(spec, 600, 300, 9, "#172033", "middle"));
        Assert.Contains("minimum readable size", tooSmall.Message, StringComparison.Ordinal);

        var outOfBounds = Assert.Throws<InvalidOperationException>(() =>
            layout.Render(spec, 1199, 300, 18, "#172033", "start"));
        Assert.Contains("outside the SVG canvas", outOfBounds.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(@"F_g=", true)]
    [InlineData(@"a_cf", true)]
    [InlineData("普通中文文本", false)]
    [InlineData("W/(m·K)", false)]
    public void OrdinaryTextFormulaDetector_IsConservative(string value, bool expected)
    {
        Assert.Equal(expected, ScientificMathLayout.LooksLikeUnparsedFormula(value));
    }

    private static IReadOnlyList<ScientificMathLayout.FormulaSpec> Corpus() =>
    [
        new(@"F_g", new([
            new ScientificMathLayout.MathTextNode([
                ScientificMathLayout.MathRun.Vector("F"),
                ScientificMathLayout.MathRun.Subscript("g")])])),
        new(@"\mathbf{g}_{\mathrm{eff}}", new([
            new ScientificMathLayout.MathTextNode([
                ScientificMathLayout.MathRun.Vector("g"),
                ScientificMathLayout.MathRun.Subscript("eff")])])),
        new(@"\mathbf{g}_{\mathrm{grav}}", new([
            new ScientificMathLayout.MathTextNode([
                ScientificMathLayout.MathRun.Vector("g"),
                ScientificMathLayout.MathRun.Subscript("grav")])])),
        new(@"\mathbf{a}_{\mathrm{cf}}=\omega^2r_\perp", new([
            new ScientificMathLayout.MathTextNode([
                ScientificMathLayout.MathRun.Vector("a"),
                ScientificMathLayout.MathRun.Subscript("cf"),
                ScientificMathLayout.MathRun.Normal(" = ω"),
                ScientificMathLayout.MathRun.Superscript("2"),
                ScientificMathLayout.MathRun.Normal("r"),
                ScientificMathLayout.MathRun.Subscript("⊥")])])),
        new(@"\mathbf{a}_{\mathrm{orbit}}\ne0", new([
            new ScientificMathLayout.MathTextNode([
                ScientificMathLayout.MathRun.Vector("a"),
                ScientificMathLayout.MathRun.Subscript("orbit"),
                ScientificMathLayout.MathRun.Normal(" ≠ 0")])])),
        new(@"\mathbf{F}_{\mathrm{inertial}}", new([
            new ScientificMathLayout.MathTextNode([
                ScientificMathLayout.MathRun.Vector("F"),
                ScientificMathLayout.MathRun.Subscript("inertial")])])),
        new(@"\mathbf{R}+m\mathbf{g}_{\mathrm{eff}}=0", new([
            new ScientificMathLayout.MathTextNode([
                ScientificMathLayout.MathRun.Vector("R"),
                ScientificMathLayout.MathRun.Normal(" + m"),
                ScientificMathLayout.MathRun.Vector("g"),
                ScientificMathLayout.MathRun.Subscript("eff"),
                ScientificMathLayout.MathRun.Normal(" = 0")])])),
        new(@"x=\frac{u}{f}", new([
            new ScientificMathLayout.MathTextNode([ScientificMathLayout.MathRun.Normal("x = ")]),
            new ScientificMathLayout.MathFractionNode("u", "f")]))
    ];

    private static Bounds ParseBounds(string value)
    {
        var values = value.Split(',').Select(item =>
            double.Parse(item, CultureInfo.InvariantCulture)).ToArray();
        return new(values[0], values[1], values[0] + values[2], values[1] + values[3]);
    }

    private readonly record struct Bounds(double Left, double Top, double Right, double Bottom);
}
