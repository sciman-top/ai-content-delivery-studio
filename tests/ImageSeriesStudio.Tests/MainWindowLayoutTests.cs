using System.IO;

namespace ImageSeriesStudio.Tests;

public sealed class MainWindowLayoutTests
{
    [Fact]
    public void MainWindowXaml_UsesBriefWorkflowUserControl()
    {
        var mainWindowXaml = ReadRepoFile("src/ImageSeriesStudio.App", "MainWindow.xaml");
        var tabHostViewXaml = ReadRepoFile("src", "ImageSeriesStudio.App", "Views", "WorkbenchTabHostView.xaml");

        Assert.DoesNotContain("<views:BriefWorkflowView Grid.Row=\"1\" Margin=\"0,16,0,0\" />", mainWindowXaml);
        Assert.DoesNotContain(
            "ItemsSource=\"{Binding DataContext.DesignBlueprintRows, RelativeSource={RelativeSource AncestorType=TabControl}}\"",
            mainWindowXaml);
        Assert.DoesNotContain(
            "ItemsSource=\"{Binding DataContext.PromptDirectionRows, RelativeSource={RelativeSource AncestorType=TabControl}}\"",
            mainWindowXaml);

        var briefWorkflowViewPath = GetRepoFilePath("src", "ImageSeriesStudio.App", "Views", "BriefWorkflowView.xaml");
        Assert.True(File.Exists(briefWorkflowViewPath));

        Assert.Contains("<views:BriefWorkflowView Grid.Row=\"1\" Margin=\"0,16,0,0\" />", tabHostViewXaml);

        var briefWorkflowViewXaml = File.ReadAllText(briefWorkflowViewPath);
        Assert.Contains("<views:BlueprintRoutesView Grid.Row=\"3\" />", briefWorkflowViewXaml);
        Assert.Contains("<views:PromptDirectionsView Grid.Row=\"5\" />", briefWorkflowViewXaml);
        Assert.DoesNotContain(
            "ItemsSource=\"{Binding DataContext.DesignBlueprintRows, RelativeSource={RelativeSource AncestorType=TabControl}}\"",
            briefWorkflowViewXaml);
        Assert.DoesNotContain(
            "ItemsSource=\"{Binding DataContext.PromptDirectionRows, RelativeSource={RelativeSource AncestorType=TabControl}}\"",
            briefWorkflowViewXaml);

        var blueprintRoutesViewPath = GetRepoFilePath("src", "ImageSeriesStudio.App", "Views", "BlueprintRoutesView.xaml");
        Assert.True(File.Exists(blueprintRoutesViewPath));
        var blueprintRoutesViewXaml = File.ReadAllText(blueprintRoutesViewPath);
        Assert.Contains(
            "ItemsSource=\"{Binding DataContext.DesignBlueprintRows, RelativeSource={RelativeSource AncestorType=TabControl}}\"",
            blueprintRoutesViewXaml);

        var promptDirectionsViewPath = GetRepoFilePath("src", "ImageSeriesStudio.App", "Views", "PromptDirectionsView.xaml");
        Assert.True(File.Exists(promptDirectionsViewPath));
        var promptDirectionsViewXaml = File.ReadAllText(promptDirectionsViewPath);
        Assert.Contains(
            "ItemsSource=\"{Binding DataContext.PromptDirectionRows, RelativeSource={RelativeSource AncestorType=TabControl}}\"",
            promptDirectionsViewXaml);
    }

    [Fact]
    public void MainWindowXaml_UsesPlanUserControl()
    {
        var mainWindowXaml = ReadRepoFile("src/ImageSeriesStudio.App", "MainWindow.xaml");
        var tabHostViewXaml = ReadRepoFile("src", "ImageSeriesStudio.App", "Views", "WorkbenchTabHostView.xaml");

        Assert.DoesNotContain("<views:PlanView Grid.Row=\"1\" Margin=\"0,16,0,0\" />", mainWindowXaml);
        Assert.DoesNotContain(
            "ItemsSource=\"{Binding DataContext.PlanRows, RelativeSource={RelativeSource AncestorType=TabControl}}\"",
            mainWindowXaml);

        var planViewPath = GetRepoFilePath("src", "ImageSeriesStudio.App", "Views", "PlanView.xaml");
        Assert.True(File.Exists(planViewPath));

        Assert.Contains("<views:PlanView Grid.Row=\"1\" Margin=\"0,16,0,0\" />", tabHostViewXaml);

        var planViewXaml = File.ReadAllText(planViewPath);
        Assert.Contains(
            "ItemsSource=\"{Binding DataContext.PlanRows, RelativeSource={RelativeSource AncestorType=TabControl}}\"",
            planViewXaml);
    }

