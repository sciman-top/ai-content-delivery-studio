using System.Diagnostics;
using System.Text.Json;

namespace ContentDeliveryStudio.Tests;

public sealed class LocalOutputOrganizationScriptTests
{
    [Theory]
    [InlineData("Validation", "outputs/validation/article-figure-sets/sample-article/20260821-v2")]
    [InlineData("ReviewReady", "outputs/review-ready/article-figure-sets/sample-article/20260821-v2")]
    public async Task ArticleRunner_NestsClassifiedOutputByArticleAndRun(
        string outputClass,
        string expectedRelativePath)
    {
        var root = Path.Combine(Path.GetTempPath(), $"article-output-routing-{Guid.NewGuid():N}");
        try
        {
            Directory.CreateDirectory(root);
            Assert.Equal(0, (await RunAsync(root, "git", "init", "-q")).ExitCode);
            var sourcePath = Path.Combine(root, "article.pdf");
            await File.WriteAllTextAsync(sourcePath, "%PDF-1.4");
            var script = Path.Combine(
                FindRepositoryRoot(),
                "scripts",
                "run-article-scientific-figure-set.ps1");

            var result = await RunAsync(
                root,
                "pwsh",
                "-NoProfile",
                "-ExecutionPolicy",
                "Bypass",
                "-File",
                script,
                "-SourcePath",
                sourcePath,
                "-OutputClass",
                outputClass,
                "-ArticleSlug",
                "sample-article",
                "-RunName",
                "20260821-v2",
                "-ResolveOutputDirectoryOnly");

            Assert.Equal(0, result.ExitCode);
            Assert.Equal(
                Path.GetFullPath(Path.Combine(root, expectedRelativePath)),
                result.StandardOutput.Trim());
            Assert.False(Directory.Exists(result.StandardOutput.Trim()));
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }

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
                        new
                        {
                            SourceRelativePath = "01.svg",
                            PackageRelativePath = "figures/01.svg",
                            Role = "figure",
                            Sha256 = $"sha256:{new string('a', 64)}",
                        },
                        new
                        {
                            SourceRelativePath = "article-figure-set-plan.json",
                            PackageRelativePath = "metadata/article-figure-set-plan.json",
                            Role = "metadata",
                            Sha256 = $"sha256:{new string('b', 64)}",
                        },
                    },
                }));
            await File.WriteAllTextAsync(
                Path.Combine(package, "approvals.json"),
                JsonSerializer.Serialize(new
                {
                    gateOne = new { approved = true },
                    gateTwo = new { approved = true },
                }));
            var reviewReady = Path.Combine(
                root,
                "outputs",
                "review-ready",
                "article-figure-sets",
                "sample-article",
                "20260820-v1");
            Directory.CreateDirectory(reviewReady);
            await File.WriteAllTextAsync(
                Path.Combine(reviewReady, "authorized-agent-visual-receipt.json"),
                JsonSerializer.Serialize(new
                {
                    schemaVersion = 1,
                    reviewer = "authorized-agent",
                    authorityFiles = new[]
                    {
                        new
                        {
                            relativePath = "article-figure-set-plan.json",
                            sha256 = $"sha256:{new string('b', 64)}",
                        },
                    },
                    candidates = new[]
                    {
                        new
                        {
                            files = new[]
                            {
                                new
                                {
                                    relativePath = "01.svg",
                                    sha256 = $"sha256:{new string('a', 64)}",
                                },
                            },
                        },
                    },
                }));
            await File.WriteAllTextAsync(
                Path.Combine(reviewReady, "human-review-assessment.json"),
                JsonSerializer.Serialize(new
                {
                    schemaVersion = 1,
                    route = "AuthorizedAgentAccept",
                    eligibleForPromotion = true,
                    requiresHumanOnsiteReview = false,
                    requiresPerCandidateUserReview = false,
                    requiresIndependentHumanExpert = false,
                    eligibleForFutureStandingAutomation = false,
                    candidateCount = 2,
                    maximumRiskLevel = "High",
                    visualReviewProvider = "fake-scientific-visual",
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
            Assert.Equal(3, catalog.RootElement.GetProperty("schemaVersion").GetInt32());
            var finalPackage = Assert.Single(catalog.RootElement
                .GetProperty("finalDeliveryPackages")
                .EnumerateArray());
            Assert.Equal("sample-article", finalPackage.GetProperty("articleSlug").GetString());
            Assert.Equal(1, finalPackage.GetProperty("figureAssetCount").GetInt32());
            Assert.Equal("authorized_agent", finalPackage.GetProperty("actor").GetString());
            Assert.Equal(
                "AuthorizedAgentAccept",
                finalPackage.GetProperty("humanReviewRoute").GetString());
            var assessment = Assert.Single(catalog.RootElement
                .GetProperty("reviewReadyAssessments")
                .EnumerateArray());
            Assert.Equal("AuthorizedAgentAccept", assessment.GetProperty("route").GetString());
            Assert.False(assessment.GetProperty("requiresHumanOnsiteReview").GetBoolean());
            Assert.False(assessment.GetProperty("requiresPerCandidateUserReview").GetBoolean());
            var readme = await File.ReadAllTextAsync(Path.Combine(root, "deliveries", "README.txt"));
            Assert.Contains(
                "article-figure-sets/sample-article/20260820-v1",
                readme,
                StringComparison.Ordinal);
            Assert.Contains("1 figure assets", readme, StringComparison.Ordinal);
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
