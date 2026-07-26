using System.IO.Compression;
using System.Text.Json;

namespace ContentDeliveryStudio.Tests;

public sealed class ScientificFigureDeliveryPackageTests
{
    [Fact]
    public void ApprovedPackage_ContainsArtifactsProvenanceReviewsRepairsProvidersAndBothApprovals()
    {
        var fixture = ScientificDeliveryTestFixture.Create();

        var result = fixture.Service.DecideGateTwo(fixture.Request);

        using var stream = new MemoryStream(result.PackageBytes!);
        using var archive = new ZipArchive(stream, ZipArchiveMode.Read);
        var names = archive.Entries.Select(entry => entry.FullName).ToHashSet(StringComparer.Ordinal);
        Assert.Contains("figure.svg", names);
        Assert.Contains("figure.png", names);
        Assert.Contains("figure.pdf", names);
        Assert.Contains("specification.json", names);
        Assert.Contains("claim-evidence-item-map.json", names);
        Assert.Contains("reviews.json", names);
        Assert.Contains("repairs.json", names);
        Assert.Contains("providers.json", names);
        Assert.Contains("approvals.json", names);
        Assert.Contains("manifest.json", names);

        var approvals = ReadJson(archive, "approvals.json");
        Assert.Equal(
            "gate-one-reviewer",
            approvals.RootElement.GetProperty("GateOne").GetProperty("Reviewer").GetString());
        Assert.Equal(
            "final-reviewer",
            approvals.RootElement.GetProperty("GateTwo").GetProperty("Reviewer").GetString());
        var providers = ReadJson(archive, "providers.json");
        Assert.Equal(2, providers.RootElement.GetArrayLength());
        var map = ReadJson(archive, "claim-evidence-item-map.json");
        Assert.Equal(
            fixture.Workflow.Specification.Elements.Count
                + fixture.Workflow.Specification.Relations.Count,
            map.RootElement.GetArrayLength());
        Assert.All(
            map.RootElement.EnumerateArray(),
            item => Assert.False(string.IsNullOrWhiteSpace(
                item.GetProperty("SpecificationItemId").GetString())));
    }

    [Fact]
    public void ApprovedPackage_ManifestHashesMatchAuthorityArtifacts()
    {
        var fixture = ScientificDeliveryTestFixture.Create();
        var result = fixture.Service.DecideGateTwo(fixture.Request);

        using var stream = new MemoryStream(result.PackageBytes!);
        using var archive = new ZipArchive(stream, ZipArchiveMode.Read);
        var manifest = ReadJson(archive, "manifest.json");

        Assert.Equal(
            fixture.ScientificFixture.Svg.Sha256,
            manifest.RootElement.GetProperty("SvgSha256").GetString());
        Assert.Equal(
            fixture.ScientificFixture.Exports.SemanticSha256,
            manifest.RootElement.GetProperty("SemanticSha256").GetString());
        Assert.Equal(
            fixture.ScientificFixture.Exports.Artifacts.Count,
            manifest.RootElement.GetProperty("ArtifactSha256").EnumerateObject().Count());
    }

    private static JsonDocument ReadJson(ZipArchive archive, string name)
    {
        var entry = archive.GetEntry(name)
            ?? throw new InvalidOperationException($"Package entry not found: {name}");
        using var stream = entry.Open();
        return JsonDocument.Parse(stream);
    }
}
