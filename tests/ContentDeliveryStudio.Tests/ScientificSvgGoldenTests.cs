using System.Xml.Linq;
using ContentDeliveryStudio.Infrastructure.ScientificFigures;

namespace ContentDeliveryStudio.Tests;

public sealed class ScientificSvgGoldenTests
{
    private static readonly XNamespace Svg = "http://www.w3.org/2000/svg";

    [Fact]
    public void Render_MatchesCanonicalScientificStructureGolden()
    {
        var artifact = new DeterministicSvgRenderer().Render(
            DeterministicSvgRendererTests.Plan());
        var document = XDocument.Parse(artifact.Svg);
        var semanticSnapshot = document
            .Descendants()
            .Where(element => element.Attribute("id") is not null)
            .Select(element =>
                $"{element.Name.LocalName}:{(string)element.Attribute("id")!}:{(string?)element.Attribute("data-spec-id") ?? "-"}")
            .ToArray();

        Assert.Equal(
            [
                "title:svg-title:-",
                "desc:svg-description:-",
                "marker:arrowhead:-",
                "g:layer-scientific-content:-",
                "path:render-relation-force-formula:relation-force-formula",
                "g:render-element-force:element-force",
                "g:render-element-formula:element-formula",
            ],
            semanticSnapshot);
    }

    [Fact]
    public void Render_EmitsAccessibilityAndAuthorityMetadata()
    {
        var artifact = new DeterministicSvgRenderer().Render(
            DeterministicSvgRendererTests.Plan());
        var document = XDocument.Parse(artifact.Svg);
        var root = document.Root!;
        var metadata = root.Element(Svg + "metadata")!;

        Assert.Equal("img", (string?)root.Attribute("role"));
        Assert.Equal("svg-title svg-description", (string?)root.Attribute("aria-labelledby"));
        Assert.Contains("render-plan:test:v1", metadata.Value, StringComparison.Ordinal);
        Assert.Contains("37d178b6-e2bd-4e75-89b7-d64858919d41", metadata.Value, StringComparison.OrdinalIgnoreCase);
    }
}
