[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [ValidateSet(
        'bounded-implementation',
        'read-only-review',
        'isolated-complex-implementation',
        'high-risk-adjudication')]
    [string]$WorkItemKind,

    [string]$ProjectRoot = (Split-Path -Parent $PSScriptRoot),

    [string]$Prompt,

    [switch]$Execute
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Resolve-ModelRoute {
    param([string]$Kind)

    switch ($Kind) {
        'bounded-implementation' {
            return [pscustomobject]@{
                Model = 'gpt-5.6-sol'
                ReasoningEffort = 'medium'
                Sandbox = 'workspace-write'
            }
        }
        'read-only-review' {
            return [pscustomobject]@{
                Model = 'gpt-5.6-terra'
                ReasoningEffort = 'high'
                Sandbox = 'read-only'
            }
        }
        'isolated-complex-implementation' {
            return [pscustomobject]@{
                Model = 'gpt-5.6-terra'
                ReasoningEffort = 'xhigh'
                Sandbox = 'workspace-write'
            }
        }
        'high-risk-adjudication' {
            return [pscustomobject]@{
                Model = 'gpt-5.6-sol'
                ReasoningEffort = 'xhigh'
                Sandbox = 'workspace-write'
            }
        }
    }

    throw "Unsupported work item kind '$Kind'."
}

$resolvedRoot = (Resolve-Path -LiteralPath $ProjectRoot).Path
$route = Resolve-ModelRoute -Kind $WorkItemKind
$configOverride = 'model_reasoning_effort = "{0}"' -f $route.ReasoningEffort
$plan = [ordered]@{
    schemaVersion = 1
    projectRoot = $resolvedRoot
    workItemKind = $WorkItemKind
    model = $route.Model
    reasoningEffort = $route.ReasoningEffort
    sandbox = $route.Sandbox
    executionMode = if ($Execute) { 'execute' } else { 'dry-run' }
    promptSupplied = -not [string]::IsNullOrWhiteSpace($Prompt)
    command = @(
        'codex',
        'exec',
        '-C', $resolvedRoot,
        '-m', $route.Model,
        '-c', $configOverride,
        '-s', $route.Sandbox,
        '<prompt>')
}

if (-not $Execute) {
    $plan | ConvertTo-Json -Depth 3
    return
}

if ([string]::IsNullOrWhiteSpace($Prompt)) {
    throw '-Prompt is required with -Execute.'
}

$plan | ConvertTo-Json -Depth 3
& codex exec -C $resolvedRoot -m $route.Model -c $configOverride -s $route.Sandbox $Prompt
if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}
