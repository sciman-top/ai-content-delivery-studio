# FOCUS-011 Data-Grounded Deterministic Scientific Charts

**Date:** 2026-08-03
**Status:** repo-side completed with fresh canonical Full evidence.
**Boundary:** no provider, external service, new renderer dependency, desktop launch, or publication was used. Human scientific/visual acceptance remains open.

## Product Result

- A bounded chart domain owns hashed structured rows, unambiguous numeric columns and units, selected row ids, axes, series, explicit transforms, and approval identity.
- Missing/fabricated columns, duplicate or unknown rows, non-finite values, unit mismatch, empty axes, unlabeled transforms, and approval drift fail before rendering.
- The only initial renderer is a repository-owned deterministic bar chart. It emits no interpolated path or curve and records the exact source value and rendered value on every bar.
- Scaling is allowed only with a finite non-zero factor, visible label, and explicit output unit. Aggregation and uncertainty are initially fixed to visible `none` rather than inferred.
- The scientific specification workspace projects the exact rows, axes, units, selection, aggregation, uncertainty, transform, source hash, and approval before Gate 1 through stable UIA ids.
- Delivery packages optionally include `chart-provenance.json`; the manifest binds data hash, chart-spec hash, and renderer version. Gate 2 rejects incomplete or mismatched chart provenance.

## Structured Reference Decision

```reference-decision
{
  "schemaVersion": 1,
  "area": "scientific-figure-workflow",
  "trigger": "deterministic-svg-generation",
  "consultedSources": [
    { "path": "docs/research/SCIENTIFIC_FIGURE_WORKFLOW_RESEARCH.md", "revision": "repo-current-2026-08-03" },
    { "path": "src/ContentDeliveryStudio.Core/ScientificFigures/SvgRenderPlan.cs", "revision": "repo-current-2026-08-03" },
    { "path": "src/ContentDeliveryStudio.Infrastructure/ScientificFigures/DeterministicSvgRenderer.cs", "revision": "repo-current-2026-08-03" }
  ],
  "observedBehavior": "The repository already owns a deterministic SVG authority and explicitly freezes automatic charts from raw data. The product-focus queue reopens only a bounded data-grounded chart contract: values must remain tied to hashed rows and approved transforms, while new chart libraries or inferred analytics would widen supply-chain and scientific-authority risk.",
  "decision": "adapt",
  "affectedContract": "Add a repository-owned structured chart model and deterministic bar renderer with exact point provenance. Require explicit row filters, axes, units, transforms, approval, aggregation=none, and uncertainty=none; reject invalid or invented inputs before rendering. Project these inputs before Gate 1 and preserve them in Gate 2 delivery without adopting a chart framework.",
  "focusedVerification": [
    "dotnet test tests/ContentDeliveryStudio.Tests/ContentDeliveryStudio.Tests.csproj --no-restore --filter FullyQualifiedName~ScientificChart|FullyQualifiedName~ScientificFigureDeliveryPackageTests|FullyQualifiedName~ScientificFigureWorkspaceLayoutTests",
    "pwsh -NoProfile -ExecutionPolicy Bypass -File scripts/verify-reference-evidence.ps1",
    "pwsh -NoProfile -ExecutionPolicy Bypass -File scripts/verify-product-focus-plan.ps1"
  ]
}
```

## Verification Boundary

The fixed corpus is repository-authored structured data and mutation cases. It proves deterministic local contract behavior only; it does not prove arbitrary CSV/Excel import, statistical correctness beyond explicit no-aggregation transforms, native visual quality, accessibility narration, or human scientific acceptance.

## Verification

- Focused chart domain, deterministic rendering, WPF projection, Gate 2, and delivery tests: `25/25` passed.
- Canonical Full: `scripts/verify-repo.ps1 -NoRestore` passed with build `0` warnings / `0` errors and tests `790/790`.
- Reference evidence, product-focus contract, format, and diff hygiene passed.
- Release preflight passed with `84` files in the `win-x64` framework-dependent ZIP; SHA-256 `326fc8fbadbd6b8dc770aa35733fe0f60541eb1f3edeaff4ead2b2ffe551f6a8`.
