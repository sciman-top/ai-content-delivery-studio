# Accessibility Review

Date: 2026-06-01

Scope: WPF MVP shell, bilingual labels, project planning, prompt editing, queue, gallery, review, and delivery panels.

## Current Baseline

- The app supports Chinese and English UI text through stable localization keys.
- The window defines minimum dimensions to reduce layout collapse.
- Form inputs and commands are ordinary WPF controls, which keeps keyboard and screen-reader support possible.
- The MVP uses text labels for major navigation and workflow areas.

## Requirements For Release

- Every icon-only command must expose an accessible name and tooltip.
- Keyboard tab order must follow visible workflow order.
- Focus must remain visible in header, navigation, tab content, inspector, and status areas.
- Error and status messages must be reachable by assistive technologies.
- Text must remain readable at common Windows scaling settings.
- Color must not be the only signal for item status, review decisions, warnings, or failures.
- Chinese and English strings must fit their containers without clipping.

## Known Gaps

- A live Windows accessibility pass with Narrator, keyboard-only navigation, and high-contrast mode is still required before release.
- Automated UI accessibility tests are not yet wired into CI.
- Large data grids and galleries need focus and selection behavior verification after virtualization is added.

## Gate

Before release, run:

```powershell
dotnet build
dotnet test
dotnet format --verify-no-changes
```

Then perform a manual WPF accessibility pass on the packaged app and record screenshots or notes in this folder.
