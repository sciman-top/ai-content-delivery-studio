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
  -> deterministic optical science package and typed expected visual checks
  -> full-resolution plus responsibility-region visual-provider review
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
- `article-optics-v1` separately blocks formula/domain/sign/unit drift, convex or
  concave lens swaps, `L2/S` plane-order drift, reversed ray propagation,
  convergence/divergence topology changes, unchanged intervention focus, and
  missing source-photo coverage.
- Every crop carries an `ExpectedVisualCheck`: meaning, exact content,
  relationship direction, conditions, forbidden content, evidence block ids,
  and authority status. Article candidates use
  `LocatedSourceEvidencePendingGateOne`; the formal workflow uses
  `ApprovedSpecification`.
- GPT-5.6 Sol scientific review uses `detail: original`; models without original
  detail support use `high`. The configured model is not changed implicitly by
  this policy.
- Visual-provider `fail` or `uncertain` blocks the candidate.
- Automatic repair is limited to three presentation-only attempts. A renderer
  cannot change the candidate id, source references, or scientific scope during
  repair.
- A complete run requires one result for every planned candidate and a review
  record for every generated PNG.

Scientific failures never enter automatic repair. Layout, text, label, and
non-evidentiary asset defects may use the existing three-attempt repair bound;
any scientific relationship change invalidates Gate 1 and requires a person.

## Provider Resume Boundary

Scientific semantic and visual provider requests remain independent Responses
calls with `store: false`; the workflow does not use remote conversation state
or `previous_response_id`. After strict parsing and responsibility-id
validation, the application may atomically persist a local checkpoint containing
only the parsed verdict, findings, trace id, timestamp, and request identity.

The identity binds checkpoint schema, operation, absolute endpoint, exact model,
reasoning effort, and SHA-256 of the same serialized JSON bytes sent over HTTP.
Only an exact identity hit can resume. Missing state continues to the bounded
provider call; malformed, oversized, unknown-field, or identity-mismatched state
fails closed. The resumed result is revalidated against current responsible item
ids and tagged `PersistedCheckpoint`; `Fail` and `Uncertain` cannot become
`Pass`. Fake mode is checked before checkpoint access, so persisted live state
cannot bypass the fake-first opt-in boundary.

No checkpoint contains an API key, input image bytes/data URLs, or a raw provider
response. This is crash/restart recovery, not a new provider acceptance record
and not scientific authority.

## Persistence

The sample runner writes the plan, source-figure audit, per-candidate SVG/PNG/PDF
where applicable, source evidence board, visual review records, repair history,
and an aggregate report under ignored `outputs/article-scientific-figure-runs/`.
This output is auditable local evidence, not Gate 2 delivery.
