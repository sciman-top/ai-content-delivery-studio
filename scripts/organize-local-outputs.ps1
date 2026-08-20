param(
    [switch]$WhatIf
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$repoRoot = (& git rev-parse --show-toplevel 2>$null)
if ($LASTEXITCODE -ne 0 -or [string]::IsNullOrWhiteSpace($repoRoot)) {
    throw "Failed to resolve repository root with git rev-parse --show-toplevel."
}

$repoRoot = [System.IO.Path]::GetFullPath($repoRoot.Trim())
$outputsRoot = [System.IO.Path]::GetFullPath((Join-Path $repoRoot "outputs"))
$deliveriesRoot = [System.IO.Path]::GetFullPath((Join-Path $repoRoot "deliveries"))
$moves = [System.Collections.Generic.List[object]]::new()

function Assert-ManagedPath {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path,
        [Parameter(Mandatory = $true)]
        [string[]]$AllowedRoots
    )

    $resolved = [System.IO.Path]::GetFullPath($Path)
    foreach ($root in $AllowedRoots) {
        $normalizedRoot = [System.IO.Path]::TrimEndingDirectorySeparator(
            [System.IO.Path]::GetFullPath($root))
        if ($resolved.StartsWith(
                $normalizedRoot + [System.IO.Path]::DirectorySeparatorChar,
                [System.StringComparison]::OrdinalIgnoreCase)) {
            return $resolved
        }
    }

    throw "Managed path escaped the repository output roots: $resolved"
}

