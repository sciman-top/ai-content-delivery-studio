using System.Xml.Linq;

namespace ContentDeliveryStudio.Tests;

public sealed class Phase7AccessibilityContractTests
{
    [Fact]
    public void Application_DeclaresPerMonitorV2AndGlobalKeyboardFocusStyles()
    {
        var project = ReadRepoFile("src", "ContentDeliveryStudio.App", "ContentDeliveryStudio.App.csproj");
        var manifest = ReadRepoFile("src", "ContentDeliveryStudio.App", "app.manifest");
        var appXaml = ReadRepoFile("src", "ContentDeliveryStudio.App", "App.xaml");

        Assert.Contains("<ApplicationManifest>app.manifest</ApplicationManifest>", project);
        Assert.Contains(">PerMonitorV2</dpiAwareness>", manifest);
        Assert.Contains(">true/pm</dpiAware>", manifest);
        foreach (var control in new[] { "Button", "TextBox", "ComboBox", "ListBox" })
        {
            Assert.Contains($"<Style TargetType=\"{control}\">", appXaml);
        }
        Assert.Contains("SystemColors.HighlightBrushKey", appXaml);
    }

    [Theory]
    [InlineData("ProjectSetupPanelView.xaml")]
    [InlineData("FakePlanningPanelView.xaml")]
    [InlineData("StyleRecipeInspectorPanelView.xaml")]
    [InlineData("DocumentIllustrationPanelView.xaml")]
    [InlineData("PlanEditorPanelView.xaml")]
    [InlineData("PromptEditorPanelView.xaml")]
    [InlineData("ImageEditPanelView.xaml")]
    [InlineData("ReviewApprovalPanelView.xaml")]
    [InlineData("BackupRestorePanelView.xaml")]
    public void PrincipalInspectorForms_NameAndIdentifyEveryInteractiveControl(string fileName)
    {
        var document = XDocument.Load(GetRepoFilePath("src", "ContentDeliveryStudio.App", "Views", fileName));
        var interactiveNames = new HashSet<string>(StringComparer.Ordinal)
        {
            "Button",
            "TextBox",
            "ComboBox",
            "ListBox",
        };

        var controls = document.Descendants()
            .Where(element => interactiveNames.Contains(element.Name.LocalName))
            .ToArray();
        Assert.NotEmpty(controls);
        foreach (var control in controls)
        {
            Assert.True(
                control.Attributes().Any(attribute =>
                    attribute.Name.LocalName.EndsWith(".AutomationId", StringComparison.Ordinal)),
                $"{fileName} {control.Name.LocalName} is missing AutomationProperties.AutomationId.");
            Assert.True(
                control.Attributes().Any(attribute =>
                    attribute.Name.LocalName.EndsWith(".Name", StringComparison.Ordinal)),
                $"{fileName} {control.Name.LocalName} is missing AutomationProperties.Name.");
        }
    }

    private static string ReadRepoFile(params string[] segments) => File.ReadAllText(GetRepoFilePath(segments));

    private static string GetRepoFilePath(params string[] segments)
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
        return Path.GetFullPath(Path.Combine(pathSegments.ToArray()));
    }
}
