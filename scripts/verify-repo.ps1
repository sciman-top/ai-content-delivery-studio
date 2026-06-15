param(
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

    return $combined.Contains("being used by another process", [System.StringComparison]::OrdinalIgnoreCase) `
        -or $combined.Contains("cannot access the file", [System.StringComparison]::OrdinalIgnoreCase)
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
            Write-Host "Transient dotnet build file lock detected. Retrying in $RetryDelaySeconds second(s) ($attempt/$MaxAttempts)..." -ForegroundColor Yellow
            Start-Sleep -Seconds $RetryDelaySeconds
            continue
        }

        $global:LASTEXITCODE = $exitCode
        return
    }
}

Invoke-Step -Label "Reference governance parity" -Action {
    & ".\scripts\sync-reference-governance.ps1" -Check
}

if (-not $SkipReferenceEvidence) {
    Invoke-Step -Label "Reference evidence gate" -Action {
        if (-not [string]::IsNullOrWhiteSpace($ReferenceEvidenceBaseRef) -or -not [string]::IsNullOrWhiteSpace($ReferenceEvidenceHeadRef)) {
            & ".\scripts\verify-reference-evidence.ps1" -BaseRef $ReferenceEvidenceBaseRef -HeadRef $ReferenceEvidenceHeadRef
        } else {
            & ".\scripts\verify-reference-evidence.ps1"
        }
    }
}

$buildArgs = @("build")
$testArgs = @("test")
if ($NoRestore) {
    $buildArgs += "--no-restore"
    $testArgs += @("--no-build", "--no-restore")
}

Invoke-Step -Label "dotnet build" -Action {
    Invoke-DotNetBuildWithRetry -Arguments $buildArgs
}

Invoke-Step -Label "dotnet test" -Action {
    & dotnet @testArgs
}

Invoke-Step -Label "dotnet format --verify-no-changes" -Action {
    & dotnet format --verify-no-changes
}

Write-Host "Repository verification passed." -ForegroundColor Green
