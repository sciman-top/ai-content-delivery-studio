param(
    [switch]$Check,
    [string]$ExternalShelfManifestPath,
    [string]$ExternalShelfSnapshotPath
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

function Get-EntryValue {
    param(
        [Parameter(Mandatory = $true)]
        [psobject]$Entry,

        [Parameter(Mandatory = $true)]
        [string]$Name,

        $Default = $null
    )

    $property = $Entry.PSObject.Properties[$Name]
    if ($null -eq $property) {
        return $Default
    }

    return $property.Value
}

function Format-StableScalar {
    param($Value)

    if ($null -eq $Value) {
        return ""
    }

    if ($Value -is [datetime]) {
        return $Value.ToString("yyyy-MM-ddTHH:mm:ssK")
    }

    return [string]$Value
}

function Normalize-Text {
    param([string]$Text)

    if ($null -eq $Text) {
        return ""
    }

    return ($Text -replace "`r`n", "`n") -replace "`r", "`n"
}

function Write-Utf8NoBom {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path,

        [Parameter(Mandatory = $true)]
        [string]$Content
    )

    $parent = Split-Path -Parent $Path
    if (-not [string]::IsNullOrWhiteSpace($parent)) {
        New-Item -ItemType Directory -Force -Path $parent | Out-Null
    }

    [System.IO.File]::WriteAllText($Path, $Content, [System.Text.UTF8Encoding]::new($false))
}

function Join-Lines {
    param([System.Collections.Generic.List[string]]$Lines)

    return (($Lines -join "`n") + "`n")
}

function Format-Code {
    param([Parameter(Mandatory = $true)][string]$Text)

    return ([string][char]96) + $Text + ([string][char]96)
}

function Format-InlineList {
    param(
        [Parameter(Mandatory = $true)]
        [object[]]$Values
    )

    $rendered = @(
        $Values |
            Where-Object { $null -ne $_ -and -not [string]::IsNullOrWhiteSpace([string]$_) } |
            ForEach-Object { Format-Code -Text ([string]$_) }
    )

    if ($rendered.Count -eq 0) {
        return "_none_"
    }

    return ($rendered -join ", ")
}

function Render-ReferenceBasisManagedBlock {
    param(
        [Parameter(Mandatory = $true)]
        [psobject]$Manifest
    )

    $lines = [System.Collections.Generic.List[string]]::new()
    $lines.Add("<!-- BEGIN GENERATED REFERENCE BASIS SUMMARY -->")
    $lines.Add("")
    $lines.Add("## Machine-Checked Summary")
    $lines.Add("")
    $lines.Add("This section is generated from " + (Format-Code -Text "scripts/reference-basis.json") + " by " + (Format-Code -Text "scripts/sync-reference-governance.ps1") + ".")
    $lines.Add("Do not edit this block by hand. Update the JSON manifest and rerun the sync script instead.")
    $lines.Add("")
    $lines.Add("- Manifest version: " + (Format-Code -Text (Format-StableScalar -Value $Manifest.version)))
    $lines.Add("- Manifest updatedAt: " + (Format-Code -Text (Format-StableScalar -Value $Manifest.updatedAt)))
    $lines.Add("")

    foreach ($area in @($Manifest.areas)) {
        $lines.Add("### " + (Format-Code -Text ([string]$area.name)))
        $lines.Add("")
        $lines.Add("- " + (Format-Code -Text "required") + ": " + (Format-Code -Text $(if ([bool]$area.required) { 'true' } else { 'false' })))
        $lines.Add("- Source rules: $(Format-InlineList -Values @($area.sourceRules))")
        $lines.Add("- Evidence rules: $(Format-InlineList -Values @($area.evidenceRules))")
        $lines.Add("- Required triggers: $(Format-InlineList -Values @($area.requiredTriggers))")
        $lines.Add("- Local references:")

        foreach ($reference in @($area.localReferences)) {
            $kind = [string](Get-EntryValue -Entry $reference -Name "kind" -Default "")
            $reuseLevel = [string](Get-EntryValue -Entry $reference -Name "reuseLevel" -Default "")
            $detailParts = @()
            if (-not [string]::IsNullOrWhiteSpace($kind)) {
                $detailParts += "kind: " + (Format-Code -Text $kind)
            }

            if (-not [string]::IsNullOrWhiteSpace($reuseLevel)) {
                $detailParts += "reuse: " + (Format-Code -Text $reuseLevel)
            }

            if ($detailParts.Count -gt 0) {
                $lines.Add("  - " + (Format-Code -Text ([string]$reference.path)) + " (" + ($detailParts -join "; ") + ")")
            }
            else {
                $lines.Add("  - " + (Format-Code -Text ([string]$reference.path)))
            }
        }

        $lines.Add("")
    }

    $lines.Add("<!-- END GENERATED REFERENCE BASIS SUMMARY -->")
    return Join-Lines -Lines $lines
}

