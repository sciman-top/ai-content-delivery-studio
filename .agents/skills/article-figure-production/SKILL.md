---
name: article-figure-production
description: Generate, inspect, repair, and organize article illustration sets from PDF sources in AI Content Delivery Studio. Use for article/PDF illustration production, scientific-figure regeneration, raster-versus-SVG routing, visual/scientific QA, or review-ready preparation in this repository; do not use for unrelated standalone image requests.
---

# Article Figure Production

Produce article-grounded illustrations through the repository's real fake-first workflow, then inspect the latest PNG and PDF renders before reporting them ready for a human gate.

## Choose the rendering authority

- Use the repository's deterministic SVG path for formulas, circuits, force diagrams, quantitative graphs, geometry, arrows, labels, or any relation whose placement carries scientific meaning.
- Use the available `imagegen` skill for raster scenes, photographs, textures, conceptual backgrounds, or reference-image edits where exact geometry and text are not authoritative.
- Use a hybrid only when it adds real value: generate the raster layer first, then compose authoritative text, formulas, arrows, dimensions, and labels deterministically.
- Never rely on an image model to render final Chinese text, equations, numerical values, circuit connections, axes, or scientific directions.

Using Codex image generation creates task assets; it does not prove that the desktop application's live image provider is configured or accepted.

## Run the article workflow

1. Read the source PDF completely enough to understand its claims, figures, tables, and boundary conditions. Visually inspect relevant original pages; text extraction alone is not layout evidence.
2. Confirm that `ArticleScientificFigurePlanningService` admits a matching domain profile from located source evidence. Do not force an unrelated profile. Unsupported, contradictory, OCR-heavy, or missing-evidence inputs fail closed.
3. Use `scripts/run-article-scientific-figure-set.ps1` for a complete set. During iteration use `-OutputClass Validation`, plus separate `-ArticleSlug` and `-RunName` values. Use `-ResolveOutputDirectoryOnly` first when path classification is uncertain.
4. Keep classified output under `article-figure-sets/<article-slug>/<run-name>`. Do not use an explicit output directory unless the user requires a compatibility path and its classification is clear.
5. Use `ReviewReady` only for the latest machine-complete candidate identity after every candidate has been visually inspected. Older attempts belong in `Validation`; do not flatten retries into the category root.

Example shape:

```powershell
pwsh -NoProfile -ExecutionPolicy Bypass -File scripts/run-article-scientific-figure-set.ps1 `
  -SourcePath paper/example.pdf `
  -OutputClass Validation `
  -ArticleSlug example `
  -RunName 20260821-v1
```

## Review the latest artifacts

- Inspect every generated PNG with visual understanding. Check clipping, overlap, missing glyphs, low contrast, inconsistent styles, wrong arrows, incorrect wiring, axes, legends, and source-evidence crops.
- Render every generated PDF through Poppler at a readable DPI and inspect the rerendered pages. PDF existence or text extraction is not visual proof.
- Check the domain review sidecars and report. A fake visual pass and a deterministic scientific pass are separate facts.
- Verify scientific claims against located source evidence and the profile reviewer. Treat model review as advisory; it cannot change accepted scientific meaning.
- For text-dense artwork, retain deterministic post-render text composition rather than asking an image model to regenerate the whole image.

If visual or scientific review finds a defect, identify whether it originates in planning, evidence selection, renderer geometry, reviewer logic, export, or output routing. Repair that causal seam and add a focused regression for the observed failure mode. Regenerate the affected set and inspect the new bytes; do not approve an older rendering after the code changes.

## Authority and closeout

- Fake providers remain the default. Do not call a paid or live provider without explicit authorization for that call.
- Do not self-approve physics expert Gate 1, Gate 2, or delivery merely because machine checks pass. Run review or promotion commands only with current authorization for the exact candidate hashes.
- Report `machine_preflight`, agent visual inspection, deterministic scientific review, Gate 1, Gate 2, delivery, and live acceptance separately.
- Keep generated assets under ignored workspace/output roots. Commit only repository-owned code, tests, scripts, documentation, or this skill—not generated article runs.
- Run the focused affected tests after a repair. Use `scripts/verify-repo.ps1 -Mode Full` only when the change crosses the shared article workflow, provider, persistence, rendering, or delivery contract, or when the repository closeout rules require it.
