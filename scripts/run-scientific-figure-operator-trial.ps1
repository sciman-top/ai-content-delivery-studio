param(
    [ValidateSet("Prepare", "Run", "Finalize")]
    [string]$Mode = "Run",
    [string]$RunId,
    [string]$SessionPath,
    [string]$PackagePath,
    [ValidateSet("accepted", "rejected")]
    [string]$Outcome,
    [string]$Reviewer,
    [string]$Notes,
    [switch]$ConfirmFiveWorkspaces,
    [switch]$NoBuild,
    [switch]$NoLaunch
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$requiredPackageEntries = @(
    "figure.svg",
    "figure.png",
    "figure.pdf",
    "specification.json",
    "claim-evidence-item-map.json",
    "reviews.json",
    "repairs.json",
    "providers.json",
    "approvals.json",
    "manifest.json"
)
$requiredWorkspaces = @(
    "Source",
    "Understanding",
    "FigureSpec",
    "RenderAndReview",
    "Delivery"
)

function Resolve-RepositoryRoot {
    $root = (& git -C $PSScriptRoot rev-parse --show-toplevel 2>$null)
    if ($LASTEXITCODE -ne 0 -or [string]::IsNullOrWhiteSpace($root)) {
        throw "Failed to resolve repository root from script directory $PSScriptRoot."
    }

    return [System.IO.Path]::GetFullPath($root.Trim())
}

function Assert-PathWithinRoot {
    param(
        [Parameter(Mandatory)]
        [string]$Path,
        [Parameter(Mandatory)]
        [string]$Root,
        [Parameter(Mandatory)]
        [string]$Description
    )

    $normalizedPath = [System.IO.Path]::GetFullPath($Path)
    $normalizedRoot = [System.IO.Path]::GetFullPath($Root).TrimEnd(
        [System.IO.Path]::DirectorySeparatorChar,
        [System.IO.Path]::AltDirectorySeparatorChar)
    $rootPrefix = $normalizedRoot + [System.IO.Path]::DirectorySeparatorChar
    if (-not $normalizedPath.StartsWith($rootPrefix, [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "$Description must stay within $normalizedRoot."
    }

    return $normalizedPath
}

function Write-TrialRecord {
    param(
        [Parameter(Mandatory)]
        [object]$Trial,
        [Parameter(Mandatory)]
        [string]$Path
    )

    $temporaryPath = "$Path.$([Guid]::NewGuid().ToString('N')).tmp"
    try {
        $Trial | ConvertTo-Json -Depth 20 | Set-Content -LiteralPath $temporaryPath -Encoding utf8
        Move-Item -LiteralPath $temporaryPath -Destination $Path -Force
    } finally {
        if (Test-Path -LiteralPath $temporaryPath) {
            Remove-Item -LiteralPath $temporaryPath -Force
        }
    }
}

function Write-OperatorChecklist {
    param(
        [Parameter(Mandatory)]
        [string]$Path,
        [Parameter(Mandatory)]
        [string]$ExpectedPackagePath
    )

    @"
# Scientific Figure Fake-First Operator Trial

This checklist is a human operation record. Automated tests do not complete it.

1. Confirm the title bar shows fake provider mode.
2. Open Scientific Figures and inspect Source evidence and extraction status.
3. Inspect Understanding claims, exact evidence, conflicts, and limitations.
4. Inspect Figure Spec elements, relations, provenance, and Gate 1 authority.
5. Inspect Render & Review SVG/PNG/PDF previews and all three review layers.
6. In Delivery, compare formats, provider metadata, repairs, and both gates.
7. Enter the human reviewer and non-empty notes, then approve or reject Gate 2.
8. If approved, export the package to: $ExpectedPackagePath
9. Close the app and run Finalize with the same reviewer, outcome, and notes.

An accepted package is not live-provider evidence. This trial excludes OCR-heavy
sources, measured or fabricated data plots, microscope-like observations,
automatic scientific-meaning changes, and generated visuals represented as
observed evidence.
"@ | Set-Content -LiteralPath $Path -Encoding utf8
}

function New-TrialSession {
    param(
        [Parameter(Mandatory)]
        [string]$RepositoryRoot,
        [Parameter(Mandatory)]
        [string]$OutputsRoot
    )

    if ([string]::IsNullOrWhiteSpace($RunId)) {
        $script:RunId = if ([string]::IsNullOrWhiteSpace($SessionPath)) {
            [DateTimeOffset]::Now.ToString("yyyyMMdd-HHmmss-fff")
        } else {
            Split-Path -Leaf ([System.IO.Path]::GetFullPath($SessionPath))
        }
    }

    if ($RunId -notmatch '^[A-Za-z0-9][A-Za-z0-9._-]{0,63}$') {
        throw "RunId must contain only letters, digits, dot, underscore, or dash."
    }

    $candidateSessionPath = if ([string]::IsNullOrWhiteSpace($SessionPath)) {
        Join-Path $OutputsRoot "scientific-figure-operator-trials/$RunId"
    } elseif ([System.IO.Path]::IsPathRooted($SessionPath)) {
        $SessionPath
    } else {
        Join-Path $RepositoryRoot $SessionPath
    }
    $resolvedSessionPath = Assert-PathWithinRoot `
        -Path $candidateSessionPath `
        -Root $OutputsRoot `
        -Description "Operator trial session"

    if (Test-Path -LiteralPath $resolvedSessionPath) {
        throw "Operator trial session already exists: $resolvedSessionPath"
    }

    $dataRoot = Join-Path $resolvedSessionPath "data"
    $deliveryRoot = Join-Path $resolvedSessionPath "delivery"
    $expectedPackagePath = Join-Path $deliveryRoot "scientific-figure.zip"
    New-Item -ItemType Directory -Path $dataRoot -Force | Out-Null
    New-Item -ItemType Directory -Path $deliveryRoot -Force | Out-Null

    $trial = [ordered]@{
        schemaVersion = 1
        runId = $RunId
        status = "pending_operator"
        evidenceLevel = "pending_operator"
        liveAccepted = $false
        providerMode = "fake"
        createdAt = [DateTimeOffset]::Now.ToString("O")
        updatedAt = [DateTimeOffset]::Now.ToString("O")
        repositoryCommit = (& git -C $RepositoryRoot rev-parse HEAD).Trim()
        sessionPath = $resolvedSessionPath
        dataRoot = $dataRoot
        expectedPackagePath = $expectedPackagePath
        workspaces = $requiredWorkspaces
        operator = $null
        validation = $null
        error = $null
    }

    Write-OperatorChecklist `
        -Path (Join-Path $resolvedSessionPath "operator-checklist.md") `
        -ExpectedPackagePath $expectedPackagePath
    Write-TrialRecord -Trial $trial -Path (Join-Path $resolvedSessionPath "trial.json")
    return $trial
}

function Get-Sha256 {
    param([Parameter(Mandatory)][byte[]]$Bytes)

    return [Convert]::ToHexString([System.Security.Cryptography.SHA256]::HashData($Bytes)).ToLowerInvariant()
}

function Normalize-Sha256 {
    param(
        [Parameter(Mandatory)]
        [string]$Value,
        [Parameter(Mandatory)]
        [string]$Description
    )

    if ($Value -notmatch '^(?:sha256:)?([0-9a-fA-F]{64})$') {
        throw "$Description must be a valid SHA-256 value."
    }

    return $Matches[1].ToLowerInvariant()
}

function Read-ZipEntryBytes {
    param(
        [Parameter(Mandatory)]
        [System.IO.Compression.ZipArchive]$Archive,
        [Parameter(Mandatory)]
        [string]$Name
    )

    $entry = $Archive.GetEntry($Name)
    if ($null -eq $entry) {
        throw "Scientific delivery package entry is missing: $Name"
    }

    $stream = $entry.Open()
    try {
        $memory = [System.IO.MemoryStream]::new()
        try {
            $stream.CopyTo($memory)
            return $memory.ToArray()
        } finally {
            $memory.Dispose()
        }
    } finally {
        $stream.Dispose()
    }
}

function Read-ZipEntryJson {
    param(
        [Parameter(Mandatory)]
        [System.IO.Compression.ZipArchive]$Archive,
        [Parameter(Mandatory)]
        [string]$Name
    )

    $bytes = Read-ZipEntryBytes -Archive $Archive -Name $Name
    return [System.Text.Encoding]::UTF8.GetString($bytes) | ConvertFrom-Json
}

function Test-ScientificDeliveryPackage {
    param(
        [Parameter(Mandatory)]
        [string]$Path,
        [Parameter(Mandatory)]
        [string]$ExpectedReviewer
    )

    Add-Type -AssemblyName System.IO.Compression
    $stream = [System.IO.File]::OpenRead($Path)
    try {
        $archive = [System.IO.Compression.ZipArchive]::new(
            $stream,
            [System.IO.Compression.ZipArchiveMode]::Read,
            $false)
        try {
            $names = [System.Collections.Generic.HashSet[string]]::new(
                [System.StringComparer]::OrdinalIgnoreCase)
            foreach ($entry in $archive.Entries) {
                $normalizedName = $entry.FullName.Replace('\', '/')
                $segments = $normalizedName.Split('/', [System.StringSplitOptions]::RemoveEmptyEntries)
                if ([System.IO.Path]::IsPathRooted($normalizedName) `
                    -or $normalizedName.StartsWith('/') `
                    -or $normalizedName.Contains(':') `
                    -or $segments -contains '..') {
                    throw "Scientific delivery package contains an unsafe entry path: $normalizedName"
                }

                if (-not $names.Add($normalizedName)) {
                    throw "Scientific delivery package contains a duplicate entry: $normalizedName"
                }

                if ($normalizedName -match '(?i)(^|/)(\.env(?:\..*)?|credentials?|secrets?|api[_-]?keys?|tokens?)(/|\.|$)') {
                    throw "Scientific delivery package contains a secret-like entry name: $normalizedName"
                }
            }

            foreach ($requiredEntry in $requiredPackageEntries) {
                if (-not $names.Contains($requiredEntry)) {
                    throw "Scientific delivery package entry is missing: $requiredEntry"
                }
            }

            foreach ($entryName in $names.Where({ $_.EndsWith('.json') -or $_.EndsWith('.svg') })) {
                $text = [System.Text.Encoding]::UTF8.GetString(
                    (Read-ZipEntryBytes -Archive $archive -Name $entryName))
                if ($text -match '(?i)([A-Z]:\\|/Users/|/home/)') {
                    throw "Scientific delivery package contains an absolute local path in: $entryName"
                }
            }

            $approvals = Read-ZipEntryJson -Archive $archive -Name "approvals.json"
            if ($null -eq $approvals.GateTwo `
                -or [string]::IsNullOrWhiteSpace([string]$approvals.GateTwo.Reviewer)) {
                throw "Scientific delivery package does not contain an approved Gate 2 decision."
            }

            if (-not [string]::Equals(
                    [string]$approvals.GateTwo.Reviewer,
                    $ExpectedReviewer,
                    [System.StringComparison]::Ordinal)) {
                throw "Scientific delivery package Gate 2 reviewer does not match the operator trial reviewer."
            }

            $manifest = Read-ZipEntryJson -Archive $archive -Name "manifest.json"
            $svgHash = Get-Sha256 (Read-ZipEntryBytes -Archive $archive -Name "figure.svg")
            $manifestSvgHash = Normalize-Sha256 `
                -Value ([string]$manifest.SvgSha256) `
                -Description "Scientific delivery package SVG hash"
            if (-not [string]::Equals(
                    $svgHash,
                    $manifestSvgHash,
                    [System.StringComparison]::OrdinalIgnoreCase)) {
                throw "Scientific delivery package SVG hash does not match manifest.json."
            }

            foreach ($format in @("png", "pdf")) {
                $hashProperty = $manifest.ArtifactSha256.PSObject.Properties[$format]
                if ($null -eq $hashProperty) {
                    throw "Scientific delivery package manifest is missing the $format hash."
                }

                $artifactHash = Get-Sha256 (
                    Read-ZipEntryBytes -Archive $archive -Name "figure.$format")
                $manifestArtifactHash = Normalize-Sha256 `
                    -Value ([string]$hashProperty.Value) `
                    -Description "Scientific delivery package $format hash"
                if (-not [string]::Equals(
                        $artifactHash,
                        $manifestArtifactHash,
                        [System.StringComparison]::OrdinalIgnoreCase)) {
                    throw "Scientific delivery package $format hash does not match manifest.json."
                }
            }

            return [ordered]@{
                passed = $true
                entryCount = $archive.Entries.Count
                packageSha256 = Get-Sha256 ([System.IO.File]::ReadAllBytes($Path))
                reviewerMatched = $true
                hashesMatched = $true
            }
        } finally {
            $archive.Dispose()
        }
    } finally {
        $stream.Dispose()
    }
}

function Complete-Trial {
    param(
        [Parameter(Mandatory)]
        [string]$RepositoryRoot,
        [Parameter(Mandatory)]
        [string]$OutputsRoot
    )

    if ([string]::IsNullOrWhiteSpace($SessionPath)) {
        throw "Finalize requires -SessionPath."
    }

    $candidateSessionPath = if ([System.IO.Path]::IsPathRooted($SessionPath)) {
        $SessionPath
    } else {
        Join-Path $RepositoryRoot $SessionPath
    }
    $resolvedSessionPath = Assert-PathWithinRoot `
        -Path $candidateSessionPath `
        -Root $OutputsRoot `
        -Description "Operator trial session"
    $trialPath = Join-Path $resolvedSessionPath "trial.json"
    if (-not (Test-Path -LiteralPath $trialPath -PathType Leaf)) {
        throw "Operator trial record was not found: $trialPath"
    }

    $trial = Get-Content -Raw -LiteralPath $trialPath | ConvertFrom-Json
    if ($trial.status -in @("accepted", "rejected")) {
        throw "Operator trial is already finalized: $($trial.status)"
    }

    if (-not $ConfirmFiveWorkspaces) {
        throw "Finalize requires explicit confirmation of all five scientific workspaces."
    }

    if ([string]::IsNullOrWhiteSpace($Reviewer) -or [string]::IsNullOrWhiteSpace($Notes)) {
        throw "Finalize requires non-empty -Reviewer and -Notes."
    }

    if ([string]::IsNullOrWhiteSpace($Outcome)) {
        throw "Finalize requires -Outcome accepted or rejected."
    }

    $validation = $null
    $resolvedPackagePath = $null
    if ($Outcome -eq "accepted") {
        $candidatePackagePath = if ([string]::IsNullOrWhiteSpace($PackagePath)) {
            [string]$trial.expectedPackagePath
        } elseif ([System.IO.Path]::IsPathRooted($PackagePath)) {
            $PackagePath
        } else {
            Join-Path $RepositoryRoot $PackagePath
        }
        $resolvedPackagePath = Assert-PathWithinRoot `
            -Path $candidatePackagePath `
            -Root (Join-Path $resolvedSessionPath "delivery") `
            -Description "Scientific delivery package"
        if (-not (Test-Path -LiteralPath $resolvedPackagePath -PathType Leaf)) {
            throw "Accepted operator trial requires a delivery ZIP: $resolvedPackagePath"
        }

        if ([System.IO.Path]::GetExtension($resolvedPackagePath) -ne ".zip") {
            throw "Accepted operator trial package must use the .zip extension."
        }

        $validation = Test-ScientificDeliveryPackage `
            -Path $resolvedPackagePath `
            -ExpectedReviewer $Reviewer.Trim()
    } elseif (-not [string]::IsNullOrWhiteSpace($PackagePath)) {
        throw "Rejected operator trial must not attach a delivery package."
    }

    $now = [DateTimeOffset]::Now.ToString("O")
    $trial.status = $Outcome
    $trial.evidenceLevel = "operator/manual evidence"
    $trial.updatedAt = $now
    $trial.operator = [ordered]@{
        reviewer = $Reviewer.Trim()
        notes = $Notes.Trim()
        outcome = $Outcome
        completedAt = $now
        confirmedWorkspaces = $requiredWorkspaces
        packagePath = $resolvedPackagePath
    }
    $trial.validation = $validation
    $trial.error = $null
    Write-TrialRecord -Trial $trial -Path $trialPath
    Write-Host "[OK] Scientific figure operator trial finalized: $Outcome" -ForegroundColor Green
    Write-Host "     $trialPath"
}

function Write-FinalizeFailure {
    param(
        [Parameter(Mandatory)]
        [string]$RepositoryRoot,
        [Parameter(Mandatory)]
        [string]$OutputsRoot,
        [Parameter(Mandatory)]
        [string]$FailureMessage
    )

    if ([string]::IsNullOrWhiteSpace($SessionPath)) {
        return
    }

    try {
        $candidateSessionPath = if ([System.IO.Path]::IsPathRooted($SessionPath)) {
            $SessionPath
        } else {
            Join-Path $RepositoryRoot $SessionPath
        }
        $resolvedSessionPath = Assert-PathWithinRoot `
            -Path $candidateSessionPath `
            -Root $OutputsRoot `
            -Description "Operator trial session"
        $trialPath = Join-Path $resolvedSessionPath "trial.json"
        if (-not (Test-Path -LiteralPath $trialPath -PathType Leaf)) {
            return
        }

        $trial = Get-Content -Raw -LiteralPath $trialPath | ConvertFrom-Json
        if ($trial.status -in @("accepted", "rejected")) {
            return
        }

        $trial.error = $FailureMessage
        $trial.updatedAt = [DateTimeOffset]::Now.ToString("O")
        Write-TrialRecord -Trial $trial -Path $trialPath
    } catch {
        Write-Warning "Could not append the finalize failure to the trial record: $($_.Exception.Message)"
    }
}

$repoRoot = Resolve-RepositoryRoot
$outputsRoot = Join-Path $repoRoot "outputs"
New-Item -ItemType Directory -Path $outputsRoot -Force | Out-Null

if ($Mode -eq "Finalize") {
    try {
        Complete-Trial -RepositoryRoot $repoRoot -OutputsRoot $outputsRoot
    } catch {
        $failure = $_
        Write-FinalizeFailure `
            -RepositoryRoot $repoRoot `
            -OutputsRoot $outputsRoot `
            -FailureMessage $failure.Exception.Message
        throw $failure
    }

    exit 0
}

$trial = New-TrialSession -RepositoryRoot $repoRoot -OutputsRoot $outputsRoot
$resolvedSessionPath = [string]$trial.sessionPath
$trialPath = Join-Path $resolvedSessionPath "trial.json"
Write-Host "[OK] Scientific figure operator trial prepared: $resolvedSessionPath" -ForegroundColor Green

if ($Mode -eq "Prepare" -or $NoLaunch) {
    Write-Host "     Status: pending_operator"
    exit 0
}

$previousProviderMode = $env:PROVIDER_MODE
$previousDataRoot = $env:CONTENT_DELIVERY_STUDIO_DATA_ROOT
try {
    $env:PROVIDER_MODE = "fake"
    $env:CONTENT_DELIVERY_STUDIO_DATA_ROOT = [string]$trial.dataRoot
    $trial.status = "operator_in_progress"
    $trial.updatedAt = [DateTimeOffset]::Now.ToString("O")
    Write-TrialRecord -Trial $trial -Path $trialPath

    $arguments = @(
        "run",
        "--project",
        (Join-Path $repoRoot "src/ContentDeliveryStudio.App/ContentDeliveryStudio.App.csproj")
    )
    if ($NoBuild) {
        $arguments += "--no-build"
    }

    & dotnet @arguments
    if ($LASTEXITCODE -ne 0) {
        $trial.status = "run_failed"
        $trial.error = "WPF host exited with code $LASTEXITCODE."
        throw $trial.error
    }

    $trial.status = "awaiting_finalize"
    $trial.updatedAt = [DateTimeOffset]::Now.ToString("O")
    Write-TrialRecord -Trial $trial -Path $trialPath
} finally {
    $env:PROVIDER_MODE = $previousProviderMode
    $env:CONTENT_DELIVERY_STUDIO_DATA_ROOT = $previousDataRoot
    if ($trial.status -eq "run_failed") {
        $trial.updatedAt = [DateTimeOffset]::Now.ToString("O")
        Write-TrialRecord -Trial $trial -Path $trialPath
    }
}

Write-Host "[OK] WPF trial closed. Finalize after completing the human checklist." -ForegroundColor Green
Write-Host "     $trialPath"
