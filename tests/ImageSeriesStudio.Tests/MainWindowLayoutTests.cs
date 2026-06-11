using System.IO;

namespace ImageSeriesStudio.Tests;

public sealed class MainWindowLayoutTests
{
    [Fact]
    public void MainWindowXaml_UsesBriefWorkflowUserControl()
    {
        var mainWindowXaml = ReadRepoFile("src/ImageSeriesStudio.App", "MainWindow.xaml");

        Assert.Contains("<views:BriefWorkflowView Grid.Row=\"1\" Margin=\"0,16,0,0\" />", mainWindowXaml);
        Assert.DoesNotContain(
            "ItemsSource=\"{Binding DataContext.DesignBlueprintRows, RelativeSource={RelativeSource AncestorType=TabControl}}\"",
            mainWindowXaml);
        Assert.DoesNotContain(
            "ItemsSource=\"{Binding DataContext.PromptDirectionRows, RelativeSource={RelativeSource AncestorType=TabControl}}\"",
            mainWindowXaml);

        var briefWorkflowViewPath = GetRepoFilePath("src", "ImageSeriesStudio.App", "Views", "BriefWorkflowView.xaml");
        Assert.True(File.Exists(briefWorkflowViewPath));

        var briefWorkflowViewXaml = File.ReadAllText(briefWorkflowViewPath);
        Assert.Contains(
            "ItemsSource=\"{Binding DataContext.DesignBlueprintRows, RelativeSource={RelativeSource AncestorType=TabControl}}\"",
            briefWorkflowViewXaml);
        Assert.Contains(
            "ItemsSource=\"{Binding DataContext.PromptDirectionRows, RelativeSource={RelativeSource AncestorType=TabControl}}\"",
            briefWorkflowViewXaml);
    }

    [Fact]
    public void MainWindowXaml_UsesPlanUserControl()
    {
        var mainWindowXaml = ReadRepoFile("src/ImageSeriesStudio.App", "MainWindow.xaml");

        Assert.Contains("<views:PlanView Grid.Row=\"1\" Margin=\"0,16,0,0\" />", mainWindowXaml);
        Assert.DoesNotContain(
            "ItemsSource=\"{Binding DataContext.PlanRows, RelativeSource={RelativeSource AncestorType=TabControl}}\"",
            mainWindowXaml);

        var planViewPath = GetRepoFilePath("src", "ImageSeriesStudio.App", "Views", "PlanView.xaml");
        Assert.True(File.Exists(planViewPath));

        var planViewXaml = File.ReadAllText(planViewPath);
        Assert.Contains(
            "ItemsSource=\"{Binding DataContext.PlanRows, RelativeSource={RelativeSource AncestorType=TabControl}}\"",
            planViewXaml);
    }

    [Fact]
    public void MainWindowXaml_UsesPromptsUserControl()
    {
        var mainWindowXaml = ReadRepoFile("src/ImageSeriesStudio.App", "MainWindow.xaml");

        Assert.Contains("<views:PromptsView Grid.Row=\"1\" Margin=\"0,16,0,0\" />", mainWindowXaml);
        Assert.DoesNotContain(
            "ItemsSource=\"{Binding DataContext.PromptRows, RelativeSource={RelativeSource AncestorType=TabControl}}\"",
            mainWindowXaml);

        var promptsViewPath = GetRepoFilePath("src", "ImageSeriesStudio.App", "Views", "PromptsView.xaml");
        Assert.True(File.Exists(promptsViewPath));

        var promptsViewXaml = File.ReadAllText(promptsViewPath);
        Assert.Contains(
            "ItemsSource=\"{Binding DataContext.PromptRows, RelativeSource={RelativeSource AncestorType=TabControl}}\"",
            promptsViewXaml);
    }

    [Fact]
    public void MainWindowXaml_UsesPlanEditorPanelUserControl()
    {
        var mainWindowXaml = ReadRepoFile("src/ImageSeriesStudio.App", "MainWindow.xaml");

        Assert.Contains("<views:PlanEditorPanelView Margin=\"0,0,0,14\" />", mainWindowXaml);
        Assert.DoesNotContain(
            "ItemsSource=\"{Binding SeriesItems}\"",
            mainWindowXaml);

        var planEditorPanelViewPath = GetRepoFilePath("src", "ImageSeriesStudio.App", "Views", "PlanEditorPanelView.xaml");
        Assert.True(File.Exists(planEditorPanelViewPath));

        var planEditorPanelViewXaml = File.ReadAllText(planEditorPanelViewPath);
        Assert.Contains("ItemsSource=\"{Binding SeriesItems}\"", planEditorPanelViewXaml);
        Assert.Contains("Command=\"{Binding CreateSeriesCommand}\"", planEditorPanelViewXaml);
    }

