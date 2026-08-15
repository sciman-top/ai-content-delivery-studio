# AI Content Delivery Studio

Windows-first desktop workbench for controlled image-series production and evidence-grounded scientific figures.

The repository's real product path is:

```text
source or requirement -> plan -> fake-first generation -> structured review -> human approval -> delivery package
```

`D:\CODE\physicist_chinese_poster_batch_tool` is a production case study, not this application's implementation root.

Chinese entrypoints: [README.zh-CN.md](./README.zh-CN.md) and [docs/zh-CN/README.md](./docs/zh-CN/README.md).

## Product Boundary

- Controlled image series: brief and blueprint comparison, editable plans, durable generation queue, candidate review, explicit human approval, and path-safe delivery.
- Scientific figures: source evidence, approved figure specification, deterministic SVG authority, independent review, bounded repair, two human gates, and evidence-backed packaging.
- Document illustration is an input route into those two production lanes, not a third platform.
- Scenario names such as article, poster, courseware, or report are profiles and delivery categories, not separate workflow engines.
- Fake providers are the default. Real or paid providers require explicit opt-in configuration and current authorization.
- `workspace/`, `outputs/`, local SQLite files, provider secrets, and generated assets stay outside Git.

Historical launch and live-provider claims remain bounded by [docs/V1_LAUNCH_EVIDENCE.md](docs/V1_LAUNCH_EVIDENCE.md). Repository verification never implies a fresh paid-provider, human, hardware, or field acceptance.

## Verify

Focused feedback:

```powershell
.\scripts\verify-repo.ps1 -Mode Quick -TestFilter <focused-filter> -NoRestore
```

Repository closeout:

```powershell
.\scripts\verify-repo.ps1 -Mode Full
```

Release closeout:

```powershell
.\scripts\preflight-release.ps1
```

Quick runs one build and the selected tests. Full runs the non-release suite, reference contract, and diff hygiene once. Release invokes Full once, then adds release-only tests, changed-C# formatting, scans, and publish/package checks.

Publish a local Windows build:

```powershell
.\scripts\publish-app.ps1 -Configuration Release -Runtime win-x64
```

Preview publishing with `-WhatIfOnly`.

## Repository Shape

- `src/ContentDeliveryStudio.App`: WPF shell and user-facing workspaces.
- `src/ContentDeliveryStudio.Application`: product use cases and workflow services.
- `src/ContentDeliveryStudio.Core`: domain models, provider contracts, review, approval, and delivery invariants.
- `src/ContentDeliveryStudio.Infrastructure`: persistence, provider implementations, extraction, rendering, diagnostics, and packaging.
- `tests/ContentDeliveryStudio.Tests`: behavior, persistence, provider-contract, packaging, and release-only coverage.
- `scripts/`: proportional repository verification, publishing, and reference checks.
- `docs/`: durable product, architecture, operator, provider, evidence, research, and ADR material.

## Engineering Posture

- Prefer one concrete product path over extension platforms without consumers.
- Add an abstraction only for two real consumers or one unavoidable external boundary.
- Test observable behavior and contracts; avoid private source-shape and exact-token assertions.
- Use the lowest sufficient verification lane once after inputs are frozen.
- Do not create per-change specs, plans, evidence receipts, queue entries, or governance checks by default.
- Keep text planning, image generation, image editing, and vision review as separate provider contracts.
- Deterministic post-render text composition remains the authority for text-dense output.

The lightweight implementation loop is in [docs/AI_CODING_WORKFLOW.md](docs/AI_CODING_WORKFLOW.md).

## Documentation

- Product: [docs/PRD_V1.md](docs/PRD_V1.md), [docs/PRD_POST_V1_PRODUCT_FOCUS.md](docs/PRD_POST_V1_PRODUCT_FOCUS.md)
- Current external blockers: [docs/TASKS.md](docs/TASKS.md)
- Direction: [docs/ROADMAP.md](docs/ROADMAP.md)
- Architecture: [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md)
- User and operator guidance: [docs/USER_GUIDE.md](docs/USER_GUIDE.md), [docs/OPERATOR_RISK_POLICY.md](docs/OPERATOR_RISK_POLICY.md)
- Provider boundaries: [docs/PROVIDER_CONFIGURATION.md](docs/PROVIDER_CONFIGURATION.md), [docs/PROVIDER_ROUTING_POLICY.md](docs/PROVIDER_ROUTING_POLICY.md)
- Reference routing: [docs/REFERENCE_BASIS.md](docs/REFERENCE_BASIS.md), [docs/REFERENCE_EVIDENCE_POLICY.md](docs/REFERENCE_EVIDENCE_POLICY.md)
- Durable decisions: [docs/adr](docs/adr)

The external reference shelf is read-only input. `scripts/reference-basis.json` is the routing source used to generate the managed section of `docs/REFERENCE_BASIS.md`; neither source overrides repository code, tests, or ADRs.
