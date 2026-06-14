# Reference Evidence Policy

## Purpose

This policy strengthens the repository's reference discipline for high-drift engineering work.

The project already maintains local reference sources and in-repo research notes. This policy adds a stronger rule: when certain engineering areas change, the change must also leave a visible in-repo evidence trail.
The durable area-to-reference mapping now lives in [REFERENCE_BASIS.md](./REFERENCE_BASIS.md) and the machine-readable source at `scripts/reference-basis.json`.

## When This Policy Applies

This policy applies when a change touches one or more of these engineering areas:

- `openai-provider`
- `host-and-observability`
- `persistence-and-schema`
- `tooling-and-operator`
- `workflow-and-ux-architecture`

It does **not** apply to every small edit. Docs-only wording changes, localization text tweaks, and unrelated tiny view-level cleanup should not be blocked by this policy unless they also touch one of the enforced areas below. Large WPF shell, view-model, or workflow-boundary changes are intentionally enforced through `workflow-and-ux-architecture`.

## Enforced Change Areas

| Area | Typical source paths | Why it is enforced |
| --- | --- | --- |
| `openai-provider` | `src/ImageSeriesStudio.Infrastructure/OpenAI/`, `src/ImageSeriesStudio.Core/Providers/` | Official API semantics, provider-role boundaries, and SDK-vs-raw transport behavior drift over time. |
| `host-and-observability` | `src/ImageSeriesStudio.App/App.xaml.cs`, `src/ImageSeriesStudio.App/Telemetry/`, `src/ImageSeriesStudio.App/Services/ProviderCenterServices.cs`, `src/ImageSeriesStudio.Infrastructure/Diagnostics/` | Host lifetime, resilience, diagnostics, and telemetry behavior should stay aligned with official .NET guidance. |
| `persistence-and-schema` | `src/ImageSeriesStudio.Infrastructure/Persistence/`, `src/ImageSeriesStudio.Core/Projects/`, `src/ImageSeriesStudio.Core/Artifacts/`, `src/ImageSeriesStudio.Core/Sources/` | Schema and persistence changes are easy to get subtly wrong without explicit design and provider evidence. |
| `tooling-and-operator` | `src/ImageSeriesStudio.Application/ToolAdapters/`, `src/ImageSeriesStudio.Infrastructure/ToolAdapters/`, `src/ImageSeriesStudio.Core/Operators/` | Local tool execution and operator boundaries need explicit risk and evidence discipline, not ad hoc behavior drift. |
| `workflow-and-ux-architecture` | `src/ImageSeriesStudio.App/MainWindow.xaml*`, `src/ImageSeriesStudio.App/ViewModels/`, `src/ImageSeriesStudio.App/Views/`, `src/ImageSeriesStudio.Application/Modules/`, `src/ImageSeriesStudio.Application/Workflows/` | Large WPF shell and workflow-boundary changes need explicit MVVM and modular-composition evidence so the UI does not regress into one giant orchestrator. |

## What Counts As Evidence

At least one relevant evidence update must appear in the same change set when an enforced area is touched.

Accepted evidence files:

- `docs/research/REFERENCE_RESEARCH.md`
- `docs/REFERENCE_BASIS.md`
- `docs/ARCHITECTURE.md`
- `docs/PROVIDER_CONFIGURATION.md`
- `docs/PROVIDER_ROUTING_POLICY.md`
- `docs/OPERATOR_RISK_POLICY.md`
- `docs/V1_LAUNCH_EVIDENCE.md`
- a new or updated file under `docs/superpowers/specs/`
- a new or updated file under `docs/superpowers/plans/`

The right evidence file depends on the change. For example:

- provider transport or readiness changes should usually update provider docs, launch evidence, or a new spec
- operator boundary changes should usually update operator policy or a new spec
- persistence boundary changes should usually update architecture or research notes
- workflow/view-model split slices should usually update architecture state, reference basis, or a focused plan/spec

## Local Reference Shelf Priority

When this policy applies, prefer these local references first:

### `openai-provider`

- `D:\CODE\external\ai-content-delivery-studio-references\01-openai`
- then `docs/research/REFERENCE_RESEARCH.md`

### `host-and-observability`

- `D:\CODE\external\ai-content-delivery-studio-references\02-dotnet-wpf`
- `D:\CODE\external\ai-content-delivery-studio-references\08-platform-and-observability`
- then `docs/research/REFERENCE_RESEARCH.md`

### `persistence-and-schema`

- `D:\CODE\external\ai-content-delivery-studio-references\03-data-persistence`
- then `docs/research/REFERENCE_RESEARCH.md`

### `tooling-and-operator`

- `D:\CODE\external\ai-content-delivery-studio-references\05-document-rendering`
- `D:\CODE\external\ai-content-delivery-studio-references\06-automation-testing`
- `D:\CODE\external\ai-content-delivery-studio-references\07-image-workflow-references`
- then `docs/research/REFERENCE_RESEARCH.md`

### `workflow-and-ux-architecture`

- `D:\CODE\external\ai-content-delivery-studio-references\02-dotnet-wpf`
- `D:\CODE\external\ai-content-delivery-studio-references\07-image-workflow-references`
- then `docs/ARCHITECTURE.md` and `docs/TARGET_ENGINEERING_STATE.md`

## Local Gate

Run:

```powershell
.\scripts\verify-reference-evidence.ps1
```

The gate inspects the current change set by default. It also accepts explicit paths if a narrower review is needed.

For the normal repository-wide local gate, run:

```powershell
.\scripts\verify-repo.ps1
```

That wrapper runs this reference-evidence gate first, then the standard `build -> test -> format` sequence.

For a stronger release-style preflight, run:

```powershell
.\scripts\preflight-release.ps1
```

The repository also includes a GitHub Actions workflow at `.github/workflows/verify-repo.yml`. On normal `push` and `pull_request` events with a usable git diff range, that workflow reuses the same gate before running the standard repository verification sequence.

The gate passes when:

- no enforced engineering area is touched
- or the change set contains at least one acceptable evidence-file update for each touched area

Before area matching runs, the repository also checks reference-governance parity through:

```powershell
.\scripts\sync-reference-governance.ps1 -Check
```

That parity check currently enforces two repository-owned truths:

- the generated summary block inside `docs/REFERENCE_BASIS.md` must stay in sync with `scripts/reference-basis.json`
- `scripts/external-reference-shelf.snapshot.json` must stay in sync with the external shelf manifest at `D:\CODE\external\ai-content-delivery-studio-references\references.manifest.json` when that shelf is available on the local machine

The gate fails when:

- an enforced engineering area is touched
- and the change set contains no acceptable evidence-file update for that area

## Relationship To Other Docs

- [DOCUMENTATION_GOVERNANCE.md](./DOCUMENTATION_GOVERNANCE.md) explains which core product docs answer which kinds of questions.
- [EXTERNAL_REFERENCE_STRATEGY.md](./EXTERNAL_REFERENCE_STRATEGY.md) explains which local references should exist and why.
- [REFERENCE_BASIS.md](./REFERENCE_BASIS.md) explains which code areas and task families should consult which local references.
- This file is the enforcement rule for when those references must visibly influence a change.

## Current Limitation

This policy is now enforced both locally and in GitHub Actions, but the CI path still depends on having a usable git diff range from the event payload. In rare cases such as an initial push without a meaningful base SHA, the workflow falls back to the standard repository verification sequence without diff-range reference enforcement.