    [Fact]
    public void MainWindowXaml_UsesPromptsUserControl()
    {
        var mainWindowXaml = ReadRepoFile("src/ImageSeriesStudio.App", "MainWindow.xaml");
        var tabHostViewXaml = ReadRepoFile("src", "ImageSeriesStudio.App", "Views", "WorkbenchTabHostView.xaml");

        Assert.DoesNotContain("<views:PromptsView Grid.Row=\"1\" Margin=\"0,16,0,0\" />", mainWindowXaml);
        Assert.DoesNotContain(
            "ItemsSource=\"{Binding DataContext.PromptRows, RelativeSource={RelativeSource AncestorType=TabControl}}\"",
            mainWindowXaml);

        var promptsViewPath = GetRepoFilePath("src", "ImageSeriesStudio.App", "Views", "PromptsView.xaml");
        Assert.True(File.Exists(promptsViewPath));

        Assert.Contains("<views:PromptsView Grid.Row=\"1\" Margin=\"0,16,0,0\" />", tabHostViewXaml);

        var promptsViewXaml = File.ReadAllText(promptsViewPath);
        Assert.Contains(
            "ItemsSource=\"{Binding DataContext.PromptRows, RelativeSource={RelativeSource AncestorType=TabControl}}\"",
            promptsViewXaml);
    }

    [Fact]
    public void MainWindowXaml_UsesPlanEditorPanelUserControl()
    {
        var mainWindowXaml = ReadRepoFile("src/ImageSeriesStudio.App", "MainWindow.xaml");
        var inspectorViewXaml = ReadRepoFile("src", "ImageSeriesStudio.App", "Views", "WorkbenchInspectorView.xaml");

        Assert.DoesNotContain("<views:PlanEditorPanelView Margin=\"0,0,0,14\" />", mainWindowXaml);
        Assert.DoesNotContain(
            "ItemsSource=\"{Binding SeriesItems}\"",
            mainWindowXaml);

        var planEditorPanelViewPath = GetRepoFilePath("src", "ImageSeriesStudio.App", "Views", "PlanEditorPanelView.xaml");
        Assert.True(File.Exists(planEditorPanelViewPath));

        Assert.Contains("<views:PlanEditorPanelView Margin=\"0,0,0,14\" />", inspectorViewXaml);

        var planEditorPanelViewXaml = File.ReadAllText(planEditorPanelViewPath);
        Assert.Contains("ItemsSource=\"{Binding SeriesItems}\"", planEditorPanelViewXaml);
        Assert.Contains("Command=\"{Binding CreateSeriesCommand}\"", planEditorPanelViewXaml);
    }

    [Fact]
    public void MainWindowXaml_UsesPromptEditorPanelUserControl()
    {
        var mainWindowXaml = ReadRepoFile("src/ImageSeriesStudio.App", "MainWindow.xaml");
        var inspectorViewXaml = ReadRepoFile("src", "ImageSeriesStudio.App", "Views", "WorkbenchInspectorView.xaml");

        Assert.DoesNotContain("<views:PromptEditorPanelView Margin=\"0,0,0,12\" />", mainWindowXaml);
        Assert.DoesNotContain(
            "ItemsSource=\"{Binding PromptVersions}\"",
            mainWindowXaml);

        var promptEditorPanelViewPath = GetRepoFilePath("src", "ImageSeriesStudio.App", "Views", "PromptEditorPanelView.xaml");
        Assert.True(File.Exists(promptEditorPanelViewPath));

        Assert.Contains("<views:PromptEditorPanelView Margin=\"0,0,0,12\" />", inspectorViewXaml);

        var promptEditorPanelViewXaml = File.ReadAllText(promptEditorPanelViewPath);
        Assert.Contains("ItemsSource=\"{Binding PromptVersions}\"", promptEditorPanelViewXaml);
        Assert.Contains("Command=\"{Binding CreatePromptVersionCommand}\"", promptEditorPanelViewXaml);
    }

