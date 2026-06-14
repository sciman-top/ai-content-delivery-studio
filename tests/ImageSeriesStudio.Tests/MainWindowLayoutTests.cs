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
        Assert.Contains("<views:BriefWorkflowActionsView Grid.Row=\"1\" />", briefWorkflowViewXaml);
        Assert.Contains("<views:BlueprintRoutesPanelView Grid.Row=\"2\" Grid.RowSpan=\"2\" />", briefWorkflowViewXaml);
        Assert.Contains("<views:PromptDirectionsPanelView Grid.Row=\"4\" Grid.RowSpan=\"2\" />", briefWorkflowViewXaml);
        Assert.DoesNotContain(
            "Command=\"{Binding DataContext.CreateBriefCommand, RelativeSource={RelativeSource AncestorType=TabControl}}\"",
            briefWorkflowViewXaml);
        Assert.DoesNotContain(
            "Text=\"{Binding DataContext.BlueprintRoutesHeader, RelativeSource={RelativeSource AncestorType=TabControl}}\"",
            briefWorkflowViewXaml);
        Assert.DoesNotContain(
            "Text=\"{Binding DataContext.PromptDirectionsHeader, RelativeSource={RelativeSource AncestorType=TabControl}}\"",
            briefWorkflowViewXaml);
        Assert.DoesNotContain(
            "ItemsSource=\"{Binding DataContext.DesignBlueprintRows, RelativeSource={RelativeSource AncestorType=TabControl}}\"",
            briefWorkflowViewXaml);
        Assert.DoesNotContain(
            "ItemsSource=\"{Binding DataContext.PromptDirectionRows, RelativeSource={RelativeSource AncestorType=TabControl}}\"",
            briefWorkflowViewXaml);

        var briefWorkflowActionsViewPath = GetRepoFilePath("src", "ImageSeriesStudio.App", "Views", "BriefWorkflowActionsView.xaml");
        Assert.True(File.Exists(briefWorkflowActionsViewPath));
        var briefWorkflowActionsViewXaml = File.ReadAllText(briefWorkflowActionsViewPath);
        Assert.Contains(
            "Command=\"{Binding DataContext.CreateBriefCommand, RelativeSource={RelativeSource AncestorType=TabControl}}\"",
            briefWorkflowActionsViewXaml);
        Assert.Contains(
            "Command=\"{Binding DataContext.GeneratePromptDirectionsCommand, RelativeSource={RelativeSource AncestorType=TabControl}}\"",
            briefWorkflowActionsViewXaml);

        var blueprintRoutesPanelViewPath = GetRepoFilePath("src", "ImageSeriesStudio.App", "Views", "BlueprintRoutesPanelView.xaml");
        Assert.True(File.Exists(blueprintRoutesPanelViewPath));
        var blueprintRoutesPanelViewXaml = File.ReadAllText(blueprintRoutesPanelViewPath);
        Assert.Contains(
            "Text=\"{Binding DataContext.BlueprintRoutesHeader, RelativeSource={RelativeSource AncestorType=TabControl}}\"",
            blueprintRoutesPanelViewXaml);
        Assert.Contains("<views:BlueprintRoutesView Grid.Row=\"1\" />", blueprintRoutesPanelViewXaml);

        var promptDirectionsPanelViewPath = GetRepoFilePath("src", "ImageSeriesStudio.App", "Views", "PromptDirectionsPanelView.xaml");
        Assert.True(File.Exists(promptDirectionsPanelViewPath));
        var promptDirectionsPanelViewXaml = File.ReadAllText(promptDirectionsPanelViewPath);
        Assert.Contains(
            "Text=\"{Binding DataContext.PromptDirectionsHeader, RelativeSource={RelativeSource AncestorType=TabControl}}\"",
            promptDirectionsPanelViewXaml);
        Assert.Contains("<views:PromptDirectionsView Grid.Row=\"1\" />", promptDirectionsPanelViewXaml);

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
        Assert.Contains("<views:PlanHeaderView Grid.Row=\"0\" />", planViewXaml);
        Assert.Contains("<views:PlanRowsListView Grid.Row=\"2\" />", planViewXaml);
        Assert.DoesNotContain(
            "Text=\"{Binding DataContext.PlanSeriesColumn, RelativeSource={RelativeSource AncestorType=TabControl}}\"",
            planViewXaml);
        Assert.DoesNotContain(
            "ItemsSource=\"{Binding DataContext.PlanRows, RelativeSource={RelativeSource AncestorType=TabControl}}\"",
            planViewXaml);

        var planHeaderViewPath = GetRepoFilePath("src", "ImageSeriesStudio.App", "Views", "PlanHeaderView.xaml");
        Assert.True(File.Exists(planHeaderViewPath));
        var planHeaderViewXaml = File.ReadAllText(planHeaderViewPath);
        Assert.Contains(
            "Text=\"{Binding DataContext.PlanSeriesColumn, RelativeSource={RelativeSource AncestorType=TabControl}}\"",
            planHeaderViewXaml);
        Assert.Contains(
            "Text=\"{Binding DataContext.PlanStatusColumn, RelativeSource={RelativeSource AncestorType=TabControl}}\"",
            planHeaderViewXaml);

        var planRowsListViewPath = GetRepoFilePath("src", "ImageSeriesStudio.App", "Views", "PlanRowsListView.xaml");
        Assert.True(File.Exists(planRowsListViewPath));

        var planRowsListViewXaml = File.ReadAllText(planRowsListViewPath);
        Assert.Contains(
            "ItemsSource=\"{Binding DataContext.PlanRows, RelativeSource={RelativeSource AncestorType=TabControl}}\"",
            planRowsListViewXaml);
        Assert.Contains(
            "Binding DataContext.HasPlanRows, RelativeSource={RelativeSource AncestorType=TabControl}",
            planRowsListViewXaml);
        Assert.Contains("Text=\"{Binding SeriesTitle}\"", planRowsListViewXaml);
        Assert.Contains("Text=\"{Binding StatusText}\"", planRowsListViewXaml);
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
        Assert.Contains("<views:PromptsHeaderView Grid.Row=\"0\" />", promptsViewXaml);
        Assert.Contains("<views:PromptRowsListView Grid.Row=\"2\" />", promptsViewXaml);
        Assert.DoesNotContain(
            "Text=\"{Binding DataContext.PromptVersionColumn, RelativeSource={RelativeSource AncestorType=TabControl}}\"",
            promptsViewXaml);
        Assert.DoesNotContain(
            "ItemsSource=\"{Binding DataContext.PromptRows, RelativeSource={RelativeSource AncestorType=TabControl}}\"",
            promptsViewXaml);

        var promptsHeaderViewPath = GetRepoFilePath("src", "ImageSeriesStudio.App", "Views", "PromptsHeaderView.xaml");
        Assert.True(File.Exists(promptsHeaderViewPath));
        var promptsHeaderViewXaml = File.ReadAllText(promptsHeaderViewPath);
        Assert.Contains(
            "Text=\"{Binding DataContext.PromptVersionColumn, RelativeSource={RelativeSource AncestorType=TabControl}}\"",
            promptsHeaderViewXaml);
        Assert.Contains(
            "Text=\"{Binding DataContext.PromptCreatedColumn, RelativeSource={RelativeSource AncestorType=TabControl}}\"",
            promptsHeaderViewXaml);

        var promptRowsListViewPath = GetRepoFilePath("src", "ImageSeriesStudio.App", "Views", "PromptRowsListView.xaml");
        Assert.True(File.Exists(promptRowsListViewPath));

        var promptRowsListViewXaml = File.ReadAllText(promptRowsListViewPath);
        Assert.Contains(
            "ItemsSource=\"{Binding DataContext.PromptRows, RelativeSource={RelativeSource AncestorType=TabControl}}\"",
            promptRowsListViewXaml);
        Assert.Contains(
            "Binding DataContext.HasPromptRows, RelativeSource={RelativeSource AncestorType=TabControl}",
            promptRowsListViewXaml);
        Assert.Contains("Text=\"{Binding Version}\"", promptRowsListViewXaml);
        Assert.Contains("Text=\"{Binding PromptText}\"", promptRowsListViewXaml);
        Assert.Contains("Text=\"{Binding CreatedAt}\"", promptRowsListViewXaml);
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
        Assert.Contains("<views:WorkflowGraphHeaderView Grid.Row=\"0\" />", workflowGraphViewXaml);
        Assert.Contains("<views:WorkflowGraphRowsListView Grid.Row=\"2\" />", workflowGraphViewXaml);
        Assert.DoesNotContain(
            "Text=\"{Binding DataContext.GraphNodeColumn, RelativeSource={RelativeSource AncestorType=TabControl}}\"",
            workflowGraphViewXaml);
        Assert.DoesNotContain(
            "ItemsSource=\"{Binding DataContext.WorkflowGraphRows, RelativeSource={RelativeSource AncestorType=TabControl}}\"",
            workflowGraphViewXaml);

        var workflowGraphHeaderViewPath = GetRepoFilePath("src", "ImageSeriesStudio.App", "Views", "WorkflowGraphHeaderView.xaml");
        Assert.True(File.Exists(workflowGraphHeaderViewPath));
        var workflowGraphHeaderViewXaml = File.ReadAllText(workflowGraphHeaderViewPath);
        Assert.Contains(
            "Text=\"{Binding DataContext.GraphNodeColumn, RelativeSource={RelativeSource AncestorType=TabControl}}\"",
            workflowGraphHeaderViewXaml);
        Assert.Contains(
            "Text=\"{Binding DataContext.GraphLinksColumn, RelativeSource={RelativeSource AncestorType=TabControl}}\"",
            workflowGraphHeaderViewXaml);

        var workflowGraphRowsListViewPath = GetRepoFilePath("src", "ImageSeriesStudio.App", "Views", "WorkflowGraphRowsListView.xaml");
        Assert.True(File.Exists(workflowGraphRowsListViewPath));

        var workflowGraphRowsListViewXaml = File.ReadAllText(workflowGraphRowsListViewPath);
        Assert.Contains(
            "ItemsSource=\"{Binding DataContext.WorkflowGraphRows, RelativeSource={RelativeSource AncestorType=TabControl}}\"",
            workflowGraphRowsListViewXaml);
        Assert.Contains(
            "Binding DataContext.HasWorkflowGraphRows, RelativeSource={RelativeSource AncestorType=TabControl}",
            workflowGraphRowsListViewXaml);
        Assert.Contains("Text=\"{Binding NodeType}\"", workflowGraphRowsListViewXaml);
        Assert.Contains("Text=\"{Binding LinksTo}\"", workflowGraphRowsListViewXaml);
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
        Assert.Contains("<views:DeliveryHeaderView Grid.Row=\"0\" />", deliveryViewXaml);
        Assert.Contains("<views:DeliveryResultsListView Grid.Row=\"2\" />", deliveryViewXaml);
        Assert.DoesNotContain(
            "Text=\"{Binding DataContext.DeliveryPackageColumn, RelativeSource={RelativeSource AncestorType=TabControl}}\"",
            deliveryViewXaml);
        Assert.DoesNotContain(
            "ItemsSource=\"{Binding DataContext.DeliveryRows, RelativeSource={RelativeSource AncestorType=TabControl}}\"",
            deliveryViewXaml);

        var deliveryHeaderViewPath = GetRepoFilePath("src", "ImageSeriesStudio.App", "Views", "DeliveryHeaderView.xaml");
        Assert.True(File.Exists(deliveryHeaderViewPath));
        var deliveryHeaderViewXaml = File.ReadAllText(deliveryHeaderViewPath);
        Assert.Contains(
            "Text=\"{Binding DataContext.DeliveryPackageColumn, RelativeSource={RelativeSource AncestorType=TabControl}}\"",
            deliveryHeaderViewXaml);
        Assert.Contains(
            "Text=\"{Binding DataContext.DeliveryFinalImagesColumn, RelativeSource={RelativeSource AncestorType=TabControl}}\"",
            deliveryHeaderViewXaml);

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
        Assert.Contains("<views:ReviewHeaderView Grid.Row=\"0\" />", reviewViewXaml);
        Assert.Contains("<views:ReviewResultsListView Grid.Row=\"2\" />", reviewViewXaml);
        Assert.DoesNotContain(
            "Text=\"{Binding DataContext.ReviewItemColumn, RelativeSource={RelativeSource AncestorType=TabControl}}\"",
            reviewViewXaml);
        Assert.DoesNotContain(
            "ItemsSource=\"{Binding DataContext.ReviewRows, RelativeSource={RelativeSource AncestorType=TabControl}}\"",
            reviewViewXaml);

        var reviewHeaderViewPath = GetRepoFilePath("src", "ImageSeriesStudio.App", "Views", "ReviewHeaderView.xaml");
        Assert.True(File.Exists(reviewHeaderViewPath));
        var reviewHeaderViewXaml = File.ReadAllText(reviewHeaderViewPath);
        Assert.Contains(
            "Text=\"{Binding DataContext.ReviewItemColumn, RelativeSource={RelativeSource AncestorType=TabControl}}\"",
            reviewHeaderViewXaml);
        Assert.Contains(
            "Text=\"{Binding DataContext.HumanApprovalColumn, RelativeSource={RelativeSource AncestorType=TabControl}}\"",
            reviewHeaderViewXaml);

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
        Assert.Contains("<views:QueueHeaderView Grid.Row=\"0\" />", queueViewXaml);
        Assert.Contains("<views:QueueRowsListView Grid.Row=\"2\" />", queueViewXaml);
        Assert.DoesNotContain(
            "Text=\"{Binding DataContext.QueueItemColumn, RelativeSource={RelativeSource AncestorType=TabControl}}\"",
            queueViewXaml);
        Assert.DoesNotContain(
            "ItemsSource=\"{Binding DataContext.QueueRows, RelativeSource={RelativeSource AncestorType=TabControl}}\"",
            queueViewXaml);

        var queueHeaderViewPath = GetRepoFilePath("src", "ImageSeriesStudio.App", "Views", "QueueHeaderView.xaml");
        Assert.True(File.Exists(queueHeaderViewPath));
        var queueHeaderViewXaml = File.ReadAllText(queueHeaderViewPath);
        Assert.Contains(
            "Text=\"{Binding DataContext.QueueItemColumn, RelativeSource={RelativeSource AncestorType=TabControl}}\"",
            queueHeaderViewXaml);
        Assert.Contains(
            "Text=\"{Binding DataContext.QueueErrorColumn, RelativeSource={RelativeSource AncestorType=TabControl}}\"",
            queueHeaderViewXaml);

        var queueRowsListViewPath = GetRepoFilePath("src", "ImageSeriesStudio.App", "Views", "QueueRowsListView.xaml");
        Assert.True(File.Exists(queueRowsListViewPath));

        var queueRowsListViewXaml = File.ReadAllText(queueRowsListViewPath);
        Assert.Contains(
            "ItemsSource=\"{Binding DataContext.QueueRows, RelativeSource={RelativeSource AncestorType=TabControl}}\"",
            queueRowsListViewXaml);
        Assert.Contains(
            "Binding DataContext.HasQueueRows, RelativeSource={RelativeSource AncestorType=TabControl}",
            queueRowsListViewXaml);
        Assert.Contains("Text=\"{Binding ItemTitle}\"", queueRowsListViewXaml);
        Assert.Contains("Text=\"{Binding ErrorMessage}\"", queueRowsListViewXaml);
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
        Assert.Contains("<views:GalleryHeaderView Grid.Row=\"0\" />", galleryViewXaml);
        Assert.Contains("<views:GalleryRowsListView Grid.Row=\"2\" />", galleryViewXaml);
        Assert.DoesNotContain(
            "Text=\"{Binding DataContext.GalleryItemColumn, RelativeSource={RelativeSource AncestorType=TabControl}}\"",
            galleryViewXaml);
        Assert.DoesNotContain(
            "ItemsSource=\"{Binding DataContext.GalleryRows, RelativeSource={RelativeSource AncestorType=TabControl}}\"",
            galleryViewXaml);
        Assert.DoesNotContain(
            "SelectedItem=\"{Binding DataContext.SelectedGalleryRow, RelativeSource={RelativeSource AncestorType=TabControl}, Mode=TwoWay}\"",
            galleryViewXaml);

        var galleryHeaderViewPath = GetRepoFilePath("src", "ImageSeriesStudio.App", "Views", "GalleryHeaderView.xaml");
        Assert.True(File.Exists(galleryHeaderViewPath));
        var galleryHeaderViewXaml = File.ReadAllText(galleryHeaderViewPath);
        Assert.Contains(
            "Text=\"{Binding DataContext.GalleryItemColumn, RelativeSource={RelativeSource AncestorType=TabControl}}\"",
            galleryHeaderViewXaml);
        Assert.Contains(
            "Text=\"{Binding DataContext.GalleryMetadataColumn, RelativeSource={RelativeSource AncestorType=TabControl}}\"",
            galleryHeaderViewXaml);

        var galleryRowsListViewPath = GetRepoFilePath("src", "ImageSeriesStudio.App", "Views", "GalleryRowsListView.xaml");
        Assert.True(File.Exists(galleryRowsListViewPath));

        var galleryRowsListViewXaml = File.ReadAllText(galleryRowsListViewPath);
        Assert.Contains(
            "ItemsSource=\"{Binding DataContext.GalleryRows, RelativeSource={RelativeSource AncestorType=TabControl}}\"",
            galleryRowsListViewXaml);
        Assert.Contains(
            "SelectedItem=\"{Binding DataContext.SelectedGalleryRow, RelativeSource={RelativeSource AncestorType=TabControl}, Mode=TwoWay}\"",
            galleryRowsListViewXaml);
        Assert.Contains(
            "Binding DataContext.HasGalleryRows, RelativeSource={RelativeSource AncestorType=TabControl}",
            galleryRowsListViewXaml);
        Assert.Contains("Text=\"{Binding ItemTitle}\"", galleryRowsListViewXaml);
        Assert.Contains("Source=\"{Binding AssetPath}\"", galleryRowsListViewXaml);
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
