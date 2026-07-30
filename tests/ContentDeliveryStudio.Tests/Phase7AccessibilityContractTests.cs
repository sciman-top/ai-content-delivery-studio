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
        var path = GetRepoFilePath("src", "ContentDeliveryStudio.App", "Views", fileName);
        var document = XDocument.Load(path);
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

    [Fact]
    public void PackagedProbe_VerifiesPackageAndKeepsManualBoundariesExplicit()
    {
        var script = ReadRepoFile("scripts", "run-packaged-accessibility-probe.ps1");

        Assert.Contains("verify-publish-package.ps1", script);
        Assert.Contains("PROVIDER_MODE = 'fake'", script);
        Assert.Contains("GetDpiForWindow", script);
        Assert.Contains("'CreateSafeBackup'", script);
        Assert.Contains("'RestoreSafeBackup'", script);
        Assert.Contains("doesNotProve", script);
        Assert.Contains("Narrator behavior", script);

        var verifier = ReadRepoFile("scripts", "verify-publish-package.ps1");
        Assert.Contains("maximumEntryCount", verifier);
        Assert.Contains("unsupported link-like entry", verifier);
        Assert.Contains("unexpected nested ZIP", verifier);
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
