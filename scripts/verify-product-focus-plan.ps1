param(
    [string]$QueuePath = "docs/product-focus-execution.json"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$repoRoot = (& git rev-parse --show-toplevel 2>$null)
if ($LASTEXITCODE -ne 0 -or [string]::IsNullOrWhiteSpace($repoRoot)) {
    throw "Failed to resolve repository root with git rev-parse --show-toplevel."
}

$repoRoot = $repoRoot.Trim()
Set-Location $repoRoot

$errors = [System.Collections.Generic.List[string]]::new()

function Add-PlanError {
    param([string]$Message)

    $script:errors.Add($Message)
}

function Test-HasProperty {
    param(
        [Parameter(Mandatory = $true)]$Object,
        [Parameter(Mandatory = $true)][string]$Name
    )

    return $null -ne $Object -and $Object.PSObject.Properties.Name -contains $Name
}

function Test-NonEmptyText {
    param($Value)

    return $Value -is [string] -and -not [string]::IsNullOrWhiteSpace($Value)
}

function Test-ExactSet {
    param(
        [object[]]$Actual,
        [string[]]$Expected,
        [string]$Label
    )

    $actualText = @($Actual | ForEach-Object { $_.ToString() })
    $duplicates = @($actualText | Group-Object | Where-Object Count -gt 1 | ForEach-Object Name)
    if ($duplicates.Count -gt 0) {
        Add-PlanError "$Label contains duplicate value(s): $($duplicates -join ', ')."
    }

    $missing = @($Expected | Where-Object { $_ -notin $actualText })
    $unexpected = @($actualText | Where-Object { $_ -notin $Expected })
    if ($missing.Count -gt 0 -or $unexpected.Count -gt 0) {
        Add-PlanError "$Label set mismatch. Missing: $($missing -join ', '); unexpected: $($unexpected -join ', ')."
    }
}

function Resolve-RepoDocument {
    param(
        [string]$RelativePath,
        [string]$Label
    )

    if (-not (Test-NonEmptyText $RelativePath)) {
        Add-PlanError "$Label must be a non-empty repository-relative path."
        return $null
    }

    if ($RelativePath -match '^[A-Za-z]:[\\/]' -or $RelativePath -match '^[/\\]{1,2}' -or $RelativePath -match '(^|[\\/])\.\.([\\/]|$)') {
        Add-PlanError "$Label must remain repository-relative: $RelativePath"
        return $null
    }

    $resolved = Join-Path $repoRoot $RelativePath
    if (-not (Test-Path -LiteralPath $resolved -PathType Leaf)) {
        Add-PlanError "$Label does not exist: $RelativePath"
        return $null
    }

    return $resolved
}

$queueFile = Resolve-RepoDocument -RelativePath $QueuePath -Label "Queue path"
if ($null -eq $queueFile) {
    throw ($errors -join [Environment]::NewLine)
}

try {
    $plan = Get-Content -LiteralPath $queueFile -Raw -Encoding utf8 | ConvertFrom-Json
} catch {
    throw "Product-focus queue is not valid JSON: $($_.Exception.Message)"
}

if (-not (Test-HasProperty $plan "schemaVersion") -or $plan.schemaVersion -ne 1) {
    Add-PlanError "schemaVersion must be the integer 1."
}

if (-not (Test-HasProperty $plan "planId") -or -not (Test-NonEmptyText $plan.planId)) {
    Add-PlanError "planId must be non-empty."
}

$requiredTopLevelPaths = @("prd", "spec", "implementationPlan")
$resolvedDocuments = @{}
foreach ($property in $requiredTopLevelPaths) {
    if (-not (Test-HasProperty $plan $property)) {
        Add-PlanError "Missing top-level path property: $property."
        continue
    }

    $resolvedDocuments[$property] = Resolve-RepoDocument -RelativePath $plan.$property -Label $property
}

$expectedMaturityStates = @(
    "production-proven",
    "repo-verified",
    "experimental-contract",
    "frozen",
    "excluded"
)
$expectedProductionLanes = @(
    "image-series-production",
    "trustworthy-scientific-figures"
)
$expectedFrozenCapabilities = @(
    "remote-workflows",
    "public-pack-ecosystem",
    "general-operator-platform",
    "additional-provider-abstractions",
    "graph-editor",
    "partial-image-streaming"
)

foreach ($setContract in @(
    @{ Name = "maturityStates"; Expected = $expectedMaturityStates },
    @{ Name = "productionLanes"; Expected = $expectedProductionLanes },
    @{ Name = "frozenCapabilities"; Expected = $expectedFrozenCapabilities }
)) {
    if (-not (Test-HasProperty $plan $setContract.Name)) {
        Add-PlanError "Missing top-level set: $($setContract.Name)."
        continue
    }

    Test-ExactSet -Actual @($plan.($setContract.Name)) -Expected $setContract.Expected -Label $setContract.Name
}

if (-not (Test-HasProperty $plan "tasks") -or @($plan.tasks).Count -eq 0) {
    Add-PlanError "tasks must contain at least one task."
    $tasks = @()
} else {
    $tasks = @($plan.tasks)
}

$validStates = @("completed", "ready", "in_progress", "proposed", "blocked_external")
$validPriorities = @("P0", "P1", "P2")
$validRisks = @("low", "medium", "high")
$validAuthorities = @(
    "repo-only",
    "human-expert",
    "paid-live-approval",
    "repo-only-until-live-probe",
    "manual-hardware",
    "mixed-explicit"
)
$validLanes = @("shared-foundation") + $expectedProductionLanes

$taskById = @{}
$seenOrders = @{}

foreach ($task in $tasks) {
    $taskId = if (Test-HasProperty $task "id") { $task.id } else { $null }
    if (-not (Test-NonEmptyText $taskId) -or $taskId -notmatch '^FOCUS-\d{3}$') {
        Add-PlanError "Every task id must match FOCUS- followed by three digits. Found: '$taskId'."
        continue
    }

    if ($taskById.ContainsKey($taskId)) {
        Add-PlanError "Duplicate task id: $taskId."
    } else {
        $taskById[$taskId] = $task
    }

    $orderValue = 0L
    $hasValidOrder = (Test-HasProperty $task "order") `
        -and [long]::TryParse($task.order.ToString(), [ref]$orderValue) `
        -and $orderValue -gt 0
    if (-not $hasValidOrder) {
        Add-PlanError "$taskId order must be a positive integer."
    } elseif ($seenOrders.ContainsKey($orderValue)) {
        Add-PlanError "$taskId duplicates order $orderValue used by $($seenOrders[$orderValue])."
    } else {
        $seenOrders[$orderValue] = $taskId
    }

    foreach ($textProperty in @("title", "goal", "evidence", "rollback")) {
        if (-not (Test-HasProperty $task $textProperty) -or -not (Test-NonEmptyText $task.$textProperty)) {
            Add-PlanError "$taskId must define non-empty $textProperty."
        }
    }

    foreach ($enumContract in @(
        @{ Name = "state"; Valid = $validStates },
        @{ Name = "priority"; Valid = $validPriorities },
        @{ Name = "risk"; Valid = $validRisks },
        @{ Name = "authority"; Valid = $validAuthorities },
        @{ Name = "lane"; Valid = $validLanes }
    )) {
        if (-not (Test-HasProperty $task $enumContract.Name) -or $task.($enumContract.Name) -notin $enumContract.Valid) {
            Add-PlanError "$taskId has invalid $($enumContract.Name): '$($task.($enumContract.Name))'."
        }
    }

    foreach ($arrayProperty in @("writeSet", "verification", "acceptance")) {
        if (-not (Test-HasProperty $task $arrayProperty) -or @($task.$arrayProperty).Count -eq 0) {
            Add-PlanError "$taskId must define a non-empty $arrayProperty array."
            continue
        }

        foreach ($entry in @($task.$arrayProperty)) {
            if (-not (Test-NonEmptyText $entry)) {
                Add-PlanError "$taskId $arrayProperty contains an empty entry."
            }
        }
    }

    if (-not (Test-HasProperty $task "dependencies")) {
        Add-PlanError "$taskId must define a dependencies array."
    }

    foreach ($writeTarget in @($task.writeSet)) {
        if (-not (Test-NonEmptyText $writeTarget)) {
            continue
        }

        if ($writeTarget -match '^[A-Za-z]:[\\/]' -or $writeTarget -match '^[/\\]{1,2}') {
            Add-PlanError "$taskId writeSet contains an absolute path: $writeTarget"
        }

        if ($writeTarget -match '(^|[\\/])\.\.([\\/]|$)') {
            Add-PlanError "$taskId writeSet escapes the repository: $writeTarget"
        }
    }
}

foreach ($taskId in $taskById.Keys) {
    $task = $taskById[$taskId]
    $dependencies = @($task.dependencies)
    $duplicateDependencies = @($dependencies | Group-Object | Where-Object Count -gt 1 | ForEach-Object Name)
    if ($duplicateDependencies.Count -gt 0) {
        Add-PlanError "$taskId has duplicate dependencies: $($duplicateDependencies -join ', ')."
    }

    foreach ($dependency in $dependencies) {
        if ($dependency -eq $taskId) {
            Add-PlanError "$taskId cannot depend on itself."
        } elseif (-not $taskById.ContainsKey($dependency)) {
            Add-PlanError "$taskId depends on missing task $dependency."
        }
    }

    if ($task.state -eq "ready") {
        foreach ($dependency in $dependencies) {
            if ($taskById.ContainsKey($dependency) -and $taskById[$dependency].state -ne "completed") {
                Add-PlanError "$taskId is ready but dependency $dependency is not completed."
            }
        }
    }

    if ($task.state -eq "completed" -and (Test-NonEmptyText $task.evidence)) {
        $evidencePath = $task.evidence
        $looksLikeRepoPath = $evidencePath -match '^(docs|scripts|src|tests|eval)/'
        if ($looksLikeRepoPath) {
            [void](Resolve-RepoDocument -RelativePath $evidencePath -Label "$taskId evidence")
        }
    }
}

$inProgress = @($taskById.Values | Where-Object state -eq "in_progress")
if ($inProgress.Count -gt 1) {
    Add-PlanError "At most one task may be in_progress; found $($inProgress.Count)."
}

$inDegree = @{}
$dependents = @{}
foreach ($taskId in $taskById.Keys) {
    $inDegree[$taskId] = 0
    $dependents[$taskId] = [System.Collections.Generic.List[string]]::new()
}

foreach ($taskId in $taskById.Keys) {
    foreach ($dependency in @($taskById[$taskId].dependencies)) {
        if ($dependency -ne $taskId -and $taskById.ContainsKey($dependency)) {
            $inDegree[$taskId]++
            $dependents[$dependency].Add($taskId)
        }
    }
}

$readyForSort = [System.Collections.Generic.Queue[string]]::new()
foreach ($taskId in $taskById.Keys) {
    if ($inDegree[$taskId] -eq 0) {
        $readyForSort.Enqueue($taskId)
    }
}

$visited = 0
while ($readyForSort.Count -gt 0) {
    $current = $readyForSort.Dequeue()
    $visited++
    foreach ($dependent in $dependents[$current]) {
        $inDegree[$dependent]--
        if ($inDegree[$dependent] -eq 0) {
            $readyForSort.Enqueue($dependent)
        }
    }
}

if ($visited -ne $taskById.Count) {
    Add-PlanError "Task dependency graph contains a cycle."
}

$taskListPath = Resolve-RepoDocument -RelativePath "docs/TASKS.md" -Label "Task checklist"
if ($null -ne $taskListPath -and $resolvedDocuments.ContainsKey("implementationPlan") -and $null -ne $resolvedDocuments["implementationPlan"]) {
    $taskListText = Get-Content -LiteralPath $taskListPath -Raw -Encoding utf8
    $implementationPlanText = Get-Content -LiteralPath $resolvedDocuments["implementationPlan"] -Raw -Encoding utf8
    foreach ($taskId in $taskById.Keys) {
        if (-not $taskListText.Contains($taskId, [System.StringComparison]::Ordinal)) {
            Add-PlanError "TASKS.md does not mention $taskId."
        }
        if (-not $implementationPlanText.Contains($taskId, [System.StringComparison]::Ordinal)) {
            Add-PlanError "Implementation plan does not mention $taskId."
        }
    }
}

if ($errors.Count -gt 0) {
    Write-Host "Product-focus plan verification failed with $($errors.Count) error(s):" -ForegroundColor Red
    foreach ($errorMessage in $errors) {
        Write-Host "- $errorMessage" -ForegroundColor Red
    }
    exit 1
}

$stateSummary = $taskById.Values |
    Group-Object state |
    Sort-Object Name |
    ForEach-Object { "$($_.Name)=$($_.Count)" }
$nextReady = $taskById.Values |
    Where-Object state -eq "ready" |
    Sort-Object order |
    Select-Object -First 1

Write-Host "Product-focus plan verification passed: tasks=$($taskById.Count); $($stateSummary -join '; ')." -ForegroundColor Green
if ($null -eq $nextReady) {
    Write-Host "Next ready task: none."
} else {
    Write-Host "Next ready task: $($nextReady.id) - $($nextReady.title) [$($nextReady.authority)]."
}
