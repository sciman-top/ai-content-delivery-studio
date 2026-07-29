# WPF Shell Accessibility Baseline Evidence

## Scope And Truth Boundary

- Slice: native WPF main shell and Diagnostics accessibility baseline.
- Repo-side result: implemented. The shell now has localized accessible names, stable AutomationIds, region-level keyboard traversal, a dynamic system-brush focus visual, and non-focusable polite Diagnostics/activity live regions.
- Automated evidence: `WpfShellAccessibilityTests` protects the XAML and UIA notification contract; focused layout and Diagnostics regressions also pass.
- Operator evidence: an `authorized_agent` operated and inspected the native WPF app under the user's explicit equivalent-operator acceptance authority. The actor remains truthfully identified as `authorized_agent` and is not relabeled as human.
- Live accepted evidence: unchanged. Startup remained fake-first, no paid provider was called, and no live-provider evidence was refreshed.
- Open Future Trigger Lanes: Narrator, system high-contrast switching, non-default DPI, full-workbench form coverage, virtualized gallery/grid focus, touch/pen, and packaged-app accessibility.
- Existing accepted Tasks 1-30, Checkpoints 0-5, scientific-figure artifacts, and V1 launch evidence were not modified or reopened.

## Reference Basis

| Area | Revision | Decision |
| --- | --- | --- |
| `02-dotnet-wpf/docs-desktop` | `3ed3bb19178883827aa0a81427576797db141862` | Retain as WPF accessibility and keyboard behavior authority. |
| `02-dotnet-wpf/WPF-Samples/Elements/FocusVisualStyle/MainWindow.xaml` | `ecd9529fb6941272eff1ee1e7e2554e3ecb2f1e4` | Adapt the focus adorner pattern with dynamic Windows system brushes. |
| `02-dotnet-wpf/WPF-Samples/Accessibility/SelectionPattern/SelectionPatternSample/MainWindow.xaml` | `ecd9529fb6941272eff1ee1e7e2554e3ecb2f1e4` | Preserve native list/tab keyboard and selection behavior. |
| `02-dotnet-wpf/WPF-Samples/Sample Applications/CustomComboBox/MainWindow.xaml(.cs)` | `ecd9529fb6941272eff1ee1e7e2554e3ecb2f1e4` | Adapt `NotifyOnTargetUpdated` plus AutomationPeer event notification for the long-lived localized language selector. |

No external source was modified or executed and no source code was copied verbatim.

## Red-Green And Focused Verification

- Initial accessibility contract run: `0 / 3` passed, proving the focus resource, shell UIA/keyboard contract, and Diagnostics/activity live-region contract were absent.
- After the XAML implementation: build exit `0` with `0` warnings and `0` errors; accessibility/layout/Diagnostics focus set passed `36 / 36`.
- The first native language-switch probe exposed a stale `LanguageSelector` UIA name while visible text had localized. Root-cause tracing showed the long-lived ComboBox peer needed an explicit Name property-change notification.
- A focused regression contract was added before the fix and failed `1 / 3`; after the official WPF target-update/AutomationPeer pattern was applied, the same focused set passed `36 / 36`.

## Native WPF Authorized-Agent Probe

- Date: `2026-07-29`.
- Actor type: `authorized_agent`.
- Decision authority: `equivalent_operator_acceptance` under the user's explicit instruction.
- Mode: fake-first. The header showed `假 Provider` / `Fake providers`, and activity stated that no real API call was enabled.
- Application targeting: exactly one `ContentDeliveryStudio.App` window was selected by returned process path and window handle.
- Minimum size: the outer captured window was `988 x 671`, corresponding to the declared `980 x 640` WPF client minimum plus native window chrome. No clipping, overlap, or incoherent region layout was observed.
- Keyboard path: language selector -> three navigation items -> workbench tab -> inspector controls -> Diagnostics output path -> Browse. Focus remained visibly outlined; list/tab arrow-key behavior remained native.
- UIA targets: `LanguageSelector`, `WorkspaceNavigation`, `WorkbenchTabs`, `WorkbenchInspector`, `DiagnosticsOutputDirectory`, `BrowseDiagnosticsDirectory`, `ExportDiagnosticsPackage`, `DiagnosticsExportStatus`, and `ActivityStatus` were all present.
- Bilingual result: after waiting for the asynchronous UIA update to stabilize, names changed from Chinese to `Language`, `Workspace`, `Inspector`, `Output parent folder`, `Browse`, `Export`, the localized status text, and `Activity`.
- Live-region result: the committed contract declares Diagnostics and activity as `Polite` and non-focusable; the UIA tree exposed both status surfaces without adding them to the Tab sequence.
- Probe shutdown: the WPF window was closed after inspection. No Windows accessibility, theme, privacy, high-contrast, or scaling setting was changed.

The Computer Use screenshots were transient visual observations and are not committed binaries.

## Final Fixed-Order Verification

| Stage | Command | Result |
| --- | --- | --- |
| Build | `dotnet build ContentDeliveryStudio.sln` | Exit `0`; `0` warnings, `0` errors. |
| Test | `dotnet test ContentDeliveryStudio.sln --no-build` | Exit `0`; `699 / 699` passed, `0` skipped. |
| Contract | `pwsh -NoProfile -ExecutionPolicy Bypass -File scripts/verify-reference-evidence.ps1` | Exit `0`; reference governance was in sync and `workflow-and-ux-architecture` evidence was detected in this slice's spec/plan. |
| Format | `dotnet format --verify-no-changes` | Exit `0`; no formatting drift. |
| Hotspot | `pwsh -NoProfile -ExecutionPolicy Bypass -File scripts/preflight-release.ps1 -NoRestore` | Exit `0`; placeholder/conflict scans, nested `699 / 699` repository verification, publish WhatIf, and cached/uncached diff hygiene passed. |

## Compatibility, Risk, And Rollback

- Compatibility: no provider, schema, domain, delivery-format, localization-key, accepted-artifact, or dependency change.
- Security: no provider call, upload, credential access, workspace/database mutation, or generated output entered the slice.
- Performance: only WPF attached properties, a focus adorner, and one localized UIA property-change notification were added; no hot data path changed.
- Residual risk: UIA/Narrator behavior under high contrast, non-default DPI, virtualization, touch/pen, and packaged deployment remains deliberately unclaimed.
- Rollback: revert this XAML/code-behind/test/documentation/evidence slice only. No data or provider migration is required.
