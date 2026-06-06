# PRD V1

## Title

AI Content Delivery Studio V1 is a Windows-first local workbench for turning a short requirement into a reviewed, reproducible image-series delivery package.

## Release Thesis

The long-term product vision is a multimodal content delivery studio. V1 does not attempt to launch that whole vision at once.

V1 launches a narrower promise:

- Help a power user start from a short requirement instead of raw prompts.
- Generate and compare several blueprint or direction options before paid generation.
- Preserve prompt, provider, review, and approval evidence through delivery export.
- Prove one text-heavy educational output path through deterministic text composition.

The primary product value is not "more image generation knobs." The primary value is a traceable local workbench for planning, producing, reviewing, repairing, and delivering coherent visual output.

## Main Issues

The current product direction is strong, but these issues still need active control:

- The long-term multimodal vision is larger than what V1 can safely launch.
- Provider routing can become inconsistent if `Images API` and `Responses API` roles are not enforced from policy.
- Text-heavy outputs still need a deterministic rendering lane to be credible.
- Operator automation is modeled earlier than it is proven.
- Reference shelves can become maintenance noise if shared and project-specific concerns are mixed without governance.

These issues do not invalidate the V1 strategy. They explain why the launch boundary must stay narrow.

## Release Decision

AI 推荐: ship one primary launch route, one supporting validation route, and one proof path.

- Primary launch route: short requirement -> brief -> blueprint -> series plan -> prompts -> generation -> review -> delivery.
- Supporting validation route: article or plain text -> evidence anchors -> illustration targets -> promoted plan -> same review and delivery flow.
- Proof path: text-heavy educational poster -> generated background plate -> deterministic text, formula, and label composition -> approval evidence export.

This order keeps the release centered on one repeatable product loop while still validating that the architecture can support document-derived planning and text-heavy delivery.

## Locked V1 Decisions

These decisions are now locked as V1 defaults and should not be treated as open questions unless a later PRD or ADR explicitly reopens them:

1. Primary V1 audience: solo creator or teacher-like power user, not a broad document-automation audience.
2. Primary launch workflow: the short requirement -> image-series route is the only primary launch workflow.
3. First real operator slice: additive local validation or diagnostics generation, not browser or desktop side-effect automation.
4. Deterministic composition implementation choice: `SkiaSharp`.
5. Pack posture in V1: internal reusable configuration only, not a public template-sharing or marketplace feature.

These defaults exist to reduce ambiguity during implementation and hardening.

## Audience

Primary V1 users:

- Solo creator who wants a controlled image-series workflow instead of ad hoc prompt files.
- Teacher or courseware author who needs factual fit, readable text, and delivery traceability.
- Power user or developer who wants local files, explicit provider settings, and audit-friendly exports.

Important but not primary for V1:

- Broad document-processing users.
- Operators who need browser or desktop automation against third-party systems.
- Teams needing collaboration, assignment, or cloud sync.

## User Problems

V1 is designed to solve these problems:

1. Users start from vague requirements, not from stable prompts.
2. Prompt-only workflows make it too easy to spend on the wrong visual route.
3. Text-heavy outputs often fail because image-model text rendering is unreliable.
4. Review and approval decisions are easy to lose when outputs are regenerated repeatedly.
5. Generated folders often lack enough provenance to defend or reuse the result later.

## Product Promise

If V1 works, a user should be able to say:

- "I can start from a requirement, not from prompt engineering trivia."
- "I can compare several visual routes before I spend money."
- "I can tell how this final artifact was produced and approved."
- "I can make text-heavy educational output without trusting the model to render exact text."

## In Scope

V1 must support:

- Local Windows workbench with WPF shell.
- Fake-first end-to-end flow for the primary launch route.
- Requirement-first brief capture.
- Two to four blueprint or prompt-direction candidates before paid generation.
- Promotion from blueprint into an editable series plan.
- Prompt generation, prompt editing, and prompt history.
- Queue-based generation with fake providers first and OpenAI opt-in second.
- Candidate gallery with prompt and metadata context.
- Structured AI-assisted review with human final approval.
- Delivery export with manifest and approval evidence.
- Article or plain-text illustration planning that promotes approved targets into the same downstream workflow.
- Deterministic post-render text composition for labels, formulas, legends, and callouts in the educational poster proof path.
- One real low-risk operator action executed end-to-end with audit evidence.

## Explicitly Out Of Scope

V1 does not need to launch with:

- Broad high-fidelity binary extraction across `pdf`, `docx`, `pptx`, and `xlsx`.
- PDF, DOCX, or slide publishing as first-class launch outputs.
- End-user pack marketplace, public template sharing, or external pack ecosystem.
- Remote workflow-engine integration as a required runtime path.
- Broad browser automation or Windows desktop automation with side effects.
- Full graph editor or workflow-node authoring surface.
- Cloud sync, multi-user collaboration, or assignment workflows.
- Physical repository rename or namespace migration.