    [Fact]
    public void MainWindowXaml_UsesImageEditPanelUserControl()
    {
        var mainWindowXaml = ReadRepoFile("src/ImageSeriesStudio.App", "MainWindow.xaml");
        var inspectorViewXaml = ReadRepoFile("src", "ImageSeriesStudio.App", "Views", "WorkbenchInspectorView.xaml");

        Assert.DoesNotContain("<views:ImageEditPanelView Margin=\"0,0,0,12\" />", mainWindowXaml);
        Assert.DoesNotContain(
            "Text=\"{Binding SelectedCandidateSummary}\"",
            mainWindowXaml);

        var imageEditPanelViewPath = GetRepoFilePath("src", "ImageSeriesStudio.App", "Views", "ImageEditPanelView.xaml");
        Assert.True(File.Exists(imageEditPanelViewPath));

        Assert.Contains("<views:ImageEditPanelView Margin=\"0,0,0,12\" />", inspectorViewXaml);

        var imageEditPanelViewXaml = File.ReadAllText(imageEditPanelViewPath);
        Assert.Contains("Text=\"{Binding SelectedCandidateSummary}\"", imageEditPanelViewXaml);
        Assert.Contains("Command=\"{Binding RunFakeImageEditCommand}\"", imageEditPanelViewXaml);
    }

    [Fact]
    public void MainWindowXaml_UsesReviewApprovalPanelUserControl()
    {
        var mainWindowXaml = ReadRepoFile("src/ImageSeriesStudio.App", "MainWindow.xaml");
        var inspectorViewXaml = ReadRepoFile("src", "ImageSeriesStudio.App", "Views", "WorkbenchInspectorView.xaml");

        Assert.DoesNotContain("<views:ReviewApprovalPanelView />", mainWindowXaml);
        Assert.DoesNotContain(
            "Command=\"{Binding ApproveSelectedReviewCommand}\"",
            mainWindowXaml);

        var reviewApprovalPanelViewPath = GetRepoFilePath("src", "ImageSeriesStudio.App", "Views", "ReviewApprovalPanelView.xaml");
        Assert.True(File.Exists(reviewApprovalPanelViewPath));

        Assert.Contains("<views:ReviewApprovalPanelView />", inspectorViewXaml);

        var reviewApprovalPanelViewXaml = File.ReadAllText(reviewApprovalPanelViewPath);
        Assert.Contains("Command=\"{Binding ApproveSelectedReviewCommand}\"", reviewApprovalPanelViewXaml);
        Assert.Contains("Command=\"{Binding ExportDeliveryCommand}\"", reviewApprovalPanelViewXaml);
    }

    [Fact]
    public void WorkbenchInspectorXaml_UsesStyleRecipeInspectorPanelUserControl()
    {
        var mainWindowXaml = ReadRepoFile("src", "ImageSeriesStudio.App", "MainWindow.xaml");
        var inspectorViewXaml = ReadRepoFile("src", "ImageSeriesStudio.App", "Views", "WorkbenchInspectorView.xaml");

        Assert.DoesNotContain("<views:StyleRecipeInspectorPanelView />", mainWindowXaml);
        Assert.DoesNotContain("ItemsSource=\"{Binding ImageTypePresetOptions}\"", mainWindowXaml);
        Assert.DoesNotContain("ItemsSource=\"{Binding ImageTypePresetOptions}\"", inspectorViewXaml);

        var styleRecipeInspectorPanelViewPath = GetRepoFilePath(
            "src",
            "ImageSeriesStudio.App",
            "Views",
            "StyleRecipeInspectorPanelView.xaml");
        Assert.True(File.Exists(styleRecipeInspectorPanelViewPath));

        Assert.Contains("<views:StyleRecipeInspectorPanelView />", inspectorViewXaml);

        var styleRecipeInspectorPanelViewXaml = File.ReadAllText(styleRecipeInspectorPanelViewPath);
        Assert.Contains("ItemsSource=\"{Binding ImageTypePresetOptions}\"", styleRecipeInspectorPanelViewXaml);
        Assert.Contains("SelectedItem=\"{Binding SelectedImageTypePresetOption, Mode=TwoWay}\"", styleRecipeInspectorPanelViewXaml);
        Assert.Contains("SelectedItem=\"{Binding SelectedStyleGuideOption, Mode=TwoWay}\"", styleRecipeInspectorPanelViewXaml);
        Assert.Contains("SelectedItem=\"{Binding SelectedGenerationRecipeOption, Mode=TwoWay}\"", styleRecipeInspectorPanelViewXaml);
        Assert.Contains("Text=\"{Binding StyleRecipeSummaryText}\"", styleRecipeInspectorPanelViewXaml);
    }

