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
    $runName = "{0}-complete-set" -f (Get-Date -Format "yyyyMMdd-HHmmss")
    $OutputDirectory = Join-Path $studioDataRoot (Join-Path "workspace\article-figure-runs" $runName)
} elseif (-not [System.IO.Path]::IsPathRooted($OutputDirectory)) {
    $OutputDirectory = Join-Path $repoRoot $OutputDirectory
}
$resolvedOutputDirectory = [System.IO.Path]::GetFullPath($OutputDirectory)
New-Item -ItemType Directory -Force -Path $resolvedOutputDirectory | Out-Null

$previousSourcePath = $env:ARTICLE_SCIENTIFIC_FIGURE_SET_SOURCE_PATH
$previousOutputDirectory = $env:ARTICLE_SCIENTIFIC_FIGURE_SET_OUTPUT_DIRECTORY
try {
    $env:ARTICLE_SCIENTIFIC_FIGURE_SET_SOURCE_PATH = $resolvedSourcePath
    $env:ARTICLE_SCIENTIFIC_FIGURE_SET_OUTPUT_DIRECTORY = $resolvedOutputDirectory
    $testName = "ContentDeliveryStudio.Tests.ArticleScientificFigureSetTests.SamplePdf_ProducesCompleteAuditedFigureSetWhenExplicitlyRequested"
    & dotnet test (Join-Path $repoRoot "ContentDeliveryStudio.sln") --filter "FullyQualifiedName=$testName"
    if ($LASTEXITCODE -ne 0) {
        throw "Article scientific figure-set run failed."
    }
} finally {
    $env:ARTICLE_SCIENTIFIC_FIGURE_SET_SOURCE_PATH = $previousSourcePath
    $env:ARTICLE_SCIENTIFIC_FIGURE_SET_OUTPUT_DIRECTORY = $previousOutputDirectory
}

$requiredFiles = @(
    "article-figure-set-plan.json",
    "source-figure-audit.json",
    "article-figure-set-report.json",
    "01-secondary-imaging.svg",
    "01-secondary-imaging.png",
    "01-secondary-imaging.pdf",
    "02-lens-equation.svg",
    "02-lens-equation.png",
    "02-lens-equation.pdf",
    "03-screen-retina.svg",
    "03-screen-retina.png",
    "03-screen-retina.pdf",
    "04-observation-position.svg",
    "04-observation-position.png",
    "04-observation-position.pdf",
    "05-corrective-lens.svg",
    "05-corrective-lens.png",
    "05-corrective-lens.pdf",
    "06-source-evidence-board.png"
)
foreach ($prefix in @(
    "01-secondary-imaging",
    "02-lens-equation",
    "03-screen-retina",
    "04-observation-position",
    "05-corrective-lens",
    "06-source-evidence-board")) {
    $requiredFiles += "$prefix.visual-review.json"
}

$missing = @($requiredFiles | Where-Object {
    -not (Test-Path -LiteralPath (Join-Path $resolvedOutputDirectory $_) -PathType Leaf)
})
if ($missing.Count -gt 0) {
    throw "Article run did not produce required files: $($missing -join ', ')"
}

$report = Get-Content -Raw -LiteralPath (Join-Path $resolvedOutputDirectory "article-figure-set-report.json") |
    ConvertFrom-Json
if (-not $report.complete -or $report.resultCount -ne 6 -or $report.requestedCandidateCount -ne 6 `
    -or $report.deterministicReview -ne "article-optics-v1" `
    -or $report.gateOneStatus -ne "pending for every candidate") {
    throw "Article figure-set report is incomplete."
}

foreach ($prefix in @(
    "01-secondary-imaging",
    "02-lens-equation",
    "03-screen-retina",
    "04-observation-position",
    "05-corrective-lens",
    "06-source-evidence-board")) {
    $review = Get-Content -Raw -LiteralPath (Join-Path $resolvedOutputDirectory "$prefix.visual-review.json") |
        ConvertFrom-Json
    if (-not $review.deterministicScientificPassed `
        -or $review.deterministicScientificPackage -ne "article-optics-v1" `
        -or $review.gateOneStatus -ne "PendingHumanApproval" `
        -or @($review.expectedVisualChecks).Count -eq 0 `
        -or @($review.typedCrops).Count -eq 0) {
        throw "Article review evidence is incomplete: $prefix"
    }
}

Write-Host "[OK] Six-item article candidate set persisted under workspace: $resolvedOutputDirectory" -ForegroundColor Green
Write-Host "[OK] Every PNG has article-optics-v1, typed-crop, and fake-first visual-review evidence." -ForegroundColor Green
Write-Host "[BOUNDARY] Scientific Gate 1, live multimodal review, expert acceptance, Gate 2, and delivery are not complete." -ForegroundColor Yellow
