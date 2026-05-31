using ImageSeriesStudio.Infrastructure.Workspaces;

namespace ImageSeriesStudio.Tests;

public sealed class WorkspaceFolderServiceTests
{
    [Fact]
    public void WorkspaceFolderService_CreatesExpectedFolderConvention()
    {
        var rootDirectory = Path.Combine(Path.GetTempPath(), "ImageSeriesStudio.Tests", Guid.NewGuid().ToString("N"));

        try
        {
            var service = new WorkspaceFolderService();
            var folders = service.EnsureWorkspace(rootDirectory);

            Assert.True(Directory.Exists(folders.RootPath));
            Assert.True(Directory.Exists(folders.ProjectsPath));
            Assert.True(Directory.Exists(folders.AssetsPath));
            Assert.True(Directory.Exists(folders.DeliveriesPath));
            Assert.True(Directory.Exists(folders.LogsPath));
            Assert.DoesNotContain("ai-image-series-studio", folders.RootPath, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            if (Directory.Exists(rootDirectory))
            {
                Directory.Delete(rootDirectory, recursive: true);
            }
        }
    }
}
