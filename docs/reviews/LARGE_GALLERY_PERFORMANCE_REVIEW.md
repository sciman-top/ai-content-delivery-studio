# Large Gallery Performance Review

Date: 2026-06-01

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
- The current benchmark exercises 1,000 candidates and records list population time, thumbnail warmup, cached revisit time, delivery export time, import row-limit behavior, and peak managed memory.
- Gallery thumbnail binding now resolves thumbnails asynchronously so first render and scrolling do not synchronously decode source images on the UI binding path.
- The benchmark still does not measure real WPF scroll responsiveness or low-memory Windows behavior.
- No separate memory budget test exists for large manifest import outside this benchmark.
- Large image previews have not been tested on low-memory Windows machines.

## Gate

Before release, extend the repeatable local benchmark to create or import at least 1,000 candidate rows with placeholder image paths, then record:

- list population time
- scroll responsiveness
- peak managed memory
- delivery manifest export time
- import row-limit behavior
