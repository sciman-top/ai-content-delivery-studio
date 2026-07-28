using System.Diagnostics;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace ContentDeliveryStudio.Tests;

public sealed class ScientificFigureOperatorTrialScriptTests
{
    [Fact]
    public async Task Prepare_CreatesPendingOperatorSessionAndChecklist()
    {
        var fixture = CreateFixture();
        try
        {
            var result = await RunScriptAsync(
                fixture.RepositoryRoot,
                "-Mode", "Prepare",
                "-RunId", fixture.RunId,
                "-SessionPath", fixture.RelativeSessionPath);

            Assert.Equal(0, result.ExitCode);
            using var trial = ReadTrial(fixture.TrialPath);
            Assert.Equal("pending_operator", trial.RootElement.GetProperty("status").GetString());
            Assert.Equal("pending_operator", trial.RootElement.GetProperty("evidenceLevel").GetString());
            Assert.Equal("fake", trial.RootElement.GetProperty("providerMode").GetString());
            Assert.False(trial.RootElement.GetProperty("liveAccepted").GetBoolean());
            Assert.Equal(5, trial.RootElement.GetProperty("workspaces").GetArrayLength());
            Assert.True(File.Exists(fixture.ChecklistPath));
        }
        finally
        {
            fixture.Dispose();
        }
    }

    [Fact]
    public async Task Prepare_FromOutsideRepository_ResolvesRootFromScriptLocation()
    {
        var fixture = CreateFixture();
        try
        {
            var result = await RunScriptFromWorkingDirectoryAsync(
                fixture.RepositoryRoot,
                Path.GetTempPath(),
                "-Mode", "Prepare",
                "-RunId", fixture.RunId,
                "-SessionPath", fixture.RelativeSessionPath);

            Assert.Equal(0, result.ExitCode);
            using var trial = ReadTrial(fixture.TrialPath);
            Assert.Equal(
                Path.GetFullPath(fixture.SessionPath),
                Path.GetFullPath(trial.RootElement.GetProperty("sessionPath").GetString()!));
        }
        finally
        {
            fixture.Dispose();
        }
    }

    [Fact]
    public async Task FinalizeAccepted_ValidatesPackageAndRecordsManualEvidence()
    {
        var fixture = CreateFixture();
        try
        {
            Assert.Equal(0, (await PrepareAsync(fixture)).ExitCode);
            Directory.CreateDirectory(Path.GetDirectoryName(fixture.PackagePath)!);
            WritePackage(fixture.PackagePath, "operator-reviewer");

            var result = await FinalizeAsync(
                fixture,
                "accepted",
                "operator-reviewer",
                "Inspected all five workspaces and approved the fake-first package.",
                includePackage: true);

            Assert.Equal(0, result.ExitCode);
            using var trial = ReadTrial(fixture.TrialPath);
            Assert.Equal("accepted", trial.RootElement.GetProperty("status").GetString());
            Assert.Equal("operator/manual evidence", trial.RootElement.GetProperty("evidenceLevel").GetString());
            Assert.False(trial.RootElement.GetProperty("liveAccepted").GetBoolean());
            Assert.True(trial.RootElement.GetProperty("validation").GetProperty("passed").GetBoolean());
            Assert.Equal(
                "operator-reviewer",
                trial.RootElement.GetProperty("operator").GetProperty("reviewer").GetString());
        }
        finally
        {
            fixture.Dispose();
        }
    }

    [Fact]
    public async Task FinalizeRejected_RecordsManualRejectionWithoutPackage()
    {
        var fixture = CreateFixture();
        try
        {
            Assert.Equal(0, (await PrepareAsync(fixture)).ExitCode);

            var result = await FinalizeAsync(
                fixture,
                "rejected",
                "operator-reviewer",
                "The visual hierarchy requires a human-directed revision.",
                includePackage: false);

            Assert.Equal(0, result.ExitCode);
            using var trial = ReadTrial(fixture.TrialPath);
            Assert.Equal("rejected", trial.RootElement.GetProperty("status").GetString());
            Assert.Equal(JsonValueKind.Null, trial.RootElement.GetProperty("validation").ValueKind);
            Assert.Equal(
                "rejected",
                trial.RootElement.GetProperty("operator").GetProperty("outcome").GetString());
        }
        finally
        {
            fixture.Dispose();
        }
    }

