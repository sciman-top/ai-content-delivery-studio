using System.Security.Cryptography;
using System.Text;
using System.Xml.Linq;
using ContentDeliveryStudio.Core.ScientificFigures;
using ContentDeliveryStudio.Infrastructure.ScientificFigures;

namespace ContentDeliveryStudio.Tests;

public sealed class ScientificChartTests
{
    [Fact]
    public void CreateDataSet_HashesExactStructuredRowsAndRejectsFabricatedShape()
    {
        var data = CreateDataSet();
        var changed = ScientificChartDataSet.Create(
            data.SourceAssetId,
            data.CategoryColumn,
            data.NumericColumns,
            [
                new ScientificChartDataRow("row-a", "A", Values(("Force", 10.1), ("Error", 0.5))),
                new ScientificChartDataRow("row-b", "B", Values(("Force", 15.0), ("Error", 0.8))),
            ]);

        Assert.StartsWith("sha256:", data.SourceSha256, StringComparison.Ordinal);
        Assert.NotEqual(data.SourceSha256, changed.SourceSha256);
        Assert.Throws<ArgumentException>(() => ScientificChartDataSet.Create(
            Guid.NewGuid(),
            "Sample",
            [new ScientificChartNumericColumn("Force", "N")],
            [new ScientificChartDataRow("row-a", "A", Values(("Invented", 1)))]));
    }

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void CreateDataSet_RejectsNonFiniteValues(double value)
    {
        Assert.Throws<ArgumentException>(() => ScientificChartDataSet.Create(
            Guid.NewGuid(),
            "Sample",
            [new ScientificChartNumericColumn("Force", "N")],
            [new ScientificChartDataRow("row-a", "A", Values(("Force", value)))]));
    }

    [Fact]
    public void CreateSpecification_RejectsUnitMismatchUnknownRowsAndUnlabeledTransforms()
    {
        var data = CreateDataSet();
        Assert.Throws<ArgumentException>(() => CreateSpec(
            data,
            unit: "kN"));
        Assert.Throws<ArgumentException>(() => CreateSpec(
            data,
            rows: ["row-not-in-source"]));
        Assert.Throws<ArgumentException>(() => CreateSpec(
            data,
            transform: new ScientificChartTransform(ScientificChartTransformKind.Scale, 2, string.Empty, "N")));
    }

    [Fact]
    public void DataAndSpecification_RejectAmbiguousColumnsAndMissingAxisLabels()
    {
        Assert.Throws<ArgumentException>(() => ScientificChartDataSet.Create(
            Guid.NewGuid(),
            "Sample",
            [
                new ScientificChartNumericColumn("Force", "N"),
                new ScientificChartNumericColumn("force", "N"),
            ],
            [
                new ScientificChartDataRow(
                    "row-a",
                    "A",
                    Values(("Force", 1), ("force", 2))),
            ]));
        var data = CreateDataSet();
        Assert.Throws<ArgumentException>(() => ScientificChartSpec.Create(
            Guid.NewGuid(),
            data,
            "Measured force",
            string.Empty,
            "Force",
            "N",
            ["row-a"],
            [new ScientificChartSeriesSpec("Force", "Force", ScientificChartTransform.None)]));
    }

    [Fact]
    public void Approval_FailsClosedWhenHashedDataOrSpecificationDrifts()
    {
        var data = CreateDataSet();
        var spec = CreateSpec(data);
        var approval = ScientificChartApproval.Approve(
            data,
            spec,
            "reviewer",
            "Values, units, axes, and transform approved.",
            DateTimeOffset.Parse("2026-08-03T00:00:00Z"));
        var changed = ScientificChartDataSet.Create(
            data.SourceAssetId,
            data.CategoryColumn,
            data.NumericColumns,
            [
                new ScientificChartDataRow("row-a", "A", Values(("Force", 11.0), ("Error", 0.5))),
                new ScientificChartDataRow("row-b", "B", Values(("Force", 15.0), ("Error", 0.8))),
            ]);

        Assert.Throws<InvalidOperationException>(() => approval.Validate(changed, spec));
    }

