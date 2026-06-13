# AI Content Delivery Studio

Windows-first AI content delivery workbench with image-series production as the current core launch path.

This repository is the active implementation home for the product. It is intentionally separate from `D:\CODE\physicist_chinese_poster_batch_tool`, which remains a production case study and migration sample rather than the implementation root. The product-facing name is `AI Content Delivery Studio`, while the active repository, solution, and namespaces still use `ai-image-series-studio` / `ImageSeriesStudio` until the staged rename gate in [docs/adr/0008-product-identity-and-repository-rename.md](docs/adr/0008-product-identity-and-repository-rename.md) is executed.

## Current Readout

- Active implementation root: `D:\CODE\ai-image-series-studio`
- Intended medium-term root after the rename gate: `D:\CODE\ai-content-delivery-studio`
- Latest local repository verification: `2026-06-13` via `.\scripts\verify-repo.ps1` with `382 / 382` tests passing
- Latest recorded V1 release-verification snapshot: `2026-06-11`
- Latest recorded live OpenAI sample: `artifacts/live-openai-v1-sample/20260611-132947`
- Current launch-verification readout: the latest recorded snapshot in [docs/V1_LAUNCH_EVIDENCE.md](docs/V1_LAUNCH_EVIDENCE.md) closes all `5 / 5` V1 launch metrics
- Current strongest user-visible routes:
  - short requirement -> brief -> blueprint -> series -> review -> delivery
  - plain-text or article -> evidence anchors -> illustration targets -> promoted downstream workflow
  - text-heavy educational poster -> deterministic text composition -> approval evidence export
- Current bounded built-in local tool adapters in the desktop host:
  - `artifact-validation`
  - `deterministic-text-composition`
  - read-only `openai-launch-preflight`

Important truth boundary:

- `README.md` is an overview and local-start entrypoint.
- Current V1 release-claim truth lives only in [docs/V1_LAUNCH_EVIDENCE.md](docs/V1_LAUNCH_EVIDENCE.md).
- Product promise lives in [docs/PRD_V1.md](docs/PRD_V1.md).

## Product Goal

Help a user turn a requirement, source file, or draft into a reviewed delivery package. The current launch spine remains image-series-first:

1. Capture goal, audience, constraints, references, and quality standards.
2. Produce a reusable brief, blueprint candidates, and a promoted route.
3. Generate candidate visuals in controlled batches.
4. Review candidates against structured rubric criteria with human final approval.
5. Repair the correct layer: brief, blueprint, prompt, settings, deterministic composition, or source evidence.
6. Export a delivery package with images, prompt snapshots, provenance, review evidence, and manifests.

## Quick Start

### Verify The Repository

Use the canonical local full gate:

```powershell
.\scripts\verify-repo.ps1
```

This runs the repository-local reference-evidence gate first, then `dotnet build`, `dotnet test`, and `dotnet format --verify-no-changes`.

### Run The Stronger Release-Style Preflight

```powershell
.\scripts\preflight-release.ps1
```

This adds placeholder and merge-conflict scans, reuses the reference-evidence gate, runs the canonical repository verification path, performs a publish dry run, and finishes with diff-hygiene checks.

### Publish A Local Windows Build

```powershell
.\scripts\publish-app.ps1 -Configuration Release -Runtime win-x64
```

Preview only:

```powershell
.\scripts\publish-app.ps1 -WhatIfOnly
```

### Docs-Only Hygiene Check

```powershell
rg -n "(TB[D]|TO[D]O|PLACE''HOLDER)" .
git status --short
```

## Repository Shape

### Source Projects

- `src/ImageSeriesStudio.App`: WPF shell, localization, workbench UI, diagnostics, and operator-facing flows.
- `src/ImageSeriesStudio.Application`: application services, workflow coordinators, repair routing, and delivery orchestration.
- `src/ImageSeriesStudio.Core`: domain model, provider contracts, workflow records, review and delivery invariants.
- `src/ImageSeriesStudio.Infrastructure`: EF Core persistence, provider adapters, local tool adapters, diagnostics, and composition services.

### Supporting Areas

- `tests/ImageSeriesStudio.Tests`: unit, SQLite reload, workflow, provider, launch-route, and delivery verification coverage.
- `docs/`: product, architecture, roadmap, tasks, launch evidence, provider policy, operator policy, ADRs, and reference governance.
- `scripts/`: repository verification, release preflight, publish, reference-evidence, and related local automation.
- `artifacts/`: checked-in evidence bundles such as live OpenAI sample outputs and diagnostics reruns.
- `workspace/`: local project state and runtime data, ignored by git.
- `outputs/`: generated delivery outputs, ignored by git.

