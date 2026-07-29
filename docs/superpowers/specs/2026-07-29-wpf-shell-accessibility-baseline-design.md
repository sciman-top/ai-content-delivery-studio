# WPF Shell Accessibility Baseline Design

**Status:** Approved for implementation planning
**Date:** 2026-07-29
**Scope:** Native WPF main shell and local Diagnostics panel accessibility baseline

## Problem

The application uses ordinary WPF controls and exposes several stable AutomationIds, but the main shell does not yet provide a deliberate accessibility contract. The language selector, workspace navigation, workbench tabs, inspector, activity surface, Diagnostics output path, and Diagnostics status rely mostly on control defaults or visual adjacency. Keyboard focus visibility is not a repo-owned style, status changes are not declared as live regions, and the current automated tests do not prevent accessible-name or tab-order regressions.

The existing accessibility review explicitly leaves a live Windows pass and automated accessibility checks open. A full application audit would be too broad for one slice, so this design establishes a bounded shell baseline without claiming that the Phase 7 accessibility lane is complete.

## Decision

Add an explicit, localized accessibility contract to the native WPF shell and Diagnostics panel:

1. Give the language selector, workspace navigation, workbench tab host, inspector, activity panel, Diagnostics path, and Diagnostics commands stable AutomationIds and localized accessible names.
2. Declare the Diagnostics status and activity surface as polite live regions so assistive technologies can observe non-blocking updates.
3. Define one application-owned keyboard focus visual using dynamic Windows system brushes, and apply it to the shell's interactive controls without replacing their native control templates.
4. Declare deterministic keyboard traversal at the shell-region level. The order is header language selector -> workspace navigation -> workbench tabs -> inspector controls; non-interactive status text does not become a tab stop.
5. Add XAML contract tests plus an `authorized_agent` native WPF keyboard-only and UI Automation probe.

## User Experience

The visible layout and command labels do not change. Keyboard users receive a consistent focus indicator around the active shell control. Screen readers and UI Automation clients receive localized names matching the visible language, stable identifiers for shell regions, and polite notification semantics for Diagnostics/activity status changes.

The slice must preserve the current compact three-region desktop layout at the minimum supported window size. It must not add instructional text, modal dialogs, decorative UI, or a separate accessibility settings page.

## Architecture

### Accessibility Presentation Model

Existing localized ViewModel properties remain the accessible-name source wherever they already describe the control: `LanguageLabel`, `WorkspaceHeader`, `InspectorTitle`, `ActivityTitle`, and Diagnostics labels. If a shell region needs a distinct name that cannot reuse an existing visible label without ambiguity, add a localization key rather than hard-coding English in XAML.

No accessibility-specific state enters the domain or application workflow layers. This is a WPF presentation contract only.

### Focus Visual

`App.xaml` owns a keyed `AccessibleKeyboardFocusVisual` resource. The adorner uses dynamic `SystemColors.HighlightBrushKey` and a visible thickness/offset so it remains compatible with Windows theme and high-contrast brush substitution. It is applied explicitly to the language selector, navigation list, workbench tab host, and Diagnostics interactive controls. Existing native templates and disabled-state behavior remain intact.

### Keyboard Traversal

The main window and shell regions declare `KeyboardNavigation.TabNavigation`/`ControlTabNavigation` only where needed to make region traversal deterministic. The implementation does not assign a brittle numeric TabIndex to every descendant. List and tab selection continue to use standard WPF arrow-key behavior.

### Live Status

The Diagnostics status and activity collection receive `AutomationProperties.LiveSetting="Polite"`. They remain non-focusable because forcing informational text into the tab sequence would slow repeated operation. UI Automation tree inspection must still expose their current localized text.

## Truth And Safety Boundaries

- Repo-side completion means the shell contract, automated checks, native keyboard probe, and evidence are present and green.
- The user has authorized `authorized_agent` native operation as equivalent operator acceptance. Evidence preserves that actor type and does not relabel it as human.
- UI Automation evidence is not represented as Narrator evidence.
- This slice does not change Windows accessibility, privacy, theme, scaling, or high-contrast settings.
- A full Narrator pass, system high-contrast switch, non-default DPI matrix, all workflow forms, virtualized gallery/grid focus, touch, pen, and packaged-app accessibility remain open.
- Existing Tasks 1-30, Checkpoints 0-5, live accepted evidence, and accepted scientific-figure artifacts remain unchanged.
- No provider call, upload, database mutation, backup/restore operation, or release publication occurs.

