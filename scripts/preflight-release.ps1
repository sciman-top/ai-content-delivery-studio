param(
    [switch]$SkipReferenceEvidence,
    [switch]$SkipPublishWhatIf,
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

function Invoke-RgFilteredCheck {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Pattern,

        [Parameter(Mandatory = $true)]
        [string[]]$Targets,

        [string[]]$IgnorePatterns = @()
    )

    $raw = & rg -n $Pattern @Targets
    $exitCode = $LASTEXITCODE
    if ($exitCode -eq 1) {
        $global:LASTEXITCODE = 0
        return @()
    }

    if ($exitCode -ne 0) {
        throw "rg failed with exit code $exitCode."
    }

    $lines = @($raw | ForEach-Object { $_.ToString() })
    if ($IgnorePatterns.Count -gt 0) {
        $lines = @(
            $lines | Where-Object {
                $line = $_
                -not ($IgnorePatterns | Where-Object { $line -match $_ } | Select-Object -First 1)
            }
        )
    }

    foreach ($line in $lines) {
        Write-Host $line
    }

    return $lines
}

Invoke-Step -Label "Reference governance parity" -Action {
    & ".\scripts\sync-reference-governance.ps1" -Check
}

Invoke-Step -Label "Placeholder scan" -Action {
    $hits = @(Invoke-RgFilteredCheck -Pattern "\b(TBD|TODO|PLACEHOLDER)\b" -Targets @("docs", "src", "tests", "scripts", "README.md", "AGENTS.md") -IgnorePatterns @(
        "^scripts[\\\\/]preflight-release\.ps1:",
        "rg -n .*(TBD|TODO|PLACE''HOLDER)",
        "rg -n .*(TBD|TODO|PLACEHOLDER)",
        "Placeholder scan"
    ))
    if ($hits.Count -gt 0) {
        throw "Placeholder markers detected."
    }
}

Invoke-Step -Label "Merge conflict marker scan" -Action {
    $hits = @(Invoke-RgFilteredCheck -Pattern "^(<<<<<<<|=======|>>>>>>>)" -Targets @("docs", "src", "tests", "scripts", ".github", "README.md", "AGENTS.md"))
    if ($hits.Count -gt 0) {
        throw "Merge conflict markers detected."
    }
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

Invoke-Step -Label "Repository verification" -Action {
    $verifyParams = @{
        SkipReferenceEvidence = $true
    }
    if ($NoRestore) {
        $verifyParams.NoRestore = $true
    }
    if (-not [string]::IsNullOrWhiteSpace($ReferenceEvidenceBaseRef)) {
        $verifyParams.ReferenceEvidenceBaseRef = $ReferenceEvidenceBaseRef
    }
    if (-not [string]::IsNullOrWhiteSpace($ReferenceEvidenceHeadRef)) {
        $verifyParams.ReferenceEvidenceHeadRef = $ReferenceEvidenceHeadRef
    }

    & ".\scripts\verify-repo.ps1" @verifyParams
}

if (-not $SkipPublishWhatIf) {
    Invoke-Step -Label "Publish preflight (WhatIf)" -Action {
        & ".\scripts\publish-app.ps1" -Configuration Release -Runtime win-x64 -WhatIfOnly
    }
}

Invoke-Step -Label "git diff --check" -Action {
    & git diff --check
}

Invoke-Step -Label "git diff --cached --check" -Action {
    & git diff --cached --check
}

Write-Host "Release preflight passed." -ForegroundColor Green