    [Fact]
    public void WorkbenchInspectorXaml_UsesFakePlanningPanelUserControl()
    {
        var mainWindowXaml = ReadRepoFile("src", "ImageSeriesStudio.App", "MainWindow.xaml");
        var inspectorViewXaml = ReadRepoFile("src", "ImageSeriesStudio.App", "Views", "WorkbenchInspectorView.xaml");

        Assert.DoesNotContain("<views:FakePlanningPanelView />", mainWindowXaml);
        Assert.DoesNotContain("Text=\"{Binding NewPlanningGoal, UpdateSourceTrigger=PropertyChanged}\"", mainWindowXaml);
        Assert.DoesNotContain("Text=\"{Binding NewPlanningGoal, UpdateSourceTrigger=PropertyChanged}\"", inspectorViewXaml);

        var fakePlanningPanelViewPath = GetRepoFilePath(
            "src",
            "ImageSeriesStudio.App",
            "Views",
            "FakePlanningPanelView.xaml");
        Assert.True(File.Exists(fakePlanningPanelViewPath));

        Assert.Contains("<views:FakePlanningPanelView />", inspectorViewXaml);

        var fakePlanningPanelViewXaml = File.ReadAllText(fakePlanningPanelViewPath);
        Assert.Contains("Text=\"{Binding NewPlanningGoal, UpdateSourceTrigger=PropertyChanged}\"", fakePlanningPanelViewXaml);
        Assert.Contains("Text=\"{Binding NewPlanningAudience, UpdateSourceTrigger=PropertyChanged}\"", fakePlanningPanelViewXaml);
        Assert.Contains("Text=\"{Binding NewPlanningItemCount, UpdateSourceTrigger=PropertyChanged}\"", fakePlanningPanelViewXaml);
        Assert.Contains("Text=\"{Binding NewPlanningStyleBrief, UpdateSourceTrigger=PropertyChanged}\"", fakePlanningPanelViewXaml);
        Assert.Contains("Command=\"{Binding RunFakePlanningCommand}\"", fakePlanningPanelViewXaml);
    }

    [Fact]
    public void WorkbenchInspectorXaml_UsesDocumentIllustrationPanelUserControl()
    {
        var mainWindowXaml = ReadRepoFile("src", "ImageSeriesStudio.App", "MainWindow.xaml");
        var inspectorViewXaml = ReadRepoFile("src", "ImageSeriesStudio.App", "Views", "WorkbenchInspectorView.xaml");

        Assert.DoesNotContain("<views:DocumentIllustrationPanelView />", mainWindowXaml);
        Assert.DoesNotContain("Text=\"{Binding DocumentIllustrationTitle}\"", mainWindowXaml);
        Assert.DoesNotContain("Text=\"{Binding DocumentIllustrationTitle}\"", inspectorViewXaml);

        var documentIllustrationPanelViewPath = GetRepoFilePath(
            "src",
            "ImageSeriesStudio.App",
            "Views",
            "DocumentIllustrationPanelView.xaml");
        Assert.True(File.Exists(documentIllustrationPanelViewPath));

        Assert.Contains("<views:DocumentIllustrationPanelView />", inspectorViewXaml);

        var documentIllustrationPanelViewXaml = File.ReadAllText(documentIllustrationPanelViewPath);
        Assert.Contains("Text=\"{Binding NewDocumentSourceText, UpdateSourceTrigger=PropertyChanged}\"", documentIllustrationPanelViewXaml);
        Assert.Contains("Text=\"{Binding NewDocumentAudience, UpdateSourceTrigger=PropertyChanged}\"", documentIllustrationPanelViewXaml);
        Assert.Contains("ItemsSource=\"{Binding DocumentStrictnessOptions}\"", documentIllustrationPanelViewXaml);
        Assert.Contains("SelectedItem=\"{Binding SelectedDocumentStrictnessOption, Mode=TwoWay}\"", documentIllustrationPanelViewXaml);
        Assert.Contains("Command=\"{Binding RunFakeDocumentPlanningCommand}\"", documentIllustrationPanelViewXaml);
        Assert.Contains("Text=\"{Binding DocumentPlanningResultSummary}\"", documentIllustrationPanelViewXaml);
    }

