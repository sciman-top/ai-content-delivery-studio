# Phase 7 Product Hardening Closeout Design

**Status:** Approved for implementation under the user's autonomous-execution authorization
**Date:** 2026-07-30
**Scope:** Backup/restore, distributable Windows package, large-gallery performance, automatable accessibility, and documentation truth

## Problem

Phase 7 already contains durable queue recovery, local diagnostics, WPF row virtualization, thumbnail caching, and a shell accessibility baseline. The remaining roadmap language is broader than the implemented evidence: backup/restore is test-only and trusts archive metadata, publish preflight is only a command preview, the 1,000-row gallery benchmark records metrics without budgets, and accessibility evidence does not cover the gallery or packaged runtime. `docs/TASKS.md` has no open checkboxes while `docs/ROADMAP.md` still calls the phase partially complete.

## Decision

Close the repository-owned portion of Phase 7 by hardening the existing seams instead of adopting a new installer, backup framework, or UI architecture:

1. make backup archives versioned and content-addressed, validate the complete archive before writing, enforce bounded extraction, and expose conservative backup/restore through the existing Workbench inspector;
2. extend `publish-app.ps1` to create a deterministic distributable ZIP plus SHA-256 manifest and add an offline package-verification script;
3. turn the existing 1,000-row benchmark into a budgeted regression gate and add explicit virtualized gallery keyboard/UI Automation contracts;
4. add repo-owned system-brush, high-contrast-compatible, DPI/layout, full-form naming, gallery-focus, and packaged-binary probes where those claims are objectively automatable;
5. synchronize roadmap, task, architecture, user guidance, performance review, and change evidence without inventing a second sample project.

Narrator behavior, touch/pen ergonomics, real low-memory hardware, and subjective scrolling quality remain manual/live acceptance. A verified ZIP is a packaged release artifact, not an installed MSIX/MSI experience.

## Backup Contract

The ZIP contains one `backup-manifest.json` and only regular file entries. Manifest schema v1 records normalized relative path, uncompressed size, and SHA-256 for every file. Creation writes a temporary archive beside the destination and moves it into place only after success.

Restore performs a complete preflight before creating the target directory or writing a file:

- exactly one supported manifest is required;
- archive entries must be unique after case-insensitive normalized-path comparison;
- every payload entry must have one manifest row and vice versa;
- directory, absolute, parent-traversal, alternate data stream, and unsupported link-like entries are rejected;
- entry count, per-file size, and total uncompressed size are bounded;
- declared sizes and streamed SHA-256 values must match;
- existing targets fail before the first write unless overwrite is explicit.

The desktop exposes only `BackupOptions.SafeDefaults`. It does not offer full-state backup, does not include SQLite/workspace/outputs/secrets, and never restores into the live app data root automatically.

## Windows Package Contract

The existing Release `win-x64` publish remains framework-dependent by default and supports an explicit self-contained switch. After publish, the script writes a schema-versioned manifest containing relative paths, byte lengths, and SHA-256 values, then creates a ZIP beside the publish directory. Timestamps and machine-absolute paths are not part of the portable integrity contract.

`verify-publish-package.ps1` validates archive path safety, duplicate paths, manifest membership, file sizes, hashes, expected executable/runtime files, and rejects unexpected nested archives. Preflight executes an actual isolated framework-dependent publish/package/verify cycle under ignored `publish/preflight`, then removes that temporary output. No code signing or installer registration is claimed.

## Performance And Accessibility Contracts

The 1,000-row benchmark gets deliberately generous local regression budgets so ordinary host variance does not create a flaky microbenchmark: row projection, initial thumbnail warmup, cached revisit, delivery export, bounded import, and peak managed memory each have an explicit ceiling. Cached revisit must also be materially faster than initial warmup. The benchmark remains a local service-path gate, not a WPF frame-time measurement.

The gallery list receives a stable AutomationId/name, keyboard focus visual, selection semantics, recycling virtualization, and container-focus behavior. Static tests cover system brushes rather than fixed theme colors, minimum-size layout constraints, accessible names for the app's principal forms/commands, and package manifest accessibility metadata. A native packaged-app probe may establish launch/UIA/layout facts on this machine, but it is not Narrator or assistive-technology acceptance.

## Samples And Documentation

The existing physics-poster import/trial assets and scientific-figure operator kit already provide representative sample workflows. The closeout audits their discoverability and validity; it does not create a duplicate sample solely to satisfy roadmap prose.

## Safety And Compatibility

- No paid provider call, network upload, code signing, installation, live data migration, or external publication occurs.
- Backup defaults remain conservative and backward restore of the old unversioned manifest is intentionally rejected because integrity cannot be proven.
- Publish output stays under ignored `publish/`; backup/runtime output stays outside Git.
- WPF changes remain presentation-only and preserve native control behavior.
- Existing queue, diagnostics, provider, persistence, and scientific workflow contracts remain unchanged.

## Acceptance Criteria

- Backup creation is atomic and restore rejects missing/duplicate/tampered/oversized/path-escaping archives before writing.
- The desktop can create and restore a safe backup through localized, keyboard/UIA-readable controls without touching excluded data.
- Release publish produces a ZIP and portable hash manifest; independent verification passes and tampering tests fail.
- The 1,000-row benchmark passes explicit budgets and records a report; gallery virtualization/focus contracts pass.
- Automatable high-contrast/DPI/form/packaged checks have repo-owned evidence, while hardware/manual gaps remain explicit.
- `TASKS`, `ROADMAP`, architecture, guides, review, and evidence agree on repo-side completion versus live acceptance.
- Fixed-order repository gates and a five-axis review pass without paid-provider use.

## Rollback

Revert only the Phase 7 source, tests, scripts, docs, and evidence slice. Generated `publish/`, backup ZIPs, restored test directories, and local benchmark outputs are ignored runtime artifacts and are removed only by the owning verification command. Git rollback is not a runtime-data restore mechanism.
