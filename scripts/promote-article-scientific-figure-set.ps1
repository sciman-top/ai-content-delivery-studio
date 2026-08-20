param(
    [Parameter(Mandatory = $true)]
    [ValidateNotNullOrEmpty()]
    [string]$ReviewReadyDirectory,
    [Parameter(Mandatory = $true)]
    [ValidateNotNullOrEmpty()]
    [string]$ArticleSlug,
    [Parameter(Mandatory = $true)]
    [ValidateNotNullOrEmpty()]
    [string]$PackageId,
    [Parameter(Mandatory = $true)]
    [ValidateNotNullOrEmpty()]
    [string]$Reviewer,
    [ValidateSet("human", "authorized_agent")]
    [string]$OperatorKind = "human",
    [string]$AuthorizationReference,
    [Parameter(Mandatory = $true)]
    [switch]$ApproveGateOne,
    [Parameter(Mandatory = $true)]
    [ValidateNotNullOrEmpty()]
    [string]$GateOneNotes,
    [Parameter(Mandatory = $true)]
    [switch]$ApproveGateTwo,
    [Parameter(Mandatory = $true)]
    [ValidateNotNullOrEmpty()]
    [string]$GateTwoNotes,
    [datetimeoffset]$ApprovedAt = [datetimeoffset]::Now,
    [string]$DeliveryRoot
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$repoRoot = (& git rev-parse --show-toplevel 2>$null)
if ($LASTEXITCODE -ne 0 -or [string]::IsNullOrWhiteSpace($repoRoot)) {
    throw "Failed to resolve repository root with git rev-parse --show-toplevel."
}
$repoRoot = $repoRoot.Trim()
if ([string]::IsNullOrWhiteSpace($DeliveryRoot)) {
    $DeliveryRoot = Join-Path $repoRoot "deliveries\article-figure-sets"
}
if (-not $ApproveGateOne.IsPresent -or -not $ApproveGateTwo.IsPresent) {
    throw "Article promotion requires explicit -ApproveGateOne and -ApproveGateTwo switches."
}

$toolArguments = @(
    "run",
    "--project", (Join-Path $repoRoot "src\ContentDeliveryStudio.Tools\ContentDeliveryStudio.Tools.csproj"),
    "--",
    "promote-article-figure-set",
    "--source", ([System.IO.Path]::GetFullPath($ReviewReadyDirectory)),
    "--delivery-root", ([System.IO.Path]::GetFullPath($DeliveryRoot)),
    "--article-slug", $ArticleSlug,
    "--package-id", $PackageId,
    "--reviewer", $Reviewer,
    "--operator-kind", $OperatorKind,
    "--approve-gate-one",
    "--gate-one-notes", $GateOneNotes,
    "--approve-gate-two",
    "--gate-two-notes", $GateTwoNotes,
    "--approved-at", $ApprovedAt.ToString("O")
)
if (-not [string]::IsNullOrWhiteSpace($AuthorizationReference)) {
    $toolArguments += @("--authorization-reference", $AuthorizationReference.Trim())
}

& dotnet @toolArguments
if ($LASTEXITCODE -ne 0) {
    throw "Article scientific figure-set promotion failed."
}
