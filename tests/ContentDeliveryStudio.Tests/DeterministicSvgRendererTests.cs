using System.Xml.Linq;
using ContentDeliveryStudio.Core.ScientificFigures;
using ContentDeliveryStudio.Infrastructure.ScientificFigures;

namespace ContentDeliveryStudio.Tests;

public sealed class DeterministicSvgRendererTests
{
    private static readonly XNamespace Svg = "http://www.w3.org/2000/svg";

    [Fact]
    public void Render_RepeatedPlanProducesIdenticalSvgAndHash()
    {
        var renderer = new DeterministicSvgRenderer();
        var plan = Plan();

        var first = renderer.Render(plan);
        var second = renderer.Render(plan);

        Assert.Equal(first.Svg, second.Svg);
        Assert.Equal(first.Sha256, second.Sha256);
        Assert.Equal(plan.PlanId, first.PlanId);
    }

    [Fact]
    public void Render_PreservesStableElementRelationAndProvenanceIds()
    {
        var artifact = new DeterministicSvgRenderer().Render(Plan());
        var document = XDocument.Parse(artifact.Svg);

        var force = document.Descendants(Svg + "g")
            .Single(element => (string?)element.Attribute("id") == "render-element-force");
        var relation = document.Descendants(Svg + "path")
            .Single(element => (string?)element.Attribute("id") == "render-relation-force-formula");

        Assert.Equal("element-force", (string?)force.Attribute("data-spec-id"));
        Assert.Equal("ClaimEvidence", (string?)force.Attribute("data-provenance-kind"));
        Assert.Equal("relation-force-formula", (string?)relation.Attribute("data-spec-id"));
        Assert.Equal("ClaimEvidence", (string?)relation.Attribute("data-provenance-kind"));
    }

    [Fact]
    public void Render_PreservesExactFormulaAndEscapesXml()
    {
        var artifact = new DeterministicSvgRenderer().Render(Plan());
        var document = XDocument.Parse(artifact.Svg);
        var formula = document.Descendants(Svg + "text")
            .Single(element => (string?)element.Attribute("data-content-kind") == "Formula");

        Assert.Equal("F < m × a & bounded", formula.Value);
        Assert.Contains("F &lt; m × a &amp; bounded", artifact.Svg, StringComparison.Ordinal);
    }

    [Fact]
    public void Render_DecorativeAssetsCannotIntroduceAuthoritativeStructure()
    {
        var plan = Plan(includeDecoration: true);
        var document = XDocument.Parse(new DeterministicSvgRenderer().Render(plan).Svg);
        var decoration = document.Descendants(Svg + "g")
            .Single(element => (string?)element.Attribute("id") == "render-decoration");

        Assert.Equal("false", (string?)decoration.Attribute("data-authoritative"));
        Assert.Empty(decoration.Descendants(Svg + "text"));
        Assert.Empty(decoration.Descendants(Svg + "path"));
        Assert.Empty(decoration.Descendants(Svg + "image"));
    }

    [Fact]
    public void Render_MultipleScientificLayersEmitEachRelationOnce()
    {
        var original = Plan();
        var plan = SvgRenderPlan.Create(
            original.PlanId,
            original.SpecificationId,
            original.SpecificationVersion,
            original.Canvas,
            [
                .. original.Layers,
                new SvgRenderLayer("scientific-annotations", 1, IsScientific: true),
            ],
            original.Elements,
            original.Connections,
            original.Accessibility,
            original.Export,
            original.LayoutConstraints,
            original.StyleTokens);

        var document = XDocument.Parse(new DeterministicSvgRenderer().Render(plan).Svg);
        var relations = document.Descendants(Svg + "path")
            .Where(element => (string?)element.Attribute("id") == "render-relation-force-formula");

        Assert.Single(relations);
    }

    internal static SvgRenderPlan Plan(bool includeDecoration = false)
    {
        var elements = new List<SvgRenderElement>
        {
            new(
                "render-element-force",
                "element-force",
                FigureElementKind.Entity,
                "Net force acting on the body.",
                "Net force",
                "deterministic-node",
                "scientific-content",
                IsCritical: true,
                ScientificProvenanceKind.ClaimEvidence),
            new(
                "render-element-formula",
                "element-formula",
                FigureElementKind.Formula,
                "A bounded formula example.",
                "F < m × a & bounded",
                "deterministic-formula",
                "scientific-content",
                IsCritical: true,
                ScientificProvenanceKind.ScientificConvention),
        };
        var layers = new List<SvgRenderLayer>
        {
            new("scientific-content", 0, IsScientific: true),
        };
        if (includeDecoration)
        {
            layers.Insert(0, new SvgRenderLayer("decorative", -1, IsScientific: false));
            elements.Add(new SvgRenderElement(
                "render-decoration",
                "element-decoration",
                FigureElementKind.DecorativeAsset,
                "Non-evidentiary background shape.",
                ExactContent: null,
                "bounded-raster-asset",
                "decorative",
                IsCritical: false,
                ProvenanceKind: null));
        }

        return SvgRenderPlan.Create(
            "render-plan:test:v1",
            Guid.Parse("37D178B6-E2BD-4E75-89B7-D64858919D41"),
            specificationVersion: 1,
            new SvgCanvas(1200, 800, "0 0 1200 800"),
            layers,
            elements,
            [
                new SvgRenderConnection(
                    "render-relation-force-formula",
                    "relation-force-formula",
                    "render-element-force",
                    "render-element-formula",
                    FigureRelationKind.AssociatesWith,
                    FigureRelationDirection.Directed,
                    "constrains",
                    "single directed arrow",
                    IsCritical: true,
                    ScientificProvenanceKind.ClaimEvidence),
            ],
            new SvgAccessibilityMetadata(
                "Force and formula",
                "A source-grounded relationship between force and a bounded formula."),
            new SvgExportSettings("svg", IncludeMetadata: true),
            [
                new SvgLayoutConstraint("canvas-padding", "padding", 48),
                new SvgLayoutConstraint("minimum-item-spacing", "minimum-spacing", 24),
            ],
            new Dictionary<string, string>
            {
                ["font-family"] = "Segoe UI",
                ["scientific-stroke"] = "#1F2937",
                ["scientific-fill"] = "#F8FAFC",
                ["accent-fill"] = "#0F766E",
            });
    }
}
