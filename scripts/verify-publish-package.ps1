param(
    [Parameter(Mandatory = $true)]
    [string] $PackagePath
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$maximumEntryCount = 20000
$maximumEntrySizeBytes = 1GB
$maximumTotalSizeBytes = 8GB

$resolvedPackagePath = [System.IO.Path]::GetFullPath($PackagePath)
if (-not (Test-Path -LiteralPath $resolvedPackagePath -PathType Leaf)) {
    throw "Package does not exist: $resolvedPackagePath"
}

function Get-NormalizedArchivePath {
    param([Parameter(Mandatory = $true)][string]$Path)

    if ([string]::IsNullOrWhiteSpace($Path) -or $Path.EndsWith('/') -or $Path.EndsWith('\')) {
        throw "Package contains an unsupported directory or empty entry: $Path"
    }

    $normalized = $Path.Replace('\', '/')
    if ($normalized.StartsWith('/') -or [System.IO.Path]::IsPathRooted($normalized) -or $normalized.Contains(':')) {
        throw "Package entry escapes the archive root: $Path"
    }

    $parts = @($normalized.Split('/'))
    if (@($parts | Where-Object { [string]::IsNullOrWhiteSpace($_) -or $_ -eq '.' -or $_ -eq '..' }).Count -gt 0) {
        throw "Package entry escapes the archive root: $Path"
    }

    return ($parts -join '/')
}

function Get-StreamSha256 {
    param([Parameter(Mandatory = $true)][System.IO.Stream]$Stream)

    $sha = [System.Security.Cryptography.SHA256]::Create()
    try {
        return [System.Convert]::ToHexString($sha.ComputeHash($Stream)).ToLowerInvariant()
    }
    finally {
        $sha.Dispose()
    }
}

$fileStream = [System.IO.File]::OpenRead($resolvedPackagePath)
try {
    $archive = [System.IO.Compression.ZipArchive]::new(
        $fileStream,
        [System.IO.Compression.ZipArchiveMode]::Read,
        $false)
    try {
        $entries = @{}
        $totalSizeBytes = [long]0
        if ($archive.Entries.Count -gt $maximumEntryCount) {
            throw "Package exceeds the supported entry limit of $maximumEntryCount."
        }
        foreach ($entry in $archive.Entries) {
            $normalizedPath = Get-NormalizedArchivePath -Path $entry.FullName
            $unixFileType = ($entry.ExternalAttributes -shr 16) -band 0xF000
            if ($unixFileType -eq 0xA000 -or (($entry.ExternalAttributes -band 0x400) -ne 0)) {
                throw "Package contains an unsupported link-like entry: $normalizedPath"
            }
            if ($entry.Length -gt $maximumEntrySizeBytes) {
                throw "Package entry exceeds the supported size limit: $normalizedPath"
            }
            $totalSizeBytes += [long]$entry.Length
            if ($totalSizeBytes -gt $maximumTotalSizeBytes) {
                throw 'Package exceeds the supported total size limit.'
            }
            if (-not $normalizedPath.Equals('publish-manifest.json', [StringComparison]::OrdinalIgnoreCase) -and
                [System.IO.Path]::GetExtension($normalizedPath).Equals('.zip', [StringComparison]::OrdinalIgnoreCase)) {
                throw "Package contains an unexpected nested ZIP: $normalizedPath"
            }
            $key = $normalizedPath.ToLowerInvariant()
            if ($entries.ContainsKey($key)) {
                throw "Package contains a duplicate entry path: $normalizedPath"
            }
            $entries[$key] = [ordered]@{ path = $normalizedPath; entry = $entry }
        }

        $manifestKey = 'publish-manifest.json'
        if (-not $entries.ContainsKey($manifestKey)) {
            throw 'Package is missing publish-manifest.json.'
        }

        $manifestEntry = $entries[$manifestKey].entry
        if ($manifestEntry.Length -gt 4MB) {
            throw 'Package manifest exceeds the supported size limit.'
        }

        $reader = [System.IO.StreamReader]::new($manifestEntry.Open(), [System.Text.Encoding]::UTF8)
        try {
            $manifest = ($reader.ReadToEnd() | ConvertFrom-Json -Depth 20)
        }
        finally {
            $reader.Dispose()
        }

        if ($manifest.schemaVersion -ne 1) {
            throw "Unsupported publish manifest schema: $($manifest.schemaVersion)"
        }
        if ([string]::IsNullOrWhiteSpace($manifest.entryExecutable)) {
            throw 'Publish manifest does not declare an entry executable.'
        }

        $manifestFiles = @{}
        foreach ($file in @($manifest.files)) {
            $normalizedPath = Get-NormalizedArchivePath -Path ([string]$file.path)
            $key = $normalizedPath.ToLowerInvariant()
            if ($key -eq $manifestKey -or $manifestFiles.ContainsKey($key)) {
                throw "Publish manifest contains a duplicate file path: $normalizedPath"
            }
            if ([long]$file.sizeBytes -lt 0 -or ([string]$file.sha256) -notmatch '^[0-9a-fA-F]{64}$') {
                throw "Publish manifest contains invalid metadata: $normalizedPath"
            }
            $manifestFiles[$key] = $file
        }

        if ($entries.Count -ne $manifestFiles.Count + 1) {
            throw 'Package payload membership does not match publish-manifest.json.'
        }

        foreach ($key in $manifestFiles.Keys) {
            if (-not $entries.ContainsKey($key)) {
                throw "Package payload is missing: $($manifestFiles[$key].path)"
            }

            $entry = $entries[$key].entry
            $expected = $manifestFiles[$key]
            if ($entry.Length -ne [long]$expected.sizeBytes) {
                throw "Package payload size does not match its manifest: $($expected.path)"
            }

            $entryStream = $entry.Open()
            try {
                $actualHash = Get-StreamSha256 -Stream $entryStream
            }
            finally {
                $entryStream.Dispose()
            }
            if ($actualHash -ne ([string]$expected.sha256).ToLowerInvariant()) {
                throw "Package payload hash does not match its manifest: $($expected.path)"
            }
        }

        foreach ($requiredPath in @(
                [string]$manifest.entryExecutable,
                'ContentDeliveryStudio.App.deps.json',
                'ContentDeliveryStudio.App.runtimeconfig.json')) {
            if (-not $manifestFiles.ContainsKey($requiredPath.ToLowerInvariant())) {
                throw "Package is missing required application file: $requiredPath"
            }
        }
    }
    finally {
        $archive.Dispose()
    }
}
finally {
    $fileStream.Dispose()
}

$sidecarPath = "$resolvedPackagePath.sha256"
if (Test-Path -LiteralPath $sidecarPath -PathType Leaf) {
    $sidecar = (Get-Content -LiteralPath $sidecarPath -Raw).Trim()
    if ($sidecar -notmatch '^([0-9a-fA-F]{64})\s{2}(.+)$') {
        throw 'Package SHA-256 sidecar has an invalid format.'
    }
    $expectedPackageHash = $Matches[1].ToLowerInvariant()
    $expectedFileName = $Matches[2]
    if ($expectedFileName -ne [System.IO.Path]::GetFileName($resolvedPackagePath)) {
        throw 'Package SHA-256 sidecar names a different file.'
    }
    $actualPackageHash = (Get-FileHash -LiteralPath $resolvedPackagePath -Algorithm SHA256).Hash.ToLowerInvariant()
    if ($actualPackageHash -ne $expectedPackageHash) {
        throw 'Package SHA-256 sidecar does not match the archive.'
    }
}

[ordered]@{
    schemaVersion = 1
    packagePath = $resolvedPackagePath
    packageSha256 = (Get-FileHash -LiteralPath $resolvedPackagePath -Algorithm SHA256).Hash.ToLowerInvariant()
    payloadFileCount = $manifestFiles.Count
    entryExecutable = [string]$manifest.entryExecutable
    runtime = [string]$manifest.runtime
    selfContained = [bool]$manifest.selfContained
} | ConvertTo-Json -Depth 4