    [Fact]
    public void WorkbenchInspectorXaml_UsesProjectSetupPanelUserControl()
    {
        var mainWindowXaml = ReadRepoFile("src", "ImageSeriesStudio.App", "MainWindow.xaml");
        var inspectorViewXaml = ReadRepoFile("src", "ImageSeriesStudio.App", "Views", "WorkbenchInspectorView.xaml");

        Assert.DoesNotContain("<views:ProjectSetupPanelView />", mainWindowXaml);
        Assert.DoesNotContain("Text=\"{Binding NewProjectName, UpdateSourceTrigger=PropertyChanged}\"", mainWindowXaml);
        Assert.DoesNotContain("Text=\"{Binding NewProjectName, UpdateSourceTrigger=PropertyChanged}\"", inspectorViewXaml);
        Assert.DoesNotContain("ItemsSource=\"{Binding Projects}\"", inspectorViewXaml);

        var projectSetupPanelViewPath = GetRepoFilePath(
            "src",
            "ImageSeriesStudio.App",
            "Views",
            "ProjectSetupPanelView.xaml");
        Assert.True(File.Exists(projectSetupPanelViewPath));

        Assert.Contains("<views:ProjectSetupPanelView />", inspectorViewXaml);

        var projectSetupPanelViewXaml = File.ReadAllText(projectSetupPanelViewPath);
        Assert.Contains("Text=\"{Binding NewProjectName, UpdateSourceTrigger=PropertyChanged}\"", projectSetupPanelViewXaml);
        Assert.Contains("Command=\"{Binding CreateProjectCommand}\"", projectSetupPanelViewXaml);
        Assert.Contains("Text=\"{Binding CurrentProjectSummary}\"", projectSetupPanelViewXaml);
        Assert.Contains("ItemsSource=\"{Binding Projects}\"", projectSetupPanelViewXaml);
        Assert.Contains("SelectedItem=\"{Binding SelectedProject, Mode=TwoWay}\"", projectSetupPanelViewXaml);
    }

    [Fact]
    public void MainWindowXaml_UsesWorkflowGraphUserControl()
    {
        var mainWindowXaml = ReadRepoFile("src/ImageSeriesStudio.App", "MainWindow.xaml");
        var tabHostViewXaml = ReadRepoFile("src", "ImageSeriesStudio.App", "Views", "WorkbenchTabHostView.xaml");

        Assert.Contains("xmlns:views=\"clr-namespace:ImageSeriesStudio.App.Views\"", mainWindowXaml);
        Assert.DoesNotContain("<views:WorkflowGraphView Grid.Row=\"1\" Margin=\"0,16,0,0\" />", mainWindowXaml);
        Assert.DoesNotContain(
            "ItemsSource=\"{Binding DataContext.WorkflowGraphRows, RelativeSource={RelativeSource AncestorType=TabControl}}\"",
            mainWindowXaml);

        var workflowGraphViewPath = GetRepoFilePath("src", "ImageSeriesStudio.App", "Views", "WorkflowGraphView.xaml");
        Assert.True(File.Exists(workflowGraphViewPath));

        Assert.Contains("<views:WorkflowGraphView Grid.Row=\"1\" Margin=\"0,16,0,0\" />", tabHostViewXaml);

        var workflowGraphViewXaml = File.ReadAllText(workflowGraphViewPath);
        Assert.Contains(
            "ItemsSource=\"{Binding DataContext.WorkflowGraphRows, RelativeSource={RelativeSource AncestorType=TabControl}}\"",
            workflowGraphViewXaml);
    }

