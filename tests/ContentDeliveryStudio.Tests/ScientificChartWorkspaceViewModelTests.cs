using ContentDeliveryStudio.App.ViewModels;
using ContentDeliveryStudio.Core.ScientificFigures;
using ContentDeliveryStudio.Infrastructure.ScientificFigures;

namespace ContentDeliveryStudio.Tests;

public sealed class ScientificChartWorkspaceViewModelTests
{
    [Fact]
    public void WorkspaceProjectsExactApprovedInputsWithoutEditableOrPromptState()
    {
        var data = ScientificChartDataSet.Create(
            Guid.NewGuid(),
            "Sample",
            [new ScientificChartNumericColumn("Velocity", "m/s")],
            [
                new ScientificChartDataRow(
                    "row-1",
                    "Trial 1",
                    new Dictionary<string, double> { ["Velocity"] = 4.25 }),
            ]);
        var specification = ScientificChartSpec.Create(
            Guid.NewGuid(),
            data,
            "Velocity trial",
            "Sample",
            "Velocity",
            "m/s",
            ["row-1"],
            [new ScientificChartSeriesSpec("Velocity", "Velocity", ScientificChartTransform.None)]);
        var approval = ScientificChartApproval.Approve(
            data,
            specification,
            "reviewer",
            "approved",
            DateTimeOffset.Parse("2026-08-03T00:00:00Z"));
        var artifact = new DeterministicScientificChartRenderer().Render(data, specification, approval);

        var workspace = new ScientificChartWorkspaceViewModel(data, specification, artifact);

        Assert.Equal("X: Sample; Y: Velocity (m/s)", workspace.AxesSummary);
        Assert.Contains("aggregation: none", workspace.SelectionSummary, StringComparison.Ordinal);
        Assert.Equal("Velocity: none", workspace.TransformSummary);
        Assert.Equal("row-1", Assert.Single(workspace.Rows).RowId);
        Assert.DoesNotContain(
            typeof(ScientificChartWorkspaceViewModel).GetProperties(),
            property => property.CanWrite || property.Name.Contains("Prompt", StringComparison.OrdinalIgnoreCase));
    }
}
