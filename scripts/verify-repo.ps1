param(
    [ValidateSet("Quick", "Full", "Release")]
    [string]$Mode = "Full",
    [string]$TestFilter,
    [switch]$SkipReferenceEvidence,
    [switch]$NoRestore,
    [string]$ReferenceEvidenceBaseRef,
    [string]$ReferenceEvidenceHeadRef
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$repoRoot = (& git rev-parse --show-toplevel 2>$null)
if ($LASTEXITCODE -ne 0 -or [string]::IsNullOrWhiteSpace($repoRoot)) {
    throw "Failed to resolve repository root with git rev-parse --show-toplevel."
}

$repoRoot = $repoRoot.Trim()
Set-Location $repoRoot

if ($Mode -eq "Quick" -and [string]::IsNullOrWhiteSpace($TestFilter)) {
    throw "Quick mode requires -TestFilter so it cannot masquerade as full repository verification."
}

if ($Mode -ne "Quick" -and -not [string]::IsNullOrWhiteSpace($TestFilter)) {
    throw "-TestFilter is available only in Quick mode. Full and Release use fixed test lanes."
}

function Invoke-Step {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Label,

        [Parameter(Mandatory = $true)]
        [scriptblock]$Action
    )

    Write-Host "==> $Label" -ForegroundColor Cyan
    & $Action
    if ($LASTEXITCODE -ne 0) {
        throw "Step failed: $Label"
    }
}

