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
    public void QueueViews_BindThroughImageSeriesWorkspaceAndKeepStableAutomationIds()
    {
        var queueView = ReadRepoFile("src", "ContentDeliveryStudio.App", "Views", "QueueView.xaml");
        var queueHeader = ReadRepoFile("src", "ContentDeliveryStudio.App", "Views", "QueueHeaderView.xaml");
        var queueRows = ReadRepoFile("src", "ContentDeliveryStudio.App", "Views", "QueueRowsListView.xaml");
        var promptEditor = ReadRepoFile("src", "ContentDeliveryStudio.App", "Views", "PromptEditorPanelView.xaml");

        foreach (var file in new[] { queueView, queueHeader, queueRows })
        {
            Assert.Contains("ImageSeriesWorkspace", file);
        }

        foreach (var automationId in new[]
        {
            "QueueExecuteButton",
            "QueueExecuteApprovedLiveButton",
            "QueuePauseButton",
            "QueueResumeButton",
            "QueueRetryButton",
            "QueueMoveUpButton",
            "QueueMoveDownButton",
            "QueueTaskList",
        })
        {
            Assert.Contains(automationId, queueView + queueRows);
        }

        Assert.Contains("ImageSeriesWorkspace.PrepareGenerationQueueCommand", promptEditor);
        Assert.Contains("ImageSeriesWorkspace.PrepareGenerationQueueText", promptEditor);
        Assert.Contains("ImageSeriesWorkspace.IsLiveGenerationExecutionAvailable", queueView);
        Assert.Contains("ImageSeriesWorkspace.LiveGenerationAuthorityRequiredText", queueView);
        Assert.Contains("RequestSummary", queueRows);
        Assert.Contains("ApprovalSummary", queueRows);
    }

    [Fact]
    public void GalleryAndImageEditViews_BindThroughGalleryOwnerAndKeepStableAutomationIds()
    {
        var galleryView = ReadRepoFile("src", "ContentDeliveryStudio.App", "Views", "GalleryView.xaml");
        var galleryHeader = ReadRepoFile("src", "ContentDeliveryStudio.App", "Views", "GalleryHeaderView.xaml");
        var galleryRows = ReadRepoFile("src", "ContentDeliveryStudio.App", "Views", "GalleryRowsListView.xaml");
        var imageEdit = ReadRepoFile("src", "ContentDeliveryStudio.App", "Views", "ImageEditPanelView.xaml");

        foreach (var file in new[] { galleryView, galleryHeader, galleryRows, imageEdit })
        {
            Assert.Contains("ImageSeriesWorkspace.Gallery", file);
        }

        Assert.Contains("GalleryCandidateList", galleryRows);
        Assert.Contains("ImageEditPromptInput", imageEdit);
        Assert.Contains("ImageEditMaskPathInput", imageEdit);
        Assert.Contains("RunFakeImageEditButton", imageEdit);
        Assert.Contains("ApprovedImageEditCapabilitySummary", imageEdit);
        Assert.Contains("RunApprovedImageEditButton", imageEdit);
    }

    [Fact]
    public void ReviewViews_BindThroughReviewOwnerAndKeepStableAutomationIds()
    {
        var reviewView = ReadRepoFile("src", "ContentDeliveryStudio.App", "Views", "ReviewView.xaml");
        var reviewHeader = ReadRepoFile("src", "ContentDeliveryStudio.App", "Views", "ReviewHeaderView.xaml");
        var reviewRows = ReadRepoFile("src", "ContentDeliveryStudio.App", "Views", "ReviewResultsListView.xaml");
        var approvalPanel = ReadRepoFile("src", "ContentDeliveryStudio.App", "Views", "ReviewApprovalPanelView.xaml");

        foreach (var file in new[] { reviewView, reviewHeader, reviewRows, approvalPanel })
        {
            Assert.Contains("ImageSeriesWorkspace.Review", file);
        }

        foreach (var automationId in new[]
        {
            "RunFakeReviewButton",
            "FinalApprovalReviewerInput",
            "FinalApprovalNotesInput",
            "ApproveSelectedReviewButton",
            "RejectSelectedReviewButton",
        })
        {
            Assert.Contains(automationId, approvalPanel);
        }
    }

    [Fact]
    public void DeliveryViews_BindThroughDeliveryOwnerAndKeepStableAutomationIds()
    {
        var deliveryView = ReadRepoFile("src", "ContentDeliveryStudio.App", "Views", "DeliveryView.xaml");
        var deliveryHeader = ReadRepoFile("src", "ContentDeliveryStudio.App", "Views", "DeliveryHeaderView.xaml");
        var deliveryRows = ReadRepoFile("src", "ContentDeliveryStudio.App", "Views", "DeliveryResultsListView.xaml");
        var approvalPanel = ReadRepoFile("src", "ContentDeliveryStudio.App", "Views", "ReviewApprovalPanelView.xaml");

        foreach (var file in new[] { deliveryView, deliveryHeader, deliveryRows, approvalPanel })
        {
            Assert.Contains("ImageSeriesWorkspace.Delivery", file);
        }

        foreach (var automationId in new[]
        {
            "FinalDeliveryCategorySelector",
            "FinalDeliveryRootInput",
            "BrowseFinalDeliveryRootButton",
            "FinalDeliveryDestinationPreview",
            "ExportDeliveryButton",
        })
        {
            Assert.Contains(automationId, approvalPanel);
        }
    }

    [Fact]
    public void PlanAndPromptViews_BindThroughPlanningOwnerAndKeepStableAutomationIds()
    {
        var files = new[]
        {
            "PlanView.xaml",
            "PlanHeaderView.xaml",
            "PlanRowsListView.xaml",
            "PromptsView.xaml",
            "PromptsHeaderView.xaml",
            "PromptRowsListView.xaml",
            "PlanEditorPanelView.xaml",
            "PromptEditorPanelView.xaml",
        }.Select(fileName => ReadRepoFile("src", "ContentDeliveryStudio.App", "Views", fileName)).ToArray();

        Assert.All(files, file => Assert.Contains("ImageSeriesWorkspace.Planning", file));
        Assert.Contains("CreateSeriesButton", files[6]);
        Assert.Contains("AddSeriesItemButton", files[6]);
        Assert.Contains("CreatePromptVersionButton", files[7]);
    }

    [Fact]
    public void BriefViews_BindThroughBriefOwnerAndKeepStableAutomationIds()
    {
        var files = new[]
        {
            "FakePlanningPanelView.xaml",
            "BriefWorkflowView.xaml",
            "BriefWorkflowActionsView.xaml",
            "BlueprintRoutesPanelView.xaml",
            "BlueprintRoutesView.xaml",
            "PromptDirectionsPanelView.xaml",
            "PromptDirectionsView.xaml",
        }.Select(fileName => ReadRepoFile("src", "ContentDeliveryStudio.App", "Views", fileName)).ToArray();

        Assert.All(files, file => Assert.Contains("ImageSeriesWorkspace.Brief", file));
        Assert.Contains("PlanningGoalInput", files[0]);
        Assert.Contains("RunFakePlanningButton", files[0]);
    }

    [Fact]
    public void GenerationSettingsView_BindsThroughOwnerAndKeepsStableAutomationIds()
    {
        var inspector = ReadRepoFile(
            "src",
            "ContentDeliveryStudio.App",
            "Views",
            "StyleRecipeInspectorPanelView.xaml");

        Assert.Contains("ImageSeriesWorkspace.GenerationSettings", inspector);
        Assert.Contains("ImageTypePresetSelector", inspector);
        Assert.Contains("StyleGuideSelector", inspector);
        Assert.Contains("GenerationRecipeSelector", inspector);
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
