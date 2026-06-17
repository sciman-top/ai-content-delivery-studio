using ContentDeliveryStudio.Application.Projects;

namespace ContentDeliveryStudio.Tests;

public sealed class LocalStudioDataPathsTests
{
    [Fact]
    public void ResolveStudioRootPath_PrefersNewRootWhenPresent()
    {
        var baseRoot = CreateTempRoot();

        try
        {
            var newRoot = Path.Combine(baseRoot, "ContentDeliveryStudio");
            var legacyRoot = Path.Combine(baseRoot, "ImageSeriesStudio");
            Directory.CreateDirectory(newRoot);
            Directory.CreateDirectory(legacyRoot);

            var resolved = LocalStudioDataPaths.ResolveStudioRootPath(baseRoot);

            Assert.Equal(Path.GetFullPath(newRoot), resolved);
        }
        finally
        {
            Cleanup(baseRoot);
        }
    }

    [Fact]
    public void ResolveStudioRootPath_FallsBackToLegacyRootWhenOnlyLegacyExists()
    {
        var baseRoot = CreateTempRoot();

        try
        {
            var legacyRoot = Path.Combine(baseRoot, "ImageSeriesStudio");
            Directory.CreateDirectory(legacyRoot);

            var resolved = LocalStudioDataPaths.ResolveStudioRootPath(baseRoot);

            Assert.Equal(Path.GetFullPath(legacyRoot), resolved);
        }
        finally
        {
            Cleanup(baseRoot);
        }
    }

    [Fact]
    public void ResolveStudioRootPath_UsesNewRootWhenNeitherExists()
    {
        var baseRoot = CreateTempRoot();

        try
        {
            var resolved = LocalStudioDataPaths.ResolveStudioRootPath(baseRoot);

            Assert.Equal(
                Path.GetFullPath(Path.Combine(baseRoot, "ContentDeliveryStudio")),
                resolved);
        }
        finally
        {
            Cleanup(baseRoot);
        }
    }

    private static string CreateTempRoot()
    {
        var root = Path.Combine(
            Path.GetTempPath(),
            "ContentDeliveryStudio.Tests",
            Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        return root;
    }

    private static void Cleanup(string root)
    {
        if (Directory.Exists(root))
        {
            Directory.Delete(root, recursive: true);
        }
    }
}
