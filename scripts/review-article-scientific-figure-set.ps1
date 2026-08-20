param(
    [Parameter(Mandatory = $true)]
    [ValidateNotNullOrEmpty()]
    [string]$ReviewReadyDirectory,
    [Parameter(Mandatory = $true)]
    [ValidateNotNullOrEmpty()]
    [string]$Reviewer,
    [Parameter(Mandatory = $true)]
    [ValidateNotNullOrEmpty()]
    [string]$AuthorizationReference,
    [Parameter(Mandatory = $true)]
    [ValidateNotNullOrEmpty()]
    [string]$Notes,
    [Parameter(Mandatory = $true)]
    [switch]$ConfirmEveryCandidateVisuallyInspected,
    [switch]$RequireIndependentHumanExpert,
    [datetimeoffset]$ReviewedAt = [datetimeoffset]::Now
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$repoRoot = (& git rev-parse --show-toplevel 2>$null)
if ($LASTEXITCODE -ne 0 -or [string]::IsNullOrWhiteSpace($repoRoot)) {
    throw "Failed to resolve repository root with git rev-parse --show-toplevel."
}
$repoRoot = $repoRoot.Trim()
if (-not $ConfirmEveryCandidateVisuallyInspected.IsPresent) {
    throw "Authorized-agent review requires explicit -ConfirmEveryCandidateVisuallyInspected."
}

$toolArguments = @(
    "run",
    "--project", (Join-Path $repoRoot "src\ContentDeliveryStudio.Tools\ContentDeliveryStudio.Tools.csproj"),
    "--",
    "assess-article-figure-review",
    "--source", ([System.IO.Path]::GetFullPath($ReviewReadyDirectory)),
    "--reviewer", $Reviewer,
    "--authorization-reference", $AuthorizationReference,
    "--notes", $Notes,
    "--confirm-every-candidate-visually-inspected",
    "--reviewed-at", $ReviewedAt.ToString("O")
)
if ($RequireIndependentHumanExpert.IsPresent) {
    $toolArguments += "--require-independent-human-expert"
}

& dotnet @toolArguments
if ($LASTEXITCODE -ne 0) {
    throw "Article scientific figure-set review assessment failed."
}