function Set-ManagedBlock {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path,

        [Parameter(Mandatory = $true)]
        [string]$Content,

        [Parameter(Mandatory = $true)]
        [string]$Replacement,

        [Parameter(Mandatory = $true)]
        [string]$InsertionHeading,

        [switch]$Check
    )

    $pattern = "(?s)<!-- BEGIN GENERATED REFERENCE BASIS SUMMARY -->.*?<!-- END GENERATED REFERENCE BASIS SUMMARY -->"
    if ($Content -match $pattern) {
        $updated = [System.Text.RegularExpressions.Regex]::Replace($Content, $pattern, [System.Text.RegularExpressions.MatchEvaluator]{ param($m) $Replacement.TrimEnd("`r", "`n") }, 1)
    }
    else {
        $insertionPattern = [System.Text.RegularExpressions.Regex]::Escape($InsertionHeading)
        if ($Content -match $insertionPattern) {
            $updated = [System.Text.RegularExpressions.Regex]::Replace(
                $Content,
                $insertionPattern,
                [System.Text.RegularExpressions.MatchEvaluator]{ param($m) $Replacement.TrimEnd("`r", "`n") + "`n`n" + $m.Value },
                1
            )
        }
        else {
            $updated = ($Content.TrimEnd() + "`n`n" + $Replacement.TrimEnd("`r", "`n") + "`n")
        }
    }

    $updated = [System.Text.RegularExpressions.Regex]::Replace(
        $updated,
        "(?s)(<!-- END GENERATED REFERENCE BASIS SUMMARY -->)\s*(## Reference Areas)",
        '$1' + "`n`n" + '$2',
        1
    )

    if ($Check) {
        $normalizedUpdated = Normalize-Text -Text $updated
        $normalizedContent = Normalize-Text -Text $Content
        if ($normalizedUpdated -ne $normalizedContent) {
            $firstDifference = 0
            $sharedLength = [Math]::Min($normalizedUpdated.Length, $normalizedContent.Length)
            while ($firstDifference -lt $sharedLength -and $normalizedUpdated[$firstDifference] -eq $normalizedContent[$firstDifference]) {
                $firstDifference++
            }

            $expectedCodePoint = if ($firstDifference -lt $normalizedUpdated.Length) { [int][char]$normalizedUpdated[$firstDifference] } else { -1 }
            $actualCodePoint = if ($firstDifference -lt $normalizedContent.Length) { [int][char]$normalizedContent[$firstDifference] } else { -1 }
            throw "Reference basis document is out of sync with scripts/reference-basis.json (first_difference=$firstDifference; expected_code_point=$expectedCodePoint; actual_code_point=$actualCodePoint; expected_length=$($normalizedUpdated.Length); actual_length=$($normalizedContent.Length)). Run .\scripts\sync-reference-governance.ps1."
        }

        return
    }

    if ((Normalize-Text -Text $updated) -ne (Normalize-Text -Text $Content)) {
        Write-Utf8NoBom -Path $Path -Content $updated
    }
}

