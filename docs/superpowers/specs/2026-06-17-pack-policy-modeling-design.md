# Pack Policy Modeling Design

## Goal

Add the first scenario-specific pack and policy modeling hardening slice by making the generic image-series workflow pack declare explicit scenario-selection and policy references instead of relying only on informal naming and separate documentation.

This slice should strengthen the pack contract for selection and validation without widening into a general public pack marketplace or a broad workflow redesign.

## Why Now

- `docs/TASKS.md` and `docs/ROADMAP.md` still leave `pack and policy modeling` as an active follow-through lane once a bounded spec and plan exist.
- The repository already has durable pack families, starter pack catalog, and workflow-pack UI defaults, but it does not yet model scenario-selection or policy-linking explicitly in the pack contract.
- `docs/REFERENCE_BASIS.md` already marks `scenario-selection-contract`, `industry-policy-shape`, `renderer-policy-shape`, and `review-rubric-policy-shape` as required triggers for this area.

## Scope

This slice covers:

- explicit scenario tags or IDs on workflow packs for stable scenario selection
- explicit references from the generic image-series workflow pack to industry packs, renderer packs, and review-rubric packs
- registry validation that referenced policy packs exist and remain compatible
- starter-pack catalog updates so the generic workflow pack populates those references
- focused tests for positive and negative validation cases

## Non-Goals

This slice does not include:

- pack import/export UX changes
- new top-level shell tabs or workflow modules
- public user-authored pack editing
- broad policy propagation into every application workflow
- partial-image streaming UX
- OCR or broader source-ingestion work

## Problem

The current pack model has strong metadata for:

- workflow stages
- workflow UI defaults
- blueprint pack references
- pack compatibility and lifecycle state

But the current workflow packs still rely on implicit conventions for:

- what user scenario a workflow pack is meant to serve
- which industry pack should contextualize it
- which renderer policy should constrain downstream artifact shapes
- which review-rubric policy pack should anchor its quality loop

That leaves a contract gap:

- the shell or future planning layers cannot select a workflow pack by a stable scenario token
- registry validation cannot prove that workflow-policy links are complete
- documentation carries more truth than the executable pack contract

## Design

### Workflow Pack Contract Additions

Add additive fields to `WorkflowPack`:

- `ScenarioIds`
- `IndustryPackIds`
- `RendererPackIds`
- `ReviewRubricPackIds`

Rules:

- all IDs are stable normalized pack-like identifiers
- at least one scenario is required
- duplicate IDs are rejected case-insensitively
- empty lists for the policy references are allowed only where the scenario intentionally does not need that policy family

### Scenario Selection Contract

`ScenarioIds` should be product-facing scenario selectors, not free-form marketing text.

Examples:

- `generic-image-series`
- `article-illustration`
- `document-review-translation`
- `courseware-visual`
- `poster-report-delivery`

The first slice should reuse the generic workflow pack ID as the first scenario ID where that keeps the contract simple and stable.

### Registry Validation

`PackRegistry.Create(...)` should additionally verify:

- every `IndustryPackId` on a workflow pack exists in the registry as an `IndustryPack`
- every `RendererPackId` on a workflow pack exists in the registry as a `RendererPack`
- every `ReviewRubricPackId` on a workflow pack exists in the registry as a `ReviewRubricPack`
- every `ScenarioId` is unique across workflow packs, so selection is deterministic in the current registry

Fail closed with explicit messages when a policy reference is missing or a scenario is ambiguous.

### Built-In Catalog

Update `BuiltInPackCatalog` so the generic workflow pack populates the new contract:

- the generic workflow pack declares its scenario ID
- the generic workflow pack references one matching industry pack
- the generic workflow pack references one matching renderer pack
- the generic workflow pack references one matching review-rubric pack

This slice may require adding missing built-in policy packs if the generic workflow currently has no concrete policy-pack counterpart.

### Selection Posture

The slice does not need to expose a full scenario-selection service yet.

It only needs to make selection possible and trustworthy by:

- giving workflow packs stable scenario IDs
- ensuring one workflow pack can be found unambiguously for a scenario in a registry

## Testing Strategy

Add focused tests for:

- workflow packs storing normalized scenario and policy IDs
- registry rejecting missing industry/renderer/review-rubric references
- registry rejecting duplicate scenario IDs across workflow packs
- built-in starter registry satisfying the stronger generic policy-link contract

## Acceptance Criteria

- The generic workflow pack declares explicit scenario-selection and policy-pack references.
- Pack registry validation fails closed when referenced policy packs are missing or scenarios are ambiguous.
- Built-in starter packs satisfy the stronger contract for the generic scenario.
- Tests cover both happy-path and missing-reference behavior.
- `.\scripts\verify-repo.ps1` passes after the slice lands.

## Rollback

Rollback by reverting the additive workflow-pack fields, the registry validation, and the built-in catalog/test updates together.

Because this slice is contract-oriented, partial rollback would leave pack metadata and validation out of sync.
