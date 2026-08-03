using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Xml.Linq;
using ContentDeliveryStudio.Core.ScientificFigures;

namespace ContentDeliveryStudio.Infrastructure.ScientificFigures;

public sealed class DeterministicScientificChartRenderer
{
    public const string RendererVersion = "deterministic-scientific-bar-chart.v1";

    private static readonly XNamespace Svg = "http://www.w3.org/2000/svg";
    private static readonly string[] Palette = ["#2563EB", "#0F766E", "#B45309", "#7C3AED"];

    public ScientificChartSvgArtifact Render(
        ScientificChartDataSet data,
        ScientificChartSpec specification,
        ScientificChartApproval approval)
    {
        ArgumentNullException.ThrowIfNull(data);
        ArgumentNullException.ThrowIfNull(specification);
        ArgumentNullException.ThrowIfNull(approval);
        approval.Validate(data, specification);

        var selectedRows = specification.IncludedRowIds
            .Select(id => data.Rows.Single(row => string.Equals(row.RowId, id, StringComparison.Ordinal)))
            .ToArray();
        var points = selectedRows.SelectMany(row => specification.Series.Select(series =>
        {
            var sourceValue = row.Values[series.SourceColumn];
            var renderedValue = series.Transform.Kind == ScientificChartTransformKind.Scale
                ? sourceValue * series.Transform.Factor
                : sourceValue;
            if (!double.IsFinite(renderedValue))
            {
                throw new InvalidOperationException("Chart transform produced a non-finite value.");
            }

            return new RenderPoint(row, series, sourceValue, renderedValue);
        })).ToArray();

        var min = Math.Min(0, points.Min(point => point.RenderedValue));
        var max = Math.Max(0, points.Max(point => point.RenderedValue));
        if (min == max)
        {
            max = min + 1;
        }

        const double width = 960;
        const double height = 640;
        const double left = 96;
        const double right = 32;
        const double top = 72;
        const double bottom = 112;
        var plotWidth = width - left - right;
        var plotHeight = height - top - bottom;
        var zeroY = MapY(0, min, max, top, plotHeight);
        var root = new XElement(
            Svg + "svg",
            new XAttribute("xmlns", Svg.NamespaceName),
            new XAttribute("width", width),
            new XAttribute("height", height),
            new XAttribute("viewBox", $"0 0 {width} {height}"),
            new XAttribute("role", "img"),
            new XAttribute("aria-labelledby", "chart-title chart-description"),
            new XAttribute("data-renderer-version", RendererVersion),
            new XAttribute("data-source-sha256", data.SourceSha256),
            new XAttribute("data-specification-sha256", specification.SpecificationSha256),
            new XElement(Svg + "title", new XAttribute("id", "chart-title"), specification.Title),
            new XElement(
                Svg + "desc",
                new XAttribute("id", "chart-description"),
                $"Bar chart. X axis: {specification.XAxisLabel}. Y axis: {specification.YAxisLabel} ({specification.YAxisUnit})."),
            new XElement(
                Svg + "metadata",
                $"source={data.SourceAssetId:D};data={data.SourceSha256};spec={specification.SpecificationSha256};aggregation=none;uncertainty=none"),
            Text(width / 2, 34, specification.Title, "middle", 22, "700"),
            Line(left, top, left, top + plotHeight, "#111827", 2),
            Line(left, zeroY, left + plotWidth, zeroY, "#111827", 2),
            Text(22, top + (plotHeight / 2), $"{specification.YAxisLabel} ({specification.YAxisUnit})", "middle", 15, "600",
                new XAttribute("transform", $"rotate(-90 22 {F(top + (plotHeight / 2))})")),
            Text(left + (plotWidth / 2), height - 24, specification.XAxisLabel, "middle", 15, "600"));

        AddTicks(root, min, max, left, top, plotWidth, plotHeight);
        var groupWidth = plotWidth / selectedRows.Length;
        var barGap = 6d;
        var barWidth = Math.Max(8, Math.Min(72, (groupWidth - 18) / specification.Series.Count));
        var provenance = new List<ScientificChartPointProvenance>();
        for (var rowIndex = 0; rowIndex < selectedRows.Length; rowIndex++)
        {
            var row = selectedRows[rowIndex];
            var groupCenter = left + ((rowIndex + 0.5) * groupWidth);
            var totalBarsWidth = (barWidth * specification.Series.Count) + (barGap * (specification.Series.Count - 1));
            root.Add(Text(groupCenter, top + plotHeight + 28, row.Category, "middle", 13, "400"));
            for (var seriesIndex = 0; seriesIndex < specification.Series.Count; seriesIndex++)
            {
                var series = specification.Series[seriesIndex];
                var sourceValue = row.Values[series.SourceColumn];
                var renderedValue = series.Transform.Kind == ScientificChartTransformKind.Scale
                    ? sourceValue * series.Transform.Factor
                    : sourceValue;
                var valueY = MapY(renderedValue, min, max, top, plotHeight);
                var x = groupCenter - (totalBarsWidth / 2) + (seriesIndex * (barWidth + barGap));
                var y = Math.Min(valueY, zeroY);
                var barHeight = Math.Max(1, Math.Abs(zeroY - valueY));
                root.Add(new XElement(
                    Svg + "rect",
                    new XAttribute("x", F(x)),
                    new XAttribute("y", F(y)),
                    new XAttribute("width", F(barWidth)),
                    new XAttribute("height", F(barHeight)),
                    new XAttribute("fill", Palette[seriesIndex % Palette.Length]),
                    new XAttribute("data-row-id", row.RowId),
                    new XAttribute("data-source-column", series.SourceColumn),
                    new XAttribute("data-source-value", F(sourceValue)),
                    new XAttribute("data-rendered-value", F(renderedValue)),
                    new XAttribute("data-unit", specification.YAxisUnit),
                    new XAttribute("data-transform", series.Transform.Label)));
                root.Add(Text(
                    x + (barWidth / 2),
                    renderedValue >= 0 ? y - 7 : y + barHeight + 17,
                    F(renderedValue),
                    "middle",
                    11,
                    "600"));
                provenance.Add(new ScientificChartPointProvenance(
                    row.RowId,
                    series.SourceColumn,
                    sourceValue,
                    renderedValue,
                    specification.YAxisUnit,
                    series.Transform.Label));
            }
        }

        var legendY = height - 68;
        for (var index = 0; index < specification.Series.Count; index++)
        {
            var series = specification.Series[index];
            var x = left + (index * 210);
            root.Add(new XElement(
                Svg + "rect",
                new XAttribute("x", F(x)),
                new XAttribute("y", F(legendY - 12)),
                new XAttribute("width", 14),
                new XAttribute("height", 14),
                new XAttribute("fill", Palette[index % Palette.Length])));
            root.Add(Text(x + 22, legendY, $"{series.LegendLabel} [{series.Transform.Label}]", "start", 12, "400"));
        }

        var document = new XDocument(new XDeclaration("1.0", "utf-8", null), root);
        var svg = document.ToString(SaveOptions.DisableFormatting);
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(svg))).ToLowerInvariant();
        return new ScientificChartSvgArtifact(
            svg,
            $"sha256:{hash}",
            new ScientificChartProvenance(
                data.SourceAssetId,
                data.SourceSha256,
                specification.ChartId,
                specification.SpecificationSha256,
                RendererVersion,
                specification.Aggregation,
                specification.UncertaintyRepresentation,
                provenance.AsReadOnly(),
                approval));
    }

    private static void AddTicks(
        XElement root,
        double min,
        double max,
        double left,
        double top,
        double plotWidth,
        double plotHeight)
    {
        for (var index = 0; index <= 5; index++)
        {
            var value = min + ((max - min) * index / 5d);
            var y = MapY(value, min, max, top, plotHeight);
            root.Add(Line(left, y, left + plotWidth, y, "#D1D5DB", 1));
            root.Add(Text(left - 10, y + 4, F(value), "end", 11, "400"));
        }
    }

    private static double MapY(double value, double min, double max, double top, double plotHeight) =>
        top + (((max - value) / (max - min)) * plotHeight);

    private static XElement Line(double x1, double y1, double x2, double y2, string stroke, double width) =>
        new(
            Svg + "line",
            new XAttribute("x1", F(x1)),
            new XAttribute("y1", F(y1)),
            new XAttribute("x2", F(x2)),
            new XAttribute("y2", F(y2)),
            new XAttribute("stroke", stroke),
            new XAttribute("stroke-width", F(width)));

    private static XElement Text(
        double x,
        double y,
        string value,
        string anchor,
        int size,
        string weight,
        params XAttribute[] attributes) =>
        new(
            Svg + "text",
            new XAttribute("x", F(x)),
            new XAttribute("y", F(y)),
            new XAttribute("text-anchor", anchor),
            new XAttribute("font-family", "Segoe UI,Arial,sans-serif"),
            new XAttribute("font-size", size),
            new XAttribute("font-weight", weight),
            new XAttribute("fill", "#111827"),
            attributes,
            value);

    private static string F(double value) => value.ToString("0.###", CultureInfo.InvariantCulture);

    private sealed record RenderPoint(
        ScientificChartDataRow Row,
        ScientificChartSeriesSpec Series,
        double SourceValue,
        double RenderedValue);
}
