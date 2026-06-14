param(
    [string[]]$Paths,
    [string]$BaseRef,
    [string]$HeadRef,
    [switch]$RequireReferenceBasisFile
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Get-RepoRoot {
    $root = (& git rev-parse --show-toplevel 2>$null)
    if ($LASTEXITCODE -ne 0 -or [string]::IsNullOrWhiteSpace($root)) {
        throw "Failed to resolve repository root with git rev-parse --show-toplevel."
    }

    return $root.Trim()
}

function Normalize-RelativePath {
    param(
        [Parameter(Mandatory = $true)]
        [string]$RepoRoot,

        [Parameter(Mandatory = $true)]
        [string]$Path
    )

    $trimmed = $Path.Trim()
    if ([string]::IsNullOrWhiteSpace($trimmed)) {
        return $null
    }

    if ([System.IO.Path]::IsPathRooted($trimmed)) {
        $fullPath = [System.IO.Path]::GetFullPath($trimmed)
        $repoRootFull = [System.IO.Path]::GetFullPath($RepoRoot)
        if (-not $fullPath.StartsWith($repoRootFull, [System.StringComparison]::OrdinalIgnoreCase)) {
            return $null
        }

        $relative = [System.IO.Path]::GetRelativePath($repoRootFull, $fullPath)
        return $relative.Replace("\", "/")
    }

    return $trimmed.Replace("\", "/")
}

function Get-ChangedPaths {
    param(
        [Parameter(Mandatory = $true)]
        [string]$RepoRoot
    )

    $sets = @(
        (& git -C $RepoRoot diff --name-only),
        (& git -C $RepoRoot diff --name-only --cached),
        (& git -C $RepoRoot ls-files --others --exclude-standard)
    )

    return @(
        $sets |
            ForEach-Object { $_ } |
            Where-Object { -not [string]::IsNullOrWhiteSpace($_) } |
            ForEach-Object { Normalize-RelativePath -RepoRoot $RepoRoot -Path $_ } |
            Where-Object { $_ } |
            Sort-Object -Unique
    )
}

function Get-ChangedPathsFromRange {
    param(
        [Parameter(Mandatory = $true)]
        [string]$RepoRoot,

        [Parameter(Mandatory = $true)]
        [string]$BaseRef,

        [Parameter(Mandatory = $true)]
        [string]$HeadRef
    )

    return @(
        (& git -C $RepoRoot diff --name-only $BaseRef $HeadRef) |
            Where-Object { -not [string]::IsNullOrWhiteSpace($_) } |
            ForEach-Object { Normalize-RelativePath -RepoRoot $RepoRoot -Path $_ } |
            Where-Object { $_ } |
            Sort-Object -Unique
    )
}

function Test-MatchAnyRule {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path,

        [Parameter(Mandatory = $true)]
        [string[]]$Rules
    )

    foreach ($rule in $Rules) {
        if ($rule.EndsWith("/")) {
            if ($Path.StartsWith($rule, [System.StringComparison]::OrdinalIgnoreCase)) {
                return $true
            }
            continue
        }

        if ($Path.Equals($rule, [System.StringComparison]::OrdinalIgnoreCase)) {
            return $true
        }
    }

    return $false
}

function Invoke-ReferenceGovernanceSyncCheck {
    param(
        [Parameter(Mandatory = $true)]
        [string]$RepoRoot
    )

    $syncPath = Join-Path $RepoRoot "scripts/sync-reference-governance.ps1"
    if (-not (Test-Path -LiteralPath $syncPath)) {
        throw "Missing reference governance sync script: $syncPath"
    }

    & $syncPath -Check
    if ($LASTEXITCODE -ne 0) {
        throw "Reference governance parity check failed."
    }
}

function Get-AreaDefinitions {
    param(
        [Parameter(Mandatory = $true)]
        [string]$RepoRoot
    )

    $basisPath = Join-Path $RepoRoot "scripts/reference-basis.json"
    if (-not (Test-Path -LiteralPath $basisPath)) {
        throw "Missing reference basis manifest: $basisPath"
    }

    $basis = Get-Content -LiteralPath $basisPath -Raw | ConvertFrom-Json
    $areas = @($basis.areas)
    if (-not $areas.Count) {
        throw "Reference basis manifest does not define any areas: $basisPath"
    }

    return @(
        $areas | ForEach-Object {
            [pscustomobject]@{
                Name = [string]$_.name
                Required = [bool]$_.required
                SourceRules = @($_.sourceRules)
                EvidenceRules = @($_.evidenceRules)
                RecommendedReferences = @($_.localReferences | ForEach-Object { [string]$_.path })
                RequiredTriggers = @($_.requiredTriggers)
            }
        }
    )
}

$repoRoot = Get-RepoRoot
$hasExplicitPaths = $Paths -and $Paths.Count -gt 0
$hasDiffRange = -not [string]::IsNullOrWhiteSpace($BaseRef) -or -not [string]::IsNullOrWhiteSpace($HeadRef)

if ($hasExplicitPaths -and $hasDiffRange) {
    throw "Use either -Paths or -BaseRef/-HeadRef, not both."
}

if ($hasDiffRange -and ([string]::IsNullOrWhiteSpace($BaseRef) -or [string]::IsNullOrWhiteSpace($HeadRef))) {
    throw "Both -BaseRef and -HeadRef are required when using diff-range mode."
}

$changedPaths = if ($hasExplicitPaths) {
    @(
        $Paths |
            ForEach-Object { Normalize-RelativePath -RepoRoot $repoRoot -Path $_ } |
            Where-Object { $_ } |
            Sort-Object -Unique
    )
} elseif ($hasDiffRange) {
    Get-ChangedPathsFromRange -RepoRoot $repoRoot -BaseRef $BaseRef.Trim() -HeadRef $HeadRef.Trim()
} else {
    Get-ChangedPaths -RepoRoot $repoRoot
}
$changedPaths = @($changedPaths)

if (-not $changedPaths -or $changedPaths.Count -eq 0) {
    Write-Host "[OK] No changed paths detected. Reference evidence gate passed."
    exit 0
}

Invoke-ReferenceGovernanceSyncCheck -RepoRoot $repoRoot

$areas = Get-AreaDefinitions -RepoRoot $repoRoot
$touchedAreas = @()

foreach ($area in $areas) {
    if (-not $area.Required) {
        continue
    }

    $triggeringPaths = @($changedPaths | Where-Object { Test-MatchAnyRule -Path $_ -Rules $area.SourceRules })
    if ($triggeringPaths.Count -eq 0) {
        continue
    }

    $evidenceHits = @($changedPaths | Where-Object { Test-MatchAnyRule -Path $_ -Rules $area.EvidenceRules })
    $referenceBasisHit = $changedPaths | Where-Object { $_ -eq "docs/REFERENCE_BASIS.md" -or $_ -eq "scripts/reference-basis.json" } | Select-Object -First 1

    $touchedAreas += [pscustomobject]@{
        Name = $area.Name
        TriggeringPaths = @($triggeringPaths)
        EvidenceHits = @($evidenceHits)
        EvidenceRules = $area.EvidenceRules
        RecommendedReferences = $area.RecommendedReferences
        RequiredTriggers = $area.RequiredTriggers
        HasReferenceBasisHit = [bool]$referenceBasisHit
    }
}

if ($touchedAreas.Count -eq 0) {
    Write-Host "[OK] No enforced engineering area was touched. Reference evidence gate passed."
    exit 0
}

$failedAreas = @(
    $touchedAreas | Where-Object {
        @($_.EvidenceHits).Count -eq 0 -or ($RequireReferenceBasisFile -and -not $_.HasReferenceBasisHit)
    }
)

foreach ($area in $touchedAreas) {
    $hasEvidence = @($area.EvidenceHits).Count -gt 0
    $hasRequiredBasis = -not $RequireReferenceBasisFile -or $area.HasReferenceBasisHit
    if ($hasEvidence -and $hasRequiredBasis) {
        Write-Host "[OK] $($area.Name): evidence update detected." -ForegroundColor Green
        foreach ($hit in $area.EvidenceHits) {
            Write-Host "  - $hit"
        }
        if ($RequireReferenceBasisFile) {
            Write-Host "  - docs/REFERENCE_BASIS.md or scripts/reference-basis.json updated"
        }
    } else {
        Write-Host "[FAIL] $($area.Name): required reference evidence is incomplete." -ForegroundColor Red
        Write-Host "  Triggering paths:"
        foreach ($path in $area.TriggeringPaths) {
            Write-Host "  - $path"
        }
        Write-Host "  Typical trigger families:"
        foreach ($trigger in $area.RequiredTriggers) {
            Write-Host "  - $trigger"
        }
        Write-Host "  Acceptable evidence updates:"
        foreach ($rule in $area.EvidenceRules) {
            Write-Host "  - $rule"
        }
        if ($RequireReferenceBasisFile) {
            Write-Host "  Required basis updates:"
            Write-Host "  - docs/REFERENCE_BASIS.md"
            Write-Host "  - scripts/reference-basis.json"
        }
        Write-Host "  Recommended local references:"
        foreach ($reference in $area.RecommendedReferences) {
            Write-Host "  - $reference"
        }
    }

    Write-Host ""
}

if ($failedAreas.Count -gt 0) {
    throw "Reference evidence gate failed for $($failedAreas.Count) area(s). Update in-repo evidence before closing the change."
}

Write-Host "[OK] Reference evidence gate passed for all touched enforced areas." -ForegroundColor Green
