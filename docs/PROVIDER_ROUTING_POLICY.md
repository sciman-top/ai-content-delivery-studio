# Provider Routing Policy

## Purpose

This document defines how AI Content Delivery Studio chooses between OpenAI surfaces for planning, generation, and review in V1 and near-term hardening slices.

This policy is about request routing, statefulness, provenance, and safe defaults. Role-scoped credential naming stays in [PROVIDER_CONFIGURATION.md](./PROVIDER_CONFIGURATION.md).

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

#### Paid Queue Approval Boundary

- Preparing, pausing, resuming, reordering, and constructing a retry remain local-only operations and never authorize or invoke a provider.
- A live Images API queue requires a persisted `generation-approval-receipt.v1` whose canonical hash binds the project, ordered task and series identities, prompt-version content hashes, provider profile, direct provider identity, endpoint class, model, settings, retry ceiling, per-operation estimate, total estimate, cost ceiling, expiry, approval actor, and authority reference.
- The current request set is rebuilt and checked immediately before every new provider dispatch. Missing, expired, inconsistent, or drifted receipts fail before the call.
- Pause does not claim to cancel an in-flight request; it prevents later queued requests from starting. Interrupted `Running` work is recovered as failed and is never automatically replayed.
- A retry is a new task identity and is unapproved. Reorder invalidates the whole batch receipt. Direct-provider approval does not cover a failover destination; live failover stays disabled until destination-specific receipt coverage exists.
- The WPF queue displays request and receipt summaries but does not mint paid authority. Live Execute remains disabled in the desktop host until an explicit current authority source is wired.

#### Reference And Edit Approval Boundary

- A real image edit uses the primary image profile and `POST /images/edits`. The bounded request contains one persisted `Subject` source candidate and may include one same-size PNG mask.
- Approval issues an immutable `image-edit-approval-receipt.v1` without calling the provider. It binds project, item, source candidate, source/mask/instruction hashes, provider, `images/edits`, model, output settings, reference roles, cost estimate, ceiling, actor, authority reference, issue time, and expiry.
- Execution reloads the persisted source candidate and rebuilds that request identity immediately before dispatch. Path mismatch, source or mask drift, unsupported roles/counts, expired authority, model/provider change, destructive output, or mask format/dimension mismatch fails before transport.
- A successful edit creates a new `ReviewPending` candidate with an explicit source-candidate edge. It never replaces the source asset. Delivery manifests retain the edit hashes, reference roles, provider operation, model, receipt identity, and request-set hash without copying private source or mask paths.
- Multi-reference editing and live edit failover remain frozen. A different provider destination or expanded reference set requires a separately approved contract and receipt.
- The desktop UI advertises capability and the missing-authority reason, but it cannot mint paid authority. Captured transport, fake editing, XML/UIA contracts, and package provenance are repo-side evidence only.

The current repository now carries an explicit opt-in image-generation request path for those stateful cases. It stays fail-closed unless the image provider configuration also supplies a Responses-capable mainline model, and single-shot generation continues to default to the simpler Images API path.

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

For the current implementation slice, stateful Responses image-generation metadata records `previous_response_id`, `revised_prompt`, and the image-generation tool call id when the upstream response exposes them.

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

- Keep role-scoped credential separation intact. The same-provider single-key fallback is allowed only through the documented provider-configuration path where no `IMAGE_PROVIDER_API_KEY*` is present; explicit image keys still take precedence.
- Keep the fake-first gate as the default regression path.
- Use the official OpenAI .NET SDK for the stable Images API path; keep raw `HttpClient` on Responses-backed planning and review until the SDK surface no longer requires the current `OPENAI001` evaluation fallback.
- Allow one bounded retry for transient official SDK Images `502 upstream_error` failures before surfacing the error to the live route.
- Reuse the same bounded transient `502 upstream_error` policy for official SDK Responses text-planning calls. This keeps future real-provider brief or blueprint planning from inventing a broader retry policy when those paths leave the fake-first boundary.
- Treat Responses multi-turn image state as a hardening slice, not as a prerequisite for the primary launch route.
- Prepare compact local review artifacts before remote vision review: thumbnail grids, candidate manifests, prompt or setting summaries, and selected evidence anchors.
- Keep normal production review stateless and bounded by batch thresholds; if a review request grows too large, split the batch instead of chaining more remote state.
- For current Responses-backed vision review, default to compact review payloads: low-detail image understanding, compressed review assets when needed, and a minimal strict schema with local score backfill instead of oversized remote review JSON.
- Record any deviation from this policy in roadmap or implementation-plan evidence before changing runtime behavior.
