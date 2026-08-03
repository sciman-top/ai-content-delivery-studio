using ContentDeliveryStudio.Core.ScientificFigures;

namespace ContentDeliveryStudio.App.ViewModels;

public sealed class ScientificChartWorkspaceViewModel
{
    public ScientificChartWorkspaceViewModel(
        ScientificChartDataSet data,
        ScientificChartSpec specification,
        ScientificChartSvgArtifact artifact)
    {
        Data = data ?? throw new ArgumentNullException(nameof(data));
        Specification = specification ?? throw new ArgumentNullException(nameof(specification));
        Artifact = artifact ?? throw new ArgumentNullException(nameof(artifact));
        artifact.Provenance.Approval.Validate(data, specification);
        if (!string.Equals(artifact.Provenance.DataSha256, data.SourceSha256, StringComparison.OrdinalIgnoreCase)
            || !string.Equals(
                artifact.Provenance.SpecificationSha256,
                specification.SpecificationSha256,
                StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("Chart workspace artifact does not match the current data and specification.", nameof(artifact));
        }

        Rows = specification.IncludedRowIds.Select(rowId =>
        {
            var row = data.Rows.Single(item => string.Equals(item.RowId, rowId, StringComparison.Ordinal));
            return new ScientificChartRowViewModel(
                row.RowId,
                row.Category,
                string.Join(
                    "; ",
                    specification.Series.Select(series =>
                        $"{series.LegendLabel}={row.Values[series.SourceColumn]:R} {specification.YAxisUnit}")));
        }).ToArray();
    }

    public ScientificChartDataSet Data { get; }

    public ScientificChartSpec Specification { get; }

    public ScientificChartSvgArtifact Artifact { get; }

    public string Title => Specification.Title;

    public string SourceHash => Data.SourceSha256;

    public string SpecificationHash => Specification.SpecificationSha256;

    public string ArtifactHash => Artifact.Sha256;

    public string AxesSummary =>
        $"X: {Specification.XAxisLabel}; Y: {Specification.YAxisLabel} ({Specification.YAxisUnit})";

    public string SelectionSummary =>
        $"Rows: {string.Join(", ", Specification.IncludedRowIds)}; aggregation: {Specification.Aggregation}; uncertainty: {Specification.UncertaintyRepresentation}";

    public string TransformSummary => string.Join(
        "; ",
        Specification.Series.Select(series => $"{series.LegendLabel}: {series.Transform.Label}"));

    public string ApprovalSummary =>
        $"{Artifact.Provenance.Approval.Reviewer} / {Artifact.Provenance.Approval.ApprovedAt:O}";

    public IReadOnlyList<ScientificChartRowViewModel> Rows { get; }
}

public sealed record ScientificChartRowViewModel(
    string RowId,
    string Category,
    string Values);
