namespace ContentDeliveryStudio.Tests;

public sealed class AppPresentationBoundaryTests
{
    [Fact]
    public void ViewModelsAndViews_DoNotReferenceInfrastructure()
    {
        var repoRoot = FindRepoRoot();
        var appProject = Path.Combine(repoRoot, "src", "ContentDeliveryStudio.App");
        var violatingFiles = new List<string>();

        foreach (var directory in new[] { "ViewModels", "Views" })
        {
            var path = Path.Combine(appProject, directory);
            Assert.True(Directory.Exists(path), $"Expected App presentation directory was not found: {path}");

            foreach (var file in Directory.EnumerateFiles(path, "*.cs", SearchOption.AllDirectories))
            {
                if (File.ReadAllText(file).Contains("ContentDeliveryStudio.Infrastructure", StringComparison.Ordinal))
                {
                    violatingFiles.Add(Path.GetRelativePath(repoRoot, file));
                }
            }
        }

        Assert.True(
            violatingFiles.Count == 0,
            "App ViewModels and Views must not import ContentDeliveryStudio.Infrastructure directly. "
            + "Route construction through App.xaml.cs or an App/Services seam instead. Violations: "
            + string.Join(", ", violatingFiles));
    }

    private static string FindRepoRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "ContentDeliveryStudio.sln")))
        {
            directory = directory.Parent!;
        }

        Assert.NotNull(directory);
        return directory.FullName;
    }
}
