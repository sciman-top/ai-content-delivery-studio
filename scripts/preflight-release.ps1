param(
    [switch]$SkipReferenceEvidence,
    [switch]$SkipPublishWhatIf,
    [switch]$NoRestore,
    [switch]$ForcePowerShellTextScan,
    [string]$ReferenceEvidenceBaseRef,
    [string]$ReferenceEvidenceHeadRef
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$repoRoot = (& git rev-parse --show-toplevel 2>$null)
if ($LASTEXITCODE -ne 0 -or [string]::IsNullOrWhiteSpace($repoRoot)) {
    throw "Failed to resolve repository root with git rev-parse --show-toplevel."
}

$repoRoot = $repoRoot.Trim()
Set-Location $repoRoot

function Invoke-Step {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Label,

        [Parameter(Mandatory = $true)]
        [scriptblock]$Action
    )

    Write-Host "==> $Label" -ForegroundColor Cyan
    & $Action
    if ($LASTEXITCODE -ne 0) {
        throw "Step failed: $Label"
    }
}

function Invoke-RgFilteredCheck {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Pattern,

        [Parameter(Mandatory = $true)]
        [string[]]$Targets,

        [string[]]$IgnorePatterns = @()
    )

    $rgCommand = if ($ForcePowerShellTextScan) {
        $null
    } else {
        Get-Command rg -CommandType Application -ErrorAction SilentlyContinue | Select-Object -First 1
    }

    if ($null -ne $rgCommand) {
        $raw = & $rgCommand.Source -n $Pattern @Targets
        $exitCode = $LASTEXITCODE
        if ($exitCode -eq 1) {
            $global:LASTEXITCODE = 0
            return @()
        }

        if ($exitCode -ne 0) {
            throw "rg failed with exit code $exitCode."
        }

        $lines = @($raw | ForEach-Object { $_.ToString() })
    } else {
        Write-Host "rg is unavailable; using the tracked-file PowerShell scanner." -ForegroundColor Yellow
        $trackedFiles = @(& git ls-files -- @Targets)
        $exitCode = $LASTEXITCODE
        if ($exitCode -ne 0) {
            throw "git ls-files failed with exit code $exitCode."
        }

        $lines = @(
            foreach ($trackedFile in $trackedFiles) {
                $relativePath = $trackedFile.ToString()
                if ([string]::IsNullOrWhiteSpace($relativePath)) {
                    continue
                }

                if (-not (Test-Path -LiteralPath $relativePath -PathType Leaf)) {
                    continue
                }

                foreach ($match in @(Select-String -LiteralPath $relativePath -Pattern $Pattern -AllMatches -CaseSensitive -Encoding utf8)) {
                    "{0}:{1}:{2}" -f $relativePath, $match.LineNumber, $match.Line
                }
            }
        )
        $global:LASTEXITCODE = 0
    }

    if ($IgnorePatterns.Count -gt 0) {
        $lines = @(
            $lines | Where-Object {
                $line = $_
                -not ($IgnorePatterns | Where-Object { $line -match $_ } | Select-Object -First 1)
            }
        )
    }

    foreach ($line in $lines) {
        Write-Host $line
    }

    return $lines
}

function Get-ChangedPaths {
    $paths = [System.Collections.Generic.HashSet[string]]::new(
        [System.StringComparer]::OrdinalIgnoreCase)
    $gitQueries = @(
        @("diff", "--name-only", "--diff-filter=ACMR"),
        @("diff", "--cached", "--name-only", "--diff-filter=ACMR"),
        @("ls-files", "--others", "--exclude-standard")
    )

    if (-not [string]::IsNullOrWhiteSpace($ReferenceEvidenceBaseRef) -and
        -not [string]::IsNullOrWhiteSpace($ReferenceEvidenceHeadRef)) {
        $gitQueries += ,@(
            "diff",
            "--name-only",
            "--diff-filter=ACMR",
            "$ReferenceEvidenceBaseRef...$ReferenceEvidenceHeadRef")
    }

    foreach ($query in $gitQueries) {
        $output = @(& git @query)
        if ($LASTEXITCODE -ne 0) {
            throw "Failed to discover changed paths with: git $($query -join ' ')"
        }

        foreach ($path in $output) {
            $relativePath = $path.ToString().Trim()
            if (-not [string]::IsNullOrWhiteSpace($relativePath) -and
                (Test-Path -LiteralPath $relativePath -PathType Leaf)) {
                $null = $paths.Add($relativePath)
            }
        }
    }

    return @($paths | Sort-Object)
}