    [Fact]
    public void MainWindowXaml_UsesDeliveryUserControl()
    {
        var mainWindowXaml = ReadRepoFile("src/ImageSeriesStudio.App", "MainWindow.xaml");
        var tabHostViewXaml = ReadRepoFile("src", "ImageSeriesStudio.App", "Views", "WorkbenchTabHostView.xaml");

        Assert.DoesNotContain("<views:DeliveryView Grid.Row=\"1\" Margin=\"0,16,0,0\" />", mainWindowXaml);
        Assert.DoesNotContain(
            "ItemsSource=\"{Binding DataContext.DeliveryRows, RelativeSource={RelativeSource AncestorType=TabControl}}\"",
            mainWindowXaml);

        var deliveryViewPath = GetRepoFilePath("src", "ImageSeriesStudio.App", "Views", "DeliveryView.xaml");
        Assert.True(File.Exists(deliveryViewPath));

        Assert.Contains("<views:DeliveryView Grid.Row=\"1\" Margin=\"0,16,0,0\" />", tabHostViewXaml);

        var deliveryViewXaml = File.ReadAllText(deliveryViewPath);
        Assert.Contains("<views:DeliveryResultsListView Grid.Row=\"2\" />", deliveryViewXaml);
        Assert.DoesNotContain(
            "ItemsSource=\"{Binding DataContext.DeliveryRows, RelativeSource={RelativeSource AncestorType=TabControl}}\"",
            deliveryViewXaml);

        var deliveryResultsListViewPath = GetRepoFilePath("src", "ImageSeriesStudio.App", "Views", "DeliveryResultsListView.xaml");
        Assert.True(File.Exists(deliveryResultsListViewPath));
        var deliveryResultsListViewXaml = File.ReadAllText(deliveryResultsListViewPath);
        Assert.Contains(
            "ItemsSource=\"{Binding DataContext.DeliveryRows, RelativeSource={RelativeSource AncestorType=TabControl}}\"",
            deliveryResultsListViewXaml);
    }

    [Fact]
    public void MainWindowXaml_UsesReviewUserControl()
    {
        var mainWindowXaml = ReadRepoFile("src/ImageSeriesStudio.App", "MainWindow.xaml");
        var tabHostViewXaml = ReadRepoFile("src", "ImageSeriesStudio.App", "Views", "WorkbenchTabHostView.xaml");

        Assert.DoesNotContain("<views:ReviewView Grid.Row=\"1\" Margin=\"0,16,0,0\" />", mainWindowXaml);
        Assert.DoesNotContain(
            "ItemsSource=\"{Binding DataContext.ReviewRows, RelativeSource={RelativeSource AncestorType=TabControl}}\"",
            mainWindowXaml);

        var reviewViewPath = GetRepoFilePath("src", "ImageSeriesStudio.App", "Views", "ReviewView.xaml");
        Assert.True(File.Exists(reviewViewPath));

        Assert.Contains("<views:ReviewView Grid.Row=\"1\" Margin=\"0,16,0,0\" />", tabHostViewXaml);

        var reviewViewXaml = File.ReadAllText(reviewViewPath);
        Assert.Contains("<views:ReviewResultsListView Grid.Row=\"2\" />", reviewViewXaml);
        Assert.DoesNotContain(
            "ItemsSource=\"{Binding DataContext.ReviewRows, RelativeSource={RelativeSource AncestorType=TabControl}}\"",
            reviewViewXaml);

        var reviewResultsListViewPath = GetRepoFilePath("src", "ImageSeriesStudio.App", "Views", "ReviewResultsListView.xaml");
        Assert.True(File.Exists(reviewResultsListViewPath));
        var reviewResultsListViewXaml = File.ReadAllText(reviewResultsListViewPath);
        Assert.Contains(
            "ItemsSource=\"{Binding DataContext.ReviewRows, RelativeSource={RelativeSource AncestorType=TabControl}}\"",
            reviewResultsListViewXaml);
    }

