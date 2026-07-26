using System.Xml.Linq;
using ContentDeliveryStudio.Application.ScientificFigures;
using ContentDeliveryStudio.Core.ScientificFigures;
using ContentDeliveryStudio.Infrastructure.ScientificFigures;

namespace ContentDeliveryStudio.Tests;

public sealed class ScientificCheckpointTwoTests
{
    private static readonly XNamespace Svg = "http://www.w3.org/2000/svg";

    [Fact]
    public void ApprovedFakeSpecificationReachesTraceableHiddenExports()
    {
        var understanding = ScientificFigureTestFixture.ReadyUnderstanding();
        var claim = Assert.Single(understanding.Claims);
        var evidence = Assert.Single(claim.SupportingEvidence);
        var scientificElement = ScientificFigureTestFixture.RequiredElement(claim, evidence);
        var decoration = FigureElementSpec.Create(
            "element-decoration",
            "Non-evidentiary background geometry.",
            FigureElementKind.DecorativeAsset,
            labelOrFormula: null,
            "bounded-raster-asset",
            FigureContentRequirement.Optional,
            isCritical: false,
            provenance: null);
        var workflow = ScientificFigureWorkflow
            .Create(ScientificFigureTestFixture.CreateSpec(
                understanding,
                [scientificElement, decoration],
                [],
                []))
            .ApproveGate1(
                "checkpoint-two-reviewer",
                "Approved fake specification.",
                DateTimeOffset.Parse("2026-07-26T14:00:00Z"));

        var plan = new ScientificFigureSpecCompiler().Compile(workflow);
        var svg = new DeterministicSvgRenderer().Render(plan);
        var bundle = new ScientificFigureExporter().Export(
            new ScientificFigureExportRequest(svg, svg.Sha256, 1200, 800));
        var document = XDocument.Parse(svg.Svg);
        var decorationGroup = document.Descendants(Svg + "g")
            .Single(element => (string?)element.Attribute("id") == "render-element-decoration");

        Assert.Contains("render-element-force", svg.Svg, StringComparison.Ordinal);
        Assert.Equal("false", (string?)decorationGroup.Attribute("data-authoritative"));
        Assert.Equal(["png", "pdf"], bundle.Artifacts.Select(artifact => artifact.Format));
        Assert.All(bundle.Artifacts, artifact => Assert.Equal(svg.Sha256, artifact.SourceSvgSha256));
        Assert.False(ScientificFigureModule.IsUserVisible);
    }
}