    [Fact]
    public async Task FinalizeAccepted_FailsClosedOnReviewerMismatchAndBadHash()
    {
        var fixture = CreateFixture();
        try
        {
            Assert.Equal(0, (await PrepareAsync(fixture)).ExitCode);
            Directory.CreateDirectory(Path.GetDirectoryName(fixture.PackagePath)!);
            WritePackage(fixture.PackagePath, "different-reviewer", corruptPngHash: true);

            var reviewerResult = await FinalizeAsync(
                fixture,
                "accepted",
                "operator-reviewer",
                "Attested for negative-path validation.",
                includePackage: true);

            Assert.NotEqual(0, reviewerResult.ExitCode);
            Assert.Contains("reviewer does not match", reviewerResult.AllOutput, StringComparison.OrdinalIgnoreCase);
            using (var failedTrial = ReadTrial(fixture.TrialPath))
            {
                Assert.Contains(
                    "reviewer does not match",
                    failedTrial.RootElement.GetProperty("error").GetString(),
                    StringComparison.OrdinalIgnoreCase);
            }

            WritePackage(fixture.PackagePath, "operator-reviewer", corruptPngHash: true);
            var hashResult = await FinalizeAsync(
                fixture,
                "accepted",
                "operator-reviewer",
                "Attested for negative-path validation.",
                includePackage: true);

            Assert.NotEqual(0, hashResult.ExitCode);
            Assert.Contains("png hash does not match", hashResult.AllOutput, StringComparison.OrdinalIgnoreCase);
            using var trial = ReadTrial(fixture.TrialPath);
            Assert.Equal("pending_operator", trial.RootElement.GetProperty("status").GetString());
        }
        finally
        {
            fixture.Dispose();
        }
    }

