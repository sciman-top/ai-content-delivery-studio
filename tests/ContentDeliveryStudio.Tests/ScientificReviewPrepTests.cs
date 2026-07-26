using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;
using ContentDeliveryStudio.Application.ScientificFigures;
using ContentDeliveryStudio.Core.Providers;
using ContentDeliveryStudio.Infrastructure.ScientificFigures;

namespace ContentDeliveryStudio.Tests;

public sealed class ScientificReviewPrepTests
{
    [Fact]
    public void Build_CreatesStructuralRowAndRealCropForEveryCriticalItem()
    {
        var fixture = ScientificContractReviewFixture.Create();

        var bundle = Build(fixture);

        var criticalIds = fixture.Plan.Elements.Where(item => item.IsCritical)
            .Select(item => item.SourceSpecificationItemId)
            .Concat(fixture.Plan.Connections.Where(item => item.IsCritical)
                .Select(item => item.SourceSpecificationItemId))
            .OrderBy(item => item, StringComparer.Ordinal)
            .ToArray();
        Assert.Equal(
            criticalIds,
            bundle.Manifest.StructureRows
                .Select(item => item.ResponsibleItemId)
                .OrderBy(item => item, StringComparer.Ordinal));
        Assert.Equal(criticalIds.Length, bundle.VisualRequest.RegionCrops.Count);
        Assert.All(
            bundle.VisualRequest.RegionCrops,
            crop =>
            {
                Assert.True(crop.Bytes.Length > 8);
                Assert.Equal(
                    new byte[] { 137, 80, 78, 71 },
                    crop.Bytes.Take(4).ToArray());
            });
        Assert.Contains(
            bundle.Manifest.StructureRows,
            item => item.Kind == ScientificVisualRegionKind.Relation);
        Assert.Contains(
            bundle.Manifest.StructureRows,
            item => item.Kind == ScientificVisualRegionKind.Formula);
    }

    [Fact]
    public void Build_ManifestContainsOnlyMinimumEvidenceAndNoPrivatePaths()
    {
        var fixture = ScientificContractReviewFixture.Create();

        var bundle = Build(fixture);
        var json = JsonSerializer.Serialize(bundle.Manifest);

        var evidence = Assert.Single(bundle.Manifest.EvidenceSelections);
        Assert.Equal("claim-newton-second-law", evidence.ClaimId);
        Assert.Equal("block-dynamics", evidence.SourceBlockId);
        Assert.DoesNotContain(@"C:\", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(@"D:\", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("/Users/", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("/home/", json, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Build_RedactsPrivatePathFromExportableStructureSummary()
    {
        var fixture = ScientificContractReviewFixture.Create();
        var elements = fixture.Plan.Elements.Select((item, index) =>
            index == 0
                ? item with
                {
                    ScientificMeaning = @"Loaded from D:\private\research\source.pdf",
                }
                : item).ToArray();
        var plan = fixture.CopyPlan(elements: elements);
        var svg = new DeterministicSvgRenderer().Render(plan);
        var exports = new ScientificFigureExporter().Export(
            new ScientificFigureExportRequest(svg, svg.Sha256, 1200, 800));

        var bundle = new ScientificReviewPrepBuilder(
            new SkiaScientificReviewImageCropper()).Build(
                fixture.Understanding,
                fixture.Specification,
                plan,
                svg,
                exports);
        var json = JsonSerializer.Serialize(bundle.Manifest);

        Assert.Contains("[redacted-local-path]", json, StringComparison.Ordinal);
        Assert.DoesNotContain(@"D:\private", json, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Build_RejectsMissingCriticalSvgItem()
    {
        var fixture = ScientificContractReviewFixture.Create();
        var document = XDocument.Parse(fixture.Svg.Svg);
        var group = document.Descendants()
            .Single(item => (string?)item.Attribute("data-spec-id") == "element-force");
        group.Remove();
        var svgText = document.ToString(SaveOptions.DisableFormatting);
        var svg = fixture.Svg with { Svg = svgText, Sha256 = Hash(Encoding.UTF8.GetBytes(svgText)) };
        var exports = new ScientificFigureExporter().Export(
            new ScientificFigureExportRequest(svg, svg.Sha256, 1200, 800));

        var error = Assert.Throws<InvalidOperationException>(() =>
            new ScientificReviewPrepBuilder(new SkiaScientificReviewImageCropper()).Build(
                fixture.Understanding,
                fixture.Specification,
                fixture.Plan,
                svg,
                exports));

        Assert.Contains("missing critical items", error.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("element-force", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Build_RejectsOversizedOutputBeforeCropping()
    {
        var fixture = ScientificContractReviewFixture.Create();
        var oversized = new byte[ScientificReviewExecutionPolicy.MaximumFullResolutionBytes + 1];
        var artifacts = fixture.Exports.Artifacts.Select(item =>
            item.Format == "png"
                ? item with { Bytes = oversized, Sha256 = Hash(oversized) }
                : item).ToArray();
        var exports = fixture.Exports with { Artifacts = artifacts };
        var cropper = new CountingCropper();

        var error = Assert.Throws<InvalidOperationException>(() =>
            new ScientificReviewPrepBuilder(cropper).Build(
                fixture.Understanding,
                fixture.Specification,
                fixture.Plan,
                fixture.Svg,
                exports));

        Assert.Contains("dispatch budget", error.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(0, cropper.InvocationCount);
    }

    private static ScientificReviewPrepBundle Build(ScientificContractReviewFixture fixture)
    {
        return new ScientificReviewPrepBuilder(new SkiaScientificReviewImageCropper()).Build(
            fixture.Understanding,
            fixture.Specification,
            fixture.Plan,
            fixture.Svg,
            fixture.Exports);
    }

    private static string Hash(byte[] bytes)
    {
        return $"sha256:{Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant()}";
    }

    private sealed class CountingCropper : IScientificReviewImageCropper
    {
        public int InvocationCount { get; private set; }

        public byte[] CropPng(
            byte[] sourcePng,
            int sourceWidth,
            int sourceHeight,
            ScientificPixelRegion region)
        {
            InvocationCount++;
            return [1];
        }
    }
}
