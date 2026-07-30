param(
    [Parameter(Mandatory = $true)]
    [string] $PackagePath,

    [string] $OutputPath = ''
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repoRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$resolvedPackagePath = [System.IO.Path]::GetFullPath($PackagePath)
if ([string]::IsNullOrWhiteSpace($OutputPath)) {
    $OutputPath = Join-Path $repoRoot 'outputs/phase7-packaged-accessibility-probe.json'
}
$resolvedOutputPath = [System.IO.Path]::GetFullPath($OutputPath)

& (Join-Path $PSScriptRoot 'verify-publish-package.ps1') -PackagePath $resolvedPackagePath | Out-Null

Add-Type -AssemblyName UIAutomationClient
Add-Type -AssemblyName UIAutomationTypes
if (-not ('NativeDpiProbe' -as [type])) {
    Add-Type @'
using System;
using System.Runtime.InteropServices;
public static class NativeDpiProbe
{
    [DllImport("user32.dll")]
    public static extern uint GetDpiForWindow(IntPtr hwnd);
}
'@
}

$probeRoot = Join-Path ([System.IO.Path]::GetTempPath()) "content-delivery-packaged-probe-$([Guid]::NewGuid().ToString('N'))"
$extractRoot = Join-Path $probeRoot 'package'
$dataRoot = Join-Path $probeRoot 'data'
$process = $null

function Find-AutomationElementById {
    param(
        [Parameter(Mandatory = $true)]
        [System.Windows.Automation.AutomationElement] $Root,
        [Parameter(Mandatory = $true)]
        [string] $AutomationId
    )

    $condition = [System.Windows.Automation.PropertyCondition]::new(
        [System.Windows.Automation.AutomationElement]::AutomationIdProperty,
        $AutomationId)
    return $Root.FindFirst([System.Windows.Automation.TreeScope]::Descendants, $condition)
}

try {
    New-Item -ItemType Directory -Path $extractRoot -Force | Out-Null
    New-Item -ItemType Directory -Path $dataRoot -Force | Out-Null
    [System.IO.Compression.ZipFile]::ExtractToDirectory($resolvedPackagePath, $extractRoot)

    $executablePath = Join-Path $extractRoot 'ContentDeliveryStudio.App.exe'
    if (-not (Test-Path -LiteralPath $executablePath -PathType Leaf)) {
        throw 'Verified package did not extract the expected application executable.'
    }

    $env:CONTENT_DELIVERY_STUDIO_DATA_ROOT = $dataRoot
    $env:PROVIDER_MODE = 'fake'
    $process = Start-Process -FilePath $executablePath -WorkingDirectory $extractRoot -PassThru

    $window = $null
    $deadline = [DateTimeOffset]::UtcNow.AddSeconds(30)
    while ($null -eq $window -and [DateTimeOffset]::UtcNow -lt $deadline) {
        if ($process.HasExited) {
            throw "Packaged application exited before exposing a UI Automation window: $($process.ExitCode)"
        }

        $processCondition = [System.Windows.Automation.PropertyCondition]::new(
            [System.Windows.Automation.AutomationElement]::ProcessIdProperty,
            $process.Id)
        $window = [System.Windows.Automation.AutomationElement]::RootElement.FindFirst(
            [System.Windows.Automation.TreeScope]::Children,
            $processCondition)
        if ($null -eq $window) {
            Start-Sleep -Milliseconds 200
        }
    }

    if ($null -eq $window) {
        throw 'Timed out waiting for the packaged application UI Automation window.'
    }

    $requiredIds = @(
        'LanguageSelector',
        'WorkspaceNavigation',
        'WorkbenchTabs',
        'WorkbenchInspector',
        'DiagnosticsOutputDirectory',
        'BrowseDiagnosticsDirectory',
        'ExportDiagnosticsPackage',
        'BackupSourceDirectory',
        'CreateSafeBackup',
        'RestoreBackupFile',
        'RestoreSafeBackup',
        'BackupRestoreStatus',
        'ActivityStatus'
    )

    $observed = @()
    foreach ($automationId in $requiredIds) {
        $element = Find-AutomationElementById -Root $window -AutomationId $automationId
        if ($null -eq $element) {
            throw "Packaged application is missing UI Automation element: $automationId"
        }

        $name = $element.Current.Name
        if ([string]::IsNullOrWhiteSpace($name)) {
            throw "Packaged UI Automation element has no accessible name: $automationId"
        }

        $observed += [ordered]@{
            automationId = $automationId
            name = $name
            controlType = $element.Current.ControlType.ProgrammaticName
            isEnabled = $element.Current.IsEnabled
            isKeyboardFocusable = $element.Current.IsKeyboardFocusable
        }
    }

    $bounds = $window.Current.BoundingRectangle
    if ($bounds.Width -lt 980 -or $bounds.Height -lt 640) {
        throw "Packaged window is below the declared minimum layout: $($bounds.Width) x $($bounds.Height)"
    }

    $process.Refresh()
    $dpi = [NativeDpiProbe]::GetDpiForWindow([IntPtr]$process.MainWindowHandle)
    if ($dpi -eq 0) {
        throw 'Unable to read the packaged window DPI.'
    }

    $report = [ordered]@{
        schemaVersion = 1
        timestamp = [DateTimeOffset]::UtcNow.ToString('o')
        actorType = 'authorized_agent'
        providerMode = 'fake'
        packagePath = $resolvedPackagePath
        packageSha256 = (Get-FileHash -LiteralPath $resolvedPackagePath -Algorithm SHA256).Hash.ToLowerInvariant()
        executableSha256 = (Get-FileHash -LiteralPath $executablePath -Algorithm SHA256).Hash.ToLowerInvariant()
        processPath = $executablePath
        windowTitle = $window.Current.Name
        windowWidth = [Math]::Round($bounds.Width, 2)
        windowHeight = [Math]::Round($bounds.Height, 2)
        observedDpi = $dpi
        observedScalePercent = [Math]::Round(($dpi / 96.0) * 100, 2)
        perMonitorV2Contract = $true
        automationElements = $observed
        truthBoundary = [ordered]@{
            proves = 'verified ZIP launch, current-DPI minimum layout, and named UI Automation shell/backup controls'
            doesNotProve = 'Narrator behavior, high-contrast switching, non-default-DPI matrix, touch, pen, or low-memory hardware acceptance'
        }
    }

    $outputDirectory = Split-Path -Parent $resolvedOutputPath
    New-Item -ItemType Directory -Path $outputDirectory -Force | Out-Null
    $report | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $resolvedOutputPath -Encoding utf8
    $report | ConvertTo-Json -Depth 8
}
finally {
    if ($null -ne $process -and -not $process.HasExited) {
        $null = $process.CloseMainWindow()
        if (-not $process.WaitForExit(5000)) {
            Stop-Process -Id $process.Id -Force
            $process.WaitForExit()
        }
    }
    if (Test-Path -LiteralPath $probeRoot) {
        Remove-Item -LiteralPath $probeRoot -Recurse -Force
    }
}
