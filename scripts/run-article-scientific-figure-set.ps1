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
$buildMutex = [System.Threading.Mutex]::new($false, "ContentDeliveryStudio.ArticleFigureProduction.Build")
$buildMutexAcquired = $false
try {
    try {
        $buildMutexAcquired = $buildMutex.WaitOne([TimeSpan]::FromMinutes(10))
    } catch [System.Threading.AbandonedMutexException] {
        $buildMutexAcquired = $true
    }
    if (-not $buildMutexAcquired) {
        throw "Timed out waiting for the shared article-figure build gate."
    }
    & dotnet @toolArguments
    if ($LASTEXITCODE -ne 0) {
        throw "Article scientific figure-set run failed."
    }
} finally {
    if ($buildMutexAcquired) {
        $buildMutex.ReleaseMutex()
    }
    $buildMutex.Dispose()
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
    -or $report.deterministicReview -notin @("article-optics-v1", "article-thermal-v1", "article-gravity-v1", "article-thermistor-v1", "article-archimedes-v1", "article-bernoulli-v1", "article-pinhole-v1", "article-superconducting-v1", "article-meter-trial-v1", "article-boiling-bubbles-v1", "article-galilean-eyepiece-v1", "article-dry-ice-v1", "article-lever-forces-v1", "article-rest-definition-v1") `
    -or $report.gateOneStatus -ne "pending for every candidate") {
    throw "Article figure-set report is incomplete."
}

$reviewsByPath = @{}
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
    $reviewsByPath[$reviewFile] = $review
}

if (-not [string]::IsNullOrWhiteSpace($report.highStandardProfile)) {
    function Test-SvgElementVisible {
        param([Parameter(Mandatory = $true)][System.Xml.XmlElement]$Element)

        $current = $Element
        while ($null -ne $current -and $current -is [System.Xml.XmlElement]) {
            if ($null -ne $current.SelectSingleNode("ancestor::*[local-name()='defs']") -or
                $current.GetAttribute("display") -eq "none" -or
                $current.GetAttribute("visibility") -in @("hidden", "collapse") -or
                $current.GetAttribute("opacity") -eq "0") {
                return $false
            }
            foreach ($declaration in ($current.GetAttribute("style") -split ";")) {
                $parts = $declaration -split ":", 2
                if ($parts.Count -ne 2) { continue }
                $property = $parts[0].Trim().ToLowerInvariant()
                $value = $parts[1].Trim().ToLowerInvariant()
                if (($property -eq "display" -and $value -eq "none") -or
                    ($property -eq "visibility" -and $value -in @("hidden", "collapse")) -or
                    ($property -eq "opacity" -and $value -eq "0")) {
                    return $false
                }
            }
            $current = $current.ParentNode
        }
        return $true
    }

    $graphicElementNames = @("path", "rect", "circle", "ellipse", "line", "polyline", "polygon")
    foreach ($item in @($report.items)) {
        $svgFiles = @($item.files | Where-Object { $_ -like "*.svg" })
        if ($svgFiles.Count -eq 0) { continue }
        $reviewFiles = @($item.files | Where-Object { $_ -like "*.visual-review.json" })
        if ($reviewFiles.Count -ne 1) {
            throw "Article high-standard semantic gate requires exactly one visual-review sidecar for '$($item.kind)'."
        }
        $expectation = $reviewsByPath[$reviewFiles[0]].highStandardContract
        if ($null -eq $expectation) {
            throw "Article high-standard semantic gate has no effective contract for candidate kind '$($item.kind)'."
        }
        if ($expectation.packageId -ne $report.deterministicReview -or $expectation.candidateKind -ne $item.kind) {
            throw "Article high-standard contract identity drifted for '$($item.kind)'."
        }
        foreach ($svgFile in $svgFiles) {
        [xml]$svg = Get-Content -Raw -LiteralPath (Join-Path $resolvedOutputDirectory $svgFile)
        $graphicNodes = @($svg.SelectNodes("//*") | Where-Object {
            $_.LocalName -in $graphicElementNames -and (Test-SvgElementVisible $_
            )
        })
        $articleRoles = @($svg.SelectNodes("//*[@data-article-role]") | ForEach-Object {
            if (Test-SvgElementVisible $_) { $_.GetAttribute("data-article-role") }
        } | Where-Object { -not [string]::IsNullOrWhiteSpace($_) } | Sort-Object -Unique)
        $missingRoles = @($expectation.RequiredRoles | Where-Object { $_ -notin $articleRoles })
        $concreteRoles = @($expectation.RequiredConcreteObjectRoles | Where-Object { $_ -notin $articleRoles })
        $connectionIds = @($svg.SelectNodes("//*[@data-article-connection]") | ForEach-Object {
            if (Test-SvgElementVisible $_) { $_.GetAttribute("data-article-connection") }
        } | Where-Object { -not [string]::IsNullOrWhiteSpace($_) } | Sort-Object -Unique)
        $missingConnections = @($expectation.RequiredConnectionIds | Where-Object { $_ -notin $connectionIds })
        if ($graphicNodes.Count -lt $expectation.MinimumGraphicCount -or $articleRoles.Count -lt 3 -or $missingRoles.Count -gt 0 -or $concreteRoles.Count -gt 0 -or $missingConnections.Count -gt 0) {
            $missing = if ($missingRoles.Count -gt 0) { $missingRoles -join ", " } else { "none" }
            $concrete = if ($concreteRoles.Count -gt 0) { $concreteRoles -join ", " } else { "none" }
            $connections = if ($missingConnections.Count -gt 0) { $missingConnections -join ", " } else { "none" }
            throw "Article high-standard semantic gate failed for ${svgFile} ($($item.kind)): graphicNodes=$($graphicNodes.Count), articleRoles=$($articleRoles.Count), missingRoles=$missing, missingConcreteObjects=$concrete, missingConnections=$connections. The figure must contain visible apparatus/objects, causal relations, and a clear visual focus; label-only or structurally empty artwork is not an illustration."
        }
        }
    }
}

$effectiveOutputClass = if ($outputDirectoryWasExplicit) { "Explicit" } else { $OutputClass }
Write-Host "[OK] $($report.resultCount)-item article candidate set persisted: $resolvedOutputDirectory" -ForegroundColor Green
Write-Host "[OK] Every candidate has $($report.deterministicReview), typed-crop, and fake-first visual-review evidence." -ForegroundColor Green
Write-Host "[CLASS] $effectiveOutputClass; this location is not a final-delivery package." -ForegroundColor Cyan
Write-Host "[BOUNDARY] Scientific Gate 1, live multimodal review, expert acceptance, Gate 2, and delivery are not complete." -ForegroundColor Yellow