Invoke-Step -Label "Repository verification" -Action {
    $verifyParams = @{
        Mode = "Full"
    }
    if ($NoRestore) {
        $verifyParams.NoRestore = $true
    }
    if ($SkipReferenceEvidence) {
        $verifyParams.SkipReferenceEvidence = $true
    }
    if (-not [string]::IsNullOrWhiteSpace($ReferenceEvidenceBaseRef)) {
        $verifyParams.ReferenceEvidenceBaseRef = $ReferenceEvidenceBaseRef
    }
    if (-not [string]::IsNullOrWhiteSpace($ReferenceEvidenceHeadRef)) {
        $verifyParams.ReferenceEvidenceHeadRef = $ReferenceEvidenceHeadRef
    }

    & ".\scripts\verify-repo.ps1" @verifyParams
}

Invoke-Step -Label "Release-only tests" -Action {
    & dotnet test ContentDeliveryStudio.sln --no-build --no-restore --filter "Category=ReleaseOnly"
}

Invoke-Step -Label "dotnet format --verify-no-changes" -Action {
    $changedPaths = @(Get-ChangedPaths)
    $changedCSharpPaths = @($changedPaths | Where-Object {
        [System.IO.Path]::GetExtension($_) -ieq ".cs"
    })
    $formatConfigurationChanged = @($changedPaths | Where-Object {
        $name = [System.IO.Path]::GetFileName($_)
        $name -in @(".editorconfig", "global.json") -or
        $name -like "*.csproj" -or
        $name -like "Directory.Build.*"
    }).Count -gt 0

    if ($changedPaths.Count -eq 0 -or $formatConfigurationChanged) {
        & dotnet format ContentDeliveryStudio.sln --verify-no-changes --no-restore
        return
    }

    if ($changedCSharpPaths.Count -eq 0) {
        Write-Host "No changed C# files detected; formatting verification is not required."
        $global:LASTEXITCODE = 0
        return
    }

    & dotnet format ContentDeliveryStudio.sln --verify-no-changes --no-restore --include @changedCSharpPaths
}

Invoke-Step -Label "Placeholder scan" -Action {
    $hits = @(Invoke-RgFilteredCheck -Pattern "\b(TBD|TODO|PLACEHOLDER)\b" -Targets @("docs", "src", "tests", "scripts", "README.md", "AGENTS.md") -IgnorePatterns @(
        "^scripts[\\\\/]preflight-release\.ps1:",
        "rg -n .*(TBD|TODO|PLACE''HOLDER)",
        "rg -n .*(TBD|TODO|PLACEHOLDER)",
        "Placeholder scan"
    ))
    if ($hits.Count -gt 0) {
        throw "Placeholder markers detected."
    }
}

Invoke-Step -Label "Merge conflict marker scan" -Action {
    $hits = @(Invoke-RgFilteredCheck -Pattern "^(<<<<<<<|=======|>>>>>>>)" -Targets @("docs", "src", "tests", "scripts", ".github", "README.md", "AGENTS.md"))
    if ($hits.Count -gt 0) {
        throw "Merge conflict markers detected."
    }
}

if (-not $SkipPublishWhatIf) {
    Invoke-Step -Label "Publish package preflight" -Action {
        $preflightRoot = Join-Path $repoRoot 'publish/preflight'
        $preflightOutput = Join-Path $preflightRoot 'ContentDeliveryStudio.App-win-x64-Release'
        $preflightPackage = "$preflightOutput.zip"
        try {
            $publishParams = @{
                Configuration = 'Release'
                Runtime = 'win-x64'
                OutputDirectory = $preflightOutput
                PackagePath = $preflightPackage
                Clean = $true
            }
            if ($NoRestore) {
                $publishParams.NoRestore = $true
            }
            & ".\scripts\publish-app.ps1" @publishParams
            if ($LASTEXITCODE -ne 0) {
                throw 'Publish package creation failed.'
            }
            & ".\scripts\verify-publish-package.ps1" -PackagePath $preflightPackage
        }
        finally {
            if (Test-Path -LiteralPath $preflightRoot) {
                Remove-Item -LiteralPath $preflightRoot -Recurse -Force
            }
        }
    }
}

Write-Host "Release preflight passed." -ForegroundColor Green
