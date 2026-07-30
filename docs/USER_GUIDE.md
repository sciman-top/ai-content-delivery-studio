# User Guide

Chinese edition: [zh-CN/USER_GUIDE.md](./zh-CN/USER_GUIDE.md)

AI Content Delivery Studio is a Windows desktop workbench for planning, generating, reviewing, repairing, and delivering content packages. Image-series production is the current core workflow. The current implementation uses fake providers by default, so the end-to-end flow can be tested without paid API calls, while the latest recorded V1 launch snapshot keeps the opt-in live OpenAI route as evidence rather than as a default runtime mode.

## Language

Use the language selector in the title bar to choose:

- `System`
- `中文`
- `English`

Domain identifiers, provider IDs, model IDs, and error strings remain in English. User-facing labels and workflow text are localized.

## Basic Workflow

1. Create or select a project.
2. Start from a short requirement in the Brief area, or use the document illustration entry for plain-text source material.
3. Compare prompt directions or blueprint candidates, then promote the chosen route into the normal Plan and Prompts workflow.
4. Run fake planning, queue, generation, and review actions to exercise the workflow before enabling paid providers.
5. Compare candidates, apply repair guidance, and regenerate where review output indicates a problem.
6. Export a delivery package only after final human approval.

## Generation Queue

The image-series workflow offers two fake-first generation paths:

- Use **Run fake generation** for the compatibility one-click path. It prepares
  the durable queue and immediately executes it.
- Use **Prepare fake queue** when you need operator control. Preparation writes
  ordered queue tasks but makes no provider call. Open **Queue** to select a row,
  then use **Pause**, **Resume**, **Move up**, **Move down**, or **Retry** before
  choosing **Execute queue**.

Pause, resume, reorder, and retry only change local durable state. Retry creates
a new linked task and preserves the original failed or cancelled record. Queued
and paused work remains available after reopening the project; an interrupted
running task is marked failed and is never replayed automatically. Queue Execute
is currently fake-only, and enabling another provider mode does not authorize
paid execution.

## Strongest Supported Paths

- Requirement-first image series: the strongest current end-to-end path and the primary verified launch spine in the latest recorded V1 snapshot.
- Plain-text or article illustration planning: a fake-first path that promotes approved targets into the existing image-series workflow and already has automated route proof for the current V1 scope.
- Text-heavy educational or poster output: an automated-proof path for the current V1 scope. When readable labels, formulas, or callouts matter, treat generated visuals as background plates and use deterministic post-render composition plus separate readability review.
- Trustworthy scientific figures: an accepted post-V1 path for text-bearing physics and natural-science sources, evidence-grounded claims, deterministic SVG/PNG/PDF, three-layer review, two human gates, and provenance-backed delivery.

## Document Illustration

The first document illustration release is fake-provider first. The default path uses fake providers so you can validate the workflow without paid API calls or live provider credentials. The current OpenAI text-planning providers now also support the same document-illustration contract when a real-provider path is explicitly enabled.

Use the document illustration entry when you want to turn pasted text or a bounded local `pdf` / `docx` source file into illustration directions before promoting approved targets into the existing Plan and Prompts workflow.

- Input supports pasted draft text, plain text content, or a bounded local `pdf` / `docx` file import.
- The current supported document-illustration path is designed for concept illustrations and graphical abstract drafts.
- Approved targets are promoted into the existing plan structure instead of creating a separate downstream pipeline.
- Source text should be treated as planning evidence, not as permission to skip review or provenance tracking.
- Real provider execution is now contract-ready for the planning path; local `pdf` / `docx` text extraction is available for the bounded support-matrix slice, while OCR-heavy and high-fidelity binary extraction remain later work.

Recommended flow:

1. Paste the source paragraph, abstract, outline, or other plain text content, or import a local `pdf` / `docx` file into the source text box.
2. Choose a draft mode that matches the illustration intent.
3. Review generated illustration targets and prompt directions from the fake path.
4. Approve only the targets worth carrying forward.
5. Promote approved targets into the normal Plan and Prompts workflow for later editing, generation, and review.

### Scholarly Draft Mode

Scholarly draft mode has stricter safety limits.

