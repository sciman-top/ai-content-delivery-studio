param(
    [Parameter(Mandatory = $true)]
    [ValidateNotNullOrEmpty()]
    [string]$SourcePath,
    [string]$OutputDirectory
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$repoRoot = (& git rev-parse --show-toplevel 2>$null)
if ($LASTEXITCODE -ne 0 -or [string]::IsNullOrWhiteSpace($repoRoot)) {
    throw "Failed to resolve repository root with git rev-parse --show-toplevel."
}

$repoRoot = $repoRoot.Trim()
$studioDataRoot = if ([string]::IsNullOrWhiteSpace($env:CONTENT_DELIVERY_STUDIO_DATA_ROOT)) {
    Join-Path ([Environment]::GetFolderPath([Environment+SpecialFolder]::LocalApplicationData)) "ContentDeliveryStudio"
} else {
    [System.IO.Path]::GetFullPath($env:CONTENT_DELIVERY_STUDIO_DATA_ROOT)
}
$resolvedSourcePath = [System.IO.Path]::GetFullPath($SourcePath)
if (-not (Test-Path -LiteralPath $resolvedSourcePath -PathType Leaf)) {
    throw "Article source PDF was not found: $resolvedSourcePath"
}
if ([System.IO.Path]::GetExtension($resolvedSourcePath) -ine ".pdf") {
    throw "Article source must be a PDF: $resolvedSourcePath"
}

if ([string]::IsNullOrWhiteSpace($OutputDirectory)) {
    $OutputDirectory = Join-Path $studioDataRoot (Join-Path "workspace\article-figure-runs" (Get-Date -Format "yyyyMMdd-HHmmss"))
} elseif (-not [System.IO.Path]::IsPathRooted($OutputDirectory)) {
    $OutputDirectory = Join-Path $repoRoot $OutputDirectory
}
$resolvedOutputDirectory = [System.IO.Path]::GetFullPath($OutputDirectory)
New-Item -ItemType Directory -Force -Path $resolvedOutputDirectory | Out-Null

$previousSourcePath = $env:ARTICLE_SCIENTIFIC_FIGURE_SOURCE_PATH
$previousOutputDirectory = $env:ARTICLE_SCIENTIFIC_FIGURE_OUTPUT_DIRECTORY
try {
    $env:ARTICLE_SCIENTIFIC_FIGURE_SOURCE_PATH = $resolvedSourcePath
    $env:ARTICLE_SCIENTIFIC_FIGURE_OUTPUT_DIRECTORY = $resolvedOutputDirectory
    & dotnet test (Join-Path $repoRoot "ContentDeliveryStudio.sln") --filter ArticleScientificFigurePlanningTests
    if ($LASTEXITCODE -ne 0) {
        throw "Article scientific figure candidate run failed."
    }
} finally {
    $env:ARTICLE_SCIENTIFIC_FIGURE_SOURCE_PATH = $previousSourcePath
    $env:ARTICLE_SCIENTIFIC_FIGURE_OUTPUT_DIRECTORY = $previousOutputDirectory
}

$reportPath = Join-Path $resolvedOutputDirectory "article-scientific-figure-report.json"
$previewPath = Join-Path $resolvedOutputDirectory "candidate-01-secondary-lens-imaging-path.svg"
$workflowPath = Join-Path $resolvedOutputDirectory "approved-scientific-workflow.json"
$reviewPath = Join-Path $resolvedOutputDirectory "machine-review.json"
$approvedSvgPath = Join-Path $resolvedOutputDirectory "approved-mechanism.svg"
$figurePngPath = Join-Path $resolvedOutputDirectory "approved-mechanism.png"
$figurePdfPath = Join-Path $resolvedOutputDirectory "approved-mechanism.pdf"
if (@($reportPath, $previewPath, $workflowPath, $reviewPath, $approvedSvgPath, $figurePngPath, $figurePdfPath).Where({ -not (Test-Path -LiteralPath $_ -PathType Leaf) }).Count -gt 0) {
    throw "Article run did not produce the required candidate, approved-workflow, review, PNG, and PDF artifacts."
}

Write-Host "[OK] Article candidate run persisted under workspace: $resolvedOutputDirectory" -ForegroundColor Green
Write-Host "[OK] Gate 1, deterministic contract review, semantic review, and visual review were persisted." -ForegroundColor Green
Write-Host "[NEXT] A separate explicit human Gate 2 decision is required before delivery-package creation." -ForegroundColor Yellow
