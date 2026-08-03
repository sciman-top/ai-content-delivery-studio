using System.Collections.ObjectModel;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace ContentDeliveryStudio.Core.ScientificFigures;

public sealed record ScientificChartNumericColumn(string Name, string Unit);

public sealed record ScientificChartDataRow(
    string RowId,
    string Category,
    IReadOnlyDictionary<string, double> Values);

public sealed record ScientificChartDataSet
{
    private ScientificChartDataSet(
        Guid sourceAssetId,
        string categoryColumn,
        IReadOnlyList<ScientificChartNumericColumn> numericColumns,
        IReadOnlyList<ScientificChartDataRow> rows,
        string sourceSha256)
    {
        SourceAssetId = sourceAssetId;
        CategoryColumn = categoryColumn;
        NumericColumns = numericColumns;
        Rows = rows;
        SourceSha256 = sourceSha256;
    }

    public Guid SourceAssetId { get; }

    public string CategoryColumn { get; }

    public IReadOnlyList<ScientificChartNumericColumn> NumericColumns { get; }

    public IReadOnlyList<ScientificChartDataRow> Rows { get; }

    public string SourceSha256 { get; }

    public static ScientificChartDataSet Create(
        Guid sourceAssetId,
        string categoryColumn,
        IReadOnlyList<ScientificChartNumericColumn> numericColumns,
        IReadOnlyList<ScientificChartDataRow> rows)
    {
        if (sourceAssetId == Guid.Empty)
        {
            throw new ArgumentException("Chart source asset id cannot be empty.", nameof(sourceAssetId));
        }

        var normalizedCategory = RequireText(categoryColumn, nameof(categoryColumn));
        ArgumentNullException.ThrowIfNull(numericColumns);
        ArgumentNullException.ThrowIfNull(rows);
        if (numericColumns.Count == 0 || rows.Count == 0)
        {
            throw new ArgumentException("Chart data requires numeric columns and rows.");
        }

        var columns = numericColumns.Select(column =>
        {
            ArgumentNullException.ThrowIfNull(column);
            return new ScientificChartNumericColumn(
                RequireText(column.Name, nameof(numericColumns)),
                RequireText(column.Unit, nameof(numericColumns)));
        }).ToArray();
        if (columns.Select(column => column.Name).Append(normalizedCategory)
            .Distinct(StringComparer.OrdinalIgnoreCase).Count() != columns.Length + 1)
        {
            throw new ArgumentException("Chart column names must be unique and unambiguous.", nameof(numericColumns));
        }

        var columnNames = columns.Select(column => column.Name).ToHashSet(StringComparer.Ordinal);
        var rowIds = new HashSet<string>(StringComparer.Ordinal);
        var normalizedRows = rows.Select(row =>
        {
            ArgumentNullException.ThrowIfNull(row);
            var rowId = RequireText(row.RowId, nameof(rows));
            if (!rowIds.Add(rowId))
            {
                throw new ArgumentException($"Duplicate chart row id: {rowId}.", nameof(rows));
            }

            var category = RequireText(row.Category, nameof(rows));
            ArgumentNullException.ThrowIfNull(row.Values);
            if (!row.Values.Keys.ToHashSet(StringComparer.Ordinal).SetEquals(columnNames))
            {
                throw new ArgumentException(
                    $"Chart row {rowId} must contain exactly the declared numeric columns.",
                    nameof(rows));
            }

            var values = new SortedDictionary<string, double>(StringComparer.Ordinal);
            foreach (var value in row.Values)
            {
                if (!double.IsFinite(value.Value))
                {
                    throw new ArgumentException(
                        $"Chart row {rowId} contains a non-finite value.",
                        nameof(rows));
                }

                values.Add(value.Key, value.Value);
            }

            return new ScientificChartDataRow(
                rowId,
                category,
                new ReadOnlyDictionary<string, double>(values));
        }).ToArray();

        var canonical = BuildCanonical(normalizedCategory, columns, normalizedRows);
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonical)))
            .ToLowerInvariant();
        return new ScientificChartDataSet(
            sourceAssetId,
            normalizedCategory,
            Array.AsReadOnly(columns),
            Array.AsReadOnly(normalizedRows),
            $"sha256:{hash}");
    }

    private static string BuildCanonical(
        string categoryColumn,
        IReadOnlyList<ScientificChartNumericColumn> columns,
        IReadOnlyList<ScientificChartDataRow> rows)
    {
        var builder = new StringBuilder("scientific-chart-data.v1\n");
        builder.Append(categoryColumn).Append('\n');
        foreach (var column in columns.OrderBy(column => column.Name, StringComparer.Ordinal))
        {
            builder.Append(column.Name).Append('\t').Append(column.Unit).Append('\n');
        }

        foreach (var row in rows)
        {
            builder.Append(row.RowId).Append('\t').Append(row.Category);
            foreach (var value in row.Values.OrderBy(value => value.Key, StringComparer.Ordinal))
            {
                builder.Append('\t').Append(value.Key).Append('=')
                    .Append(value.Value.ToString("R", CultureInfo.InvariantCulture));
            }

            builder.Append('\n');
        }

        return builder.ToString();
    }

    internal static string RequireText(string value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Chart text cannot be empty.", parameterName);
        }

        return value.Trim();
    }
}

