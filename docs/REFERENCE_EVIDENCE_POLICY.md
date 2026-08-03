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

It does **not** apply to every small edit. Docs-only wording changes, localization text tweaks, ordinary feature ViewModel or view edits, and unrelated view-level cleanup are not blocked by this policy. Large WPF shell, central orchestration, or workflow-boundary changes are intentionally enforced through `workflow-and-ux-architecture`.

## Enforced Change Areas

| Area | Typical source paths | Why it is enforced |
| --- | --- | --- |
| `openai-provider` | `src/ContentDeliveryStudio.Infrastructure/OpenAI/`, `src/ContentDeliveryStudio.Core/Providers/` | Official API semantics, provider-role boundaries, and SDK-vs-raw transport behavior drift over time. |
| `host-and-observability` | `src/ContentDeliveryStudio.App/App.xaml.cs`, `src/ContentDeliveryStudio.App/Telemetry/`, `src/ContentDeliveryStudio.App/Services/ProviderCenterServices.cs`, `src/ContentDeliveryStudio.Infrastructure/Diagnostics/` | Host lifetime, resilience, diagnostics, and telemetry behavior should stay aligned with official .NET guidance. |
| `persistence-and-schema` | `src/ContentDeliveryStudio.Infrastructure/Persistence/`, `src/ContentDeliveryStudio.Core/Projects/`, `src/ContentDeliveryStudio.Core/Artifacts/`, `src/ContentDeliveryStudio.Core/Sources/` | Schema and persistence changes are easy to get subtly wrong without explicit design and provider evidence. |
| `tooling-and-operator` | `src/ContentDeliveryStudio.Application/ToolAdapters/`, `src/ContentDeliveryStudio.Infrastructure/ToolAdapters/`, `src/ContentDeliveryStudio.Core/Operators/` | Local tool execution and operator boundaries need explicit risk and evidence discipline, not ad hoc behavior drift. |
| `workflow-and-ux-architecture` | `src/ContentDeliveryStudio.App/MainWindow.xaml*`, central `MainWindowViewModel`/operation-gate files, `src/ContentDeliveryStudio.Application/Modules/`, `src/ContentDeliveryStudio.Application/Workflows/` | Large WPF shell, central orchestration, and workflow-boundary changes need explicit MVVM and modular-composition evidence. Ordinary feature ViewModel/view edits use focused behavior tests and do not require a ceremonial reference record. |

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
- a new or updated bounded record under `docs/change-evidence/`
- a new or updated file under `docs/superpowers/specs/`
- a new or updated file under `docs/superpowers/plans/`

An accepted path is only a candidate evidence carrier. For each touched enforced area, at least one changed candidate must contain a valid fenced `reference-decision` JSON block:

````markdown
```reference-decision
{
  "schemaVersion": 1,
  "area": "workflow-and-ux-architecture",
  "trigger": "large-viewmodel-split",
  "consultedSources": [
    { "path": "D:/CODE/external/...", "revision": "exact-revision" }
  ],
  "observedBehavior": "What the source and repository currently do.",
  "decision": "adapt",
  "affectedContract": "The repository interface or invariant affected.",
  "focusedVerification": ["exact command or probe"]
}
```
````

`decision` is one of `adopt`, `adapt`, or `reject`. `trigger` must be a declared trigger family for the area. Every consulted source records a path and revision. If no source is available, `consultedSources` may be empty only when `unavailableEvidence` records `reason`, `expiresAt`, and `recoveryCondition`.

The right evidence file depends on the change. For example:

- provider transport or readiness changes should usually update provider docs, launch evidence, or a new spec
- operator boundary changes should usually update operator policy or a new spec
- persistence boundary changes should usually update architecture or research notes
- central shell or workflow-ownership split slices should usually update architecture state or one bounded evidence record; ordinary feature ViewModel/view changes need no reference record

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

The gate inspects the current change set by default. It also accepts explicit paths if a narrower review is needed. A comma-separated `-Paths` value is supported for process-level `pwsh -File` calls.

For the normal repository-wide local gate, run:

```powershell
.\scripts\verify-repo.ps1
```

That wrapper runs `build -> complete test -> reference evidence and governance -> product-focus contract -> format`. `-Mode Quick` stops after build and an explicitly filtered test run and cannot substitute for Full.

For a stronger release-style preflight, run:

```powershell
.\scripts\preflight-release.ps1
```

Release preflight runs release-inclusive repository verification once, including all `Category=ReleaseOnly` tests and complete format rules over the trustworthy changed-C# scope, then adds release-only text scans, actual package verification, and diff hygiene. Formatting-configuration changes or an unknown clean range trigger a full-solution format scan. The GitHub Actions workflow runs Full on pull requests and Release only on pushes to `main` or `master`, avoiding duplicate feature-push and PR release jobs.

The gate passes when:

- no enforced engineering area is touched
- or the change set contains at least one valid, matching structured reference decision for each touched area

Before area matching runs, the same reference gate checks reference-governance parity once through:

```powershell
.\scripts\sync-reference-governance.ps1 -Check
```

That parity check currently enforces two repository-owned truths:

- the generated summary block inside `docs/REFERENCE_BASIS.md` must stay in sync with `scripts/reference-basis.json`
- `scripts/external-reference-shelf.snapshot.json` must stay in sync with the external shelf manifest at `D:\CODE\external\ai-content-delivery-studio-references\references.manifest.json` when that shelf is available on the local machine

The gate fails when:

- an enforced engineering area is touched
- and the change set contains no valid matching structured reference decision for that area

## Relationship To Other Docs

- [DOCUMENTATION_GOVERNANCE.md](./DOCUMENTATION_GOVERNANCE.md) explains which core product docs answer which kinds of questions.
- [EXTERNAL_REFERENCE_STRATEGY.md](./EXTERNAL_REFERENCE_STRATEGY.md) explains which local references should exist and why.
- [REFERENCE_BASIS.md](./REFERENCE_BASIS.md) explains which code areas and task families should consult which local references.
- This file is the enforcement rule for when those references must visibly influence a change.

## Current Limitation

This policy is enforced both locally and in GitHub Actions. Pull requests always use their base/head range. In rare mainline pushes without a meaningful base SHA, Release falls back to parity-only reference verification and a full format scope rather than inventing a diff range.
