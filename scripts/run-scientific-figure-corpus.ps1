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
    $OutputPath = Join-Path $repoRoot "artifacts/scientific-figure-corpus-acceptance/report.json"
} elseif (-not [System.IO.Path]::IsPathRooted($OutputPath)) {
    $OutputPath = Join-Path $repoRoot $OutputPath
}

$resolvedOutputPath = [System.IO.Path]::GetFullPath($OutputPath)
$outputDirectory = Split-Path -Parent $resolvedOutputPath
New-Item -ItemType Directory -Force -Path $outputDirectory | Out-Null

$previousReportPath = $env:SCIENTIFIC_FIGURE_CORPUS_REPORT_PATH
try {
    $env:SCIENTIFIC_FIGURE_CORPUS_REPORT_PATH = $resolvedOutputPath
    & dotnet test (Join-Path $repoRoot "ContentDeliveryStudio.sln") `
        --filter ScientificFigureCorpusAcceptanceTests
    if ($LASTEXITCODE -ne 0) {
        throw "Scientific figure corpus acceptance tests failed."
    }
} finally {
    $env:SCIENTIFIC_FIGURE_CORPUS_REPORT_PATH = $previousReportPath
}

if (-not (Test-Path -LiteralPath $resolvedOutputPath -PathType Leaf)) {
    throw "Scientific figure corpus report was not written: $resolvedOutputPath"
}

$report = Get-Content -Raw -LiteralPath $resolvedOutputPath | ConvertFrom-Json
if (-not $report.passed -or $report.itemCount -ne 12) {
    throw "Scientific figure corpus report did not pass all 12 accepted baselines."
}

Write-Host "[OK] Scientific figure corpus acceptance passed: $resolvedOutputPath" -ForegroundColor Green
