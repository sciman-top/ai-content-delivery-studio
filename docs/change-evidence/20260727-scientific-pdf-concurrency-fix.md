# Scientific PDF Concurrency Fix Evidence

Date: 2026-07-27

## Scope And Root Cause

The release hotspot intermittently failed
`ScientificExportEquivalenceTests.Export_PreservesFormulaRelationAccessibilityAndProvenanceFixtures`.
PdfPig could open the generated PDF, but its text layer contained null bytes and
did not preserve `Net force`. The same isolated export passed.

A focused `32`-way concurrent export regression reproduced the identical
failure before the implementation changed. Multiple `SKDocument.CreatePdf`
writers in one process can corrupt native Skia PDF text resources. The failure
was therefore an exporter concurrency defect, not a PdfPig assertion or Task 29
human-review issue.

## Change

- Serialize only the native Skia PDF creation and drawing critical section.
- Keep SVG semantic validation, PNG rendering, hashing, and all other exporter
  work outside the lock.
- Add a concurrent PDF export test that parses every result with PdfPig and
  verifies the approved `Net force` and `bounded` text fixtures.

## Red-Green Evidence

- Before fix: the new `32`-way regression failed with the same null-byte text
  extraction and missing `Net force` assertion.
- After fix: the unchanged regression passed.

## Compatibility, Performance, And Rollback

- Export formats, dimensions, semantic hashes, metadata, and public contracts
  are unchanged.
- Concurrent PDF calls queue only around the native writer. This trades PDF
  throughput for deterministic correctness; PNG and validation remain
  concurrent.
- No dependency, package, lock-file, persistence schema, or migration changed.
- Rollback reverts the exporter lock, concurrency regression, and this evidence.

## Repository Gates

The first fixed-order repository run passed on 2026-07-28:

1. build: exit `0`, `0` warnings, `0` errors
2. test: exit `0`, `664 / 664` passed
3. contract/invariant: scientific reference evidence and format passed
4. hotspot: release preflight, nested repository verification, publish WhatIf,
   placeholder/conflict scans, and diff hygiene passed; the nested suite also
   passed `664 / 664`

A second post-evidence fixed-order run also passed with build `0`
warnings/errors, test `664 / 664`, reference evidence, format, and the complete
release preflight. The final commit preflight repeats the same fixed order so
the submitted tree is covered by fresh verification.
