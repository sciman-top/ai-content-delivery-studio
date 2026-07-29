# WPF Shell Accessibility Baseline Implementation Plan

**Implementation status:** Completed on 2026-07-29. Automated contract, native `authorized_agent` equivalent-operator acceptance, fixed repository gates, and truth boundaries are recorded in `docs/change-evidence/20260729-wpf-shell-accessibility-baseline.md`.

**Goal:** Establish a localized, keyboard-visible, UI Automation-readable accessibility baseline for the native WPF shell and Diagnostics panel.

**Architecture:** Keep accessibility state in the WPF presentation layer. Reuse existing localized ViewModel labels, add a system-brush focus adorner, declare shell-region keyboard traversal, and expose status updates through polite UI Automation live regions.

**Tech stack:** .NET 10, WPF XAML, existing localization service, xUnit XAML contract tests, Computer Use native probe, PowerShell repository gates.

## Task 1: Lock The Accessibility Contract In Tests

**Files:**

- Add `tests/ContentDeliveryStudio.Tests/WpfShellAccessibilityTests.cs`
- Modify `tests/ContentDeliveryStudio.Tests/LocalizationTests.cs` only if new localized names are required

**Acceptance criteria:**

- Tests describe the required shell AutomationIds, names, focus resource, keyboard-navigation properties, and live settings.
- Tests assert that informational live regions remain non-focusable.
- Tests distinguish this bounded baseline from a full accessibility pass.

**Verification:** Run the focused accessibility and localization tests and confirm the contract fails before implementation, then passes after Tasks 2-3.

**Dependencies:** None.

## Task 2: Add Shell Focus And Keyboard Semantics

**Files:**

- Modify `src/ContentDeliveryStudio.App/App.xaml`
- Modify `src/ContentDeliveryStudio.App/MainWindow.xaml`
- Modify `src/ContentDeliveryStudio.App/Views/WorkspaceNavigationView.xaml`
- Modify `src/ContentDeliveryStudio.App/Views/WorkbenchTabHostView.xaml`

**Acceptance criteria:**

- The focus adorner uses dynamic Windows system brushes and leaves native control templates intact.
- Language, navigation, and workbench tabs expose stable AutomationIds and localized names.
- Shell traversal follows header -> navigation -> tabs -> inspector without numeric per-control TabIndex maintenance.

**Verification:** Build the solution and run focused accessibility tests.

**Dependencies:** Task 1.

## Task 3: Expose Inspector And Status Semantics

**Files:**

- Modify `src/ContentDeliveryStudio.App/Views/WorkbenchInspectorView.xaml`
- Modify `src/ContentDeliveryStudio.App/Views/DiagnosticsPanelView.xaml`
- Modify `src/ContentDeliveryStudio.App/Views/ActivityPanelView.xaml`
- Modify localization/ViewModel files only if existing labels cannot provide an unambiguous accessible name

**Acceptance criteria:**

- Inspector and Diagnostics interactive controls expose stable localized names and use the focus resource.
- Diagnostics/activity status surfaces expose `Polite` live settings and remain outside the tab sequence.
- Existing Diagnostics behavior and layout remain unchanged.

**Verification:** Build and run focused accessibility, diagnostics layout, and localization tests.

**Dependencies:** Tasks 1-2.

## Checkpoint 1

- Focused tests pass.
- Build reports no new warning or error.
- Diff contains no domain, provider, persistence, or generated-output change.

## Task 4: Run Native Authorized-Agent Acceptance

**Files:** None in Git during execution.

**Acceptance criteria:**

- Computer Use starts the fake-first native WPF executable and targets exactly one application window.
- Keyboard-only traversal reaches language, navigation, workbench tabs, and Diagnostics controls in visible order with visible focus.
- UI Automation exposes bilingual names, stable IDs, and live-region semantics at the minimum supported window size without clipping or overlap.

**Verification:** Record the observed focus sequence, window bounds, UI Automation tree, language switch, and visual result. Preserve actor type `authorized_agent` under the user's equivalent operator acceptance policy.

**Dependencies:** Tasks 2-3.

## Task 5: Close Documentation And Evidence

**Files:**

- Modify `docs/reviews/ACCESSIBILITY_REVIEW.md`
- Modify `docs/ROADMAP.md` and `docs/TASKS.md` only for this bounded result
- Add `docs/change-evidence/20260729-wpf-shell-accessibility-baseline.md`

**Acceptance criteria:**

- Documentation records the shell baseline as complete without closing the full accessibility lane.
- Narrator, system high contrast, non-default DPI, all workflow forms, virtualized gallery/grid focus, touch/pen, and packaged-app accessibility remain explicit open boundaries.
- Evidence records references, actor type, automation/native observations, commands, exit codes, compatibility, and rollback.

**Verification:** Review the documentation diff and run the reference evidence contract.

**Dependencies:** Task 4.

## Final Gate

Run in fixed order:

1. `dotnet build ContentDeliveryStudio.sln`
2. `dotnet test ContentDeliveryStudio.sln --no-build`
3. `pwsh -NoProfile -ExecutionPolicy Bypass -File scripts/verify-reference-evidence.ps1`
4. `dotnet format --verify-no-changes`
5. `pwsh -NoProfile -ExecutionPolicy Bypass -File scripts/preflight-release.ps1 -NoRestore`

Then perform a five-axis review, inspect the staged write set, scan for credentials/generated files, and create one local implementation commit. Do not stage `AGENTS.md` and do not push.

## Risks And Mitigations

| Risk | Mitigation |
| --- | --- |
| A custom focus style reduces theme/high-contrast compatibility | Use dynamic system brushes and a focus adorner only; do not replace control templates or backgrounds. |
| Keyboard order becomes brittle | Set region-level navigation semantics and preserve native descendant order instead of assigning numeric TabIndex everywhere. |
| Live status pollutes keyboard traversal | Use UI Automation live settings while keeping informational surfaces non-focusable. |
| Hard-coded accessible text drifts after language changes | Bind accessible names to existing localized ViewModel properties and cover both languages. |
| UI Automation proof is overstated | Record it as `authorized_agent` UI Automation/keyboard evidence, not Narrator or full accessibility acceptance. |
| Scope expands to every workspace control | Limit the write set to the shell and Diagnostics panel; keep full-form/gallery/packaged checks open. |
