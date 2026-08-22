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

- Apply the high-standard acceptance contract, not a path-count proxy. A figure is acceptable only when it is scientifically correct, visually intelligible, plausibly real in structure and context, focused on the article's key point, complete enough to show the causal chain, and materially more useful than the source paragraph alone. The contract is: "形象易懂、科学真实、重点突出、内容完整、图形独立承载信息、具有文章信息增益".
- Treat these as separate checks:
  - Scientific truth: objects, directions, formulas, units, causal claims, boundary conditions, and comparison scope are correct; a beautiful but misleading diagram fails.
  - Visual intelligibility: a reader can identify the apparatus, states, start/end points, and principal change within a few seconds; spatial layout carries meaning rather than merely repeating labels.
  - Structural realism: apparatus, rays, fluid paths, wires, fields, and state transitions are visibly connected and plausible. "Realistic" means credible structure and physics, not necessarily photorealistic rendering.
  - Focus: the visual hierarchy makes the article's key question dominant; secondary annotations cannot compete with the mechanism or comparison.
  - Completeness: include the necessary participants and links in the chain input -> mechanism -> intermediate state -> observation/output. Do not add decorative complexity to compensate for a missing causal participant.
  - Information gain: the figure must answer a question faster or more accurately than prose. If hiding the explanatory text leaves only a title, boxes, generic arrows, or disconnected lines, fail it.
- Use the following visual-understanding tests for every candidate:
  - Hide the title and explanatory prose. The remaining geometry must still communicate the subject and its main relation.
  - Apply a three-second test: identify what is being compared or explained without reading the article paragraph.
  - Apply an endpoint/topology test: trace every principal ray, flow path, circuit, and coupling from source to destination; reject floating labels and visually implied but unconnected wires.
  - Apply a counter-misreading test: ask what a novice could incorrectly infer (for example, an image position being a physical screen, a heater being in series, or a fan crossing being a no-work Bernoulli segment); repair the drawing if that misreading is plausible.
  - Apply a focus/thumbnail test: at article size, the main apparatus and relation remain obvious; whitespace, color, and text do not bury the key visual.
  - Apply a comparison test: both sides of a comparison show comparable objects, paths, and observation conditions; background color and labels alone are not evidence.
- Classify outcomes explicitly: `machine_preflight` means files and deterministic invariants are valid; `agent_visual_inspection` means the actual PNG/PDF pixels passed the above visual tests; `Gate 1` remains a human/scientific expert decision; `Gate 2` and delivery are later states. Never promote a machine pass to a visual or expert pass.
- Apply a semantic-utility gate before checking polish. Mentally hide the title and explanatory prose: the remaining picture must still communicate the subject through apparatus, objects, spatial relationships, topology, rays, fields, states, or quantitative marks. A label-only card, empty comparison box, single generic arrow, or prose broken across a canvas is not an illustration and must fail review.
- Require every figure to earn its place in the article. It must answer a question that is materially faster or clearer to understand visually than from the source paragraph alone. Delete or merge redundant candidates instead of preserving a fixed candidate count.
- For mechanism and experiment figures, verify that all causally necessary participants are visible and connected. For comparison figures, both states must contain comparable visual evidence rather than differently colored empty panels. For circuits and apparatus, wiring/topology must be explicit; labels floating without connections fail.
- Inspect every generated PNG with visual understanding. Check clipping, overlap, missing glyphs, low contrast, inconsistent styles, wrong arrows, incorrect wiring, axes, legends, and source-evidence crops.
- Render every generated PDF through Poppler at a readable DPI and inspect the rerendered pages. PDF existence or text extraction is not visual proof.
- Check the domain review sidecars and report. A fake visual pass and a deterministic scientific pass are separate facts.
- Verify scientific claims against located source evidence and the profile reviewer. Treat model review as advisory; it cannot change accepted scientific meaning.
- For text-dense artwork, retain deterministic post-render text composition rather than asking an image model to regenerate the whole image.

If visual or scientific review finds a defect, identify whether it originates in planning, evidence selection, renderer geometry, reviewer logic, export, output routing, or the skill's acceptance wording. Repair every causal seam that admitted the defect and add a focused regression for the observed failure mode. Regenerate the affected set and inspect the new bytes; do not approve an older rendering after the code changes.

## Authority and closeout

- Fake providers remain the default. Do not call a paid or live provider without explicit authorization for that call.
- Do not self-approve physics expert Gate 1, Gate 2, or delivery merely because machine checks pass. Run review or promotion commands only with current authorization for the exact candidate hashes.
- Report `machine_preflight`, agent visual inspection, deterministic scientific review, Gate 1, Gate 2, delivery, and live acceptance separately.
- Keep generated assets under ignored workspace/output roots. Commit only repository-owned code, tests, scripts, documentation, or this skill—not generated article runs.
- Run the focused affected tests after a repair. Use `scripts/verify-repo.ps1 -Mode Full` only when the change crosses the shared article workflow, provider, persistence, rendering, or delivery contract, or when the repository closeout rules require it.
