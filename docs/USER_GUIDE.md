# User Guide

AI Content Delivery Studio is a Windows desktop workbench for planning, generating, reviewing, repairing, and delivering content packages. Image-series production is the current core workflow. The current implementation uses fake providers by default, so the end-to-end flow can be tested without paid API calls.

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

## Strongest Supported Paths

- Requirement-first image series: the strongest current end-to-end path.
- Plain-text or article illustration planning: a fake-first path that promotes approved targets into the existing image-series workflow.
- Text-heavy educational or poster output: an automated-proof path for the current V1 scope. When readable labels, formulas, or callouts matter, treat generated visuals as background plates and use deterministic post-render composition plus separate readability review.

## Document Illustration

The first document illustration release is fake-provider first. The default path uses fake providers so you can validate the workflow without paid API calls or live provider credentials.

Use the document illustration entry when you want to turn pasted text into illustration directions before promoting approved targets into the existing Plan and Prompts workflow.

- Input supports pasted draft text or plain text content.
- The current slice is designed for concept illustrations and graphical abstract drafts.
- Approved targets are promoted into the existing plan structure instead of creating a separate downstream pipeline.
- Source text should be treated as planning evidence, not as permission to skip review or provenance tracking.
- Real provider execution and binary document extraction are reserved for later slices.

Recommended flow:

1. Paste the source paragraph, abstract, outline, or other plain text content.
2. Choose a draft mode that matches the illustration intent.
3. Review generated illustration targets and prompt directions from the fake path.
4. Approve only the targets worth carrying forward.
5. Promote approved targets into the normal Plan and Prompts workflow for later editing, generation, and review.

### Scholarly Draft Mode

Scholarly draft mode has stricter safety limits.

- Do use it for schematic concepts, graphical abstracts, explanatory diagrams, and background plates.
- Do not use it for fabricated data plots, experimental result images, microscope-like evidence, or any image that could be mistaken for real observed evidence.
- The workflow must not invent evidence images, simulate unpublished results, or imply that generated visuals are authentic scientific observations.

If a target requires evidence-bearing figures, measured plots, or document-native extraction from binary files, stop at planning and handle that requirement in a later slice with explicit provider and extraction support.

## Safety Defaults

- Fake providers are the default path for development and tests.
- Real OpenAI calls require explicit opt-in configuration and user approval.
- API keys are not stored in repo files.
- Local SQLite databases, workspaces, generated outputs, diagnostics, and backup artifacts must stay out of git.
- Diagnostics export records whether a secret exists, not the secret value.
- Safe backup excludes `.env`, local appsettings overrides, SQLite databases, `workspace/`, and `outputs/` by default.

## OpenAI Launch Preflight

Before attempting a live V1 OpenAI sample run, use the built-in read-only OpenAI launch preflight path.

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

Review the generated package before sharing it outside the local machine.

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

Run the standard gate before reporting a build or workflow issue:

```powershell
dotnet build
dotnet test
dotnet format --verify-no-changes
```

For real-provider readiness questions, run the OpenAI launch preflight first and inspect the generated `json` or `md` report before attempting a live sample run.
