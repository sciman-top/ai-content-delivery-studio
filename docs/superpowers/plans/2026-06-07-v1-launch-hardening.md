# V1 Launch Hardening Plan

## Goal

Convert the reviewed V1 boundary into an execution-focused plan that resolves ambiguity before more feature expansion lands.

## Main Issues

- The product vision is larger than the V1 launch boundary and can easily widen again if not defended.
- Provider routing between `Images API` and `Responses API` can drift if not implemented from an explicit policy.
- Text-heavy outputs still need a deterministic composition lane to become credible delivery artifacts.
- Operator automation is now modeled, but the first real low-risk execution slice still needs proof.
- Reference shelf governance was improving, but needed a stable low-maintenance operating model.

## Locked Clarification Decisions

1. The primary V1 user is the solo creator or teacher-like power user rather than a broad document-automation audience.
2. The short requirement -> image-series route is the only primary launch workflow.
3. The first real operator action stays additive and local, not browser or desktop side-effect automation.
4. The deterministic text-composition implementation library is `SkiaSharp`.
5. Pack infrastructure remains internal configuration in V1 and not a public sharing feature.

## Tightening Decisions

- Freeze broad binary document automation outside the support matrix.
- Freeze graph-first and remote workflow-engine ambitions until after V1 launch hardening.
- Freeze namespace or repository rename beyond planning and documentation.
- Prefer one polished real path over many partially complete workflow modes.

## Milestone 1: Boundary And Acceptance

- Keep `PRD_V1`, roadmap, and task checklist aligned to the same launch promise.
- Keep launch metrics explicit and reviewable.
- Keep frozen items visible so later work does not silently re-open them.

## Milestone 2: Provider And Operator Policies

- Implement the provider routing policy defaults in code.
- Keep `store: false` as the default until a specific stateful workflow justifies otherwise.
- Prove one real low-risk operator path with audit evidence and cleanup notes.

## Milestone 3: Deterministic Text Composition

- Use `SkiaSharp` as the deterministic composition implementation approach.
- Add rendering and review checks for labels, formulas, legends, and callouts.
- Preserve composition provenance through delivery export.

## Milestone 4: Launch Evidence

- Run the fake-first primary workflow repeatedly until it is stable.
- Run the article or plain-text supporting route without paid providers by default.
- Run the educational proof path with deterministic composition and human approval evidence.
- Run the first real low-risk operator action end-to-end.

## Verification Order

1. Documentation alignment
2. Provider routing implementation checks
3. Deterministic composition implementation checks
4. Golden-path workflow verification
5. Launch evidence capture