function Get-ExternalShelfSnapshotObject {
    param(
        [Parameter(Mandatory = $true)]
        [string]$ManifestPath
    )

    $manifest = Get-Content -LiteralPath $ManifestPath -Raw | ConvertFrom-Json
    $entries = @(
        @($manifest.entries) |
            Sort-Object relativePath |
            ForEach-Object {
                $storageMode = [string](Get-EntryValue -Entry $_ -Name "storageMode" -Default "")
                $isSharedBacked = [bool](Get-EntryValue -Entry $_ -Name "isSharedBacked" -Default $false)
                if ([string]::IsNullOrWhiteSpace($storageMode)) {
                    $storageMode = if ($isSharedBacked) { "shared-junction" } else { "direct" }
                }

                $entry = [ordered]@{
                    relativePath = [string]$_.relativePath
                    group = [string]$_.group
                    category = [string]$_.category
                    sourceType = [string]$_.sourceType
                    upstream = [string]$_.upstream
                    branch = [string](Get-EntryValue -Entry $_ -Name "branch" -Default "")
                    updateStrategy = [string](Get-EntryValue -Entry $_ -Name "updateStrategy" -Default "")
                    storageMode = $storageMode
                    isSharedBacked = $isSharedBacked
                    lastVerifiedCommit = [string](Get-EntryValue -Entry $_ -Name "lastVerifiedCommit" -Default "")
                }

                $sharedTargetPath = [string](Get-EntryValue -Entry $_ -Name "sharedTargetPath" -Default "")
                if (-not [string]::IsNullOrWhiteSpace($sharedTargetPath)) {
                    $entry.sharedTargetPath = $sharedTargetPath
                }

                [pscustomobject]$entry
            }
    )

    return [pscustomobject]([ordered]@{
        version = 1
        snapshotKind = "external-reference-shelf"
        sourceManifestPath = $ManifestPath
        sourceManifestUpdatedAt = Format-StableScalar -Value $manifest.updatedAt
        entryCount = $entries.Count
        entries = $entries
    })
}

function Get-StableJson {
    param(
        [Parameter(Mandatory = $true)]
        [object]$Value
    )

    return (($Value | ConvertTo-Json -Depth 8) + "`n")
}

$repoRoot = Get-RepoRoot
$referenceBasisPath = Join-Path $repoRoot "docs/REFERENCE_BASIS.md"
$referenceBasisManifestPath = Join-Path $repoRoot "scripts/reference-basis.json"

if ([string]::IsNullOrWhiteSpace($ExternalShelfManifestPath)) {
    $ExternalShelfManifestPath = "D:\CODE\external\ai-content-delivery-studio-references\references.manifest.json"
}

if ([string]::IsNullOrWhiteSpace($ExternalShelfSnapshotPath)) {
    $ExternalShelfSnapshotPath = Join-Path $repoRoot "scripts/external-reference-shelf.snapshot.json"
}

if (-not (Test-Path -LiteralPath $referenceBasisManifestPath)) {
    throw "Missing reference basis manifest: $referenceBasisManifestPath"
}

if (-not (Test-Path -LiteralPath $referenceBasisPath)) {
    throw "Missing reference basis document: $referenceBasisPath"
}

$referenceBasisManifest = Get-Content -LiteralPath $referenceBasisManifestPath -Raw | ConvertFrom-Json
$managedBlock = Render-ReferenceBasisManagedBlock -Manifest $referenceBasisManifest
$referenceBasisContent = Get-Content -LiteralPath $referenceBasisPath -Raw

Set-ManagedBlock `
    -Path $referenceBasisPath `
    -Content $referenceBasisContent `
    -Replacement $managedBlock `
    -InsertionHeading "## Reference Areas" `
    -Check:$Check

if (-not (Test-Path -LiteralPath $ExternalShelfManifestPath)) {
    Write-Host "[SKIP] External reference shelf manifest not found: $ExternalShelfManifestPath"
    exit 0
}

$snapshotObject = Get-ExternalShelfSnapshotObject -ManifestPath $ExternalShelfManifestPath
$snapshotJson = Get-StableJson -Value $snapshotObject

if ($Check) {
    if (-not (Test-Path -LiteralPath $ExternalShelfSnapshotPath)) {
        throw "Missing external reference shelf snapshot: $ExternalShelfSnapshotPath. Run .\scripts\sync-reference-governance.ps1."
    }

    $existingSnapshot = Get-Content -LiteralPath $ExternalShelfSnapshotPath -Raw
    if ((Normalize-Text -Text $existingSnapshot) -ne (Normalize-Text -Text $snapshotJson)) {
        throw "External reference shelf snapshot is out of sync with $ExternalShelfManifestPath. Run .\scripts\sync-reference-governance.ps1."
    }

    Write-Host "[OK] Reference governance files are in sync." -ForegroundColor Green
    exit 0
}

Write-Utf8NoBom -Path $ExternalShelfSnapshotPath -Content $snapshotJson
Write-Host "Reference governance files synced." -ForegroundColor Green