    [Fact]
    public void MainWindowXaml_UsesPromptEditorPanelUserControl()
    {
        var mainWindowXaml = ReadRepoFile("src/ImageSeriesStudio.App", "MainWindow.xaml");

        Assert.Contains("<views:PromptEditorPanelView Margin=\"0,0,0,12\" />", mainWindowXaml);
        Assert.DoesNotContain(
            "ItemsSource=\"{Binding PromptVersions}\"",
            mainWindowXaml);

        var promptEditorPanelViewPath = GetRepoFilePath("src", "ImageSeriesStudio.App", "Views", "PromptEditorPanelView.xaml");
        Assert.True(File.Exists(promptEditorPanelViewPath));

        var promptEditorPanelViewXaml = File.ReadAllText(promptEditorPanelViewPath);
        Assert.Contains("ItemsSource=\"{Binding PromptVersions}\"", promptEditorPanelViewXaml);
        Assert.Contains("Command=\"{Binding CreatePromptVersionCommand}\"", promptEditorPanelViewXaml);
    }

    [Fact]
    public void MainWindowXaml_UsesImageEditPanelUserControl()
    {
        var mainWindowXaml = ReadRepoFile("src/ImageSeriesStudio.App", "MainWindow.xaml");

        Assert.Contains("<views:ImageEditPanelView Margin=\"0,0,0,12\" />", mainWindowXaml);
        Assert.DoesNotContain(
            "Text=\"{Binding SelectedCandidateSummary}\"",
            mainWindowXaml);

        var imageEditPanelViewPath = GetRepoFilePath("src", "ImageSeriesStudio.App", "Views", "ImageEditPanelView.xaml");
        Assert.True(File.Exists(imageEditPanelViewPath));

        var imageEditPanelViewXaml = File.ReadAllText(imageEditPanelViewPath);
        Assert.Contains("Text=\"{Binding SelectedCandidateSummary}\"", imageEditPanelViewXaml);
        Assert.Contains("Command=\"{Binding RunFakeImageEditCommand}\"", imageEditPanelViewXaml);
    }

    [Fact]
    public void MainWindowXaml_UsesReviewApprovalPanelUserControl()
    {
        var mainWindowXaml = ReadRepoFile("src/ImageSeriesStudio.App", "MainWindow.xaml");

        Assert.Contains("<views:ReviewApprovalPanelView />", mainWindowXaml);
        Assert.DoesNotContain(
            "Command=\"{Binding ApproveSelectedReviewCommand}\"",
            mainWindowXaml);

        var reviewApprovalPanelViewPath = GetRepoFilePath("src", "ImageSeriesStudio.App", "Views", "ReviewApprovalPanelView.xaml");
        Assert.True(File.Exists(reviewApprovalPanelViewPath));

        var reviewApprovalPanelViewXaml = File.ReadAllText(reviewApprovalPanelViewPath);
        Assert.Contains("Command=\"{Binding ApproveSelectedReviewCommand}\"", reviewApprovalPanelViewXaml);
        Assert.Contains("Command=\"{Binding ExportDeliveryCommand}\"", reviewApprovalPanelViewXaml);
    }

    [Fact]
    public void MainWindowXaml_UsesWorkflowGraphUserControl()
    {
        var mainWindowXaml = ReadRepoFile("src/ImageSeriesStudio.App", "MainWindow.xaml");

        Assert.Contains("xmlns:views=\"clr-namespace:ImageSeriesStudio.App.Views\"", mainWindowXaml);
        Assert.Contains("<views:WorkflowGraphView Grid.Row=\"1\" Margin=\"0,16,0,0\" />", mainWindowXaml);
        Assert.DoesNotContain(
            "ItemsSource=\"{Binding DataContext.WorkflowGraphRows, RelativeSource={RelativeSource AncestorType=TabControl}}\"",
            mainWindowXaml);

        var workflowGraphViewPath = GetRepoFilePath("src", "ImageSeriesStudio.App", "Views", "WorkflowGraphView.xaml");
        Assert.True(File.Exists(workflowGraphViewPath));

        var workflowGraphViewXaml = File.ReadAllText(workflowGraphViewPath);
        Assert.Contains(
            "ItemsSource=\"{Binding DataContext.WorkflowGraphRows, RelativeSource={RelativeSource AncestorType=TabControl}}\"",
            workflowGraphViewXaml);
    }

