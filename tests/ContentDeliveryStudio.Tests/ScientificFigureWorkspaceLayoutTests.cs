namespace ContentDeliveryStudio.Tests;

public sealed class ScientificFigureWorkspaceLayoutTests
{
    [Fact]
    public void ScientificWorkspace_ExposesFiveStagesToAutomation()
    {
        var xaml = ReadRepoFile(
            "src",
            "ContentDeliveryStudio.App",
            "Views",
            "ScientificFigureWorkspaceView.xaml");

        Assert.Contains("AutomationProperties.AutomationId=\"ScientificSourceWorkspace\"", xaml);
        Assert.Contains("AutomationProperties.AutomationId=\"ScientificUnderstandingWorkspace\"", xaml);
        Assert.Contains("AutomationProperties.AutomationId=\"ScientificFigureSpecWorkspace\"", xaml);
        Assert.Contains("AutomationProperties.AutomationId=\"ScientificRenderReviewWorkspace\"", xaml);
        Assert.Contains("AutomationProperties.AutomationId=\"ScientificDeliveryWorkspace\"", xaml);
        Assert.DoesNotContain("Prompt", xaml, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void FigureSpecWorkspace_ExposesGroundedChartInputsWithStableAutomationIds()
    {
        var xaml = ReadRepoFile(
            "src",
            "ContentDeliveryStudio.App",
            "Views",
            "ScientificFigureSpecWorkspaceView.xaml");

        Assert.Contains("AutomationProperties.AutomationId=\"ScientificChartSpecificationSummary\"", xaml);
        Assert.Contains("AutomationProperties.AutomationId=\"ScientificChartSourceRows\"", xaml);
        Assert.Contains("ScientificChartWorkspace.AxesSummary", xaml);
        Assert.Contains("ScientificChartWorkspace.SelectionSummary", xaml);
        Assert.Contains("ScientificChartWorkspace.TransformSummary", xaml);
        Assert.Contains("ScientificChartWorkspace.SourceHash", xaml);
        Assert.DoesNotContain("ChartPrompt", xaml, StringComparison.OrdinalIgnoreCase);
    }

    private static string ReadRepoFile(params string[] segments)
    {
        var pathSegments = new List<string>
        {
            AppContext.BaseDirectory,
            "..",
            "..",
            "..",
            "..",
            "..",
        };
        pathSegments.AddRange(segments);
        return File.ReadAllText(Path.GetFullPath(Path.Combine(pathSegments.ToArray())));
    }
}