## Reference Basis

- Repository review: `docs/reviews/ACCESSIBILITY_REVIEW.md`; adopted as the current known-gap and release-requirement source.
- WPF official sample `02-dotnet-wpf/WPF-Samples/Elements/FocusVisualStyle/MainWindow.xaml` at `ecd9529fb6941272eff1ee1e7e2554e3ecb2f1e4`; adopt the `FocusVisualStyle` adorner pattern, replacing fixed sample colors with dynamic Windows system brushes.
- WPF official sample `02-dotnet-wpf/WPF-Samples/Accessibility/SelectionPattern/SelectionPatternSample/MainWindow.xaml` at the same revision; adapt standard tab-stop and keyboard-navigation semantics while keeping native list/tab selection patterns.
- WPF official sample application accessible-name patterns under `02-dotnet-wpf/WPF-Samples/Sample Applications/WPFGallery` at the same revision; adopt localized `AutomationProperties.Name` on interactive controls and collections.
- WPF official `02-dotnet-wpf/WPF-Samples/Sample Applications/CustomComboBox/MainWindow.xaml(.cs)` at the same revision; adapt `NotifyOnTargetUpdated` plus AutomationPeer notification so the long-lived language selector invalidates a UIA client's localized Name cache.
- WPF official documentation source `02-dotnet-wpf/docs-desktop` at `3ed3bb19178883827aa0a81427576797db141862`; retain as framework behavior authority. No external source is copied or executed.

## Acceptance Criteria

- Shell regions and Diagnostics controls expose stable AutomationIds and localized accessible names in English and Simplified Chinese.
- Diagnostics and activity status surfaces expose polite live-region semantics without entering the tab sequence.
- The application-owned focus visual uses dynamic Windows system brushes and does not replace native control templates.
- Keyboard traversal reaches language, navigation, tabs, and Diagnostics controls in visible region order; arrow keys retain native list/tab selection behavior.
- A language change updates both visible labels and UI Automation names.
- XAML contract tests fail if required shell names, identifiers, live settings, or focus resources are removed.
- An `authorized_agent` native WPF probe verifies keyboard-only traversal, focus visibility, bilingual UI Automation names, and unchanged layout at the minimum supported window size.
- Build, test, reference contract, format, and hotspot gates pass in the fixed repository order.

## Dependencies And Write Set

Expected implementation files are limited to:

- `src/ContentDeliveryStudio.App/App.xaml`
- `src/ContentDeliveryStudio.App/MainWindow.xaml`
- bounded shell/Diagnostics views under `src/ContentDeliveryStudio.App/Views/`
- localization/ViewModel fields only if an accessible name cannot reuse an existing localized property
- focused XAML/localization tests
- `docs/reviews/ACCESSIBILITY_REVIEW.md`, roadmap/task status, implementation plan, and one new change-evidence record

No new runtime dependency, `.env`, SQLite database, workspace, output, screenshot binary, generated artifact, or ZIP belongs in Git.

## Verification And Evidence

Focused checks cover XAML accessibility contracts and bilingual localization. The native probe records window bounds, focus order, UI Automation identifiers/names, status live-region visibility, and visible layout observations. It uses fake-first startup and does not activate paid providers.

Final verification order:

1. `dotnet build ContentDeliveryStudio.sln`
2. `dotnet test ContentDeliveryStudio.sln --no-build`
3. `pwsh -NoProfile -ExecutionPolicy Bypass -File scripts/verify-reference-evidence.ps1`
4. `dotnet format --verify-no-changes`
5. `pwsh -NoProfile -ExecutionPolicy Bypass -File scripts/preflight-release.ps1 -NoRestore`

## Rollback

Revert only this XAML/localization/test/documentation/evidence slice. The change has no schema, provider, project-data, or generated-artifact migration and therefore requires no data rollback.
