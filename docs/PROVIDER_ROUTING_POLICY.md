# Provider Routing Policy

## Purpose

This document defines how AI Content Delivery Studio chooses between OpenAI surfaces for planning, generation, and review in V1 and near-term hardening slices.

This policy is about request routing, statefulness, provenance, and safe defaults. Role-scoped credential naming stays in [PROVIDER_CONFIGURATION.md](/D:/CODE/ai-image-series-studio/docs/PROVIDER_CONFIGURATION.md).

## Guiding Rules

- Keep text planning, image generation, and vision review as separate provider roles.
- Prefer the simplest surface that satisfies the workflow.
- Do not pay the complexity cost of stateful Responses chains for single-shot image calls that do not benefit from them.
- Default to `store: false` unless the workflow explicitly needs remote conversation state.
- Preserve provenance regardless of which endpoint is chosen.

## Default OpenAI Surface Matrix

| Workflow | Default surface | Why |
| --- | --- | --- |
| Brief creation, blueprint generation, structured prompt directions | Responses API | Strong fit for structured outputs, multi-turn planning, and future tool use. |
| Structured visual review | Responses API | Natural fit for image input plus typed output and repair routing. |
| Single-shot image generation from a prompt | Images API | Simpler request model and lower orchestration complexity. |
| Single-shot image edit or reference-image edit without conversational state | Images API | Execution-first path; no need for stored turn history. |
| Multi-turn image revision where revised prompts or prior generated state matter | Responses API with image generation tool | Better fit for stateful iteration and provenance. |
| Partial-image streaming preview | Responses API when supported and useful | Only use when the workbench experience benefits materially. |

## V1 Routing Decisions

### Planning And Review

Use the Responses API by default for:

- `CreativeBrief` generation or refinement.
- `DesignBlueprint` candidate generation.
- Structured prompt-direction output.
- Structured review output.
- Repair-routing suggestions.

These flows should use structured output schemas, not freeform prose parsing.

For structured visual review, the preferred runtime shape is a local direct provider call from the app using a fresh bounded request. Do not make long chained review transcripts the default production path.

### Image Generation

Use the Images API by default for:

- Fake-to-real transition of standard single-shot generation.
- Direct item generation after the user has already chosen a route and prompt version.
- Edit flows that do not need multi-turn response state.

Only switch generation to Responses when the workflow gains meaningful value from:

- `revised_prompt` provenance.
- `previous_response_id` chaining.
- Multi-turn image tool context.
- Partial-image streaming in the workbench.

## Statefulness Policy

### Default

The default for V1 is:

- `store: false`
- local project persistence remains the system of record
- no silent remote state retention
- no default `previous_response_id` chaining for per-batch visual review

### When `store: true` Is Allowed

`store: true` may be enabled only when all of the following are true:

- The workflow explicitly benefits from remote multi-turn continuity.
- The user or product setting has opted into the retained-state behavior.
- The project still records enough local provenance to audit the chain without relying on the provider dashboard.
- The data being sent does not violate the project's privacy expectations.

### When `previous_response_id` Is Allowed

Use `previous_response_id` only when:

- The workflow is already on the Responses API.
- The feature needs remote state continuity for image or planning iteration.
- The cost and privacy trade-off is understood and accepted.

Do not use `previous_response_id` as a default replacement for local project history.

## Structured Output Policy

Planning, review, and repair flows must use strict schemas whenever practical.

Expected output classes include:

- brief records
- blueprint candidates
- prompt directions
- review results
- repair plans

If the SDK surface is missing a needed capability, raw HTTP is acceptable only if the same schema validation, provenance capture, and redaction rules remain in place.

## Provenance Requirements

Every provider result persisted to project state should capture as many of these fields as the surface exposes:

- provider kind
- endpoint family: `responses` or `images`
- model id
- provider profile id
- request id
- response id
- `store` flag
- `previous_response_id` when used
- `revised_prompt` when exposed
- tool or call ids when used
- latency
- token usage
- cost estimate
- capability warnings
- redacted error details when a call fails

## Privacy And Retention Defaults

- Do not assume remote state retention is acceptable just because the API supports it.
- Keep fake providers as the default path for tests and routine development.
- Avoid sending unnecessary document bodies, binary payloads, or approval notes when a narrower request would work.
- Screenshots, uploaded files, and third-party content remain untrusted input even when processed through the Responses API.

## SDK And Raw HTTP Policy

AI 推荐: adopt the official OpenAI .NET SDK where the surface is stable, and keep raw `HttpClient` only for lagging or unsupported gaps.

The adoption boundary is recorded in [ADR 0009](./adr/0009-openai-dotnet-sdk-adoption.md). The policy decision is complete; runtime migration remains a separate parity-tested implementation slice.

Rules:

- New stable planning and review flows should prefer SDK support first.
- Raw HTTP is acceptable for unsupported image or streaming gaps.
- Routing policy must not diverge by transport choice. SDK and raw HTTP paths should produce the same contract-level records.

## Explicit Non-Goals

This policy does not make every Responses feature a launch requirement.

V1 does not require:

- stored-state-by-default behavior
- remote MCP integration as part of normal generation
- multi-turn image state for every item
- broad tool orchestration inside a single response loop

## Implementation Notes For The Current Repository

- Keep role-scoped credential separation intact.
- Keep the fake-first gate as the default regression path.
- Use the official OpenAI .NET SDK for the stable Images API path; keep raw `HttpClient` on Responses-backed planning and review until the SDK surface no longer requires the current `OPENAI001` evaluation fallback.
- Treat Responses multi-turn image state as a hardening slice, not as a prerequisite for the primary launch route.
- Prepare compact local review artifacts before remote vision review: thumbnail grids, candidate manifests, prompt or setting summaries, and selected evidence anchors.
- Keep normal production review stateless and bounded by batch thresholds; if a review request grows too large, split the batch instead of chaining more remote state.
- For current Responses-backed vision review, default to compact review payloads: low-detail image understanding, compressed review assets when needed, and a minimal strict schema with local score backfill instead of oversized remote review JSON.
- Record any deviation from this policy in roadmap or implementation-plan evidence before changing runtime behavior.