    [Fact]
    public void MainWindowXaml_UsesQueueUserControl()
    {
        var mainWindowXaml = ReadRepoFile("src/ImageSeriesStudio.App", "MainWindow.xaml");
        var tabHostViewXaml = ReadRepoFile("src", "ImageSeriesStudio.App", "Views", "WorkbenchTabHostView.xaml");

        Assert.DoesNotContain("<views:QueueView Grid.Row=\"1\" Margin=\"0,16,0,0\" />", mainWindowXaml);
        Assert.DoesNotContain(
            "ItemsSource=\"{Binding DataContext.QueueRows, RelativeSource={RelativeSource AncestorType=TabControl}}\"",
            mainWindowXaml);

        var queueViewPath = GetRepoFilePath("src", "ImageSeriesStudio.App", "Views", "QueueView.xaml");
        Assert.True(File.Exists(queueViewPath));

        Assert.Contains("<views:QueueView Grid.Row=\"1\" Margin=\"0,16,0,0\" />", tabHostViewXaml);

        var queueViewXaml = File.ReadAllText(queueViewPath);
        Assert.Contains(
            "ItemsSource=\"{Binding DataContext.QueueRows, RelativeSource={RelativeSource AncestorType=TabControl}}\"",
            queueViewXaml);
    }

    [Fact]
    public void MainWindowXaml_UsesGalleryUserControl()
    {
        var mainWindowXaml = ReadRepoFile("src/ImageSeriesStudio.App", "MainWindow.xaml");
        var tabHostViewXaml = ReadRepoFile("src", "ImageSeriesStudio.App", "Views", "WorkbenchTabHostView.xaml");

        Assert.DoesNotContain("<views:GalleryView Grid.Row=\"1\" Margin=\"0,16,0,0\" />", mainWindowXaml);
        Assert.DoesNotContain(
            "ItemsSource=\"{Binding DataContext.GalleryRows, RelativeSource={RelativeSource AncestorType=TabControl}}\"",
            mainWindowXaml);

        var galleryViewPath = GetRepoFilePath("src", "ImageSeriesStudio.App", "Views", "GalleryView.xaml");
        Assert.True(File.Exists(galleryViewPath));

        Assert.Contains("<views:GalleryView Grid.Row=\"1\" Margin=\"0,16,0,0\" />", tabHostViewXaml);

        var galleryViewXaml = File.ReadAllText(galleryViewPath);
        Assert.Contains(
            "ItemsSource=\"{Binding DataContext.GalleryRows, RelativeSource={RelativeSource AncestorType=TabControl}}\"",
            galleryViewXaml);
        Assert.Contains(
            "SelectedItem=\"{Binding DataContext.SelectedGalleryRow, RelativeSource={RelativeSource AncestorType=TabControl}, Mode=TwoWay}\"",
            galleryViewXaml);
    }

    [Fact]
    public void MainWindowXaml_UsesWorkbenchInspectorUserControl()
    {
        var mainWindowXaml = ReadRepoFile("src", "ImageSeriesStudio.App", "MainWindow.xaml");

        Assert.Contains("<views:WorkbenchInspectorView />", mainWindowXaml);
        Assert.DoesNotContain("Text=\"Provider Center\"", mainWindowXaml);
        Assert.DoesNotContain("Text=\"{Binding DocumentIllustrationTitle}\"", mainWindowXaml);
        Assert.DoesNotContain("Text=\"{Binding CurrentProjectSummary}\"", mainWindowXaml);

        var inspectorViewPath = GetRepoFilePath("src", "ImageSeriesStudio.App", "Views", "WorkbenchInspectorView.xaml");
        Assert.True(File.Exists(inspectorViewPath));

        var inspectorViewXaml = File.ReadAllText(inspectorViewPath);
        Assert.Contains("<views:ProviderCenterPanelView />", inspectorViewXaml);
        Assert.Contains("<views:ProjectSetupPanelView />", inspectorViewXaml);
        Assert.Contains("<views:StyleRecipeInspectorPanelView />", inspectorViewXaml);
        Assert.Contains("<views:FakePlanningPanelView />", inspectorViewXaml);
        Assert.Contains("<views:DocumentIllustrationPanelView />", inspectorViewXaml);
        Assert.DoesNotContain("Text=\"Provider Center\"", inspectorViewXaml);
        Assert.DoesNotContain("Text=\"{Binding CurrentProjectSummary}\"", inspectorViewXaml);

        var providerCenterPanelViewPath = GetRepoFilePath("src", "ImageSeriesStudio.App", "Views", "ProviderCenterPanelView.xaml");
        Assert.True(File.Exists(providerCenterPanelViewPath));

        var providerCenterPanelViewXaml = File.ReadAllText(providerCenterPanelViewPath);
        Assert.Contains("Text=\"Provider Center\"", providerCenterPanelViewXaml);
        Assert.Contains("ItemsSource=\"{Binding ProviderCenter.ProviderRows}\"", providerCenterPanelViewXaml);
        Assert.Contains("Command=\"{Binding RefreshProviderCenterCommand}\"", providerCenterPanelViewXaml);
    }

