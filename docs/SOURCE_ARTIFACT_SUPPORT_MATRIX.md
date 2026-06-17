# Source And Artifact Support Matrix

## Purpose

This matrix defines the intended V1 release boundary for source ingestion, planning evidence, and delivery artifacts.

The goal is to make launch support explicit so roadmap and implementation slices do not silently expand into a general-purpose document automation platform before the primary image-series route is hardened.

## Source Input Matrix

| Source type | V1 status | Primary use | Expected path | Notes |
| --- | --- | --- | --- | --- |
| Short requirement text entered in-app | Launch | Primary launch route | Direct brief creation | This is the main entrypoint. |
| Pasted plain text or article text | Launch | Supporting validation route | Fake-first planning evidence and illustration-target creation | No paid provider required by default. |
| Local `.txt` file | Launch | Supporting validation route | Local text ingestion and planning evidence | Should behave like pasted text. |
| Local `.md` file | Launch | Supporting validation route | Local markdown ingestion and planning evidence | Preserve section structure where feasible. |
| Local reference image (`png`, `jpg`, `webp`) | Launch | Planning and review support | Stored as `SourceAsset`; may inform prompting or review | Not a launch promise for OCR-heavy understanding. |
| Imported physics poster sample artifacts | Launch | Migration proof and evidence | Read-only sample import | Remains a sample, not a second product root. |
| Local `pdf` file | Post-V1 hardening | Future document extraction | Boundary exists; launch does not require broad binary fidelity | Planning fixtures may exist without full support. |
| Local `docx` file | Post-V1 hardening | Future document extraction | Boundary exists; launch does not require broad binary fidelity | No launch promise for high-fidelity structured extraction. |
| Local `pptx` file | Later | Future source ingestion | Contract boundary only | Not a launch blocker. |
| Local `xlsx` or dataset file | Later | Future analytical or diagram workflows | Contract boundary only | Do not imply chart-generation support in V1. |
| OCR scan or image-heavy document | Later | Future extraction hardening | Tool adapter boundary only | Not launch-capable by default. |
| URL snapshot or remote page capture | Later | Future evidence capture | Optional later workflow | Not part of the V1 release promise. |

## Output Artifact Matrix

| Output artifact | V1 status | Primary use | Expected path | Notes |
| --- | --- | --- | --- | --- |
| Image-series delivery folder | Launch | Primary launch output | Delivery export | Canonical V1 artifact. |
| Final images plus alternates | Launch | Production output | Delivery package | Includes provenance metadata. |
| Prompt snapshots and metadata sidecars | Launch | Reproducibility | Delivery package | Required for auditability. |
| JSON or CSV manifest | Launch | Delivery traceability | Delivery export | Must redact secrets. |
| Review report | Launch | Approval evidence | Structured review export | May be JSON, markdown, or equivalent local report. |
| Approval evidence export | Launch | Delivery trust | Delivery export | Required for approved artifacts. |
| Deterministic text-composed poster image | Launch | Educational proof path | Rendered output plus provenance | Required for text-heavy proof path. |
| Validation or diagnostics report | Launch | First real low-risk operator slice | Additive operator output | Should be written to a new folder. |
| PDF delivery package | Post-V1 hardening | Future artifact output | Renderer boundary exists | Not required for launch. |
| DOCX delivery package | Post-V1 hardening | Future artifact output | Renderer boundary exists | Not required for launch. |
| Slide-ready deck or PPTX output | Later | Future multimodal delivery | Renderer boundary only | Not a V1 promise. |
| Automated publish/upload output | Later | Future operator automation | External-system action | Outside V1 operator boundary. |

## Quality Bars For Launch-Capable Paths

Launch-capable paths must meet these quality bars:

- The primary requirement-to-series route works end-to-end with fake providers.
- Launch-capable text inputs can create reviewable planning evidence without hidden manual transformation steps.
- Delivery export preserves provenance, review state, and approval evidence.
- Educational text-heavy output can separate scene generation from deterministic text composition.
- Secrets, raw private paths, and non-exported source details do not leak into delivery manifests by default.

## Current Supported Boundary Language

When the repository reports failures for local `pdf` or `docx` extraction, current truth language should describe the limit as the current supported boundary rather than as a transient implementation slice.

That wording means:

- text-bearing local `pdf` and `docx` extraction is supported within the bounded support matrix
- OCR-heavy, image-only, table-rich, or other high-fidelity binary extraction is still outside the currently supported boundary
- explicit `UseOcr = true` requests against the local binary extractor should fail closed with the same supported-boundary wording instead of implying an older temporary hardening slice
- future slices may widen that boundary, but current operator-facing errors should not imply the repository is still in a one-off temporary landing state

## Explicit N/A For V1

The following are intentionally not launch-capable:

- broad high-fidelity binary extraction across office and PDF formats
- dataset-to-chart automation
- OCR-heavy scan workflows
- direct export back into Word, LaTeX, or slide layouts
- external publishing automation

When implementation touches these boundaries before V1, treat them as planning or contract work unless the roadmap explicitly promotes them into launch scope.
