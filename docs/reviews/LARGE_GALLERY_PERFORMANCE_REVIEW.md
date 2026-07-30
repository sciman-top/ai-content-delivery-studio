# Large Gallery Performance Review

Date: 2026-07-30

Scope: Candidate gallery, delivery rows, imported finalized manifest rows, and future large image-series workspaces.

## Current Baseline

- The MVP stores structured state in SQLite and large assets on the filesystem.
- Delivery and import services pass paths rather than loading image bytes into the domain model.
- Fake providers and tests avoid network and image-model cost.
- Backup and diagnostics defaults avoid copying generated binaries unless explicitly requested by a future workflow.

## Performance Requirements

- Candidate galleries should virtualize rows and thumbnails before supporting large production batches.
- Thumbnail generation should be cached on disk and invalidated by source path plus modified timestamp.
- Full-resolution images should load on demand, not during list population.
- Background generation and review queues must remain bounded by provider, model, and local resource limits.
- Importers must support row limits or paging when reading large manifests.
- Delivery exports should stream/copy files without holding image bytes in memory.

## Known Gaps

- WPF gallery virtualization is now enabled in the gallery list view, and gallery thumbnails are cached on disk when rendered.
- The current benchmark exercises 1,000 candidates and enforces broad regression ceilings: row projection `<= 1 s`, thumbnail warmup `<= 30 s`, cached revisit `<= 5 s`, delivery export `<= 30 s`, bounded import `<= 5 s`, and peak managed memory `<= 512 MiB`. Cached revisit must be at least 25% faster than initial warmup.
- Gallery thumbnail binding now resolves thumbnails asynchronously so first render and scrolling do not synchronously decode source images on the UI binding path.
- The gallery now exposes deferred scrolling, native single-selection, recycling-container keyboard focus, localized UI Automation, and system-color focus/border contracts.
- The benchmark still does not measure real WPF frame timing, subjective scroll responsiveness, or low-memory Windows behavior.
- No separate memory budget test exists for large manifest import outside this benchmark.
- Large image previews have not been tested on low-memory Windows machines.

## Gate

Before release, extend the repeatable local benchmark to create or import at least 1,000 candidate rows with placeholder image paths, then record:

- list population time
- scroll responsiveness
- peak managed memory
- delivery manifest export time
- import row-limit behavior

## Latest Repo-Owned Readout

The 2026-07-30 focused run passed all budgets for 1,000 rows: projection `1 ms`, thumbnail warmup `2,184 ms`, cached revisit `165 ms`, delivery export `4,717 ms`, bounded 250-row import `56 ms`, and peak managed memory `17.18 MiB`. These figures are host-local regression evidence, not hardware certification.
