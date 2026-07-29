# Accessibility Review

Date: 2026-07-29

Scope: WPF MVP shell, bilingual labels, project planning, prompt editing, queue, gallery, review, and delivery panels.

## Current Baseline

- The app supports Chinese and English UI text through stable localization keys.
- The window defines minimum dimensions to reduce layout collapse.
- Form inputs and commands are ordinary WPF controls, which keeps keyboard and screen-reader support possible.
- The MVP uses text labels for major navigation and workflow areas.
- The native main shell and Diagnostics panel expose localized accessible names, stable AutomationIds, polite status live regions, and a system-brush keyboard focus visual.
- XAML contract tests protect the shell baseline. An `authorized_agent` native probe verified bilingual UIA names, keyboard traversal, visible focus, and the minimum-size layout under the user's equivalent-operator acceptance policy.

## Requirements For Release

- Every icon-only command must expose an accessible name and tooltip.
- Keyboard tab order must follow visible workflow order.
- Focus must remain visible in header, navigation, tab content, inspector, and status areas.
- Error and status messages must be reachable by assistive technologies.
- Text must remain readable at common Windows scaling settings.
- Color must not be the only signal for item status, review decisions, warnings, or failures.
- Chinese and English strings must fit their containers without clipping.

## Known Gaps

- Narrator, system high-contrast switching, and non-default DPI validation remain required before the full accessibility lane can close. The authorized-agent UIA probe is not represented as Narrator evidence.
- The shell/Diagnostics automated contract is present; full-form accessibility automation and packaged-app coverage remain open.
- Large data grids and galleries need focus and selection behavior verification after virtualization is added.
- Touch and pen accessibility behavior remains outside this shell baseline.

## Gate

Before release, run:

```powershell
dotnet build
dotnet test
dotnet format --verify-no-changes
```

Then perform the remaining Narrator, system high-contrast, DPI, full-form, virtualized-gallery, touch/pen, and packaged-app passes and record their evidence in this folder.