function Move-ManagedItem {
    param(
        [Parameter(Mandatory = $true)]
        [string]$SourceRelativePath,
        [Parameter(Mandatory = $true)]
        [string]$DestinationRelativePath
    )

    $source = Assert-ManagedPath `
        -Path (Join-Path $outputsRoot $SourceRelativePath) `
        -AllowedRoots @($outputsRoot)
    $destination = Assert-ManagedPath `
        -Path (Join-Path $outputsRoot $DestinationRelativePath) `
        -AllowedRoots @($outputsRoot)
    if (-not (Test-Path -LiteralPath $source)) {
        return
    }
    if (Test-Path -LiteralPath $destination) {
        throw "Destination already exists; refusing to merge output histories: $destination"
    }

    $moves.Add([pscustomobject]@{
        source = $SourceRelativePath.Replace('\', '/')
        destination = $DestinationRelativePath.Replace('\', '/')
    })
    if ($WhatIf) {
        return
    }

    $parent = Split-Path -Parent $destination
    New-Item -ItemType Directory -Force -Path $parent | Out-Null
    Move-Item -LiteralPath $source -Destination $destination
}

New-Item -ItemType Directory -Force -Path $outputsRoot | Out-Null

# These are machine-complete candidates, not final deliveries. Their reports
# continue to carry Gate 1 pending, Gate 2 not-run, and delivery not-created.
Move-ManagedItem `
    -SourceRelativePath "codex-validation-20260820\snow-melting-article-v5" `
    -DestinationRelativePath "review-ready\article-figure-sets\snow-melting\20260820-v5"
Move-ManagedItem `
    -SourceRelativePath "codex-validation-20260820\eye-lens-article-v5" `
    -DestinationRelativePath "review-ready\article-figure-sets\eye-lens\20260820-v5"

Move-ManagedItem `
    -SourceRelativePath "article-scientific-figure-runs" `
    -DestinationRelativePath "workspace-history\article-figure-sets"
Move-ManagedItem `
    -SourceRelativePath "codex-validation-20260819" `
    -DestinationRelativePath "validation\article-figure-sets\20260819"
Move-ManagedItem `
    -SourceRelativePath "codex-validation-20260820" `
    -DestinationRelativePath "validation\article-figure-sets\20260820"
Move-ManagedItem `
    -SourceRelativePath "scientific-figure-operator-trials" `
    -DestinationRelativePath "operator-trials\scientific-figures"
Move-ManagedItem `
    -SourceRelativePath "test-scientific-figure-operator-trials" `
    -DestinationRelativePath "operator-trials\test-scientific-figures"
Move-ManagedItem `
    -SourceRelativePath "provider-smoke" `
    -DestinationRelativePath "diagnostics\provider-smoke"
Move-ManagedItem `
    -SourceRelativePath "operator-queue-controls" `
    -DestinationRelativePath "diagnostics\operator-queue-controls"
Move-ManagedItem `
    -SourceRelativePath "verification" `
    -DestinationRelativePath "diagnostics\verification"
Move-ManagedItem `
    -SourceRelativePath "delivery-selector-packaged-accessibility-probe.json" `
    -DestinationRelativePath "diagnostics\accessibility\delivery-selector-packaged-accessibility-probe.json"
Move-ManagedItem `
    -SourceRelativePath "phase7-packaged-accessibility-probe.json" `
    -DestinationRelativePath "diagnostics\accessibility\phase7-packaged-accessibility-probe.json"

if ($WhatIf) {
    $moves | Format-Table -AutoSize
    Write-Host "[WHATIF] No files were moved." -ForegroundColor Yellow
    exit 0
}

@(
    $outputsRoot,
    (Join-Path $outputsRoot "review-ready"),
    (Join-Path $outputsRoot "validation"),
    (Join-Path $outputsRoot "workspace-history"),
    (Join-Path $outputsRoot "operator-trials"),
    (Join-Path $outputsRoot "diagnostics"),
    $deliveriesRoot,
    (Join-Path $deliveriesRoot "article-figure-sets")
) | ForEach-Object { New-Item -ItemType Directory -Force -Path $_ | Out-Null }

$managedMappings = @(
    [ordered]@{ source = "codex-validation-20260820/snow-melting-article-v5"; destination = "review-ready/article-figure-sets/snow-melting/20260820-v5" },
    [ordered]@{ source = "codex-validation-20260820/eye-lens-article-v5"; destination = "review-ready/article-figure-sets/eye-lens/20260820-v5" },
    [ordered]@{ source = "article-scientific-figure-runs"; destination = "workspace-history/article-figure-sets" },
    [ordered]@{ source = "codex-validation-20260819"; destination = "validation/article-figure-sets/20260819" },
    [ordered]@{ source = "codex-validation-20260820"; destination = "validation/article-figure-sets/20260820" },
    [ordered]@{ source = "scientific-figure-operator-trials"; destination = "operator-trials/scientific-figures" },
    [ordered]@{ source = "test-scientific-figure-operator-trials"; destination = "operator-trials/test-scientific-figures" },
    [ordered]@{ source = "provider-smoke"; destination = "diagnostics/provider-smoke" },
    [ordered]@{ source = "operator-queue-controls"; destination = "diagnostics/operator-queue-controls" },
    [ordered]@{ source = "verification"; destination = "diagnostics/verification" },
    [ordered]@{ source = "delivery-selector-packaged-accessibility-probe.json"; destination = "diagnostics/accessibility/delivery-selector-packaged-accessibility-probe.json" },
    [ordered]@{ source = "phase7-packaged-accessibility-probe.json"; destination = "diagnostics/accessibility/phase7-packaged-accessibility-probe.json" }
)
$catalog = [ordered]@{
    schemaVersion = 1
    generatedAt = [DateTimeOffset]::Now.ToString("O")
    finalDeliveryRoot = $deliveriesRoot
    finalDeliveryRule = "Only Gate-2-approved immutable packages belong under deliveries/."
    reviewReadyRoot = Join-Path $outputsRoot "review-ready"
    reviewReadyRule = "Machine-complete candidates awaiting scientific Gate 1 and final Gate 2 approval."
    validationRoot = Join-Path $outputsRoot "validation"
    workspaceHistoryRoot = Join-Path $outputsRoot "workspace-history"
    operatorTrialsRoot = Join-Path $outputsRoot "operator-trials"
    diagnosticsRoot = Join-Path $outputsRoot "diagnostics"
    managedMappings = $managedMappings
    lastRunMoves = $moves
}
$catalogJson = $catalog | ConvertTo-Json -Depth 6
[System.IO.File]::WriteAllText(
    (Join-Path $outputsRoot "OUTPUT-CATALOG.json"),
    $catalogJson + [Environment]::NewLine,
    [System.Text.UTF8Encoding]::new($false))

$outputsReadme = @"
OUTPUTS - NON-FINAL LOCAL ARTIFACTS

review-ready/       Latest machine-complete candidates awaiting Gate 1 and Gate 2.
validation/         Historical regression and Codex validation runs.
workspace-history/  Older generation workspaces and source extraction assets.
operator-trials/    Operator/manual workflow trials and their evidence.
diagnostics/        Provider smoke, accessibility probes, verification, and queue diagnostics.

FINAL DELIVERIES ARE NOT STORED HERE.
Only Gate-2-approved immutable packages belong in:
$deliveriesRoot

See OUTPUT-CATALOG.json for machine-readable roots and the migration receipt.
"@
[System.IO.File]::WriteAllText(
    (Join-Path $outputsRoot "README.txt"),
    $outputsReadme,
    [System.Text.UTF8Encoding]::new($false))

$deliveriesReadme = @"
FINAL DELIVERIES

This directory is reserved for immutable, Gate-2-approved delivery packages.
Current machine-complete article candidates remain under:
$outputsRoot\review-ready\article-figure-sets

Do not copy validation runs, crops, source assets, or fake-provider review evidence here.
The application delivery writer creates categorized packages with images/, prompts/,
metadata/, manifest.json, manifest.csv, and review-report.md after human approval.
"@
[System.IO.File]::WriteAllText(
    (Join-Path $deliveriesRoot "README.txt"),
    $deliveriesReadme,
    [System.Text.UTF8Encoding]::new($false))

Write-Host "[OK] Local outputs were classified without deleting any artifact." -ForegroundColor Green
Write-Host "[FINAL] $deliveriesRoot" -ForegroundColor Cyan
Write-Host "[REVIEW-READY] $(Join-Path $outputsRoot 'review-ready')" -ForegroundColor Cyan