public enum ScientificChartTransformKind
{
    None = 0,
    Scale = 1,
}

public sealed record ScientificChartTransform(
    ScientificChartTransformKind Kind,
    double Factor,
    string Label,
    string? OutputUnit)
{
    public static ScientificChartTransform None { get; } =
        new(ScientificChartTransformKind.None, 1, "none", null);

    public static ScientificChartTransform Scale(double factor, string label, string outputUnit)
    {
        if (!double.IsFinite(factor) || factor == 0)
        {
            throw new ArgumentOutOfRangeException(nameof(factor), factor, "Scale factor must be finite and non-zero.");
        }

        return new ScientificChartTransform(
            ScientificChartTransformKind.Scale,
            factor,
            ScientificChartDataSet.RequireText(label, nameof(label)),
            ScientificChartDataSet.RequireText(outputUnit, nameof(outputUnit)));
    }
}

public sealed record ScientificChartSeriesSpec(
    string SourceColumn,
    string LegendLabel,
    ScientificChartTransform Transform);

public sealed record ScientificChartSpec
{
    private ScientificChartSpec(
        Guid chartId,
        string title,
        string xAxisLabel,
        string yAxisLabel,
        string yAxisUnit,
        IReadOnlyList<string> includedRowIds,
        IReadOnlyList<ScientificChartSeriesSpec> series,
        string specificationSha256)
    {
        ChartId = chartId;
        Title = title;
        XAxisLabel = xAxisLabel;
        YAxisLabel = yAxisLabel;
        YAxisUnit = yAxisUnit;
        IncludedRowIds = includedRowIds;
        Series = series;
        SpecificationSha256 = specificationSha256;
    }

    public Guid ChartId { get; }
    public string Title { get; }
    public string XAxisLabel { get; }
    public string YAxisLabel { get; }
    public string YAxisUnit { get; }
    public IReadOnlyList<string> IncludedRowIds { get; }
    public IReadOnlyList<ScientificChartSeriesSpec> Series { get; }
    public string Aggregation => "none";
    public string UncertaintyRepresentation => "none";
    public string SpecificationSha256 { get; }

