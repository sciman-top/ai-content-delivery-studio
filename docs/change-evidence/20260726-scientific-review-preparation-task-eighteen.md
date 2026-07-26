# Scientific Review Preparation Task 18 Evidence

Date: 2026-07-26

## Scope

This evidence records Task 18 of the trustworthy scientific-figure workflow.
The slice adds:

- local scientific review manifests without filesystem path fields
- minimum approved evidence selections projected to claim/block/page/role
- critical SVG element and relation structure rows with bounded pixel regions
- real lossless PNG region crops produced from the full-resolution export
- formula, legend, element, and relation crop types
- full-resolution pixel/byte, crop-count, and per-crop byte guards
- exact SVG/export hash and authority validation before artifact preparation

It does not invoke a provider, route repairs, approve Gate 2, expose WPF UI,
persist a delivery package, or replace the separate Task 16 contract review.

## Trust And Resource Boundaries

- Review preparation accepts only matching specification, plan, SVG, and
  hash-bound export identities.
- Every critical render-plan element and relation must resolve to exactly one
  SVG structure row and one crop; missing rows fail closed.
- Crops are decoded from the authority PNG with existing SkiaSharp and retain
  original-resolution coordinates; the generic 384px/JPEG-70 path is not used.
- The full image is limited to 100 million pixels and 25 MiB, crop count to
  128, and each crop to 5 MiB.
- Oversized full output fails before the cropper is called.
- Exportable structure summaries redact Windows drive paths and `/Users` or
  `/home` paths as `[redacted-local-path]`.
- Manifests omit evidence quotations and retain only the minimum identifiers
  required to trace the separately bounded semantic request.

## Focused Verification

- `ScientificReviewPrepTests`: `5 / 5` passed
- `VisionReviewExecutionPolicyTests`: `6 / 6` passed, including two new
  scientific full-resolution budget tests
- Real crop assertions verify PNG signatures for every critical item.
- Adversarial checks cover missing critical SVG items, oversized PNG bytes
  before crop execution, and an explicit `D:\private\...` redaction mutation.

## Compatibility And N/A

- Paid/live provider: `gate_na`; review preparation is local only.
- New dependency/supply chain: `gate_na`; the cropper reuses existing SkiaSharp.
- Persistence/schema: `gate_na`; no stored schema changes.
- WPF acceptance: deliberately open; user visibility remains false.
- Existing generic compact-review artifacts and policies remain unchanged.

## Rollback

Revert the Task 18 commit to remove the scientific manifest builder, execution
budget, Skia cropper, tests, terminology, status, and evidence. Tasks 6-17
remain valid, and scientific provider dispatch remains blocked by missing prep.

## Repository Gates

Final closeout uses the fixed repository order:

1. `dotnet build ContentDeliveryStudio.sln`
2. `dotnet test ContentDeliveryStudio.sln --no-build`
3. `pwsh -NoProfile -ExecutionPolicy Bypass -File scripts/verify-reference-evidence.ps1`
4. `dotnet format ContentDeliveryStudio.sln --verify-no-changes --no-restore`
5. `pwsh -NoProfile -ExecutionPolicy Bypass -File scripts/preflight-release.ps1 -NoRestore`

Fresh final outcomes:

- build: exit `0`; `0` warnings, `0` errors
- test: exit `0`; `587 / 587` passed
- contract/invariant: reference evidence exit `0`; format verification exit `0`
- hotspot: preflight exit `0`; repository verification, publish WhatIf,
  placeholder/conflict scans, and staged/unstaged diff hygiene passed
