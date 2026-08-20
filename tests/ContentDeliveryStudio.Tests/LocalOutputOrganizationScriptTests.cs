using System.Diagnostics;
using System.Text.Json;

namespace ContentDeliveryStudio.Tests;

public sealed class LocalOutputOrganizationScriptTests
{
    [Fact]
    public async Task Organizer_IndexesFinalPackagesAndKeepsDeliveryReadmeCurrent()
    {
        var root = Path.Combine(Path.GetTempPath(), $"local-output-organizer-{Guid.NewGuid():N}");
        try
        {
            Directory.CreateDirectory(root);
            Assert.Equal(0, (await RunAsync(root, "git", "init", "-q")).ExitCode);
            var package = Path.Combine(
                root,
                "deliveries",
                "article-figure-sets",
                "sample-article",
                "20260820-v1");
            Directory.CreateDirectory(package);
            await File.WriteAllTextAsync(
                Path.Combine(package, "manifest.json"),
                JsonSerializer.Serialize(new
                {
                    SchemaVersion = 1,
                    ArticleSlug = "sample-article",
                    PackageId = "20260820-v1",
                    ApprovedAt = DateTimeOffset.Parse("2026-08-20T18:00:00+08:00"),
                    Actor = "authorized_agent",
                    CandidateCount = 2,
                    LiveProviderAccepted = false,
                    IndependentHumanExpertAccepted = false,
                    Files = new[]
                    {
                        new { PackageRelativePath = "figures/01.svg", Role = "figure" },
                        new { PackageRelativePath = "figures/01.png", Role = "figure" },
                        new { PackageRelativePath = "figures/01.pdf", Role = "figure" },
                    },
                }));
            await File.WriteAllTextAsync(
                Path.Combine(package, "approvals.json"),
                JsonSerializer.Serialize(new
                {
                    gateOne = new { approved = true },
                    gateTwo = new { approved = true },
                }));
            var script = Path.Combine(
                FindRepositoryRoot(),
                "scripts",
                "organize-local-outputs.ps1");

            var result = await RunAsync(
                root,
                "pwsh",
                "-NoProfile",
                "-ExecutionPolicy",
                "Bypass",
                "-File",
                script);

            Assert.Equal(0, result.ExitCode);
            using var catalog = JsonDocument.Parse(await File.ReadAllTextAsync(
                Path.Combine(root, "outputs", "OUTPUT-CATALOG.json")));
            Assert.Equal(2, catalog.RootElement.GetProperty("schemaVersion").GetInt32());
            var finalPackage = Assert.Single(catalog.RootElement
                .GetProperty("finalDeliveryPackages")
                .EnumerateArray());
            Assert.Equal("sample-article", finalPackage.GetProperty("articleSlug").GetString());
            Assert.Equal(3, finalPackage.GetProperty("figureAssetCount").GetInt32());
            Assert.Equal("authorized_agent", finalPackage.GetProperty("actor").GetString());
            var readme = await File.ReadAllTextAsync(Path.Combine(root, "deliveries", "README.txt"));
            Assert.Contains(
                "article-figure-sets/sample-article/20260820-v1",
                readme,
                StringComparison.Ordinal);
            Assert.Contains("3 figure assets", readme, StringComparison.Ordinal);
            Assert.DoesNotContain("No final article figure-set packages", readme, StringComparison.Ordinal);
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }

    private static async Task<ProcessResult> RunAsync(
        string workingDirectory,
        string fileName,
        params string[] arguments)
    {
        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = fileName,
                WorkingDirectory = workingDirectory,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            },
        };
        foreach (var argument in arguments)
        {
            process.StartInfo.ArgumentList.Add(argument);
        }

        process.Start();
        var standardOutput = process.StandardOutput.ReadToEndAsync();
        var standardError = process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();
        return new ProcessResult(
            process.ExitCode,
            await standardOutput,
            await standardError);
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

        throw new DirectoryNotFoundException("Repository root was not found.");
    }

    private sealed record ProcessResult(int ExitCode, string StandardOutput, string StandardError);
}
