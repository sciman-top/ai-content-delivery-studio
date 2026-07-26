# Scientific SVG Export Evidence

Date: 2026-07-26

## Scope

This evidence records Task 14 of the trustworthy scientific-figure workflow.
The slice adds:

- an application-owned `IScientificFigureExporter` contract
- exact approved-SVG-hash verification before any export
- bounded, local SkiaSharp PNG and PDF generation from the controlled SVG subset
- source SVG, exporter, version, dimensions, file hash, and semantic hash metadata
- canonical accessibility, element, text/formula, relation, direction, and
  specification-provenance fixtures for downstream contract review
- fail-closed XML, external-reference, unsupported-element, unbound-visible-
  content, numeric, opacity, and render-budget validation
- fake-first desktop exporter registration

It does not persist exports, expose the incomplete workflow in WPF, perform
scientific/visual AI review, approve Gate 2, or assemble a delivery package.

## Authority And Equivalence

- The exporter recomputes SHA-256 over the exact UTF-8 SVG bytes and requires
  equality with both the artifact hash and caller-supplied approved hash.
- Root plan/specification/version metadata must match the SVG artifact.
- Visible text must belong to an approved element or relation; visible paths
  and rectangles must carry relation or specification authority.
- PNG and PDF use the same parsed SVG tree and drawing path.
- Both artifacts record the same source SVG and canonical semantic hash.
- PDF metadata records the accessibility title, source SVG hash, plan,
  specification/version, exporter identity, and exporter version.
- Export dimensions are limited to 16,384 per axis and 100,000,000 pixels.

## Test-First And Review Evidence

The initial focused run failed to compile because exporter contracts and the
implementation did not exist. After implementation, three focused tests passed.

A review mutation with a valid recomputed/approved hash but unbound visible text
then demonstrated that hash approval alone did not constrain structure. The test
failed because no exception was thrown; structural authority validation made the
four Task 14 tests pass.

PDF-skill visual QA then found that relation endpoints at node centers hid the
arrowhead behind the later node rectangle. A renderer regression test failed
with `M 318 400 L 882 400` instead of boundary geometry. The renderer now emits
`M 458 400 L 742 400` and uses `auto-start-reverse` for bidirectional SVG
markers; the combined renderer/export set passes `13 / 13`.

## Artifact QA

A 1200 x 800 fake authority fixture was exported and checked before the
intermediate files were removed:

- source SVG: `sha256:50c155e1418edaf6513692ce3b7d09410982e51f0a34712cf307f772c6fcd299`
- semantic manifest: `sha256:99f2001a5bbd81cd5c2ec28a7be8dbce697db43e0437fc96f82a950edb9b5368`
- PNG decoded at 1200 x 800 with non-white scientific content
- PdfPig extracted `Net force`, `bounded`, and `constrains`
- Poppler reported one unencrypted PDF 1.4 page at 1200 x 800 points
- Poppler-rendered PDF and direct PNG both showed the same two nodes, formula,
  relation label, and visible directed arrow without clipping or overlap

## Compatibility And Supply Chain

- Existing SVG artifact and render-plan contracts remain backward compatible;
  the exporter contract is additive.
- Existing SkiaSharp `3.119.4` is reused; no package or lockfile changed.
- Paid/live provider: `gate_na`; export is deterministic and local.
- Persistence/schema: `gate_na`; Task 14 returns in-memory artifacts only.
- WPF visibility: unchanged; the scientific workflow remains hidden.

## Rollback

Revert the Task 14 commit to remove the exporter contract, implementation,
tests, DI registration, terminology/evidence, and arrow-boundary correction.
The Task 13 SVG authority remains independently usable, but PNG/PDF derivation
and the visual arrow fix will no longer be available.

## Repository Gates

Final closeout uses the fixed repository order:

1. `dotnet build ContentDeliveryStudio.sln`
2. `dotnet test ContentDeliveryStudio.sln --no-build`
3. `pwsh -NoProfile -ExecutionPolicy Bypass -File scripts/verify-reference-evidence.ps1`
4. `dotnet format ContentDeliveryStudio.sln --verify-no-changes --no-restore`
5. `pwsh -NoProfile -ExecutionPolicy Bypass -File scripts/preflight-release.ps1 -NoRestore`

Fresh final outcomes:

- build: exit `0`; `0` warnings, `0` errors
- test: exit `0`; `560 / 560` passed
- contract/invariant: reference evidence exit `0`; format verification exit `0`
- hotspot: preflight exit `0`; repository verification, publish WhatIf,
  placeholder/conflict scans, and diff hygiene passed
