param(
    [string[]]$Paths,
    [string]$BaseRef,
    [string]$HeadRef
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

function Get-AreaDefinitions {
    return @(
        [pscustomobject]@{
            Name = "openai-provider"
            SourceRules = @(
                "src/ImageSeriesStudio.Infrastructure/OpenAI/",
                "src/ImageSeriesStudio.Core/Providers/"
            )
            EvidenceRules = @(
                "docs/research/REFERENCE_RESEARCH.md",
                "docs/PROVIDER_CONFIGURATION.md",
                "docs/PROVIDER_ROUTING_POLICY.md",
                "docs/V1_LAUNCH_EVIDENCE.md",
                "docs/superpowers/specs/",
                "docs/superpowers/plans/"
            )
            RecommendedReferences = @(
                "D:/CODE/external/ai-content-delivery-studio-references/01-openai",
                "D:/CODE/ai-image-series-studio/docs/research/REFERENCE_RESEARCH.md"
            )
        }
        [pscustomobject]@{
            Name = "host-and-observability"
            SourceRules = @(
                "src/ImageSeriesStudio.App/",
                "src/ImageSeriesStudio.Infrastructure/Diagnostics/",
                "src/ImageSeriesStudio.App/Telemetry/"
            )
            EvidenceRules = @(
                "docs/research/REFERENCE_RESEARCH.md",
                "docs/ARCHITECTURE.md",
                "docs/TARGET_ENGINEERING_STATE.md",
                "docs/V1_LAUNCH_EVIDENCE.md",
                "docs/superpowers/specs/",
                "docs/superpowers/plans/"
            )
            RecommendedReferences = @(
                "D:/CODE/external/ai-content-delivery-studio-references/02-dotnet-wpf",
                "D:/CODE/external/ai-content-delivery-studio-references/08-platform-and-observability",
                "D:/CODE/ai-image-series-studio/docs/research/REFERENCE_RESEARCH.md"
            )
        }
        [pscustomobject]@{
            Name = "persistence-and-schema"
            SourceRules = @(
                "src/ImageSeriesStudio.Infrastructure/Persistence/",
                "src/ImageSeriesStudio.Core/Projects/",
                "src/ImageSeriesStudio.Core/Artifacts/",
                "src/ImageSeriesStudio.Core/Sources/"
            )
            EvidenceRules = @(
                "docs/research/REFERENCE_RESEARCH.md",
                "docs/ARCHITECTURE.md",
                "docs/ROADMAP.md",
                "docs/TARGET_ENGINEERING_STATE.md",
                "docs/superpowers/specs/",
                "docs/superpowers/plans/"
            )
            RecommendedReferences = @(
                "D:/CODE/external/ai-content-delivery-studio-references/03-data-persistence",
                "D:/CODE/ai-image-series-studio/docs/research/REFERENCE_RESEARCH.md"
            )
        }
        [pscustomobject]@{
            Name = "tooling-and-operator"
            SourceRules = @(
                "src/ImageSeriesStudio.Application/ToolAdapters/",
                "src/ImageSeriesStudio.Infrastructure/ToolAdapters/",
                "src/ImageSeriesStudio.Core/Operators/"
            )
            EvidenceRules = @(
                "docs/research/REFERENCE_RESEARCH.md",
                "docs/ARCHITECTURE.md",
                "docs/OPERATOR_RISK_POLICY.md",
                "docs/V1_LAUNCH_EVIDENCE.md",
                "docs/superpowers/specs/",
                "docs/superpowers/plans/"
            )
            RecommendedReferences = @(
                "D:/CODE/external/ai-content-delivery-studio-references/05-document-rendering",
                "D:/CODE/external/ai-content-delivery-studio-references/06-automation-testing",
                "D:/CODE/external/ai-content-delivery-studio-references/07-image-workflow-references",
                "D:/CODE/ai-image-series-studio/docs/research/REFERENCE_RESEARCH.md"
            )
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

$areas = Get-AreaDefinitions
$touchedAreas = @()

foreach ($area in $areas) {
    $triggeringPaths = @($changedPaths | Where-Object { Test-MatchAnyRule -Path $_ -Rules $area.SourceRules })
    if ($triggeringPaths.Count -eq 0) {
        continue
    }

    $evidenceHits = @($changedPaths | Where-Object { Test-MatchAnyRule -Path $_ -Rules $area.EvidenceRules })

    $touchedAreas += [pscustomobject]@{
        Name = $area.Name
        TriggeringPaths = @($triggeringPaths)
        EvidenceHits = @($evidenceHits)
        EvidenceRules = $area.EvidenceRules
        RecommendedReferences = $area.RecommendedReferences
    }
}

if ($touchedAreas.Count -eq 0) {
    Write-Host "[OK] No enforced engineering area was touched. Reference evidence gate passed."
    exit 0
}

$failedAreas = @($touchedAreas | Where-Object { @($_.EvidenceHits).Count -eq 0 })

foreach ($area in $touchedAreas) {
    if (@($area.EvidenceHits).Count -gt 0) {
        Write-Host "[OK] $($area.Name): evidence update detected." -ForegroundColor Green
        foreach ($hit in $area.EvidenceHits) {
            Write-Host "  - $hit"
        }
    } else {
        Write-Host "[FAIL] $($area.Name): no reference evidence update detected." -ForegroundColor Red
        Write-Host "  Triggering paths:"
        foreach ($path in $area.TriggeringPaths) {
            Write-Host "  - $path"
        }
        Write-Host "  Acceptable evidence updates:"
        foreach ($rule in $area.EvidenceRules) {
            Write-Host "  - $rule"
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