- Do use it for schematic concepts, graphical abstracts, explanatory diagrams, and background plates.
- Do not use it for fabricated data plots, experimental result images, microscope-like evidence, or any image that could be mistaken for real observed evidence.
- The workflow must not invent evidence images, simulate unpublished results, or imply that generated visuals are authentic scientific observations.

In the generic document-illustration route, stop at planning when a target needs
evidence-bearing scientific structure. For text-bearing physics and
natural-science sources, use the dedicated trustworthy-scientific-figure route
below. Measured plots, OCR-heavy sources, microscope-like evidence, and any
generated output presented as observed experimental evidence remain outside the
accepted boundary.

## Trustworthy Scientific Figures

The post-V1 scientific-figure workflow is accepted for its bounded scope. Its
status layers are deliberately separate:

- `Implemented`: extraction, claim/evidence understanding, persisted authority,
  deterministic SVG/PNG/PDF, contract/semantic/visual review, bounded repair,
  two gates, five WPF workspaces, and delivery packaging are present.
- `Fake-first verified`: all 12 human-approved corpus baselines replay through
  the workflow and all 40 declared blocking mutations fail closed.
- `Live verified`: run `20260727-150622` completed one mechanism, one concept
  comparison, and one graphical abstract through OpenAI understanding plus
  independent semantic and full-resolution visual review.
- `Human accepted`: reviewer `sciman` accepted all three final live PNGs at
  `2026-07-27T23:57:09.9758439+08:00` with no corrections.

Use the five scientific workspaces in order: Source, Understanding, Figure
Spec, Render & Review, then Delivery. Gate 1 freezes the approved claims,
elements, relations, limitations, and specification version before rendering.
Gate 2 is available only after deterministic contract review and both machine
reviews pass. Human rejection routes back to bounded repair; automatic repair
cannot change scientific meaning or silently replace Gate 1 authority.

The default provider mode remains fake. A live refresh requires explicit paid
call approval and runs:

```powershell
.\scripts\run-scientific-figure-live-acceptance.ps1
```

Do not rerun it merely to re-read accepted evidence. The accepted local report
is under `artifacts/scientific-figure-live-acceptance/20260727-150622`; that
directory and provider secrets stay outside Git. The committed summary is
[20260725-scientific-figure-live-acceptance.md](./change-evidence/20260725-scientific-figure-live-acceptance.md).

For a repeatable local manual trial that does not call a paid provider, use the
[scientific figure fake-first operator trial](./SCIENTIFIC_FIGURE_OPERATOR_TRIAL.md).
The prepared harness remains `pending_operator`; automated checks alone do not
count as an outcome. A human or explicitly user-authorized agent may finalize
the same `operator/manual evidence` after the five visible workspaces and
delivery package gates pass. Agent identity and its authorization reference are
recorded explicitly.

Scientific delivery packages preserve SVG, PNG, PDF, the approved
specification, provenance map, review and repair records, provider metadata,
and both human approvals. Treat every package as an immutable snapshot.

Migration and rollback boundaries:

- Existing image-series projects need no manual migration. Scientific workflow
  persistence is additive and old workspaces remain readable.
- Preserve local SQLite/workspace data and accepted artifact directories before
  changing provider mode or reverting code; Git rollback does not restore local
  runtime data.
- To stop live execution, return to fake provider mode. Reverting a scientific
  implementation commit removes code behavior but must not be represented as
  deleting or revoking already-recorded human evidence.

## Safety Defaults

- Fake providers are the default path for development and tests.
- Real OpenAI calls require explicit opt-in configuration and user approval.
- API keys are not stored in repo files.
- Local SQLite databases, workspaces, generated outputs, diagnostics, and backup artifacts must stay out of git.
- Diagnostics export records whether a secret exists, not the secret value.
- Safe backup excludes `.env`, local appsettings overrides, SQLite databases, `workspace/`, and `outputs/` by default.

## Safe Backup And Restore

Open the Workbench inspector and use **Safe backup and restore**:

1. Choose a source folder and an output ZIP, then select **Create backup**.
2. To restore, choose the ZIP and an empty or non-conflicting separate target folder, then select **Restore backup**.
3. Restore does not overwrite existing files. It validates every path, manifest row, file length, and SHA-256 before writing the first file.

