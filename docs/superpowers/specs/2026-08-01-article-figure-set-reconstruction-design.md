# Article Figure-Set Reconstruction Design

Date: 2026-08-01

## Goal

Extend the article scientific-figure sample from one approved mechanism figure
to a complete candidate figure set that accounts for both missing illustrations
and unclear source figures already embedded in the PDF.

## Authority Boundary

The source PDF remains observational and claim evidence. A reconstructed image
does not become scientific authority merely because it is clearer.

```text
source PDF text and embedded figures
  -> located replacement/consolidation candidates
  -> candidate SVG or source-faithful evidence board
  -> deterministic visual contract and visual-provider review
  -> explicit per-figure Gate 1 before semantic approval or Gate 2
```

Existing experiment photographs must not be replaced with synthesized
observations. Their PDF-embedded JPEG/PNG data may be decoded to source-pixel
PNGs and composed into a labelled evidence board without generative editing.
New deterministic diagrams may explain the optical variables without claiming
that a generated scene is experimental evidence.

## Sample Figure Set

1. Secondary-lens imaging path, replacing low-clarity Figures 1 and 2.
2. Thin-lens equation graph, replacing the small page-3 plot.
3. Screen-versus-retina experiment model, replacing the unclear setup views and
   hand-drawn Figure 5.
4. Observation-position comparison, explaining the Figure 6-8 conditions
   without automatically accepting clarity or perception claims.
5. Corrective-lens control, explaining the Figure 9 intervention without
   asserting a medical conclusion.
6. Source-observation evidence board, composed only from embedded source-photo
   pixels and labelled as evidence rather than causal proof.

## Review And Repair

- Every candidate has source page/figure references and replacement rationale.
- Candidate outputs remain `PendingHumanApproval`; visual pass is not semantic
  or scientific approval.
- Deterministic review blocks blank output, missing candidate watermark,
  missing title/description, and invalid or out-of-canvas text coordinates.
- Visual-provider `fail` or `uncertain` blocks the candidate.
- Automatic repair is limited to three presentation-only attempts. A renderer
  cannot change the candidate id, source references, or scientific scope during
  repair.
- A complete run requires one result for every planned candidate and a review
  record for every generated PNG.

## Persistence

The sample runner writes the plan, source-figure audit, per-candidate SVG/PNG/PDF
where applicable, source evidence board, visual review records, repair history,
and an aggregate report under ignored `outputs/article-scientific-figure-runs/`.
This output is auditable local evidence, not Gate 2 delivery.
