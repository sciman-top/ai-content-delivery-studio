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
        Assert.DoesNotContain("Prompt", xaml, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void WorkbenchHost_RegistersScientificViewWithoutExposingHiddenModule()
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
