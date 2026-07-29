namespace ContentDeliveryStudio.Tests;

public sealed class WpfShellAccessibilityTests
{
    [Fact]
    public void App_DefinesSystemBrushKeyboardFocusVisual()
    {
        var xaml = ReadRepoFile("src", "ContentDeliveryStudio.App", "App.xaml");

        Assert.Contains("x:Key=\"AccessibleKeyboardFocusVisual\"", xaml);
        Assert.Contains("SystemColors.HighlightBrushKey", xaml);
        Assert.Contains("StrokeThickness=\"2\"", xaml);
        Assert.DoesNotContain("Stroke=\"Red\"", xaml);
    }

    [Fact]
    public void Shell_ExposesLocalizedAutomationRegionsAndKeyboardOrder()
    {
        var mainWindow = ReadRepoFile("src", "ContentDeliveryStudio.App", "MainWindow.xaml");
        var mainWindowCodeBehind = ReadRepoFile("src", "ContentDeliveryStudio.App", "MainWindow.xaml.cs");
        var navigation = ReadRepoFile("src", "ContentDeliveryStudio.App", "Views", "WorkspaceNavigationView.xaml");
        var tabs = ReadRepoFile("src", "ContentDeliveryStudio.App", "Views", "WorkbenchTabHostView.xaml");
        var inspector = ReadRepoFile("src", "ContentDeliveryStudio.App", "Views", "WorkbenchInspectorView.xaml");

        Assert.Contains("KeyboardNavigation.TabNavigation=\"Continue\"", mainWindow);
        Assert.Contains("AutomationProperties.AutomationId=\"LanguageSelector\"", mainWindow);
        Assert.Contains("AutomationProperties.Name=\"{Binding LanguageLabel, NotifyOnTargetUpdated=True}\"", mainWindow);
        Assert.Contains("TargetUpdated=\"LanguageSelectorAutomationName_TargetUpdated\"", mainWindow);
        Assert.Contains("FocusVisualStyle=\"{StaticResource AccessibleKeyboardFocusVisual}\"", mainWindow);
        Assert.Contains("RaisePropertyChangedEvent(", mainWindowCodeBehind);
        Assert.Contains("AutomationElementIdentifiers.NameProperty", mainWindowCodeBehind);

        Assert.Contains("AutomationProperties.AutomationId=\"WorkspaceNavigation\"", navigation);
        Assert.Contains("AutomationProperties.Name=\"{Binding WorkspaceHeader}\"", navigation);
        Assert.Contains("KeyboardNavigation.TabNavigation=\"Continue\"", navigation);

        Assert.Contains("AutomationProperties.AutomationId=\"WorkbenchTabs\"", tabs);
        Assert.Contains("AutomationProperties.Name=\"{Binding WorkspaceHeader}\"", tabs);
        Assert.Contains("KeyboardNavigation.TabNavigation=\"Continue\"", tabs);

        Assert.Contains("AutomationProperties.AutomationId=\"WorkbenchInspector\"", inspector);
        Assert.Contains("AutomationProperties.Name=\"{Binding InspectorTitle}\"", inspector);
        Assert.Contains("KeyboardNavigation.TabNavigation=\"Continue\"", inspector);
    }

    [Fact]
    public void DiagnosticsAndActivity_ExposeLocalizedNamesAndPoliteLiveRegions()
    {
        var diagnostics = ReadRepoFile("src", "ContentDeliveryStudio.App", "Views", "DiagnosticsPanelView.xaml");
        var activity = ReadRepoFile("src", "ContentDeliveryStudio.App", "Views", "ActivityPanelView.xaml");

        Assert.Contains("AutomationProperties.Name=\"{Binding OutputDirectoryLabel}\"", diagnostics);
        Assert.Contains("AutomationProperties.HelpText=\"{Binding PrivacySummary}\"", diagnostics);
        Assert.Contains("AutomationProperties.Name=\"{Binding BrowseText}\"", diagnostics);
        Assert.Contains("AutomationProperties.Name=\"{Binding ExportText}\"", diagnostics);
        Assert.Contains("AutomationProperties.LiveSetting=\"Polite\"", diagnostics);
        Assert.Contains("KeyboardNavigation.IsTabStop=\"False\"", diagnostics);

        Assert.Contains("AutomationProperties.AutomationId=\"ActivityStatus\"", activity);
        Assert.Contains("AutomationProperties.Name=\"{Binding ActivityTitle}\"", activity);
        Assert.Contains("AutomationProperties.LiveSetting=\"Polite\"", activity);
        Assert.Contains("KeyboardNavigation.IsTabStop=\"False\"", activity);
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
