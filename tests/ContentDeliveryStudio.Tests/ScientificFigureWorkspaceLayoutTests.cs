using ContentDeliveryStudio.Application.ScientificFigures;

namespace ContentDeliveryStudio.Tests;

public sealed class ScientificFigureWorkspaceLayoutTests
{
    [Fact]
    public void ScientificWorkspace_HasStableFiveStageShellAndAutomationIds()
    {
        var xaml = ReadRepoFile(
            "src",
            "ContentDeliveryStudio.App",
            "Views",
            "ScientificFigureWorkspaceView.xaml");

        Assert.Contains("MinWidth=\"460\"", xaml);
        Assert.Contains("MinHeight=\"360\"", xaml);
        Assert.Equal(7, Count(xaml, "Width=\"*\""));
        Assert.Equal(10, Count(xaml, "TextWrapping=\"Wrap\""));
        Assert.Contains("AutomationProperties.AutomationId=\"ScientificSourceWorkspace\"", xaml);
        Assert.Contains("AutomationProperties.AutomationId=\"ScientificUnderstandingWorkspace\"", xaml);
        Assert.Contains("AutomationProperties.AutomationId=\"ScientificFigureSpecWorkspace\"", xaml);
        Assert.Contains("AutomationProperties.AutomationId=\"ScientificRenderReviewWorkspace\"", xaml);
        Assert.Contains("AutomationProperties.AutomationId=\"ScientificDeliveryWorkspace\"", xaml);
        Assert.Contains("<views:ScientificSourceWorkspaceView", xaml);
        Assert.Contains("<views:ScientificUnderstandingWorkspaceView", xaml);
        Assert.Contains("<views:ScientificFigureSpecWorkspaceView", xaml);
        Assert.Contains("<views:ScientificRenderReviewWorkspaceView", xaml);
        Assert.DoesNotContain("Prompt", xaml, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void WorkbenchHost_RegistersVisibleScientificViewBehindCompletedGate()
    {
        var host = ReadRepoFile(
            "src",
            "ContentDeliveryStudio.App",
            "Views",
            "WorkbenchTabHostView.xaml");

        Assert.Contains("<views:ScientificFigureWorkspaceView Grid.Row=\"1\" Margin=\"0,16,0,0\" />", host);
        Assert.Contains("Binding=\"{Binding IsScientificFigure}\" Value=\"True\"", host);

        var localization = ReadRepoFile(
            "src",
            "ContentDeliveryStudio.App",
            "ViewModels",
            "MainWindowLocalizationCoordinator.cs");
        Assert.Contains("ScientificFigureModule.IsUserVisible", localization);
        Assert.True(ScientificFigureModule.IsUserVisible);
    }

    [Fact]
    public void ScientificWorkspace_DisplayRunsUseOneWayBindings()
    {
        var viewNames = new[]
        {
            "ScientificUnderstandingWorkspaceView.xaml",
            "ScientificFigureSpecWorkspaceView.xaml",
            "ScientificRenderReviewWorkspaceView.xaml",
            "ScientificDeliveryWorkspaceView.xaml",
        };

        foreach (var viewName in viewNames)
        {
            var xaml = ReadRepoFile(
                "src",
                "ContentDeliveryStudio.App",
                "Views",
                viewName);

            Assert.Equal(
                Count(xaml, "<Run Text=\"{Binding"),
                Count(xaml, ", Mode=OneWay}\""));
        }
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

    private static int Count(string value, string token)
    {
        return value.Split(token, StringSplitOptions.None).Length - 1;
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