function Test-IsTransientDotNetBuildLock {
    param(
        [string[]]$OutputLines
    )

    if ($null -eq $OutputLines -or $OutputLines.Count -eq 0) {
        return $false
    }

    $combined = ($OutputLines -join "`n")
    if ([string]::IsNullOrWhiteSpace($combined)) {
        return $false
    }

    $hasFileLock = $combined.Contains("being used by another process", [System.StringComparison]::OrdinalIgnoreCase) `
        -or $combined.Contains("cannot access the file", [System.StringComparison]::OrdinalIgnoreCase)
    $hasTransientWpfGeneratedSourceMiss = $combined.Contains("error CS2001", [System.StringComparison]::OrdinalIgnoreCase) `
        -and $combined.Contains("_wpftmp.csproj", [System.StringComparison]::OrdinalIgnoreCase) `
        -and $combined.Contains(".g.cs", [System.StringComparison]::OrdinalIgnoreCase)

    return $hasFileLock -or $hasTransientWpfGeneratedSourceMiss
}

function Invoke-DotNetBuildWithRetry {
    param(
        [Parameter(Mandatory = $true)]
        [string[]]$Arguments,

        [int]$MaxAttempts = 3,

        [int]$RetryDelaySeconds = 2
    )

    for ($attempt = 1; $attempt -le $MaxAttempts; $attempt++) {
        $outputLines = @(& dotnet @Arguments 2>&1 | ForEach-Object { $_.ToString() })
        $exitCode = $LASTEXITCODE

        foreach ($line in $outputLines) {
            Write-Host $line
        }

        if ($exitCode -eq 0) {
            $global:LASTEXITCODE = 0
            return
        }

        if ($attempt -lt $MaxAttempts -and (Test-IsTransientDotNetBuildLock -OutputLines @($outputLines))) {
            Write-Host "Transient dotnet build failure detected. Retrying in $RetryDelaySeconds second(s) ($attempt/$MaxAttempts)..." -ForegroundColor Yellow
            Start-Sleep -Seconds $RetryDelaySeconds
            continue
        }

        $global:LASTEXITCODE = $exitCode
        return
    }
}

function Get-ChangedPaths {
    $paths = [System.Collections.Generic.HashSet[string]]::new(
        [System.StringComparer]::OrdinalIgnoreCase)
    $gitQueries = @(
        @("diff", "--name-only", "--diff-filter=ACMR"),
        @("diff", "--cached", "--name-only", "--diff-filter=ACMR"),
        @("ls-files", "--others", "--exclude-standard")
    )

    if (-not [string]::IsNullOrWhiteSpace($ReferenceEvidenceBaseRef) -and
        -not [string]::IsNullOrWhiteSpace($ReferenceEvidenceHeadRef)) {
        $gitQueries += ,@(
            "diff",
            "--name-only",
            "--diff-filter=ACMR",
            "$ReferenceEvidenceBaseRef...$ReferenceEvidenceHeadRef")
    }

    foreach ($query in $gitQueries) {
        $output = @(& git @query)
        if ($LASTEXITCODE -ne 0) {
            throw "Failed to discover changed paths with: git $($query -join ' ')"
        }

        foreach ($path in $output) {
            $relativePath = $path.ToString().Trim()
            if (-not [string]::IsNullOrWhiteSpace($relativePath) -and
                (Test-Path -LiteralPath $relativePath -PathType Leaf)) {
                $null = $paths.Add($relativePath)
            }
        }
    }

    return @($paths | Sort-Object)
}

$buildArgs = @("build", "ContentDeliveryStudio.sln")
$testArgs = @("test", "ContentDeliveryStudio.sln", "--no-build", "--no-restore")
if ($NoRestore) {
    $buildArgs += "--no-restore"
}

Invoke-Step -Label "dotnet build" -Action {
    Invoke-DotNetBuildWithRetry -Arguments $buildArgs
}

if ($Mode -eq "Quick") {
    $testArgs += @("--filter", $TestFilter)
} elseif ($Mode -eq "Full") {
    $testArgs += @("--filter", "Category!=ReleaseOnly")
    Write-Host "[BOUNDARY] Full runs the core suite; Category=ReleaseOnly remains for Release/CI or an explicit focused filter." -ForegroundColor Yellow
} else {
    Write-Host "[BOUNDARY] Release verification includes the complete suite, including Category=ReleaseOnly." -ForegroundColor Yellow
}

Invoke-Step -Label "dotnet test" -Action {
    & dotnet @testArgs
}

if ($Mode -eq "Quick") {
    Write-Host "Quick verification passed for filter: $TestFilter" -ForegroundColor Green
    exit 0
}

Invoke-Step -Label "Reference evidence and governance" -Action {
    if ($SkipReferenceEvidence) {
        & ".\scripts\verify-reference-evidence.ps1" -ParityOnly
    } elseif (-not [string]::IsNullOrWhiteSpace($ReferenceEvidenceBaseRef) -or -not [string]::IsNullOrWhiteSpace($ReferenceEvidenceHeadRef)) {
        & ".\scripts\verify-reference-evidence.ps1" -BaseRef $ReferenceEvidenceBaseRef -HeadRef $ReferenceEvidenceHeadRef
    } else {
        & ".\scripts\verify-reference-evidence.ps1"
    }
}

Invoke-Step -Label "Product focus plan contract" -Action {
    & ".\scripts\verify-product-focus-plan.ps1"
}

if ($Mode -eq "Full") {
    Invoke-Step -Label "git diff --check" -Action {
        if (-not [string]::IsNullOrWhiteSpace($ReferenceEvidenceBaseRef) -and
            -not [string]::IsNullOrWhiteSpace($ReferenceEvidenceHeadRef)) {
            & git diff --check "$ReferenceEvidenceBaseRef...$ReferenceEvidenceHeadRef"
        } else {
            & git diff --check
            if ($LASTEXITCODE -eq 0) {
                & git diff --cached --check
            }
        }
    }

    Write-Host "Repository verification passed." -ForegroundColor Green
    exit 0
}

Invoke-Step -Label "dotnet format --verify-no-changes" -Action {
    $changedPaths = @(Get-ChangedPaths)
    $changedCSharpPaths = @($changedPaths | Where-Object {
        [System.IO.Path]::GetExtension($_) -ieq ".cs"
    })
    $formatConfigurationChanged = @($changedPaths | Where-Object {
        $name = [System.IO.Path]::GetFileName($_)
        $name -in @(".editorconfig", "global.json") -or
        $name -like "*.csproj" -or
        $name -like "Directory.Build.*"
    }).Count -gt 0

    if ($changedPaths.Count -eq 0 -or $formatConfigurationChanged) {
        $formatArgs = @("format")
        $formatArgs += @("ContentDeliveryStudio.sln", "--verify-no-changes", "--no-restore")
        & dotnet @formatArgs
        return
    }

    if ($changedCSharpPaths.Count -eq 0) {
        Write-Host "No changed C# files detected; formatting verification is not required."
        $global:LASTEXITCODE = 0
        return
    }

    $formatArgs = @("format")
    $formatArgs += @(
        "ContentDeliveryStudio.sln",
        "--verify-no-changes",
        "--no-restore",
        "--include")
    $formatArgs += $changedCSharpPaths
    & dotnet @formatArgs
}

Write-Host "Repository verification passed." -ForegroundColor Green
