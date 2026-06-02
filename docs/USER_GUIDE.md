# User Guide

AI Image Series Studio is a Windows desktop workbench for planning, generating, reviewing, and delivering image series. The current implementation uses fake providers by default, so the end-to-end flow can be tested without paid API calls.

## Language

Use the language selector in the title bar to choose:

- `System`
- `中文`
- `English`

Domain identifiers, provider IDs, model IDs, and error strings remain in English. User-facing labels and workflow text are localized.

## Basic Workflow

1. Create or select a project.
2. Add a series and its items in the Plan area or right inspector.
3. Add prompt versions for selected items.
4. Run fake planning, queue, generation, and review actions to exercise the workflow.
5. Compare candidates and repair prompts when review output indicates a problem.
6. Export a delivery package only after final human approval.

## Document Illustration

The first document illustration release is fake-provider first. The default path uses fake providers so you can validate the workflow without paid API calls or live provider credentials.

Use the document illustration entry when you want to turn pasted text into illustration directions before promoting approved targets into the existing Plan and Prompts workflow.

- Input supports pasted draft text or plain text content.
- The current slice is designed for concept illustrations and graphical abstract drafts.
- Approved targets are promoted into the existing plan structure instead of creating a separate downstream pipeline.
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

Use diagnostics export for support bundles. Review the package before sharing it outside the local machine.
