param(
    [string]$OutputPath
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$repoRoot = (& git rev-parse --show-toplevel 2>$null)
if ($LASTEXITCODE -ne 0 -or [string]::IsNullOrWhiteSpace($repoRoot)) {
    throw "Failed to resolve repository root with git rev-parse --show-toplevel."
}

$repoRoot = $repoRoot.Trim()
if ([string]::IsNullOrWhiteSpace($OutputPath)) {
    $stamp = [DateTimeOffset]::UtcNow.ToString("yyyyMMdd-HHmmss")
    $OutputPath = Join-Path $repoRoot "artifacts/scientific-figure-live-acceptance/$stamp/report.json"
} elseif (-not [System.IO.Path]::IsPathRooted($OutputPath)) {
    $OutputPath = Join-Path $repoRoot $OutputPath
}

$resolvedOutputPath = [System.IO.Path]::GetFullPath($OutputPath)
$previousOptIn = $env:CONTENT_DELIVERY_STUDIO_RUN_SCIENTIFIC_LIVE_ACCEPTANCE
$previousReportPath = $env:SCIENTIFIC_FIGURE_LIVE_ACCEPTANCE_REPORT_PATH
try {
    $env:CONTENT_DELIVERY_STUDIO_RUN_SCIENTIFIC_LIVE_ACCEPTANCE = "1"
    $env:SCIENTIFIC_FIGURE_LIVE_ACCEPTANCE_REPORT_PATH = $resolvedOutputPath
    & dotnet test (Join-Path $repoRoot "ContentDeliveryStudio.sln") `
        --filter OpenAiScientificFigureLiveAcceptanceTests
    if ($LASTEXITCODE -ne 0) {
        throw "Scientific figure live-provider acceptance run failed."
    }
} finally {
    $env:CONTENT_DELIVERY_STUDIO_RUN_SCIENTIFIC_LIVE_ACCEPTANCE = $previousOptIn
    $env:SCIENTIFIC_FIGURE_LIVE_ACCEPTANCE_REPORT_PATH = $previousReportPath
}

if (-not (Test-Path -LiteralPath $resolvedOutputPath -PathType Leaf)) {
    throw "Scientific figure live-provider report was not written: $resolvedOutputPath"
}

$report = Get-Content -Raw -LiteralPath $resolvedOutputPath | ConvertFrom-Json
if (-not $report.passedMachinePath -or -not $report.readyForHumanReview -or $report.samples.Count -ne 3) {
    throw "Scientific figure live-provider report did not reach human review readiness."
}

Write-Host "[OK] Scientific live-provider review bundles written: $resolvedOutputPath" -ForegroundColor Green
