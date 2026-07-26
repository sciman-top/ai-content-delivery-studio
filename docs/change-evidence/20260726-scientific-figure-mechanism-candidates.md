# Scientific Figure Mechanism Candidate Evidence

Date: 2026-07-26

## Scope

This evidence records the machine-verifiable preparation boundary for Task 3
of the trustworthy-scientific-figure plan. Four mechanism/process candidates
and draft gold baselines are ready for human review.

This is not Task 3 closeout. No item has `admissionStatus: accepted`, no
baseline has `humanReview.status: accepted`, and the corpus remains
`admissionState: building`. The 12-item corpus, Checkpoint 0, runtime
implementation, and live-provider acceptance remain open.

Risk: low to medium. The change adds evaluation data, source metadata, draft
scientific assertions, and stronger repository contract checks. It does not
change product runtime behavior, dependencies, persistence, provider
configuration, user workspaces, or generated assets.

## Candidates

The four candidates use first-party sections of the OpenStax *Physics* open
textbook:

| Item | Domain | Bounded mechanism |
| --- | --- | --- |
| `mechanics-two-car-momentum-transfer` | Mechanics | Before/contact/after momentum transfer between two cars |
| `thermal-heat-engine-energy-flow` | Thermal physics | Hot reservoir, engine, work output, and rejected heat |
| `optics-convex-lens-real-image` | Optics | Three principal rays and a numerically constrained real image |
| `electromagnetism-rotating-coil-generator` | Electromagnetism | Rotation, changing flux, induced current, and sinusoidal emf |

Each draft records claims, exact source locations, required elements,
relations and directions, conditions, formulas, values and units where
applicable, allowed visual variation, scientific blocking mutations, and
visual blocking mutations. The detailed primary-source review is in
`docs/research/SCIENTIFIC_FIGURE_MECHANISM_CORPUS_CANDIDATES.md`.

Evidence anchors distinguish verbatim prose from normalized inline text and
normalized equations. This prevents MathML/subscript transcription from being
misrepresented as an exact plain-text quotation.

## License And Source Evidence

- Publisher/source authority: OpenStax official book, HTML sections, and PDF.
- License: `CC-BY-4.0`.
- License evidence:
  `https://openstax.org/books/physics/pages/preface#fs-id1163975727583`.
- Redistribution verdict: allowed with attribution and change notice.
- No source figure or embedded artwork is copied into the repository.
- No PDF, extracted text, or other source binary is tracked by Git.

The four section-level source records intentionally share one official
textbook artifact hash while retaining unique `sourceId`, evidence locations,
figure objectives, and baseline paths.

## Local Extraction Evidence

The official PDF was copied into the ignored local corpus cache and verified:

- local cache key:
  `openstax/physics/2026-07-26/Physics-WEB-a3f75487411e.pdf`
- PDF size: `57,463,331` bytes
- PDF SHA-256:
  `a3f75487411ef13d0270c65fc801ceff2b28e6b339afed9b407fe477f7e8453e`
- extractor: Poppler `pdftotext 24.04.0`
- command: `pdftotext -layout <pdf> <text>`
- extracted text size: `2,804,564` bytes
- text SHA-256:
  `331a75a0d1b4238943d24bfb8bef478216979abe29794e20f0ed5ca4f1a62bf4`

Both cached files matched
`.gitignore:8:/eval/scientific-figures/.cache/`. The research probe directory
also still exists at
`C:\Users\sciman\AppData\Local\Temp\cds-task3-mechanism-3eea487a73c642b2ac49d6f255a86163`;
no removal is claimed.

## Contract And Test-First Evidence

The corpus contract now represents the source honestly as `open-textbook`.
Every source also carries a machine-readable extraction assessment. Draft
human reviews do not invent a reviewer or date; accepted/rejected decisions
require both.

Test-first sequence:

1. extraction/human-review contract tests initially returned `2 / 12` failed;
   the schema and validator changes made `12 / 12` pass.
2. the four-item mechanism test initially returned expected `4`, actual `0`.
3. after each candidate/baseline increment, actual counts progressed through
   `1`, `2`, and `3`, with all generic per-item checks passing.
4. after the fourth candidate, focused tests returned `14 / 14` passed.
5. a dangling anchor/relation-endpoint negative test failed before the
   repository validator enforced semantic references, then passed after the
   guard was added.

Python `jsonschema 4.26.0` independently checked both Draft 2020-12 schemas,
the building manifest, and all four baselines. A semantic link probe also
confirmed unique anchor/element IDs, resolvable anchor references, and valid
relation endpoints.

## Human Review Gate

The following decisions remain explicitly open for a human reviewer:

- accept, revise, reject, or replace each source and figure objective;
- verify every scientific claim and equation against its source context;
- verify all required elements, directions, conditions, values, and units;
- verify allowed variation and both mutation classes;
- record reviewer identity and review date only after the decision.

Until those decisions occur, the repository must preserve
`candidate / draft / building`. Machine tests passing cannot authorize a
transition to `accepted`.

## Compatibility And N/A

- Backward compatibility: preserved; the source-type enum is additive and no
  existing runtime consumer reads the evaluation manifest.
- Runtime dependency adoption: `gate_na`; no package or executable was added.
  Recovery condition: later runtime adapter work must pass its own
  supply-chain and corpus benchmark gates.
- Persistence/schema migration: `gate_na`; these are evaluation contracts,
  not application persistence schemas. Recovery condition: later persistence
  work requires migration, compatibility, and rollback evidence.
- Paid provider/live call: `gate_na`; source and contract work is fully
  offline after the public source download. Recovery condition: live-provider
  tasks require explicit user approval.
- Human approval: not N/A and not complete. It is the remaining Task 3 gate.

## Rollback

Rollback reverts only the candidate-preparation commit: the research note,
four draft baselines, four manifest records, additive `open-textbook` enum,
contract-test extension, this evidence, and candidate-status truth updates.

The ignored PDF/TXT cache and external temporary probe are outside Git and
must not be represented as restored or removed by Git rollback.

## Repository Gate Evidence

The final closeout uses the repository fixed order:

1. `dotnet build ContentDeliveryStudio.sln`
   - exit `0`
   - 0 warnings, 0 errors
2. `dotnet test ContentDeliveryStudio.sln --no-build`
   - exit `0`
   - `475 / 475` passed, 0 failed, 0 skipped
3. Contract and invariant:
   - `scripts/verify-reference-evidence.ps1`: exit `0`
   - `dotnet format ContentDeliveryStudio.sln --verify-no-changes --no-restore`:
     exit `0`
4. `scripts/preflight-release.ps1 -NoRestore`
   - exit `0`
   - reference parity, placeholder/conflict scans, canonical repository
     verification, publish WhatIf, and diff hygiene passed

Task-specific focused tests, JSON parsing, Draft 2020-12 validation, semantic
link validation, local source/text hashes, cache-ignore checks, and
`git diff --check` also returned exit `0`.