The safe desktop path never includes `.env`, local appsettings overrides, SQLite databases, `workspace/`, `outputs/`, `bin`, or `obj`. It is not a full live-state backup. Preserve SQLite/workspace/output data separately before migration or code rollback.

## Windows ZIP Package

Create and verify a framework-dependent Windows package from the repository root:

```powershell
pwsh -NoProfile -ExecutionPolicy Bypass -File scripts/publish-app.ps1 -Configuration Release -Runtime win-x64 -Clean
pwsh -NoProfile -ExecutionPolicy Bypass -File scripts/verify-publish-package.ps1 -PackagePath publish/ContentDeliveryStudio.App-win-x64-Release.zip
```

The output directory, ZIP, `publish-manifest.json`, and `.sha256` sidecar stay under ignored `publish/`. The target machine needs the .NET 10 Desktop Runtime unless `-SelfContained` is explicitly used. The ZIP is hash-verified but is not signed and does not install/register the application like MSI/MSIX.

Repository accessibility coverage includes keyboard focus, localized UI Automation for the shell and principal forms, a virtualized gallery contract, and PerMonitorV2 declaration. Narrator, actual high-contrast switching, non-default-DPI machines, touch/pen, and low-memory hardware still require manual testing.

## OpenAI Launch Preflight

The latest recorded live OpenAI V1 sample is already captured under `artifacts/live-openai-v1-sample/20260611-132947`. Use the built-in read-only OpenAI launch preflight path before attempting any refresh run, provider-behavior revalidation, or new release-evidence snapshot.

What it checks:

- text-planning readiness
- vision-review readiness
- image-generation readiness
- opt-in smoke-test gating
- blocking reasons that would prevent a live `2-item` sample run

Important behavior:

- It reads provider configuration and secret readiness but does not persist secret values.
- If the real-provider smoke path is not explicitly opted in, the preflight stays in dry-run mode and records the blocking reason instead of attempting paid calls.
- The default opt-in environment variable is `IMAGE_SERIES_STUDIO_OPENAI_REAL_API_SMOKE`, and the enabling value is `1`.

Expected outputs:

- `diagnostics/openai-launch-preflight.json`
- `diagnostics/openai-launch-preflight.md`

Use the preflight result as readiness evidence before recording a new live-provider entry in [V1_LAUNCH_EVIDENCE.md](./V1_LAUNCH_EVIDENCE.md).

## Diagnostics Export

Diagnostics export is the main local support bundle path.

It can include:

- application and machine snapshot
- project and provider summaries
- secret presence flags without secret values
- routed repair-patch summaries
- operator-run audit summaries
- OpenAI launch-preflight readiness snapshots when that preflight has been run
- up to 500 recent validated generation-queue and provider-call events from the local structured journal, plus dropped/invalid counts

Review the generated package before sharing it outside the local machine.

The structured journal is local runtime data under the studio data root. It rotates at 1 MiB and retains no more than three files. It is not a general application-log capture: prompts, provider response bodies, source material, generated content, endpoints, request/response IDs, credentials, `.env` content, full exceptions, and user file paths are outside its schema. Invalid or hand-edited lines are skipped rather than copied into an export. The journal does not upload data and does not enable OTLP or a live provider.

## Sample Migration

The physics poster importer can read selected prompt metadata and finalized delivery manifests from `D:\CODE\physicist_chinese_poster_batch_tool`. It is a sample migration source only. It must not modify that repository, copy large generated binaries by default, or turn physics-specific vocabulary into generic product concepts.

## Delivery Package

Delivery export writes:

- final approved images
- prompt snapshots
- metadata sidecars when present
- `manifest.json`
- `manifest.csv`
- `review-report.md`

Delivery packages are immutable snapshots. Rebuild as a new package when content changes.

## Troubleshooting

Run the canonical repository gate before reporting a build or workflow issue:

```powershell
.\scripts\verify-repo.ps1
```

For stronger release-style validation, use:

```powershell
.\scripts\preflight-release.ps1
```

For real-provider readiness questions, run the OpenAI launch preflight first and inspect the generated `json` or `md` report before attempting a live sample refresh.
