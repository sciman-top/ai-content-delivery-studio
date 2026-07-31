using System.Security.Cryptography;
using System.Text;
using System.Xml.Linq;
using ContentDeliveryStudio.Application.ScientificFigures;
using ContentDeliveryStudio.Core.ScientificFigures;
using ContentDeliveryStudio.Infrastructure.ScientificFigures;

namespace ContentDeliveryStudio.Tests;

public sealed class ScientificMutationTests
{
    [Fact]
    public void Review_BlocksMissingRequiredElementEvenAtMaximumScore()
    {
        var fixture = ScientificContractReviewFixture.Create(advisoryScore: 1);
        var mutatedPlan = fixture.CopyPlan(
            elements: fixture.Plan.Elements
                .Where(item => item.SourceSpecificationItemId != "element-formula")
                .ToArray());

        var report = Review(fixture, fixture.Request with { RenderPlan = mutatedPlan });

        AssertHardFailure(report, "required-element-missing-from-plan", "element-formula");
        Assert.Equal(1, report.AdvisoryScore);
    }

    [Fact]
    public void Review_BlocksExtraScientificContent()
    {
        var fixture = ScientificContractReviewFixture.Create();
        var extra = new SvgRenderElement(
            "render-extra-claim",
            "unapproved-extra-claim",
            FigureElementKind.Entity,
            "An unsupported scientific claim.",
            "Unsupported",
            "deterministic-node",
            "scientific-content",
            IsCritical: true,
            ScientificProvenanceKind.ClaimEvidence);
        var mutatedPlan = fixture.CopyPlan(elements: [.. fixture.Plan.Elements, extra]);

        var report = Review(fixture, fixture.Request with { RenderPlan = mutatedPlan });

        AssertHardFailure(report, "extra-scientific-element-in-plan", extra.RenderElementId);
    }

    [Fact]
    public void Review_BlocksReversedArrowMarkers()
    {
        var fixture = ScientificContractReviewFixture.Create();
        var mutatedText = fixture.Svg.Svg.Replace(
            "marker-end=\"url(#arrowhead)\"",
            "marker-start=\"url(#arrowhead)\"",
            StringComparison.Ordinal);
        Assert.NotEqual(fixture.Svg.Svg, mutatedText);
        var mutatedSvg = fixture.Svg with
        {
            Svg = mutatedText,
            Sha256 = Hash(Encoding.UTF8.GetBytes(mutatedText)),
        };
        var mutatedExports = new ScientificFigureExporter().Export(
            new ScientificFigureExportRequest(
                mutatedSvg,
                mutatedSvg.Sha256,
                fixture.Exports.Width,
                fixture.Exports.Height));

        var report = Review(
            fixture,
            fixture.Request with { Svg = mutatedSvg, Exports = mutatedExports });

        AssertHardFailure(
            report,
            "relation-direction-drift-in-svg",
            "relation-force-acceleration");
    }

    [Fact]
    public void Review_BlocksOverlappingCriticalRelationLabelBackgrounds()
    {
        var fixture = ScientificContractReviewFixture.Create();
        var document = XDocument.Parse(fixture.Svg.Svg);
        var svg = (XNamespace)"http://www.w3.org/2000/svg";
        var labelBackground = document.Descendants(svg + "rect").Single(item =>
            (string?)item.Attribute("data-relation-label-background") == "true");
        labelBackground.AddAfterSelf(new XElement(labelBackground));
        var mutatedText = document.ToString(SaveOptions.DisableFormatting);
        var mutatedSvg = fixture.Svg with
        {
            Svg = mutatedText,
            Sha256 = Hash(Encoding.UTF8.GetBytes(mutatedText)),
        };
        var mutatedExports = new ScientificFigureExporter().Export(
            new ScientificFigureExportRequest(
                mutatedSvg,
                mutatedSvg.Sha256,
                fixture.Exports.Width,
                fixture.Exports.Height));

        var report = Review(
            fixture,
            fixture.Request with { Svg = mutatedSvg, Exports = mutatedExports });

        AssertHardFailure(
            report,
            "critical-relation-label-overlap",
            "render-relation-force-acceleration");
    }

    [Theory]
    [InlineData("element-formula", "F = m / a")]
    [InlineData("element-value", "9.18")]
    [InlineData("element-unit", "km/s^2")]
    public void Review_BlocksFormulaValueAndUnitDrift(string itemId, string mutation)
    {
        var fixture = ScientificContractReviewFixture.Create();
        var elements = fixture.Plan.Elements.Select(item =>
            item.SourceSpecificationItemId == itemId
                ? item with { ExactContent = mutation }
                : item).ToArray();
        var mutatedPlan = fixture.CopyPlan(elements: elements);

        var report = Review(fixture, fixture.Request with { RenderPlan = mutatedPlan });

        AssertHardFailure(report, "scientific-element-content-drift", itemId);
    }

    [Fact]
    public void Review_BlocksExportByteDrift()
    {
        var fixture = ScientificContractReviewFixture.Create();
        var artifacts = fixture.Exports.Artifacts.Select(artifact =>
            artifact.Format == "png"
                ? artifact with { Bytes = [.. artifact.Bytes, 0x00] }
                : artifact).ToArray();
        var mutatedExports = fixture.Exports with { Artifacts = artifacts };

        var report = Review(
            fixture,
            fixture.Request with { Exports = mutatedExports });

        AssertHardFailure(report, "export-artifact-hash-drift", "png");
    }

    private static ScientificContractReviewReport Review(
        ScientificContractReviewFixture fixture,
        ScientificContractReviewRequest request)
    {
        Assert.Equal(fixture.Specification, request.Specification);
        return new ScientificContractReviewer().Review(request);
    }

    private static void AssertHardFailure(
        ScientificContractReviewReport report,
        string code,
        string responsibleItemId)
    {
        Assert.False(report.Passed);
        Assert.Contains(
            report.HardFailures,
            finding => finding.Code == code
                && finding.ResponsibleItemId == responsibleItemId
                && !string.IsNullOrWhiteSpace(finding.Evidence));
    }

    private static string Hash(byte[] bytes)
    {
        return $"sha256:{Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant()}";
    }
}