    public static ScientificChartSpec Create(
        Guid chartId,
        ScientificChartDataSet data,
        string title,
        string xAxisLabel,
        string yAxisLabel,
        string yAxisUnit,
        IReadOnlyList<string> includedRowIds,
        IReadOnlyList<ScientificChartSeriesSpec> series)
    {
        if (chartId == Guid.Empty)
        {
            throw new ArgumentException("Chart id cannot be empty.", nameof(chartId));
        }

        ArgumentNullException.ThrowIfNull(data);
        ArgumentNullException.ThrowIfNull(includedRowIds);
        ArgumentNullException.ThrowIfNull(series);
        if (includedRowIds.Count == 0 || series.Count == 0)
        {
            throw new ArgumentException("A chart requires selected rows and series.");
        }

        var availableRows = data.Rows.Select(row => row.RowId).ToHashSet(StringComparer.Ordinal);
        var normalizedRows = includedRowIds.Select(row =>
            ScientificChartDataSet.RequireText(row, nameof(includedRowIds))).ToArray();
        if (normalizedRows.Distinct(StringComparer.Ordinal).Count() != normalizedRows.Length
            || normalizedRows.Any(row => !availableRows.Contains(row)))
        {
            throw new ArgumentException("Chart row selection is duplicate or not present in the hashed data.", nameof(includedRowIds));
        }

        var columns = data.NumericColumns.ToDictionary(column => column.Name, StringComparer.Ordinal);
        var normalizedUnit = ScientificChartDataSet.RequireText(yAxisUnit, nameof(yAxisUnit));
        var normalizedSeries = series.Select(item =>
        {
            ArgumentNullException.ThrowIfNull(item);
            var sourceColumn = ScientificChartDataSet.RequireText(item.SourceColumn, nameof(series));
            if (!columns.TryGetValue(sourceColumn, out var column))
            {
                throw new ArgumentException($"Chart series column is not present in hashed data: {sourceColumn}.", nameof(series));
            }

            if (item.Transform.Kind == ScientificChartTransformKind.None
                && !string.Equals(column.Unit, normalizedUnit, StringComparison.Ordinal))
            {
                throw new ArgumentException(
                    $"Chart series unit {column.Unit} does not match the y-axis unit {normalizedUnit}.",
                    nameof(series));
            }

            ArgumentNullException.ThrowIfNull(item.Transform);
            if (!Enum.IsDefined(item.Transform.Kind)
                || !double.IsFinite(item.Transform.Factor)
                || string.IsNullOrWhiteSpace(item.Transform.Label)
                || (item.Transform.Kind == ScientificChartTransformKind.None
                    && (item.Transform.Factor != 1 || item.Transform.OutputUnit is not null))
                || (item.Transform.Kind == ScientificChartTransformKind.Scale
                    && (item.Transform.Factor == 0
                        || !string.Equals(item.Transform.OutputUnit, normalizedUnit, StringComparison.Ordinal))))
            {
                throw new ArgumentException("Chart transform must be explicit, finite, and labeled.", nameof(series));
            }

            return new ScientificChartSeriesSpec(
                sourceColumn,
                ScientificChartDataSet.RequireText(item.LegendLabel, nameof(series)),
                item.Transform with { Label = item.Transform.Label.Trim() });
        }).ToArray();
        if (normalizedSeries.Select(item => item.SourceColumn).Distinct(StringComparer.Ordinal).Count()
            != normalizedSeries.Length)
        {
            throw new ArgumentException("A source column cannot be plotted more than once.", nameof(series));
        }

        var normalizedTitle = ScientificChartDataSet.RequireText(title, nameof(title));
        var normalizedX = ScientificChartDataSet.RequireText(xAxisLabel, nameof(xAxisLabel));
        var normalizedY = ScientificChartDataSet.RequireText(yAxisLabel, nameof(yAxisLabel));
        var canonical = string.Join('\n',
            "scientific-chart-spec.v1",
            data.SourceSha256,
            normalizedTitle,
            normalizedX,
            normalizedY,
            normalizedUnit,
            string.Join(',', normalizedRows),
            string.Join('|', normalizedSeries.Select(item =>
                $"{item.SourceColumn}:{item.LegendLabel}:{item.Transform.Kind}:{item.Transform.Factor.ToString("R", CultureInfo.InvariantCulture)}:{item.Transform.Label}:{item.Transform.OutputUnit}")));
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonical)))
            .ToLowerInvariant();
        return new ScientificChartSpec(
            chartId,
            normalizedTitle,
            normalizedX,
            normalizedY,
            normalizedUnit,
            Array.AsReadOnly(normalizedRows),
            Array.AsReadOnly(normalizedSeries),
            $"sha256:{hash}");
    }
}

public sealed record ScientificChartApproval(
    Guid ChartId,
    string DataSha256,
    string SpecificationSha256,
    string Reviewer,
    string Notes,
    DateTimeOffset ApprovedAt)
{
    public static ScientificChartApproval Approve(
        ScientificChartDataSet data,
        ScientificChartSpec specification,
        string reviewer,
        string notes,
        DateTimeOffset approvedAt)
    {
        ArgumentNullException.ThrowIfNull(data);
        ArgumentNullException.ThrowIfNull(specification);
        return new ScientificChartApproval(
            specification.ChartId,
            data.SourceSha256,
            specification.SpecificationSha256,
            ScientificChartDataSet.RequireText(reviewer, nameof(reviewer)),
            ScientificChartDataSet.RequireText(notes, nameof(notes)),
            approvedAt);
    }

    public void Validate(ScientificChartDataSet data, ScientificChartSpec specification)
    {
        if (ChartId != specification.ChartId
            || !string.Equals(DataSha256, data.SourceSha256, StringComparison.OrdinalIgnoreCase)
            || !string.Equals(SpecificationSha256, specification.SpecificationSha256, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Chart approval does not match the current data and specification.");
        }
    }
}

public sealed record ScientificChartPointProvenance(
    string RowId,
    string SourceColumn,
    double SourceValue,
    double RenderedValue,
    string Unit,
    string TransformLabel);

public sealed record ScientificChartProvenance(
    Guid SourceAssetId,
    string DataSha256,
    Guid ChartId,
    string SpecificationSha256,
    string RendererVersion,
    string Aggregation,
    string UncertaintyRepresentation,
    IReadOnlyList<ScientificChartPointProvenance> Points,
    ScientificChartApproval Approval);

public sealed record ScientificChartSvgArtifact(
    string Svg,
    string Sha256,
    ScientificChartProvenance Provenance);
