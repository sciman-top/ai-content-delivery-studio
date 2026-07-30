param(
    [ValidateSet('Debug', 'Release')]
    [string] $Configuration = 'Release',

    [string] $Runtime = 'win-x64',

    [string] $OutputDirectory = '',

    [string] $PackagePath = '',

    [switch] $SelfContained,

    [switch] $Clean,

    [switch] $NoRestore,

    [switch] $WhatIfOnly
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repoRoot = Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..')
$projectPath = Join-Path $repoRoot 'src/ContentDeliveryStudio.App/ContentDeliveryStudio.App.csproj'

if ([string]::IsNullOrWhiteSpace($OutputDirectory)) {
    $OutputDirectory = Join-Path $repoRoot "publish/ContentDeliveryStudio.App-$Runtime-$Configuration"
}

$resolvedOutput = [System.IO.Path]::GetFullPath($OutputDirectory)
$repoPublishRoot = [System.IO.Path]::GetFullPath((Join-Path $repoRoot 'publish'))
$repoPublishRootWithSeparator = $repoPublishRoot.TrimEnd(
    [System.IO.Path]::DirectorySeparatorChar,
    [System.IO.Path]::AltDirectorySeparatorChar) + [System.IO.Path]::DirectorySeparatorChar

if (-not $resolvedOutput.StartsWith($repoPublishRootWithSeparator, [System.StringComparison]::OrdinalIgnoreCase)) {
    throw "OutputDirectory must stay under repo publish folder: $repoPublishRoot"
}

if ([string]::IsNullOrWhiteSpace($PackagePath)) {
    $PackagePath = "$resolvedOutput.zip"
}

$resolvedPackagePath = [System.IO.Path]::GetFullPath($PackagePath)
if (-not $resolvedPackagePath.StartsWith($repoPublishRootWithSeparator, [System.StringComparison]::OrdinalIgnoreCase)) {
    throw "PackagePath must stay under repo publish folder: $repoPublishRoot"
}

if ($resolvedPackagePath.StartsWith(
        $resolvedOutput.TrimEnd([System.IO.Path]::DirectorySeparatorChar) + [System.IO.Path]::DirectorySeparatorChar,
        [System.StringComparison]::OrdinalIgnoreCase)) {
    throw 'PackagePath must stay outside OutputDirectory.'
}

$outputParent = Split-Path -Parent $resolvedOutput
$packageParent = Split-Path -Parent $resolvedPackagePath
New-Item -ItemType Directory -Path $outputParent -Force | Out-Null
New-Item -ItemType Directory -Path $packageParent -Force | Out-Null

$selfContainedValue = if ($SelfContained) { 'true' } else { 'false' }
$publishArgs = @(
    'publish',
    $projectPath,
    '--configuration',
    $Configuration,
    '--runtime',
    $Runtime,
    '--self-contained',
    $selfContainedValue,
    '--output',
    $resolvedOutput,
    '-p:PublishSingleFile=false',
    '-p:PublishReadyToRun=false'
)
if ($NoRestore) {
    $publishArgs += '--no-restore'
}

$summary = [ordered]@{
    project = $projectPath
    configuration = $Configuration
    runtime = $Runtime
    selfContained = [bool]$SelfContained
    outputDirectory = $resolvedOutput
    packagePath = $resolvedPackagePath
    command = "dotnet $($publishArgs -join ' ')"
}

$summary | ConvertTo-Json -Depth 4

if ($WhatIfOnly) {
    return
}

if ($Clean -and (Test-Path -LiteralPath $resolvedOutput)) {
    Remove-Item -LiteralPath $resolvedOutput -Recurse -Force
}

foreach ($path in @($resolvedPackagePath, "$resolvedPackagePath.sha256")) {
    if (Test-Path -LiteralPath $path) {
        Remove-Item -LiteralPath $path -Force
    }
}

dotnet @publishArgs
if ($LASTEXITCODE -ne 0) {
    throw "dotnet publish failed with exit code $LASTEXITCODE."
}

$entryExecutable = 'ContentDeliveryStudio.App.exe'
if (-not (Test-Path -LiteralPath (Join-Path $resolvedOutput $entryExecutable))) {
    throw "Published application entry point is missing: $entryExecutable"
}

$payloadFiles = @(
    Get-ChildItem -LiteralPath $resolvedOutput -Recurse -File |
        Where-Object { $_.Name -ne 'publish-manifest.json' } |
        Sort-Object FullName |
        ForEach-Object {
            $relativePath = [System.IO.Path]::GetRelativePath($resolvedOutput, $_.FullName).Replace('\', '/')
            [ordered]@{
                path = $relativePath
                sizeBytes = $_.Length
                sha256 = (Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash.ToLowerInvariant()
            }
        }
)

$manifest = [ordered]@{
    schemaVersion = 1
    product = 'AI Content Delivery Studio'
    configuration = $Configuration
    runtime = $Runtime
    selfContained = [bool]$SelfContained
    targetFramework = 'net10.0-windows'
    entryExecutable = $entryExecutable
    files = $payloadFiles
}

$manifestPath = Join-Path $resolvedOutput 'publish-manifest.json'
$manifest | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath $manifestPath -Encoding utf8

$packageStream = [System.IO.File]::Open(
    $resolvedPackagePath,
    [System.IO.FileMode]::CreateNew,
    [System.IO.FileAccess]::Write,
    [System.IO.FileShare]::None)
try {
    $archive = [System.IO.Compression.ZipArchive]::new(
        $packageStream,
        [System.IO.Compression.ZipArchiveMode]::Create,
        $false)
    try {
        $fixedTimestamp = [System.DateTimeOffset]::new(2000, 1, 1, 0, 0, 0, [System.TimeSpan]::Zero)
        foreach ($file in @(Get-ChildItem -LiteralPath $resolvedOutput -Recurse -File | Sort-Object FullName)) {
            $relativePath = [System.IO.Path]::GetRelativePath($resolvedOutput, $file.FullName).Replace('\', '/')
            $entry = $archive.CreateEntry($relativePath, [System.IO.Compression.CompressionLevel]::Optimal)
            $entry.LastWriteTime = $fixedTimestamp
            $sourceStream = $file.OpenRead()
            $entryStream = $entry.Open()
            try {
                $sourceStream.CopyTo($entryStream)
            }
            finally {
                $entryStream.Dispose()
                $sourceStream.Dispose()
            }
        }
    }
    finally {
        $archive.Dispose()
    }
}
finally {
    $packageStream.Dispose()
}

$packageHash = (Get-FileHash -LiteralPath $resolvedPackagePath -Algorithm SHA256).Hash.ToLowerInvariant()
"$packageHash  $([System.IO.Path]::GetFileName($resolvedPackagePath))" |
    Set-Content -LiteralPath "$resolvedPackagePath.sha256" -Encoding ascii

& (Join-Path $PSScriptRoot 'verify-publish-package.ps1') -PackagePath $resolvedPackagePath | Out-Host

Write-Host "Publish complete: $resolvedOutput"
Write-Host "Package complete: $resolvedPackagePath"
