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

        var hits = FindTextHits(repositoryRoot, "ImageSeriesStudio")
            .Where(relativePath => !relativePath.Equals(
                @"tests\ContentDeliveryStudio.Tests\RenameCompatibilityGuardTests.cs",
                StringComparison.OrdinalIgnoreCase))
            .Where(relativePath => !relativePath.StartsWith(@"docs\superpowers\", StringComparison.OrdinalIgnoreCase))
            .ToArray();

        Assert.NotEmpty(hits);
        Assert.All(hits, hit => Assert.Contains(hit, allowedRelativePaths));
    }

    [Fact]
    public void RepositoryScan_PrunesIgnoredDirectoriesBeforeReadingFileContents()
    {
        var repositoryRoot = Path.Combine(
            Path.GetTempPath(),
            "ContentDeliveryStudio.Tests",
            nameof(RenameCompatibilityGuardTests),
            Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(repositoryRoot);

        try
        {
            File.WriteAllText(Path.Combine(repositoryRoot, "README.md"), "ImageSeriesStudio");
            WriteFile(repositoryRoot, @".worktrees\branch\ignored.md", "ImageSeriesStudio");
            WriteFile(repositoryRoot, @".cache\ignored.md", "ImageSeriesStudio");
            WriteFile(repositoryRoot, @"src\Feature\bin\ignored.cs", "ImageSeriesStudio");
            WriteFile(repositoryRoot, @"src\Feature\obj\ignored.cs", "ImageSeriesStudio");
            WriteFile(repositoryRoot, @"publish\ignored.md", "ImageSeriesStudio");
            WriteFile(repositoryRoot, @"src\Feature\kept.cs", "ImageSeriesStudio");

            var hits = FindTextHits(repositoryRoot, "ImageSeriesStudio");

            Assert.Equal(
                [@"README.md", @"src\Feature\kept.cs"],
                hits.OrderBy(path => path, StringComparer.OrdinalIgnoreCase).ToArray());
        }
        finally
        {
            if (Directory.Exists(repositoryRoot))
            {
                Directory.Delete(repositoryRoot, recursive: true);
            }
        }
    }

    private static IReadOnlyList<string> FindTextHits(string repositoryRoot, string text)
    {
        return EnumerateRepositoryTextFiles(repositoryRoot)
            .Select(path => new
            {
                FullPath = path,
                RelativePath = Path.GetRelativePath(repositoryRoot, path),
            })
            .Where(entry => File.ReadAllText(entry.FullPath).Contains(text, StringComparison.Ordinal))
            .Select(entry => entry.RelativePath)
            .ToArray();
    }

    private static IEnumerable<string> EnumerateRepositoryTextFiles(string repositoryRoot)
    {
        var pending = new Stack<string>();
        pending.Push(repositoryRoot);

        while (pending.Count > 0)
        {
            var currentDirectory = pending.Pop();

            foreach (var childDirectory in Directory.EnumerateDirectories(currentDirectory))
            {
                if (ShouldPruneDirectory(repositoryRoot, childDirectory))
                {
                    continue;
                }

                pending.Push(childDirectory);
            }

            foreach (var file in Directory.EnumerateFiles(currentDirectory))
            {
                if (IsSearchableTextFile(file))
                {
                    yield return file;
                }
            }
        }
    }

    private static bool ShouldPruneDirectory(string repositoryRoot, string directoryPath)
    {
        var directoryName = Path.GetFileName(directoryPath);
        if (directoryName.Equals(".git", StringComparison.OrdinalIgnoreCase)
            || directoryName.Equals(".worktrees", StringComparison.OrdinalIgnoreCase)
            || directoryName.Equals("bin", StringComparison.OrdinalIgnoreCase)
            || directoryName.Equals("obj", StringComparison.OrdinalIgnoreCase)
            || directoryName.Equals("publish", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (!directoryName.StartsWith(".", StringComparison.Ordinal))
        {
            return false;
        }

        var relativePath = Path.GetRelativePath(repositoryRoot, directoryPath);
        return relativePath.IndexOf(Path.DirectorySeparatorChar) < 0
            && relativePath.IndexOf(Path.AltDirectorySeparatorChar) < 0;
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

    private static void WriteFile(string repositoryRoot, string relativePath, string content)
    {
        var fullPath = Path.Combine(repositoryRoot, relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        File.WriteAllText(fullPath, content);
    }
}
