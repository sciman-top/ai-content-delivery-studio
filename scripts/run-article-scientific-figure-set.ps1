param(
    [Parameter(Mandatory = $true)]
    [ValidateNotNullOrEmpty()]
    [string]$SourcePath,
    [string]$OutputDirectory,
    [ValidateSet("Workspace", "Validation", "ReviewReady")]
    [string]$OutputClass = "Workspace",
    [string]$ArticleSlug,
    [string]$RunName,
    [switch]$ResolveOutputDirectoryOnly
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

$outputDirectoryWasExplicit = -not [string]::IsNullOrWhiteSpace($OutputDirectory)
if (-not $outputDirectoryWasExplicit) {
    function ConvertTo-SafeDirectoryName {
        param(
            [Parameter(Mandatory = $true)]
            [string]$Value,
            [Parameter(Mandatory = $true)]
            [string]$ParameterName
        )

        $invalidFileNameChars = [System.IO.Path]::GetInvalidFileNameChars()
        $normalized = -join @($Value.Trim().ToCharArray() | ForEach-Object {
            if ($invalidFileNameChars -contains $_) { '-' } else { $_ }
        })
        $normalized = $normalized.Trim().TrimEnd('.')
        if ([string]::IsNullOrWhiteSpace($normalized) -or
            [System.IO.Path]::IsPathRooted($normalized) -or
            $normalized.Contains([System.IO.Path]::DirectorySeparatorChar) -or
            $normalized.Contains([System.IO.Path]::AltDirectorySeparatorChar) -or
            ($normalized -in @('.', '..'))) {
            throw "$ParameterName must resolve to one safe directory name."
        }

        return $normalized
    }

    $safeArticleSlug = if ([string]::IsNullOrWhiteSpace($ArticleSlug)) {
        ConvertTo-SafeDirectoryName `
            -Value ([System.IO.Path]::GetFileNameWithoutExtension($resolvedSourcePath)) `
            -ParameterName "ArticleSlug"
    } else {
        ConvertTo-SafeDirectoryName -Value $ArticleSlug -ParameterName "ArticleSlug"
    }
    $safeRunName = if ([string]::IsNullOrWhiteSpace($RunName)) {
        Get-Date -Format "yyyyMMdd-HHmmss"
    } else {
        ConvertTo-SafeDirectoryName -Value $RunName -ParameterName "RunName"
    }

    $classRoot = switch ($OutputClass) {
        "Workspace" {
            Join-Path $studioDataRoot "workspace\article-figure-runs"
        }
        "Validation" {
            Join-Path $repoRoot "outputs\validation\article-figure-sets"
        }
        "ReviewReady" {
            Join-Path $repoRoot "outputs\review-ready\article-figure-sets"
        }
    }
    $OutputDirectory = Join-Path (Join-Path $classRoot $safeArticleSlug) $safeRunName
} elseif (-not [System.IO.Path]::IsPathRooted($OutputDirectory)) {
    $OutputDirectory = Join-Path $repoRoot $OutputDirectory
}
$resolvedOutputDirectory = [System.IO.Path]::GetFullPath($OutputDirectory)
if ($ResolveOutputDirectoryOnly) {
    Write-Output $resolvedOutputDirectory
    return
}
New-Item -ItemType Directory -Force -Path $resolvedOutputDirectory | Out-Null

$toolArguments = @(
    "run",
    "--project", (Join-Path $repoRoot "src\ContentDeliveryStudio.Tools\ContentDeliveryStudio.Tools.csproj"),
    "--",
    "generate-article-figure-set",
    "--source", $resolvedSourcePath,
    "--output", $resolvedOutputDirectory
)
& dotnet @toolArguments
if ($LASTEXITCODE -ne 0) {
    throw "Article scientific figure-set run failed."
}

$reportPath = Join-Path $resolvedOutputDirectory "article-figure-set-report.json"
$report = Get-Content -Raw -LiteralPath $reportPath |
    ConvertFrom-Json
$requiredFiles = @(
    "article-figure-set-plan.json",
    "source-figure-audit.json",
    "article-figure-set-report.json"
) + @($report.items | ForEach-Object { $_.files })
$missing = @($requiredFiles | Sort-Object -Unique | Where-Object {
    -not (Test-Path -LiteralPath (Join-Path $resolvedOutputDirectory $_) -PathType Leaf)
})
if ($missing.Count -gt 0) {
    throw "Article run did not produce required files: $($missing -join ', ')"
}

if (-not $report.complete -or $report.resultCount -ne $report.requestedCandidateCount `
    -or $report.resultCount -lt 1 `
    -or $report.deterministicReview -notin @("article-optics-v1", "article-thermal-v1", "article-gravity-v1", "article-thermistor-v1", "article-archimedes-v1", "article-bernoulli-v1", "article-pinhole-v1", "article-superconducting-v1") `
    -or $report.gateOneStatus -ne "pending for every candidate") {
    throw "Article figure-set report is incomplete."
}

foreach ($reviewFile in @($report.items | ForEach-Object { $_.files | Where-Object { $_ -like "*.visual-review.json" } })) {
    $review = Get-Content -Raw -LiteralPath (Join-Path $resolvedOutputDirectory $reviewFile) |
        ConvertFrom-Json
    if (-not $review.deterministicScientificPassed `
        -or $review.deterministicScientificPackage -ne $report.deterministicReview `
        -or $review.gateOneStatus -ne "PendingHumanApproval" `
        -or @($review.expectedVisualChecks).Count -eq 0 `
        -or @($review.typedCrops).Count -eq 0) {
        throw "Article review evidence is incomplete: $reviewFile"
    }
}

$semanticUtilityProfiles = @("article-bernoulli-v1", "article-pinhole-v1", "article-superconducting-v1")
if ($report.deterministicReview -in $semanticUtilityProfiles) {
    foreach ($svgFile in @($report.items | ForEach-Object { $_.files | Where-Object { $_ -like "*.svg" } })) {
        [xml]$svg = Get-Content -Raw -LiteralPath (Join-Path $resolvedOutputDirectory $svgFile)
        $graphicNodes = @($svg.SelectNodes("//*[local-name()='path' or local-name()='rect']") | Where-Object {
            -not $_.SelectSingleNode("ancestor::*[local-name()='defs']")
        })
        $articleRoles = @($svg.SelectNodes("//*[@data-article-role]") | ForEach-Object {
            $_.GetAttribute("data-article-role")
        } | Where-Object { -not [string]::IsNullOrWhiteSpace($_) } | Sort-Object -Unique)
        if ($graphicNodes.Count -lt 10 -or $articleRoles.Count -lt 2) {
            throw "Article semantic-utility gate failed for ${svgFile}: graphicNodes=$($graphicNodes.Count), articleRoles=$($articleRoles.Count). Label-only or structurally empty artwork is not an illustration."
        }
    }
}

$effectiveOutputClass = if ($outputDirectoryWasExplicit) { "Explicit" } else { $OutputClass }
Write-Host "[OK] $($report.resultCount)-item article candidate set persisted: $resolvedOutputDirectory" -ForegroundColor Green
Write-Host "[OK] Every candidate has $($report.deterministicReview), typed-crop, and fake-first visual-review evidence." -ForegroundColor Green
Write-Host "[CLASS] $effectiveOutputClass; this location is not a final-delivery package." -ForegroundColor Cyan
Write-Host "[BOUNDARY] Scientific Gate 1, live multimodal review, expert acceptance, Gate 2, and delivery are not complete." -ForegroundColor Yellow
