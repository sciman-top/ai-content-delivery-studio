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

The document illustration workflow helps turn article or draft text into planned image targets. The first implementation uses fake providers by default and supports pasted or plain text content. It can create concept illustrations and graphical abstract drafts, then add approved targets to the existing Plan and Prompts workflow.

Scholarly draft mode blocks fake evidence imagery. Use it for schematic concepts, graphical abstracts, and background plates rather than fabricated data plots or experimental images.

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
