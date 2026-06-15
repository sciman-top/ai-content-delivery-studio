using System.Diagnostics;

namespace ImageSeriesStudio.Tests;

public sealed class VerifyRepoScriptTests
{
    [Fact]
    public async Task VerifyRepoScript_RetriesTransientDotNetBuildLock()
    {
        var repositoryRoot = FindRepositoryRoot();
        var tempRoot = Path.Combine(Path.GetTempPath(), "ImageSeriesStudio.Tests", Guid.NewGuid().ToString("N"));
        var shimDirectory = Path.Combine(tempRoot, "shim");
        var logPath = Path.Combine(tempRoot, "dotnet-invocations.log");
        var statePath = Path.Combine(tempRoot, "dotnet-build-attempt.txt");
        Directory.CreateDirectory(shimDirectory);

        try
        {
            await File.WriteAllTextAsync(
                Path.Combine(shimDirectory, "dotnet.ps1"),
                """
param(
    [Parameter(ValueFromRemainingArguments = $true)]
    [string[]]$Arguments
)

$logPath = $env:DOTNET_SHIM_LOG_PATH
$statePath = $env:DOTNET_SHIM_STATE_PATH

if (-not [string]::IsNullOrWhiteSpace($logPath)) {
    $parentDirectory = Split-Path -Parent $logPath
    if (-not [string]::IsNullOrWhiteSpace($parentDirectory)) {
        New-Item -ItemType Directory -Force -Path $parentDirectory | Out-Null
    }
}

if (-not [string]::IsNullOrWhiteSpace($statePath)) {
    $parentDirectory = Split-Path -Parent $statePath
    if (-not [string]::IsNullOrWhiteSpace($parentDirectory)) {
        New-Item -ItemType Directory -Force -Path $parentDirectory | Out-Null
    }
}

$command = if ($Arguments.Count -gt 0) { $Arguments[0] } else { '' }
if (-not [string]::IsNullOrWhiteSpace($logPath)) {
    Add-Content -LiteralPath $logPath -Value (($Arguments -join ' ').Trim())
}

switch ($command) {
    'build' {
        $attempt = 0
        if (Test-Path -LiteralPath $statePath) {
            $attempt = [int](Get-Content -LiteralPath $statePath -Raw)
        }

        $attempt++
        Set-Content -LiteralPath $statePath -Value $attempt

        if ($attempt -eq 1) {
            Write-Output "C:\\Program Files\\dotnet\\sdk\\10.0.300\\Sdks\\Microsoft.NET.Sdk.WindowsDesktop\\targets\\Microsoft.WinFX.targets(281,9): error MC2000: The process cannot access the file 'D:\\CODE\\ai-content-delivery-studio\\src\\ImageSeriesStudio.Infrastructure\\bin\\Debug\\net10.0\\ImageSeriesStudio.Infrastructure.dll' because it is being used by another process."
            exit 1
        }

        Write-Output "Build succeeded."
        exit 0
    }

    'test' {
        Write-Output "Tests passed."
        exit 0
    }

    'format' {
        Write-Output "Format clean."
        exit 0
    }

    default {
        Write-Output "dotnet shim: $command"
        exit 0
    }
}
""");
            await File.WriteAllTextAsync(
                Path.Combine(shimDirectory, "dotnet.cmd"),
                """
@echo off
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0dotnet.ps1" %*
""");

            var result = await RunPowerShellAsync(
                repositoryRoot,
                shimDirectory,
                logPath,
                statePath,
                Path.Combine(repositoryRoot, "scripts", "verify-repo.ps1"),
                "-NoRestore");

            Assert.Equal(0, result.ExitCode);
            Assert.Contains("Repository verification passed.", result.StandardOutput);

            var buildInvocations = await File.ReadAllLinesAsync(logPath);
            Assert.Equal(2, buildInvocations.Count(line => line.StartsWith("build", StringComparison.OrdinalIgnoreCase)));
        }
        finally
        {
            if (Directory.Exists(tempRoot))
            {
                Directory.Delete(tempRoot, recursive: true);
            }
        }
    }

