[CmdletBinding()]
param(
    [string]$ProjectRoot = (Split-Path -Parent $PSScriptRoot)
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$router = Join-Path $PSScriptRoot 'invoke-coding-model-route.ps1'
$expected = @{
    'bounded-implementation' = @('gpt-5.6-sol', 'medium', 'workspace-write')
    'read-only-review' = @('gpt-5.6-terra', 'high', 'read-only')
    'isolated-complex-implementation' = @('gpt-5.6-terra', 'xhigh', 'workspace-write')
    'high-risk-adjudication' = @('gpt-5.6-sol', 'xhigh', 'workspace-write')
}

foreach ($workItemKind in $expected.Keys) {
    $plan = & $router -WorkItemKind $workItemKind -ProjectRoot $ProjectRoot | ConvertFrom-Json
    $route = $expected[$workItemKind]

    $isValid = @(
        $plan.schemaVersion -eq 1
        $plan.workItemKind -eq $workItemKind
        $plan.model -eq $route[0]
        $plan.reasoningEffort -eq $route[1]
        $plan.sandbox -eq $route[2]
        $plan.executionMode -eq 'dry-run'
        -not $plan.promptSupplied
        $plan.command[-1] -eq '<prompt>'
    ) -notcontains $false

    if (-not $isValid) {
        throw "Invalid route projection for '$workItemKind'."
    }
}

Write-Output "Coding model routing verified for $($expected.Count) work-item kinds."