    [Fact]
    public async Task Finalize_RequiresExplicitFiveWorkspaceConfirmation()
    {
        var fixture = CreateFixture();
        try
        {
            Assert.Equal(0, (await PrepareAsync(fixture)).ExitCode);

            var result = await FinalizeAsync(
                fixture,
                "rejected",
                "operator-reviewer",
                "This must not finalize without explicit stage confirmation.",
                includePackage: false,
                confirmWorkspaces: false);

            Assert.NotEqual(0, result.ExitCode);
            Assert.Contains("confirmation of all five", result.AllOutput, StringComparison.OrdinalIgnoreCase);
            using var trial = ReadTrial(fixture.TrialPath);
            Assert.Equal("pending_operator", trial.RootElement.GetProperty("status").GetString());
            Assert.Contains(
                "confirmation of all five",
                trial.RootElement.GetProperty("error").GetString(),
                StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            fixture.Dispose();
        }
    }

    [Fact]
    public async Task PrepareAndFinalize_ProtectExistingLifecycleRecords()
    {
        var fixture = CreateFixture();
        try
        {
            Assert.Equal(0, (await PrepareAsync(fixture)).ExitCode);

            var duplicatePrepare = await PrepareAsync(fixture);
            Assert.NotEqual(0, duplicatePrepare.ExitCode);
            Assert.Contains("already exists", duplicatePrepare.AllOutput, StringComparison.OrdinalIgnoreCase);

            var finalize = await FinalizeAsync(
                fixture,
                "rejected",
                "operator-reviewer",
                "Rejected after reviewing the complete fake-first path.",
                includePackage: false);
            Assert.Equal(0, finalize.ExitCode);

            var duplicateFinalize = await FinalizeAsync(
                fixture,
                "rejected",
                "operator-reviewer",
                "A second finalization must be rejected.",
                includePackage: false);
            Assert.NotEqual(0, duplicateFinalize.ExitCode);
            Assert.Contains("already finalized", duplicateFinalize.AllOutput, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            fixture.Dispose();
        }
    }

    private static TrialFixture CreateFixture()
    {
        var repositoryRoot = FindRepositoryRoot();
        var runId = $"test-{Guid.NewGuid():N}";
        var relativeSessionPath = Path.Combine(
            "outputs",
            "test-scientific-figure-operator-trials",
            runId);
        return new TrialFixture(repositoryRoot, runId, relativeSessionPath);
    }

    private static Task<ProcessResult> PrepareAsync(TrialFixture fixture)
    {
        return RunScriptAsync(
            fixture.RepositoryRoot,
            "-Mode", "Prepare",
            "-RunId", fixture.RunId,
            "-SessionPath", fixture.RelativeSessionPath);
    }

    private static Task<ProcessResult> FinalizeAsync(
        TrialFixture fixture,
        string outcome,
        string reviewer,
        string notes,
        bool includePackage,
        bool confirmWorkspaces = true)
    {
        var arguments = new List<string>
        {
            "-Mode", "Finalize",
            "-SessionPath", fixture.RelativeSessionPath,
            "-Outcome", outcome,
            "-Reviewer", reviewer,
            "-Notes", notes,
        };
        if (confirmWorkspaces)
        {
            arguments.Add("-ConfirmFiveWorkspaces");
        }
        if (includePackage)
        {
            arguments.Add("-PackagePath");
            arguments.Add(Path.GetRelativePath(fixture.RepositoryRoot, fixture.PackagePath));
        }

        return RunScriptAsync(fixture.RepositoryRoot, arguments.ToArray());
    }

    private static async Task<ProcessResult> RunScriptAsync(
        string repositoryRoot,
        params string[] arguments)
    {
        return await RunScriptFromWorkingDirectoryAsync(
            repositoryRoot,
            repositoryRoot,
            arguments);
    }

    private static async Task<ProcessResult> RunScriptFromWorkingDirectoryAsync(
        string repositoryRoot,
        string workingDirectory,
        params string[] arguments)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "pwsh.exe",
            WorkingDirectory = workingDirectory,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
        };
        startInfo.ArgumentList.Add("-NoProfile");
        startInfo.ArgumentList.Add("-ExecutionPolicy");
        startInfo.ArgumentList.Add("Bypass");
        startInfo.ArgumentList.Add("-File");
        startInfo.ArgumentList.Add(Path.Combine(
            repositoryRoot,
            "scripts",
            "run-scientific-figure-operator-trial.ps1"));
        foreach (var argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        using var process = Process.Start(startInfo)
            ?? throw new InvalidOperationException("Failed to start operator trial script.");
        var standardOutputTask = process.StandardOutput.ReadToEndAsync();
        var standardErrorTask = process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();
        return new ProcessResult(
            process.ExitCode,
            await standardOutputTask,
            await standardErrorTask);
    }

    private static JsonDocument ReadTrial(string path)
    {
        return JsonDocument.Parse(File.ReadAllBytes(path));
    }

    private static void WritePackage(
        string path,
        string reviewer,
        bool corruptPngHash = false)
    {
        var svg = Encoding.UTF8.GetBytes("<svg xmlns=\"http://www.w3.org/2000/svg\"><text>Force causes acceleration</text></svg>");
        var png = new byte[] { 1, 2, 3, 4 };
        var pdf = Encoding.ASCII.GetBytes("%PDF-operator-trial");
        using var stream = File.Create(path);
        using var archive = new ZipArchive(stream, ZipArchiveMode.Create);

        WriteEntry(archive, "figure.svg", svg);
        WriteEntry(archive, "figure.png", png);
        WriteEntry(archive, "figure.pdf", pdf);
        WriteJson(archive, "specification.json", new { SpecificationId = Guid.NewGuid() });
        WriteJson(archive, "claim-evidence-item-map.json", Array.Empty<object>());
        WriteJson(archive, "reviews.json", new { Contract = new { Passed = true }, Machine = new { Passed = true } });
        WriteJson(archive, "repairs.json", Array.Empty<object>());
        WriteJson(archive, "providers.json", new[] { new { Provider = "fake" } });
        WriteJson(
            archive,
            "approvals.json",
            new
            {
                GateOne = new { Reviewer = "fake-gate-one" },
                GateTwo = new { Reviewer = reviewer },
            });
        WriteJson(
            archive,
            "manifest.json",
            new
            {
                SvgSha256 = $"sha256:{Hash(svg)}",
                SemanticSha256 = $"sha256:{Hash(svg)}",
                ArtifactSha256 = new Dictionary<string, string>
                {
                    ["png"] = $"sha256:{(corruptPngHash ? new string('0', 64) : Hash(png))}",
                    ["pdf"] = $"sha256:{Hash(pdf)}",
                },
            });
    }

    private static void WriteJson(ZipArchive archive, string name, object value)
    {
        WriteEntry(archive, name, JsonSerializer.SerializeToUtf8Bytes(value));
    }

    private static void WriteEntry(ZipArchive archive, string name, byte[] bytes)
    {
        var entry = archive.CreateEntry(name);
        using var stream = entry.Open();
        stream.Write(bytes);
    }

    private static string Hash(byte[] bytes)
    {
        return Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
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

        throw new DirectoryNotFoundException("Could not find ContentDeliveryStudio.sln.");
    }

    private sealed record ProcessResult(int ExitCode, string StandardOutput, string StandardError)
    {
        public string AllOutput => StandardOutput + Environment.NewLine + StandardError;
    }

    private sealed class TrialFixture(
        string repositoryRoot,
        string runId,
        string relativeSessionPath) : IDisposable
    {
        public string RepositoryRoot { get; } = repositoryRoot;

        public string RunId { get; } = runId;

        public string RelativeSessionPath { get; } = relativeSessionPath;

        public string SessionPath { get; } = Path.Combine(repositoryRoot, relativeSessionPath);

        public string TrialPath => Path.Combine(SessionPath, "trial.json");

        public string ChecklistPath => Path.Combine(SessionPath, "operator-checklist.md");

        public string PackagePath => Path.Combine(SessionPath, "delivery", "scientific-figure.zip");

        public void Dispose()
        {
            if (Directory.Exists(SessionPath))
            {
                Directory.Delete(SessionPath, recursive: true);
            }
        }
    }
}