    [Fact]
    public async Task VerifyRepoScript_FailsClosedOnNonTransientBuildErrorWithoutHelperBindingFailure()
    {
        var repositoryRoot = FindRepositoryRoot();
        var tempRoot = Path.Combine(Path.GetTempPath(), "ImageSeriesStudio.Tests", Guid.NewGuid().ToString("N"));
        var shimDirectory = Path.Combine(tempRoot, "shim");
        var logPath = Path.Combine(tempRoot, "dotnet-invocations.log");
        var statePath = Path.Combine(tempRoot, "dotnet-build-attempt.txt");
        Directory.CreateDirectory(shimDirectory);

        try
        {
            await File.WriteAllTextAsync(
                Path.Combine(shimDirectory, "dotnet.ps1"),
                """
param(
    [Parameter(ValueFromRemainingArguments = $true)]
    [string[]]$Arguments
)

$logPath = $env:DOTNET_SHIM_LOG_PATH
if (-not [string]::IsNullOrWhiteSpace($logPath)) {
    Add-Content -LiteralPath $logPath -Value (($Arguments -join ' ').Trim())
}

$command = if ($Arguments.Count -gt 0) { $Arguments[0] } else { '' }
switch ($command) {
    'build' {
        Write-Output ""
        Write-Output "fatal build failure"
        exit 1
    }

    'test' {
        Write-Output "Tests passed."
        exit 0
    }

    'format' {
        Write-Output "Format clean."
        exit 0
    }

    default {
        Write-Output "dotnet shim: $command"
        exit 0
    }
}
""");
            await File.WriteAllTextAsync(
                Path.Combine(shimDirectory, "dotnet.cmd"),
                """
@echo off
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0dotnet.ps1" %*
""");

            var result = await RunPowerShellAsync(
                repositoryRoot,
                shimDirectory,
                logPath,
                statePath,
                Path.Combine(repositoryRoot, "scripts", "verify-repo.ps1"),
                "-NoRestore");

            Assert.Equal(1, result.ExitCode);
            Assert.Contains("Step failed: dotnet build", result.StandardError + result.StandardOutput);
            Assert.DoesNotContain("Cannot bind argument to parameter 'OutputLines'", result.StandardError + result.StandardOutput, StringComparison.OrdinalIgnoreCase);

            var buildInvocations = await File.ReadAllLinesAsync(logPath);
            Assert.Single(buildInvocations, line => line.StartsWith("build", StringComparison.OrdinalIgnoreCase));
        }
        finally
        {
            if (Directory.Exists(tempRoot))
            {
                Directory.Delete(tempRoot, recursive: true);
            }
        }
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "ImageSeriesStudio.sln")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not find ImageSeriesStudio.sln from the test output path.");
    }

    private static async Task<ProcessResult> RunPowerShellAsync(
        string repositoryRoot,
        string shimDirectory,
        string logPath,
        string statePath,
        string scriptPath,
        params string[] scriptArguments)
    {
        var processStartInfo = new ProcessStartInfo
        {
            FileName = "pwsh.exe",
            WorkingDirectory = repositoryRoot,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
        };

        processStartInfo.ArgumentList.Add("-NoProfile");
        processStartInfo.ArgumentList.Add("-ExecutionPolicy");
        processStartInfo.ArgumentList.Add("Bypass");
        processStartInfo.ArgumentList.Add("-File");
        processStartInfo.ArgumentList.Add(scriptPath);
        foreach (var argument in scriptArguments)
        {
            processStartInfo.ArgumentList.Add(argument);
        }

        var originalPath = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
        processStartInfo.Environment["PATH"] = string.Join(
            Path.PathSeparator,
            [shimDirectory, originalPath]);
        processStartInfo.Environment["DOTNET_SHIM_LOG_PATH"] = logPath;
        processStartInfo.Environment["DOTNET_SHIM_STATE_PATH"] = statePath;

        using var process = Process.Start(processStartInfo)
            ?? throw new InvalidOperationException("Failed to start PowerShell process for verify-repo.ps1.");

        var standardOutputTask = process.StandardOutput.ReadToEndAsync();
        var standardErrorTask = process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();

        return new ProcessResult(
            process.ExitCode,
            await standardOutputTask,
            await standardErrorTask);
    }

    private sealed record ProcessResult(int ExitCode, string StandardOutput, string StandardError);
}
