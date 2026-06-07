using System.IO;

namespace ImageSeriesStudio.Tests;

public sealed class MainWindowLayoutTests
{
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
