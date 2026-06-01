param(
    [ValidateSet('Debug', 'Release')]
    [string] $Configuration = 'Release',

    [string] $Runtime = 'win-x64',

    [string] $OutputDirectory = '',

    [switch] $SelfContained,

    [switch] $Clean,

    [switch] $WhatIfOnly
)

$ErrorActionPreference = 'Stop'

$repoRoot = Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..')
$projectPath = Join-Path $repoRoot 'src/ImageSeriesStudio.App/ImageSeriesStudio.App.csproj'

if ([string]::IsNullOrWhiteSpace($OutputDirectory)) {
    $OutputDirectory = Join-Path $repoRoot "publish/ImageSeriesStudio.App-$Runtime-$Configuration"
}

$resolvedOutputParent = Split-Path -Parent $OutputDirectory
if (-not (Test-Path -LiteralPath $resolvedOutputParent)) {
    New-Item -ItemType Directory -Path $resolvedOutputParent | Out-Null
}

$resolvedOutput = [System.IO.Path]::GetFullPath($OutputDirectory)
$repoPublishRoot = [System.IO.Path]::GetFullPath((Join-Path $repoRoot 'publish'))
$repoPublishRootWithSeparator = $repoPublishRoot.TrimEnd([System.IO.Path]::DirectorySeparatorChar, [System.IO.Path]::AltDirectorySeparatorChar) + [System.IO.Path]::DirectorySeparatorChar

if (-not $resolvedOutput.StartsWith($repoPublishRootWithSeparator, [System.StringComparison]::OrdinalIgnoreCase)) {
    throw "OutputDirectory must stay under repo publish folder: $repoPublishRoot"
}

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

$summary = [ordered]@{
    timestamp = (Get-Date).ToString('o')
    project = $projectPath
    configuration = $Configuration
    runtime = $Runtime
    selfContained = [bool]$SelfContained
    outputDirectory = $resolvedOutput
    command = "dotnet $($publishArgs -join ' ')"
}

$summary | ConvertTo-Json -Depth 4

if ($WhatIfOnly) {
    return
}

if ($Clean -and (Test-Path -LiteralPath $resolvedOutput)) {
    Remove-Item -LiteralPath $resolvedOutput -Recurse -Force
}

dotnet @publishArgs

$manifestPath = Join-Path $resolvedOutput 'publish-manifest.json'
$summary | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath $manifestPath -Encoding UTF8

Write-Host "Publish complete: $resolvedOutput"
