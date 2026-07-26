using ContentDeliveryStudio.Application.ScientificFigures;
using ContentDeliveryStudio.Core.ScientificFigures;
using ContentDeliveryStudio.Infrastructure.ScientificFigures;
using SkiaSharp;
using UglyToad.PdfPig;

namespace ContentDeliveryStudio.Tests;

public sealed class ScientificExportEquivalenceTests
{
    [Fact]
    public void Export_PreservesFormulaRelationAccessibilityAndProvenanceFixtures()
    {
        var original = DeterministicSvgRendererTests.Plan();
        var plan = SvgRenderPlan.Create(
            original.PlanId,
            original.SpecificationId,
            original.SpecificationVersion,
            original.Canvas,
            original.Layers,
            [
                .. original.Elements,
                new SvgRenderElement(
                    "render-element-legend",
                    "element-legend",
                    FigureElementKind.Legend,
                    "Legend for the approved force relation.",
                    "Legend: force relation",
                    "deterministic-legend",
                    "scientific-content",
                    IsCritical: true,
                    ScientificProvenanceKind.ScientificConvention),
            ],
            original.Connections,
            original.Accessibility,
            original.Export,
            original.LayoutConstraints,
            original.StyleTokens);
        var svg = new DeterministicSvgRenderer().Render(plan);

        var bundle = new ScientificFigureExporter().Export(
            new ScientificFigureExportRequest(svg, svg.Sha256, Width: 1200, Height: 800));

        Assert.Equal("Force and formula", bundle.Semantics.AccessibilityTitle);
        Assert.Contains(
            "source-grounded relationship",
            bundle.Semantics.AccessibilityDescription,
            StringComparison.Ordinal);

        var formula = Assert.Single(
            bundle.Semantics.TextFixtures,
            fixture => fixture.ContentKind == "Formula");
        Assert.Equal("render-element-formula", formula.ElementId);
        Assert.Equal("element-formula", formula.SourceSpecificationItemId);
        Assert.Equal("F < m × a & bounded", formula.Text);
        Assert.Contains(
            bundle.Semantics.ElementFixtures,
            fixture => fixture.ElementId == formula.ElementId
                && fixture.ProvenanceKind == "ScientificConvention");

        var legend = Assert.Single(
            bundle.Semantics.TextFixtures,
            fixture => fixture.ContentKind == "Legend");
        Assert.Equal("Legend: force relation", legend.Text);

        var relation = Assert.Single(bundle.Semantics.RelationFixtures);
        Assert.Equal("render-relation-force-formula", relation.RelationId);
        Assert.Equal("relation-force-formula", relation.SourceSpecificationItemId);
        Assert.Equal("Directed", relation.Direction);
        Assert.Equal("constrains", relation.Label);
        Assert.Equal("ClaimEvidence", relation.ProvenanceKind);

        Assert.All(
            bundle.Artifacts,
            artifact => Assert.Equal(bundle.SemanticSha256, artifact.SemanticSha256));

        var png = Assert.Single(bundle.Artifacts, artifact => artifact.Format == "png");
        using var bitmap = SKBitmap.Decode(png.Bytes);
        Assert.NotNull(bitmap);
        Assert.Contains(bitmap.Pixels, pixel => pixel != SKColors.White);

        var pdf = Assert.Single(bundle.Artifacts, artifact => artifact.Format == "pdf");
        using var document = PdfDocument.Open(pdf.Bytes);
        var exportedText = string.Join(" ", document.GetPages().Select(page => page.Text));
        Assert.Contains("Net force", exportedText, StringComparison.Ordinal);
        Assert.Contains("bounded", exportedText, StringComparison.Ordinal);
        Assert.Contains("constrains", exportedText, StringComparison.Ordinal);
        Assert.Contains("Legend", exportedText, StringComparison.Ordinal);
    }
}