    [Fact]
    public void MainWindowXaml_UsesDeliveryUserControl()
    {
        var mainWindowXaml = ReadRepoFile("src/ImageSeriesStudio.App", "MainWindow.xaml");

        Assert.Contains("<views:DeliveryView Grid.Row=\"1\" Margin=\"0,16,0,0\" />", mainWindowXaml);
        Assert.DoesNotContain(
            "ItemsSource=\"{Binding DataContext.DeliveryRows, RelativeSource={RelativeSource AncestorType=TabControl}}\"",
            mainWindowXaml);

        var deliveryViewPath = GetRepoFilePath("src", "ImageSeriesStudio.App", "Views", "DeliveryView.xaml");
        Assert.True(File.Exists(deliveryViewPath));

        var deliveryViewXaml = File.ReadAllText(deliveryViewPath);
        Assert.Contains(
            "ItemsSource=\"{Binding DataContext.DeliveryRows, RelativeSource={RelativeSource AncestorType=TabControl}}\"",
            deliveryViewXaml);
    }

    [Fact]
    public void MainWindowXaml_UsesReviewUserControl()
    {
        var mainWindowXaml = ReadRepoFile("src/ImageSeriesStudio.App", "MainWindow.xaml");

        Assert.Contains("<views:ReviewView Grid.Row=\"1\" Margin=\"0,16,0,0\" />", mainWindowXaml);
        Assert.DoesNotContain(
            "ItemsSource=\"{Binding DataContext.ReviewRows, RelativeSource={RelativeSource AncestorType=TabControl}}\"",
            mainWindowXaml);

        var reviewViewPath = GetRepoFilePath("src", "ImageSeriesStudio.App", "Views", "ReviewView.xaml");
        Assert.True(File.Exists(reviewViewPath));

        var reviewViewXaml = File.ReadAllText(reviewViewPath);
        Assert.Contains(
            "ItemsSource=\"{Binding DataContext.ReviewRows, RelativeSource={RelativeSource AncestorType=TabControl}}\"",
            reviewViewXaml);
    }

    [Fact]
    public void MainWindowXaml_UsesQueueUserControl()
    {
        var mainWindowXaml = ReadRepoFile("src/ImageSeriesStudio.App", "MainWindow.xaml");

        Assert.Contains("<views:QueueView Grid.Row=\"1\" Margin=\"0,16,0,0\" />", mainWindowXaml);
        Assert.DoesNotContain(
            "ItemsSource=\"{Binding DataContext.QueueRows, RelativeSource={RelativeSource AncestorType=TabControl}}\"",
            mainWindowXaml);

        var queueViewPath = GetRepoFilePath("src", "ImageSeriesStudio.App", "Views", "QueueView.xaml");
        Assert.True(File.Exists(queueViewPath));

        var queueViewXaml = File.ReadAllText(queueViewPath);
        Assert.Contains(
            "ItemsSource=\"{Binding DataContext.QueueRows, RelativeSource={RelativeSource AncestorType=TabControl}}\"",
            queueViewXaml);
    }

    [Fact]
    public void MainWindowXaml_UsesGalleryUserControl()
    {
        var mainWindowXaml = ReadRepoFile("src/ImageSeriesStudio.App", "MainWindow.xaml");

        Assert.Contains("<views:GalleryView Grid.Row=\"1\" Margin=\"0,16,0,0\" />", mainWindowXaml);
        Assert.DoesNotContain(
            "ItemsSource=\"{Binding DataContext.GalleryRows, RelativeSource={RelativeSource AncestorType=TabControl}}\"",
            mainWindowXaml);

        var galleryViewPath = GetRepoFilePath("src", "ImageSeriesStudio.App", "Views", "GalleryView.xaml");
        Assert.True(File.Exists(galleryViewPath));

        var galleryViewXaml = File.ReadAllText(galleryViewPath);
        Assert.Contains(
            "ItemsSource=\"{Binding DataContext.GalleryRows, RelativeSource={RelativeSource AncestorType=TabControl}}\"",
            galleryViewXaml);
        Assert.Contains(
            "SelectedItem=\"{Binding DataContext.SelectedGalleryRow, RelativeSource={RelativeSource AncestorType=TabControl}, Mode=TwoWay}\"",
            galleryViewXaml);
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