    [Fact]
    public void Renderer_IsDeterministicAndCarriesPointLevelSourceProvenance()
    {
        var data = CreateDataSet();
        var spec = CreateSpec(data, transform: ScientificChartTransform.Scale(0.001, "N to kN x0.001", "kN"), unit: "kN");
        var approval = ScientificChartApproval.Approve(
            data,
            spec,
            "chart-reviewer",
            "Approved exact rows and explicit scale.",
            DateTimeOffset.Parse("2026-08-03T01:00:00Z"));
        var renderer = new DeterministicScientificChartRenderer();

        var first = renderer.Render(data, spec, approval);
        var second = renderer.Render(data, spec, approval);

        Assert.Equal(first.Svg, second.Svg);
        Assert.Equal(first.Sha256, second.Sha256);
        Assert.Equal(2, first.Provenance.Points.Count);
        Assert.All(first.Provenance.Points, point =>
        {
            Assert.Equal("Force", point.SourceColumn);
            Assert.Equal(point.SourceValue * 0.001, point.RenderedValue, precision: 12);
            Assert.Equal("N to kN x0.001", point.TransformLabel);
        });
        Assert.Equal(data.SourceSha256, first.Provenance.DataSha256);
        Assert.Equal(spec.SpecificationSha256, first.Provenance.SpecificationSha256);
        Assert.Equal("none", first.Provenance.Aggregation);
        Assert.Equal("none", first.Provenance.UncertaintyRepresentation);
        Assert.Equal(
            first.Sha256,
            $"sha256:{Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(first.Svg))).ToLowerInvariant()}");
    }

    [Fact]
    public void Renderer_EmitsOnlyApprovedBarsWithVisibleAxesUnitsFiltersAndLegend()
    {
        var data = CreateDataSet();
        var spec = CreateSpec(data, rows: ["row-b"]);
        var approval = ScientificChartApproval.Approve(
            data,
            spec,
            "chart-reviewer",
            "Approved one explicit row.",
            DateTimeOffset.Parse("2026-08-03T01:00:00Z"));

        var artifact = new DeterministicScientificChartRenderer().Render(data, spec, approval);
        var document = XDocument.Parse(artifact.Svg);
        XNamespace svg = "http://www.w3.org/2000/svg";
        var bars = document.Descendants(svg + "rect")
            .Where(element => element.Attribute("data-row-id") is not null)
            .ToArray();

        var bar = Assert.Single(bars);
        Assert.Equal("row-b", bar.Attribute("data-row-id")?.Value);
        Assert.Equal("15", bar.Attribute("data-source-value")?.Value);
        Assert.Contains("Force (N)", artifact.Svg, StringComparison.Ordinal);
        Assert.Contains("Sample", artifact.Svg, StringComparison.Ordinal);
        Assert.Contains("aggregation=none", artifact.Svg, StringComparison.Ordinal);
        Assert.DoesNotContain(document.Descendants(svg + "path"), _ => true);
    }

    private static ScientificChartDataSet CreateDataSet() =>
        ScientificChartDataSet.Create(
            Guid.Parse("5fed1adc-4e75-4aab-8810-a26548c4320e"),
            "Sample",
            [
                new ScientificChartNumericColumn("Force", "N"),
                new ScientificChartNumericColumn("Error", "N"),
            ],
            [
                new ScientificChartDataRow("row-a", "A", Values(("Force", 10.0), ("Error", 0.5))),
                new ScientificChartDataRow("row-b", "B", Values(("Force", 15.0), ("Error", 0.8))),
            ]);

    private static ScientificChartSpec CreateSpec(
        ScientificChartDataSet data,
        string unit = "N",
        IReadOnlyList<string>? rows = null,
        ScientificChartTransform? transform = null) =>
        ScientificChartSpec.Create(
            Guid.Parse("d1a7d4cc-3bdd-4984-9aca-e863283d796a"),
            data,
            "Measured force",
            "Sample",
            "Force",
            unit,
            rows ?? ["row-a", "row-b"],
            [new ScientificChartSeriesSpec("Force", "Measured force", transform ?? ScientificChartTransform.None)]);

    private static IReadOnlyDictionary<string, double> Values(params (string Name, double Value)[] values) =>
        values.ToDictionary(item => item.Name, item => item.Value, StringComparer.Ordinal);
}
