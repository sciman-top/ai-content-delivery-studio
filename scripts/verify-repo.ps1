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
    & dotnet @buildArgs
}

Invoke-Step -Label "dotnet test" -Action {
    & dotnet @testArgs
}

Invoke-Step -Label "dotnet format --verify-no-changes" -Action {
    & dotnet format --verify-no-changes
}

Write-Host "Repository verification passed." -ForegroundColor Green
