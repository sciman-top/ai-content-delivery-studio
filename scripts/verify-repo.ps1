param(
    [ValidateSet("Quick", "Full")]
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

if ($Mode -eq "Full" -and -not [string]::IsNullOrWhiteSpace($TestFilter)) {
    throw "-TestFilter is available only in Quick mode. Full mode always runs the complete test suite."
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

$buildArgs = @("build", "ContentDeliveryStudio.sln")
$testArgs = @("test", "ContentDeliveryStudio.sln")
if ($NoRestore) {
    $buildArgs += "--no-restore"
    $testArgs += @("--no-build", "--no-restore")
}

Invoke-Step -Label "dotnet build" -Action {
    Invoke-DotNetBuildWithRetry -Arguments $buildArgs
}

if ($Mode -eq "Quick") {
    $testArgs += @("--filter", $TestFilter)
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

Invoke-Step -Label "dotnet format --verify-no-changes" -Action {
    & dotnet format --verify-no-changes
}

Write-Host "Repository verification passed." -ForegroundColor Green