    [Fact]
    public void MainWindowXaml_UsesWorkspaceNavigationUserControl()
    {
        var mainWindowXaml = ReadRepoFile("src", "ImageSeriesStudio.App", "MainWindow.xaml");

        Assert.Contains("<views:WorkspaceNavigationView />", mainWindowXaml);
        Assert.DoesNotContain("Text=\"{Binding WorkspaceHeader}\"", mainWindowXaml);
        Assert.DoesNotContain("ItemsSource=\"{Binding NavigationItems}\"", mainWindowXaml);

        var navigationViewPath = GetRepoFilePath("src", "ImageSeriesStudio.App", "Views", "WorkspaceNavigationView.xaml");
        Assert.True(File.Exists(navigationViewPath));

        var navigationViewXaml = File.ReadAllText(navigationViewPath);
        Assert.Contains("Text=\"{Binding WorkspaceHeader}\"", navigationViewXaml);
        Assert.Contains("ItemsSource=\"{Binding NavigationItems}\"", navigationViewXaml);
    }

    [Fact]
    public void MainWindowXaml_UsesActivityPanelUserControl()
    {
        var mainWindowXaml = ReadRepoFile("src", "ImageSeriesStudio.App", "MainWindow.xaml");

        Assert.Contains("<views:ActivityPanelView />", mainWindowXaml);
        Assert.DoesNotContain("Text=\"{Binding ActivityTitle}\"", mainWindowXaml);
        Assert.DoesNotContain("ItemsSource=\"{Binding ActivityItems}\"", mainWindowXaml);

        var activityViewPath = GetRepoFilePath("src", "ImageSeriesStudio.App", "Views", "ActivityPanelView.xaml");
        Assert.True(File.Exists(activityViewPath));

        var activityViewXaml = File.ReadAllText(activityViewPath);
        Assert.Contains("Text=\"{Binding ActivityTitle}\"", activityViewXaml);
        Assert.Contains("ItemsSource=\"{Binding ActivityItems}\"", activityViewXaml);
    }

    [Fact]
    public void MainWindowXaml_UsesWorkbenchTabHostView()
    {
        var mainWindowXaml = ReadRepoFile("src", "ImageSeriesStudio.App", "MainWindow.xaml");

        Assert.Contains("<views:WorkbenchTabHostView Grid.Row=\"1\" Grid.Column=\"1\" />", mainWindowXaml);
        Assert.DoesNotContain("ItemsSource=\"{Binding WorkbenchTabs}\"", mainWindowXaml);
        Assert.DoesNotContain("Text=\"{Binding EmptyState}\"", mainWindowXaml);

        var tabHostViewPath = GetRepoFilePath("src", "ImageSeriesStudio.App", "Views", "WorkbenchTabHostView.xaml");
        Assert.True(File.Exists(tabHostViewPath));

        var tabHostViewXaml = File.ReadAllText(tabHostViewPath);
        Assert.Contains("ItemsSource=\"{Binding WorkbenchTabs}\"", tabHostViewXaml);
        Assert.Contains("Text=\"{Binding EmptyState}\"", tabHostViewXaml);
    }

    private static string ReadRepoFile(params string[] segments)
    {
        return File.ReadAllText(GetRepoFilePath(segments));
    }

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
