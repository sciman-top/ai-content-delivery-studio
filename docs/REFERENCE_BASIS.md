# Reference Basis

Last reviewed: 2026-07-06.

This document turns the repository's reference strategy into an actionable mapping:

- which code areas and task types map to which local references
- which references are official-first versus inspiration-only
- what may be reused directly, adapted carefully, or only studied conceptually
- when the repository gate should require visible reference evidence

Use this document together with:

- [REFERENCE_EVIDENCE_POLICY.md](./REFERENCE_EVIDENCE_POLICY.md)
- [DOCUMENTATION_GOVERNANCE.md](./DOCUMENTATION_GOVERNANCE.md)
- `D:\CODE\external\ai-content-delivery-studio-references\README.md`

## Proportional Rule

When external source can materially change a decision in a mapped area, the engineer must:

1. consult the mapped local reference shelf first
2. prefer official documentation or official source repositories before community examples
3. run the explicit decision check and update an existing durable evidence surface when the decision must persist

The local enforcement entrypoint is:

```powershell
.\scripts\verify-reference-evidence.ps1 -RequireDecision
```

Ordinary changes in a mapped directory use the default parity/advisory check and do not require an evidence receipt.

The machine-sync entrypoint is:

```powershell
.\scripts\sync-reference-governance.ps1
```

That script owns the generated summary block in this document and the repo-side snapshot at `scripts/external-reference-shelf.snapshot.json`.

## Reuse Levels

| Level | Meaning |
| --- | --- |
| `direct-pattern` | Safe to borrow structure or API usage patterns with light adaptation. |
| `adapt-with-review` | Useful reference, but local constraints and contracts must be rechecked before reuse. |
| `inspiration-only` | Study architecture or UX ideas only. Do not treat as implementation default. |

<!-- BEGIN GENERATED REFERENCE BASIS SUMMARY -->

## Machine-Checked Summary

This section is generated from `scripts/reference-basis.json` by `scripts/sync-reference-governance.ps1`.
Do not edit this block by hand. Update the JSON manifest and rerun the sync script instead.

- Manifest version: `2`
- Manifest updatedAt: `2026-08-15T00:00:00+08:00`

### `openai-provider`

- `required`: `true`
- Source rules: `src/ContentDeliveryStudio.Infrastructure/OpenAI/`, `src/ContentDeliveryStudio.Core/Providers/`
- Evidence rules: `docs/research/REFERENCE_RESEARCH.md`, `docs/PROVIDER_CONFIGURATION.md`, `docs/PROVIDER_ROUTING_POLICY.md`, `docs/V1_LAUNCH_EVIDENCE.md`, `docs/REFERENCE_BASIS.md`, `docs/change-evidence/`
- Required triggers: `request-response-shape`, `images-vs-responses-routing`, `structured-output`, `vision-review`, `real-provider-enablement`
- Local references:
  - `D:/CODE/external/ai-content-delivery-studio-references/01-openai/openai-dotnet` (kind: `official-source`; reuse: `direct-pattern`)
  - `D:/CODE/external/ai-content-delivery-studio-references/01-openai/openai-cookbook-selected` (kind: `official-examples`; reuse: `adapt-with-review`)

### `persistence-and-schema`

- `required`: `true`
- Source rules: `src/ContentDeliveryStudio.Infrastructure/Persistence/`
- Evidence rules: `docs/research/REFERENCE_RESEARCH.md`, `docs/ARCHITECTURE.md`, `docs/REFERENCE_BASIS.md`, `docs/change-evidence/`
- Required triggers: `entity-configuration`, `migration-behavior`, `sqlite-limitation`, `project-load-save-contract`
- Local references:
  - `D:/CODE/external/ai-content-delivery-studio-references/03-data-persistence/EntityFramework.Docs` (kind: `official-doc-source`; reuse: `direct-pattern`)

### `delivery-package`

- `required`: `true`
- Source rules: `src/ContentDeliveryStudio.Application/Delivery/`, `src/ContentDeliveryStudio.Infrastructure/Delivery/`
- Evidence rules: `docs/ARCHITECTURE.md`, `docs/V1_LAUNCH_EVIDENCE.md`, `docs/REFERENCE_BASIS.md`, `docs/change-evidence/`
- Required triggers: `delivery-manifest-contract`, `package-hash-contract`, `package-path-containment`, `document-rendering`
- Local references:
  - `D:/CODE/external/ai-content-delivery-studio-references/05-document-rendering/QuestPDF` (kind: `community-source`; reuse: `adapt-with-review`)

### `scientific-figure-workflow`

- `required`: `true`
- Source rules: `src/ContentDeliveryStudio.Core/ScientificFigures/`, `src/ContentDeliveryStudio.Application/ScientificFigures/`, `src/ContentDeliveryStudio.Infrastructure/ScientificFigures/`
- Evidence rules: `docs/research/SCIENTIFIC_FIGURE_WORKFLOW_RESEARCH.md`, `docs/ARCHITECTURE.md`, `docs/SOURCE_ARTIFACT_SUPPORT_MATRIX.md`, `docs/REFERENCE_BASIS.md`, `docs/REFERENCE_EVIDENCE_POLICY.md`, `docs/change-evidence/`
- Required triggers: `scholarly-source-extraction`, `claim-evidence-authority`, `formula-to-svg`, `deterministic-svg-generation`, `svg-to-png-pdf-export`, `scientific-contract-review`
- Local references:
  - `D:/CODE/external/ai-content-delivery-studio-references/05-document-rendering/GROBID` (kind: `community-source`; reuse: `adapt-with-review`)
  - `D:/CODE/external/ai-content-delivery-studio-references/05-document-rendering/PdfPig` (kind: `community-source`; reuse: `direct-pattern`)
  - `D:/CODE/external/ai-content-delivery-studio-references/05-document-rendering/MathJax` (kind: `official-source`; reuse: `adapt-with-review`)
  - `D:/CODE/external/ai-content-delivery-studio-references/05-document-rendering/Svg.Skia` (kind: `community-source`; reuse: `direct-pattern`)

<!-- END GENERATED REFERENCE BASIS SUMMARY -->
