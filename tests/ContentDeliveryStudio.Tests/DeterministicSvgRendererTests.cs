using System.Xml.Linq;
using ContentDeliveryStudio.Application.ScientificFigures;
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
    public void Render_EntityWithoutExactContentDisplaysApprovedScientificMeaning()
    {
        var original = Plan();
        var entity = original.Elements[0] with { ExactContent = null };
        var plan = SvgRenderPlan.Create(
            original.PlanId,
            original.SpecificationId,
            original.SpecificationVersion,
            original.Canvas,
            original.Layers,
            [entity, original.Elements[1]],
            original.Connections,
            original.Accessibility,
            original.Export,
            original.LayoutConstraints,
            original.StyleTokens);

        var document = XDocument.Parse(new DeterministicSvgRenderer().Render(plan).Svg);
        var group = document.Descendants(Svg + "g")
            .Single(element => (string?)element.Attribute("id") == "render-element-force");
        var display = group.Elements(Svg + "text")
            .Where(element => (string?)element.Attribute("data-display-role") == "true")
            .OrderBy(element => (int?)element.Attribute("data-label-line"))
            .ToArray();

        Assert.NotEmpty(display);
        Assert.Equal(entity.ScientificMeaning, string.Concat(display.Select(item => item.Value)));
        Assert.All(display, item => Assert.Null(item.Attribute("data-content-kind")));
    }

    [Fact]
    public void Render_LongRelationLabelPreservesExactTextAndSeparatesItFromLine()
    {
        var original = Plan();
        const string label = "changing-flux-induces-sinusoidal-electromotive-force";
        var plan = SvgRenderPlan.Create(
            original.PlanId,
            original.SpecificationId,
            original.SpecificationVersion,
            original.Canvas,
            original.Layers,
            original.Elements,
            [original.Connections[0] with { Label = label }],
            original.Accessibility,
            original.Export,
            original.LayoutConstraints,
            original.StyleTokens);

        var document = XDocument.Parse(new DeterministicSvgRenderer().Render(plan).Svg);
        var relationText = document.Descendants(Svg + "text")
            .Where(element => (string?)element.Attribute("data-relation-label") == "true")
            .OrderBy(element => (int?)element.Attribute("data-label-line"))
            .ToArray();

        Assert.True(relationText.Length > 1);
        Assert.Equal(label, string.Concat(relationText.Select(item => item.Value)));
        Assert.All(relationText, item => Assert.NotEqual("392", (string?)item.Attribute("y")));
        Assert.Contains(
            document.Descendants(Svg + "rect"),
            element => (string?)element.Attribute("data-relation-label-background") == "true");
        Assert.All(
            relationText,
            item => Assert.True(
                double.Parse((string)item.Attribute("y")!, System.Globalization.CultureInfo.InvariantCulture) > 480));
    }

    [Fact]
    public async Task Render_DisconnectedMechanismChainsOccupySeparateRowsInCausalOrder()
    {
        var repositoryRoot = FindRepositoryRoot();
        var corpus = await ScientificFigureCorpusBaselineLoader.LoadAsync(
            Path.Combine(repositoryRoot, "eval", "scientific-figures", "corpus.json"),
            CancellationToken.None);
        var item = corpus.Items.Single(item =>
            item.ItemId == "electromagnetism-rotating-coil-generator");
        var model = ScientificFigureCorpusRunner.BuildModel(item);
        var workflow = ScientificFigureWorkflow.Create(model.Specification)
            .ApproveGate1(
                "renderer-layout-test",
                "Accepted baseline layout projection.",
                DateTimeOffset.Parse("2026-07-26T00:00:00Z", System.Globalization.CultureInfo.InvariantCulture));
        var plan = new ScientificFigureSpecCompiler().Compile(workflow);
        var document = XDocument.Parse(new DeterministicSvgRenderer().Render(plan).Svg);

        (double X, double Y) Position(string specificationItemId)
        {
            var rectangle = document.Descendants(Svg + "g")
                .Single(element =>
                    (string?)element.Attribute("data-spec-id") == specificationItemId)
                .Element(Svg + "rect")!;
            return (
                double.Parse((string)rectangle.Attribute("x")!, System.Globalization.CultureInfo.InvariantCulture),
                double.Parse((string)rectangle.Attribute("y")!, System.Globalization.CultureInfo.InvariantCulture));
        }

        var main = new[]
        {
            Position("shaft-input"),
            Position("rotating-coil"),
            Position("emf-waveform"),
            Position("external-load"),
        };
        var secondary = new[]
        {
            Position("uniform-magnetic-field"),
            Position("vertical-wire-segments"),
        };

        Assert.Single(main.Select(item => item.Y).Distinct());
        Assert.Single(secondary.Select(item => item.Y).Distinct());
        Assert.NotEqual(main[0].Y, secondary[0].Y);
        Assert.True(main.Zip(main.Skip(1)).All(pair => pair.First.X < pair.Second.X));
        Assert.True(secondary[0].X < secondary[1].X);
        Assert.All(
            ["uniform-magnetic-field", "rotating-coil", "vertical-wire-segments", "emf-waveform"],
            specificationItemId =>
            {
                var group = document.Descendants(Svg + "g").Single(element =>
                    (string?)element.Attribute("data-spec-id") == specificationItemId);
                Assert.Contains(
                    group.Elements(Svg + "path"),
                    path => (string?)path.Attribute("data-element-graphic") == "true");
            });
    }

    [Fact]
    public void Render_ParallelRelationsUseDistinctPathsAndLabelBands()
    {
        var original = Plan();
        var second = original.Connections[0] with
        {
            RenderConnectionId = "render-relation-force-formula-secondary",
            SourceSpecificationItemId = "relation-force-formula-secondary",
            Label = "secondary relation",
        };
        var plan = SvgRenderPlan.Create(
            original.PlanId,
            original.SpecificationId,
            original.SpecificationVersion,
            original.Canvas,
            original.Layers,
            original.Elements,
            [original.Connections[0], second],
            original.Accessibility,
            original.Export,
            original.LayoutConstraints,
            original.StyleTokens);

        var document = XDocument.Parse(new DeterministicSvgRenderer().Render(plan).Svg);
        var paths = document.Descendants(Svg + "path")
            .Where(item => item.Attribute("data-relation-kind") is not null)
            .ToArray();
        var labelGroups = document.Descendants(Svg + "g")
            .Where(item => item.Attribute("data-connection-group") is not null)
            .ToArray();

        Assert.Equal(2, paths.Length);
        Assert.Equal(2, paths.Select(item => (string?)item.Attribute("d")).Distinct().Count());
        Assert.Equal(
            2,
            labelGroups.Select(group => group.Elements(Svg + "text").First().Attribute("y")?.Value)
                .Distinct(StringComparer.Ordinal)
                .Count());
    }

    [Fact]
    public async Task Render_PhotoelectricSummaryPlacesSurfaceBetweenPhotonsAndElectrons()
    {
        var repositoryRoot = FindRepositoryRoot();
        var corpus = await ScientificFigureCorpusBaselineLoader.LoadAsync(
            Path.Combine(repositoryRoot, "eval", "scientific-figures", "corpus.json"),
            CancellationToken.None);
        var item = corpus.Items.Single(item =>
            item.ItemId == "quantum-photoelectric-threshold-summary");
        var model = ScientificFigureCorpusRunner.BuildModel(item);
        var workflow = ScientificFigureWorkflow.Create(model.Specification)
            .ApproveGate1(
                "renderer-layout-test",
                "Accepted baseline layout projection.",
                DateTimeOffset.Parse("2026-07-26T00:00:00Z", System.Globalization.CultureInfo.InvariantCulture));
        var plan = new ScientificFigureSpecCompiler().Compile(workflow);
        var document = XDocument.Parse(new DeterministicSvgRenderer().Render(plan).Svg);

        (double X, double Y) Position(string specificationItemId)
        {
            var rectangle = document.Descendants(Svg + "g")
                .Single(element =>
                    (string?)element.Attribute("data-spec-id") == specificationItemId)
                .Element(Svg + "rect")!;
            return (
                double.Parse((string)rectangle.Attribute("x")!, System.Globalization.CultureInfo.InvariantCulture),
                double.Parse((string)rectangle.Attribute("y")!, System.Globalization.CultureInfo.InvariantCulture));
        }

        var photons = Position("incident-photons");
        var surface = Position("metal-surface");
        var electrons = Position("ejected-electrons");

        Assert.Equal(photons.Y, surface.Y);
        Assert.Equal(surface.Y, electrons.Y);
        Assert.True(photons.X < surface.X && surface.X < electrons.X);
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

    [Fact]
    public void Render_DirectedRelationTerminatesAtNodeBoundaries()
    {
        var document = XDocument.Parse(
            new DeterministicSvgRenderer().Render(Plan()).Svg);
        var relation = document.Descendants(Svg + "path")
            .Single(element => (string?)element.Attribute("id") == "render-relation-force-formula");

        Assert.Equal("M 458 400 L 742 400", (string?)relation.Attribute("d"));
        Assert.Equal("url(#arrowhead)", (string?)relation.Attribute("marker-end"));
    }

    [Fact]
    public void Render_BidirectionalRelationReversesTheStartMarker()
    {
        var original = Plan();
        var plan = SvgRenderPlan.Create(
            original.PlanId,
            original.SpecificationId,
            original.SpecificationVersion,
            original.Canvas,
            original.Layers,
            original.Elements,
            original.Connections
                .Select(connection => connection with
                {
                    Direction = FigureRelationDirection.Bidirectional,
                })
                .ToArray(),
            original.Accessibility,
            original.Export,
            original.LayoutConstraints,
            original.StyleTokens);

        var document = XDocument.Parse(new DeterministicSvgRenderer().Render(plan).Svg);
        var relation = document.Descendants(Svg + "path")
            .Single(element => (string?)element.Attribute("id") == "render-relation-force-formula");
        var marker = document.Descendants(Svg + "marker").Single();

        Assert.Equal("url(#arrowhead)", (string?)relation.Attribute("marker-start"));
        Assert.Equal("url(#arrowhead)", (string?)relation.Attribute("marker-end"));
        Assert.Equal("auto-start-reverse", (string?)marker.Attribute("orient"));
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

    private static string FindRepositoryRoot()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory);
             directory is not null;
             directory = directory.Parent)
        {
            if (File.Exists(Path.Combine(directory.FullName, "ContentDeliveryStudio.sln")))
            {
                return directory.FullName;
            }
        }

        throw new DirectoryNotFoundException("Could not locate ContentDeliveryStudio.sln.");
    }
}
