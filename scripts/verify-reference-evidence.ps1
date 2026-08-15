param(
    [string[]]$Paths,
    [string]$BaseRef,
    [string]$HeadRef,
    [switch]$RequireDecision,
    [switch]$RequireReferenceBasisFile,
    [switch]$ParityOnly
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

function Test-HasProperty {
    param(
        [Parameter(Mandatory = $true)]$Object,
        [Parameter(Mandatory = $true)][string]$Name
    )

    return $null -ne $Object -and $Object.PSObject.Properties.Name -contains $Name
}

function Test-NonEmptyText {
    param($Value)

    return $Value -is [string] -and -not [string]::IsNullOrWhiteSpace($Value)
}

function Get-StructuredReferenceDecisions {
    param(
        [Parameter(Mandatory = $true)][string]$RepoRoot,
        [AllowEmptyCollection()][string[]]$EvidencePaths = @()
    )

    $decisions = @()
    foreach ($evidencePath in $EvidencePaths) {
        $fullPath = Join-Path $RepoRoot $evidencePath
        if (-not (Test-Path -LiteralPath $fullPath -PathType Leaf)) {
            continue
        }

        $content = Get-Content -LiteralPath $fullPath -Raw -Encoding utf8
        $matches = [regex]::Matches(
            $content,
            '(?ms)```reference-decision\s*(.*?)\s*```')
        foreach ($match in $matches) {
            try {
                $decision = $match.Groups[1].Value | ConvertFrom-Json
            } catch {
                throw "Invalid reference-decision JSON in $evidencePath`: $($_.Exception.Message)"
            }

            $decisions += [pscustomobject]@{
                EvidencePath = $evidencePath
                Decision = $decision
            }
        }
    }

    return @($decisions)
}

function Get-ReferenceDecisionErrors {
    param(
        [Parameter(Mandatory = $true)]$Decision,
        [Parameter(Mandatory = $true)]$Area
    )

    $validationErrors = [System.Collections.Generic.List[string]]::new()
    if (-not (Test-HasProperty $Decision "schemaVersion") -or $Decision.schemaVersion -ne 1) {
        $validationErrors.Add("schemaVersion must be 1")
    }
    if (-not (Test-HasProperty $Decision "area") -or $Decision.area -ne $Area.Name) {
        $validationErrors.Add("area must equal $($Area.Name)")
    }
    if (-not (Test-HasProperty $Decision "trigger") -or -not (Test-NonEmptyText $Decision.trigger)) {
        $validationErrors.Add("trigger must be non-empty")
    } elseif ($Decision.trigger -notin $Area.RequiredTriggers) {
        $validationErrors.Add("trigger must be one of the area's declared trigger families")
    }
    if (-not (Test-HasProperty $Decision "observedBehavior") -or -not (Test-NonEmptyText $Decision.observedBehavior)) {
        $validationErrors.Add("observedBehavior must be non-empty")
    }
    if (-not (Test-HasProperty $Decision "decision") -or $Decision.decision -notin @("adopt", "adapt", "reject")) {
        $validationErrors.Add("decision must be adopt, adapt, or reject")
    }
    if (-not (Test-HasProperty $Decision "affectedContract") -or -not (Test-NonEmptyText $Decision.affectedContract)) {
        $validationErrors.Add("affectedContract must be non-empty")
    }

    $focusedVerification = if (Test-HasProperty $Decision "focusedVerification") { @($Decision.focusedVerification) } else { @() }
    if ($focusedVerification.Count -eq 0 -or @($focusedVerification | Where-Object { -not (Test-NonEmptyText $_) }).Count -gt 0) {
        $validationErrors.Add("focusedVerification must contain at least one non-empty command or probe")
    }

    $consultedSources = if (Test-HasProperty $Decision "consultedSources") { @($Decision.consultedSources) } else { @() }
    $invalidSources = @($consultedSources | Where-Object {
        -not (Test-HasProperty $_ "path") -or -not (Test-NonEmptyText $_.path) -or
        -not (Test-HasProperty $_ "revision") -or -not (Test-NonEmptyText $_.revision)
    })
    if ($invalidSources.Count -gt 0) {
        $validationErrors.Add("every consultedSources entry must contain non-empty path and revision")
    }

    if ($consultedSources.Count -eq 0) {
        if (-not (Test-HasProperty $Decision "unavailableEvidence")) {
            $validationErrors.Add("consultedSources is empty, so unavailableEvidence is required")
        } else {
            foreach ($property in @("reason", "expiresAt", "recoveryCondition")) {
                if (-not (Test-HasProperty $Decision.unavailableEvidence $property) -or -not (Test-NonEmptyText $Decision.unavailableEvidence.$property)) {
                    $validationErrors.Add("unavailableEvidence.$property must be non-empty")
                }
            }
        }
    }

    return @($validationErrors)
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
$Paths = @(
    $Paths |
        ForEach-Object { $_ -split ',' } |
        ForEach-Object { $_.Trim() } |
        Where-Object { -not [string]::IsNullOrWhiteSpace($_) }
)
$hasExplicitPaths = $Paths -and $Paths.Count -gt 0
$hasDiffRange = -not [string]::IsNullOrWhiteSpace($BaseRef) -or -not [string]::IsNullOrWhiteSpace($HeadRef)

if ($hasExplicitPaths -and $hasDiffRange) {
    throw "Use either -Paths or -BaseRef/-HeadRef, not both."
}

if ($ParityOnly -and ($hasExplicitPaths -or $hasDiffRange)) {
    throw "-ParityOnly cannot be combined with -Paths or diff-range mode."
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

Invoke-ReferenceGovernanceSyncCheck -RepoRoot $repoRoot

if ($ParityOnly) {
    Write-Host "[OK] Reference governance parity passed."
    exit 0
}

if (-not $changedPaths -or $changedPaths.Count -eq 0) {
    Write-Host "[OK] No changed paths detected. Reference evidence gate passed."
    exit 0
}

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

$failedAreas = @()

foreach ($area in $touchedAreas) {
    $structuredDecisions = Get-StructuredReferenceDecisions -RepoRoot $repoRoot -EvidencePaths @($area.EvidenceHits)
    $matchingDecisions = @($structuredDecisions | Where-Object {
        (Test-HasProperty $_.Decision "area") -and $_.Decision.area -eq $area.Name
    })
    $validDecisions = @()
    $invalidDecisionMessages = @()
    foreach ($candidate in $matchingDecisions) {
        $candidateErrors = @(Get-ReferenceDecisionErrors -Decision $candidate.Decision -Area $area)
        if ($candidateErrors.Count -eq 0) {
            $validDecisions += $candidate
        } else {
            $invalidDecisionMessages += "$($candidate.EvidencePath): $($candidateErrors -join '; ')"
        }
    }

    $hasEvidence = $validDecisions.Count -gt 0
    $hasRequiredBasis = -not $RequireReferenceBasisFile -or $area.HasReferenceBasisHit
    if ($hasEvidence -and $hasRequiredBasis) {
        Write-Host "[OK] $($area.Name): structured reference decision verified." -ForegroundColor Green
        foreach ($decision in $validDecisions) {
            Write-Host "  - $($decision.EvidencePath): $($decision.Decision.decision) / $($decision.Decision.trigger)"
        }
        if ($RequireReferenceBasisFile) {
            Write-Host "  - docs/REFERENCE_BASIS.md or scripts/reference-basis.json updated"
        }
    } elseif ($invalidDecisionMessages.Count -gt 0 -or $RequireDecision -or $RequireReferenceBasisFile) {
        $failedAreas += $area
        Write-Host "[FAIL] $($area.Name): required structured reference decision is incomplete." -ForegroundColor Red
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
        Write-Host "  Required reference-decision fields:"
        Write-Host "  - area, trigger, consultedSources(path/revision), observedBehavior"
        Write-Host "  - decision(adopt/adapt/reject), affectedContract, focusedVerification"
        Write-Host "  - unavailableEvidence(reason/expiresAt/recoveryCondition) when no source is available"
        foreach ($message in $invalidDecisionMessages) {
            Write-Host "  Invalid decision: $message"
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
    } else {
        Write-Host "[INFO] $($area.Name): mapped area touched; no structured reference decision was requested." -ForegroundColor Yellow
        Write-Host "  Use -RequireDecision only when the current change actually needs external-source adjudication."
    }

    Write-Host ""
}

if ($failedAreas.Count -gt 0) {
    throw "Reference evidence gate failed for $($failedAreas.Count) area(s). Add a valid structured reference decision before closing the change."
}

Write-Host "[OK] Reference governance passed for all touched mapped areas." -ForegroundColor Green