## Current Launch Boundary

V1 is intentionally narrower than the long-term multimodal vision.

- Primary launch route: short requirement -> `CreativeBrief` -> `DesignBlueprint` -> image-series workflow -> review -> approved `DeliveryPackage`
- Supporting validation route: article or plain text -> evidence anchors -> illustration targets -> promoted downstream workflow
- Proof path: generated background plate -> deterministic text, formula, and label composition -> approval evidence export

The authoritative V1 launch frame lives in:

- [docs/PRD_V1.md](docs/PRD_V1.md)
- [docs/V1_LAUNCH_EVIDENCE.md](docs/V1_LAUNCH_EVIDENCE.md)
- [docs/ROADMAP.md](docs/ROADMAP.md)
- [docs/TASKS.md](docs/TASKS.md)

## Engineering Posture

- Fake-first and local-first remain the default development and regression posture.
- Real OpenAI behavior is opt-in and must stay behind explicit provider configuration, preflight, and evidence review.
- Deterministic text composition is part of the proven V1 path for text-heavy educational or poster-style outputs.
- Operator automation stays bounded: low-risk local validation and preparation paths first, broader side-effectful automation later.
- The current transport split is deliberate: stable single-shot image generation uses the official OpenAI .NET SDK Images path, while some Responses-backed planning and review flows still keep raw `HttpClient` where the SDK surface remains incomplete for the current contract needs.

## Documentation Map

### Start Here

- [docs/DOCUMENTATION_GOVERNANCE.md](docs/DOCUMENTATION_GOVERNANCE.md)
- [docs/PRD_V1.md](docs/PRD_V1.md)
- [docs/V1_LAUNCH_EVIDENCE.md](docs/V1_LAUNCH_EVIDENCE.md)
- [docs/ROADMAP.md](docs/ROADMAP.md)
- [docs/TASKS.md](docs/TASKS.md)

### Product And Architecture

- [docs/PRODUCT_DESIGN.md](docs/PRODUCT_DESIGN.md)
- [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md)
- [docs/TARGET_ENGINEERING_STATE.md](docs/TARGET_ENGINEERING_STATE.md)
- [docs/SOURCE_ARTIFACT_SUPPORT_MATRIX.md](docs/SOURCE_ARTIFACT_SUPPORT_MATRIX.md)
- [docs/USER_GUIDE.md](docs/USER_GUIDE.md)

### Provider, Operator, And Evidence Governance

- [docs/PROVIDER_CONFIGURATION.md](docs/PROVIDER_CONFIGURATION.md)
- [docs/PROVIDER_ROUTING_POLICY.md](docs/PROVIDER_ROUTING_POLICY.md)
- [docs/OPERATOR_RISK_POLICY.md](docs/OPERATOR_RISK_POLICY.md)
- [docs/REFERENCE_EVIDENCE_POLICY.md](docs/REFERENCE_EVIDENCE_POLICY.md)
- [docs/REFERENCE_BASIS.md](docs/REFERENCE_BASIS.md)
- [docs/research/REFERENCE_RESEARCH.md](docs/research/REFERENCE_RESEARCH.md)

### ADRs And Plans

- [docs/adr/0001-independent-repo-and-stack.md](docs/adr/0001-independent-repo-and-stack.md)
- [docs/adr/0002-api-boundaries-and-review-loop.md](docs/adr/0002-api-boundaries-and-review-loop.md)
- [docs/adr/0008-product-identity-and-repository-rename.md](docs/adr/0008-product-identity-and-repository-rename.md)
- [docs/adr/0009-openai-dotnet-sdk-adoption.md](docs/adr/0009-openai-dotnet-sdk-adoption.md)
- [docs/superpowers/specs/2026-05-31-ai-image-series-studio-design.md](docs/superpowers/specs/2026-05-31-ai-image-series-studio-design.md)
- [docs/superpowers/plans/2026-05-31-ai-image-series-studio-implementation.md](docs/superpowers/plans/2026-05-31-ai-image-series-studio-implementation.md)
- [docs/superpowers/plans/2026-06-07-v1-launch-hardening.md](docs/superpowers/plans/2026-06-07-v1-launch-hardening.md)

## Local Reference Shelf

A local external reference shelf is available at:

`D:\CODE\external\ai-content-delivery-studio-references`

Use it for quick local lookup of official SDKs, WPF and host patterns, EF Core and SQLite references, resilience and observability internals, deterministic document tooling, and image workflow architecture references. These materials inform engineering decisions, but they do not override this repository's source, tests, ADRs, or project rules.
