namespace ContentDeliveryStudio.Tests;

public sealed class RenameCompatibilityGuardTests
{
    [Fact]
    public void RenameCompatibilityGuard_ImageSeriesStudioOnlyAppearsInAllowedRepositoryFiles()
    {
        var repositoryRoot = FindRepositoryRoot();
        var allowedRelativePaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            @"README.md",
            @"docs\ARCHITECTURE.md",
            @"docs\TASKS.md",
            @"docs\ROADMAP.md",
            @"docs\RENAME_COMPATIBILITY_NOTES.md",
            @"docs\adr\0008-product-identity-and-repository-rename.md",
            @"src\ContentDeliveryStudio.Application\Projects\LocalStudioDataPaths.cs",
            @"tests\ContentDeliveryStudio.Tests\LocalStudioDataPathsTests.cs",
        };

        var hits = Directory
            .EnumerateFiles(repositoryRoot, "*", SearchOption.AllDirectories)
            .Where(path => IsSearchableTextFile(path))
            .Select(path => new
            {
                FullPath = path,
                RelativePath = Path.GetRelativePath(repositoryRoot, path),
            })
            .Where(entry => !entry.RelativePath.Equals(
                @"tests\ContentDeliveryStudio.Tests\RenameCompatibilityGuardTests.cs",
                StringComparison.OrdinalIgnoreCase))
            .Where(entry => !entry.RelativePath.StartsWith(@"docs\superpowers\", StringComparison.OrdinalIgnoreCase))
            .Where(entry => File.ReadAllText(entry.FullPath).Contains("ImageSeriesStudio", StringComparison.Ordinal))
            .ToArray();

        Assert.NotEmpty(hits);
        Assert.All(hits, hit => Assert.Contains(hit.RelativePath, allowedRelativePaths));
    }

    private static bool IsSearchableTextFile(string path)
    {
        var fileName = Path.GetFileName(path);
        if (fileName.StartsWith('.'))
        {
            return false;
        }

        var extension = Path.GetExtension(path);
        return extension is ".cs" or ".csproj" or ".md" or ".json" or ".ps1" or ".props" or ".targets" or ".sln" or ".xaml";
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "ContentDeliveryStudio.sln")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not find ContentDeliveryStudio.sln from the test output path.");
    }
}