## Primary Launch Workflow

The primary route is:

1. User enters a short requirement.
2. App creates a `CreativeBrief`.
3. App creates multiple `DesignBlueprint` candidates.
4. User promotes one blueprint.
5. App turns the promoted blueprint into a series plan and prompt directions.
6. User edits prompts if needed.
7. App generates candidates through fake providers by default, then through opt-in OpenAI providers.
8. App runs structured review.
9. User approves or routes repair.
10. App exports a delivery package with provenance and approval evidence.

This workflow is the V1 release spine. Other routes should reuse it rather than create parallel product modes.

## Plan Tightening

AI 推荐: keep V1 narrow even if the architecture already supports wider futures.

- Freeze graph-first workflow ambitions.
- Freeze remote workflow-engine ambitions.
- Freeze broad binary document automation outside the support matrix.
- Freeze broad browser and desktop automation beyond the first low-risk operator slice.
- Freeze rename execution beyond documentation and planning.

This tightening is not anti-architecture. It is how the project protects launch quality.

## Supporting Validation Workflow

The article or plain-text route is in V1 only to prove that evidence-backed planning can feed the same launch spine.

Acceptance intent:

- The route can start from pasted text, `.txt`, or `.md`.
- It can create illustration targets and evidence anchors without paid APIs by default.
- Approved targets promote into the existing plan and prompts flow.
- It does not require full binary-document fidelity to count as V1-complete.

## Educational Poster Proof Path

The text-heavy educational poster path exists to prove that the product can deliver high-trust outputs where exact text matters.

Acceptance intent:

- Image generation is used for scene or background creation.
- Required labels, formulas, legends, and callouts are composed deterministically after render.
- Review can distinguish visual-scene success from text-layout failure.
- Approval evidence is exported with the final delivery package.

## OpenAI Usage Policy For V1

V1 keeps the provider split explicit:

- Use the Images API for direct single-shot generation or edit flows.
- Use the Responses API for stateful planning, structured review, and multi-turn image workflows where state, revised prompts, or partial streaming materially help.
- Default to `store: false` unless a workflow explicitly opts into remote state retention.
- Keep fake providers as the default development and regression gate.

Detailed routing rules live in [PROVIDER_ROUTING_POLICY.md](./PROVIDER_ROUTING_POLICY.md).

## Operator Policy For V1

V1 includes only one real low-risk operator execution slice.

Recommended first slice:

- Run local delivery or artifact validation against a staged project or delivery folder.
- Write a new audit report into a diagnostics or validation output folder.
- Avoid destructive side effects and avoid mutating third-party systems.

Detailed execution rules live in [OPERATOR_RISK_POLICY.md](./OPERATOR_RISK_POLICY.md).

## Source And Artifact Boundary For V1

The release boundary for supported inputs and outputs is intentionally narrow. Launch support focuses on requirement text, pasted or plain-text article input, and image-series delivery artifacts.

Detailed support status lives in [SOURCE_ARTIFACT_SUPPORT_MATRIX.md](./SOURCE_ARTIFACT_SUPPORT_MATRIX.md).

## Target State References

Two strategy documents now define how V1 relates to longer-term engineering direction:

- [TARGET_ENGINEERING_STATE.md](./TARGET_ENGINEERING_STATE.md)
- [EXTERNAL_REFERENCE_STRATEGY.md](./EXTERNAL_REFERENCE_STRATEGY.md)

## Launch Metrics

V1 is ready only when these checks pass:

- Primary route completes three consecutive fake-first end-to-end runs with no paid APIs and no manual database edits.
- A 2-item sample series completes through the opt-in OpenAI path with request provenance, review evidence, and secret redaction verified.
- Article or plain-text planning can produce and promote approved illustration targets without requiring real providers by default.
- The educational poster proof path exports deterministic text-composition provenance and human approval evidence.
- The first real low-risk operator action runs end-to-end and writes audit output plus rollback or cleanup notes.

## Launch Blockers

The release is blocked if any of these remain unresolved:

- Approved outputs cannot preserve review and approval evidence through delivery export.
- Provider routing between Images API and Responses API is ambiguous in implementation or documentation.
- Text-heavy educational output still relies on model-rendered final text for required labels or formulas.
- Operator execution lacks an auditable boundary for the first real action.
- The primary launch route is not clearly better than editing prompt files by hand.

## Assumptions Used In This PRD

- The active implementation root remains `D:\CODE\ai-image-series-studio` until ADR 0008 rename gates are executed later.
- Fake providers remain the default safety and regression path.
- Pack infrastructure exists but is treated as internal reusable configuration in V1, not as a public product surface.
- Binary document extraction quality is valuable, but not a V1 launch gate.
