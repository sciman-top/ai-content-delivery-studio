# Provider Configuration

This file defines credential naming and role boundaries. Request routing, statefulness defaults, and surface-selection rules live in [PROVIDER_ROUTING_POLICY.md](./PROVIDER_ROUTING_POLICY.md).

This project treats provider credentials as role-scoped, not just vendor-scoped. A key that is licensed only for image generation must never be used for text planning or vision review. The local development shape uses the Cockpit gateway at `http://127.0.0.1:45335/v1`; real key material is resolved from the Windows DPAPI store when `PROVIDER_SECRET_STORE=dpapi` is selected.

The text and vision path has exactly three mutually exclusive preset sets: `sol-only`, `terra-only`, and `luna-only`. At any moment the five shared execution slots use one active set only. A tier is the unit preserved when the runtime switches the whole active set; a raw reasoning string is not reused across families because Terra and Luna use a different three-level mapping:

| Quality tier | Sol | Terra | Luna | Shared slots |
| --- | --- | --- | --- | --- |
| `deep` | `gpt-5.6-sol` / `high` | `gpt-5.6-terra` / `max` | `gpt-5.6-luna` / `max` | 1 |
| `balanced` | `gpt-5.6-sol` / `medium` | `gpt-5.6-terra` / `xhigh` | `gpt-5.6-luna` / `xhigh` | 2 |
| `fast` | `gpt-5.6-sol` / `low` | `gpt-5.6-terra` / `high` | `gpt-5.6-luna` / `high` | 2 |

The five shared text/vision execution slots are allocated as `deep=1`, `balanced=2`, and `fast=2`. Thus Sol-only uses one `sol/high`, two `sol/medium`, and two `sol/low` slots; Terra-only uses one `terra/max`, two `terra/xhigh`, and two `terra/high` slots; Luna-only uses one `luna/max`, two `luna/xhigh`, and two `luna/high` slots. Repetition within a tier is intentional. The image-generation queue is separate and remains at one concurrent request.

## Role-Scoped `.env` Format

```env
TEXT_PROVIDER_KIND=openai_compatible
TEXT_PROVIDER_BASE_URL=http://127.0.0.1:45335/v1
TEXT_PROVIDER_API_KEY=sk-text-provider-key
TEXT_PROVIDER_ROUTING_MODE=auto
TEXT_PROVIDER_PRESET_SET=sol-only
TEXT_PROVIDER_QUALITY_TIER=deep
TEXT_PROVIDER_RECOVERY_MODE=prefer-sol

IMAGE_PROVIDER_KIND=openai_compatible_image_only
IMAGE_PROVIDER_BASE_URL=http://127.0.0.1:45335/v1
IMAGE_PROVIDER_MODEL=image-model
IMAGE_PROVIDER_IMAGE_SURFACE=images
IMAGE_PROVIDER_API_KEY_1=sk-image-provider-key-1
IMAGE_PROVIDER_API_KEY_2=sk-image-provider-key-2
IMAGE_PROVIDER_API_KEY_3=sk-image-provider-key-3
IMAGE_PROVIDER_API_KEY_4=sk-image-provider-key-4
IMAGE_PROVIDER_APP_ID=image-app-id
IMAGE_PROVIDER_APP_SECRET=as-image-app-secret
IMAGE_PROVIDER_CONCURRENCY_PER_KEY=10
IMAGE_PROVIDER_TOTAL_CONCURRENCY=40
```

`TEXT_PROVIDER_ROUTING_MODE` accepts `auto` or `fixed` and fails closed for any other value. The backward-compatible default is `fixed`. `TEXT_PROVIDER_PRESET_SET` selects one complete family-only set and `TEXT_PROVIDER_QUALITY_TIER` selects its configured fallback tier. They must be supplied together. In `auto` mode the configured set is also the process-start active set; when it is omitted, auto routing starts with the preferred `sol-only` set. The only accepted sets are `sol-only`, `terra-only`, and `luna-only`; a set cannot contain a model from another family.

| Preset set | Quality tier | Model | Reasoning effort |
| --- | --- | --- | --- |
| `sol-only` | `deep` | `gpt-5.6-sol` | `high` |
| `sol-only` | `balanced` | `gpt-5.6-sol` | `medium` |
| `sol-only` | `fast` | `gpt-5.6-sol` | `low` |
| `terra-only` | `deep` | `gpt-5.6-terra` | `max` |
| `terra-only` | `balanced` | `gpt-5.6-terra` | `xhigh` |
| `terra-only` | `fast` | `gpt-5.6-terra` | `high` |
| `luna-only` | `deep` | `gpt-5.6-luna` | `max` |
| `luna-only` | `balanced` | `gpt-5.6-luna` | `xhigh` |
| `luna-only` | `fast` | `gpt-5.6-luna` | `high` |

The runtime selects one active preset set per gateway and credential scope. A numbered endpoint fallback may select its own `TEXT_PROVIDER_FALLBACK_N_PRESET_SET` and `TEXT_PROVIDER_FALLBACK_N_QUALITY_TIER`, but a single endpoint configuration cannot combine Sol, Terra, and Luna in one set. Existing configurations without a set continue to use explicit model and reasoning fields.

In `auto` mode, the runtime chooses `deep`, `balanced`, or `fast` from structured request data; it does not inspect prompt keywords and does not make a second model call to classify complexity. It starts from the configured complete set (the recommended default is Sol-only). If a request fails with a retryable reachability or upstream failure, it probes the next preset set through `GET /v1/models` before dispatching the same tier in that set. After a successful fallback, the whole set is active for subsequent requests until another failure causes a health-ordered switch. Concurrent requests use a versioned transition, so an earlier in-flight success cannot overwrite a newer failover result. `fixed` is an operator lock: it sends only the configured family and never performs a cross-family fallback.

`TEXT_PROVIDER_RECOVERY_MODE` accepts `disabled` (the default) or `prefer-sol`. `prefer-sol` requires `TEXT_PROVIDER_ROUTING_MODE=auto`; it never rewrites `.env` and only runs while a live desktop host is running. When the active set is Terra-only or Luna-only, the recovery controller waits ten minutes between checks, first uses the non-generating model catalog probe, then validates all three Sol tiers with bounded `POST /v1/responses` canaries. Two consecutive complete successful rounds promote the process-local active set back to Sol-only. A failed canary resets the success count and backs off `10 -> 20 -> 40 -> 60` minutes; every canary is capped at 45 seconds and shares the corresponding execution slot. Model catalog visibility alone never promotes a set.

| Workload | Selected preset |
| --- | --- |
| Routine series or document plan | `sol-low` |
| Series with at least 6 items or 2,400 estimated input characters | `sol-medium` |
| Series with at least 12 items or 3,600 estimated input characters | `sol-high` |
| Complex educational document with at least 4,500 input weight or 8 evidence rows | `sol-high` |
| Scholarly draft planning | `sol-high` |
| Scientific understanding chunk | `sol-high` |
| Scientific semantic review | `sol-high` |
| Full-resolution scientific visual review | `sol-high` |
| General vision review | `sol-low`; 5 signals uses `sol-medium`; 8 signals uses `sol-high` |

The selected set, model, and effort travel together through HTTP or SDK payloads, telemetry, and scientific-review checkpoint identity. Provider-call telemetry and the local redacted diagnostics journal also record the bounded `presetSet`, `modelPreset`, `reasoningEffort`, and `routeReason` fields so route quality can be evaluated without retaining prompts or secrets. `TEXT_PROVIDER_PRESET_SET=sol-only` with `TEXT_PROVIDER_QUALITY_TIER=deep` remains the operator rollback/default configuration when routing is switched back to `fixed`. Fallback profiles remain `fixed` unless their routing mode is explicitly configured and validated for that gateway.

Quality-first invariant: workload classification selects `deep`, `balanced`, or `fast`; family failover never changes that tier. Complex, scholarly, scientific understanding/review, high-risk, and full-resolution scientific visual work select `deep`; large but non-complex work selects `balanced`; routine work selects `fast`. This does not replace schema validation, deterministic checks, or final approval.

`fixed` is an explicit operator override, not a second adaptive router. It preserves the configured model and effort even for a quality-first workload; a non-`sol-high` selection is recorded as `fixed-operator-override-<workload>` in provider telemetry and the redacted diagnostics journal. Operators must treat that record as an intentional downgrade to investigate, not as approval to weaken deterministic checks, human review, or live-provider authorization. A fixed `sol-high` profile records the normal `fixed-provider-configuration` reason. It also disables both cross-family failover and proactive preferred-family recovery.

The preset pairs follow [OpenAI's GPT-5.6 model guidance](https://developers.openai.com/api/docs/guides/latest-model) and the [GPT-5.6 model contracts](https://developers.openai.com/api/docs/models). The official guidance treats model choice and reasoning effort as separate workload decisions, so this repository records the chosen pair and uses representative evaluation to revise tier boundaries. Gateway-specific availability must still be confirmed through that gateway's model catalog.

## Optional Gateway Failover

Provider failover has two independent scopes. The normal text/vision path switches the entire active preset set within the same gateway while preserving the quality tier and its matching slot category. Numbered endpoint fallbacks are a separate, explicitly configured gateway boundary and are not used by the local Cockpit `.env`.

For text planning and vision review:

```env
TEXT_PROVIDER_BASE_URL=https://primary-gateway.example/v1
TEXT_PROVIDER_API_KEY=sk-primary
TEXT_PROVIDER_PRESET_SET=sol-only
TEXT_PROVIDER_QUALITY_TIER=balanced

TEXT_PROVIDER_FALLBACK_1_BASE_URL=https://backup-gateway.example/v1
TEXT_PROVIDER_FALLBACK_1_API_KEY=sk-backup
TEXT_PROVIDER_FALLBACK_1_PRESET_SET=terra-only
TEXT_PROVIDER_FALLBACK_1_QUALITY_TIER=balanced
```

For image generation:

```env
IMAGE_PROVIDER_BASE_URL=https://primary-gateway.example/v1
IMAGE_PROVIDER_MODEL=gpt-image-2
IMAGE_PROVIDER_IMAGE_SURFACE=responses
IMAGE_PROVIDER_RESPONSES_MODEL=gpt-5.6-sol
IMAGE_PROVIDER_REASONING_EFFORT=medium
IMAGE_PROVIDER_API_KEY_1=sk-primary

IMAGE_PROVIDER_FALLBACK_1_BASE_URL=https://backup-gateway.example/v1
IMAGE_PROVIDER_FALLBACK_1_MODEL=gpt-image-2
IMAGE_PROVIDER_FALLBACK_1_IMAGE_SURFACE=images
IMAGE_PROVIDER_FALLBACK_1_API_KEY_1=sk-backup
```

`IMAGE_PROVIDER_IMAGE_SURFACE=responses` means ordinary image-generation requests default to `POST /responses` with the configured `IMAGE_PROVIDER_RESPONSES_MODEL` and an `image_generation` tool. `IMAGE_PROVIDER_IMAGE_SURFACE=images` means ordinary image-generation requests use `POST /images/generations` with `IMAGE_PROVIDER_MODEL`.

The active local fixed rollback set is Sol-only/deep in the recommended `.env` shape above. Explicit legacy model/effort fields remain supported, but they cannot be used to contradict a configured preset set. Image-only `/images/generations` requests do not receive a reasoning field. Automatic text routing does not alter `gpt-image-2`, `IMAGE_PROVIDER_IMAGE_SURFACE=images`, or `POST /images/generations`. Fake providers remain the desktop default until `PROVIDER_MODE=live` is explicitly selected.

Failover should be used only for transient or reachability failures: network failure, timeout, `408`, `429`, or `5xx`. Scientific-review `429` retries honor a gateway `Retry-After` header before the bounded same-model retry budget is exhausted; the final structured `429` can then trigger family failover. Do not fail over on `400`, `401`, or `403`; those indicate request, credential, or authorization problems that should fail closed.

## Desktop Runtime Opt-In

The desktop app keeps fake providers as the default runtime. To let the App host use the live profiles above, set:

```env
PROVIDER_MODE=live
```

When live mode is enabled, startup reads the local `.env`, validates the text and image profiles, and registers live providers for text planning, image generation, image editing, vision review, and scientific understanding. Scientific understanding uses the primary text profile through bounded, strict, stateless Responses requests; generic text/image/vision retain their existing failover policies. Missing or invalid `.env` configuration fails closed at registration time instead of silently falling back to fake providers. Image editing uses the primary image profile through `POST /images/edits`; it is not covered by generation failover because the approval receipt binds one provider identity and model.

### Secret Store Selection

Live registration resolves API keys through a secret store selected by `PROVIDER_SECRET_STORE` (or the `SecretStore` registration option), failing closed for unknown values:

- `dotenv` (default): keys are read from the same `.env` file that defines the endpoint profiles.
- `dpapi`: a composite store that checks the DPAPI-protected per-user secret files first and falls back to `.env`. Real keys can then be written with `DpapiOpenAiSecretStore` (files under `%LOCALAPPDATA%\ContentDeliveryStudio\secrets\openai`) while the `.env` entry keeps a non-empty placeholder, because key-name presence still defines endpoint topology and validation.

DPAPI wins over `.env` when both hold a value, so migration is per-secret: write the real key into DPAPI (immediately effective) and then replace the plaintext `.env` entry with a placeholder so the file no longer carries a real secret while endpoint topology stays valid.

### Reference-Guided Image Edit Boundary

The current production edit contract is intentionally narrow:

- exactly one persisted source candidate with role `Subject`
- optional PNG mask with the same format and decoded dimensions as the source
- source/reference and mask inputs capped at 50 MB each
- one output candidate created beside, never over, the source or mask
- supported output sizes `1024x1024`, `1024x1536`, and `1536x1024`
- `png`, `jpeg`, or `webp` output and `auto`, `low`, `medium`, or `high` quality
- source, mask, instruction, provider, model, operation, and approval hashes persisted without absolute input paths

Multi-reference composition/style editing remains frozen in this slice. The provider capability matrix exposes only `Subject`, a maximum reference count of one, and optional mask editing. Captured multipart transport tests prove the request shape without network access; they are not evidence that a paid call or visual result was accepted.

The WPF gallery projects this capability but keeps the real-edit command disabled because the desktop host has no trusted paid-authority source. The existing fake edit button remains explicitly fake and is not evidence of live editing.

## Hard Boundaries

- `TEXT_PROVIDER_API_KEY` is reserved for text planning and vision review operations.
- `IMAGE_PROVIDER_API_KEY` and `IMAGE_PROVIDER_API_KEY_1..N` are reserved for image generation operations.
- `IMAGE_PROVIDER_API_KEY*` cannot be used by `OpenAiTextPlanningProvider` or `OpenAiVisionReviewProvider`, even if code manually sets broad provider permissions.
- `TEXT_PROVIDER_API_KEY` can be used by `OpenAiImageGenerationProvider` only through the built-in image-provider fallback path when no `IMAGE_PROVIDER_API_KEY*` value is configured.
- `OpenAiProviderOptions.FromTextProviderEnvironment(...)` creates options for `TextPlanning | VisionReview` only.
- `OpenAiProviderOptions.FromImageProviderEnvironment(...)` creates options for `ImageGeneration` only.

## Vision Review Runtime Defaults

For visual review, the preferred runtime shape is:

- local direct provider call from the desktop app
- dedicated `TEXT_PROVIDER_API_KEY`
- bounded batch request
- `store: false` by default
- no default `previous_response_id` chaining

Recommended implementation-facing defaults:

- review batch items: `3` to `6`
- high-risk review batch items: `2` to `4`
- split the batch before dispatch when a configured threshold is exceeded
- prepare compact local review artifacts first, such as thumbnail grids, candidate manifests, prompt summaries, and selected evidence anchors

Credential placement alone does not justify stateful review. If a workflow later enables retained remote state, that decision must still follow [PROVIDER_ROUTING_POLICY.md](./PROVIDER_ROUTING_POLICY.md).

### Scientific Review Local Checkpoints

Live scientific semantic and visual review requests remain remote-stateless with
`store: false` and no `previous_response_id` chaining. Separately, the desktop
runtime writes a local resume checkpoint after a response has passed the strict
scientific review parser. The default location is:

```text
%LOCALAPPDATA%\AIContentDeliveryStudio\provider-checkpoints\scientific-review
```

A checkpoint is reusable only when its schema, operation, endpoint, model,
reasoning effort, and SHA-256 of the exact transmitted JSON payload all match.
A missing checkpoint permits the normal bounded provider request; a corrupt or
identity-mismatched checkpoint fails closed. Resume revalidates all responsible
item identifiers against the current request and preserves `Pass`, `Fail`, or
`Uncertain` without upgrading the verdict.

Checkpoint files contain the parsed verdict, findings, provider trace id, and
request identity hashes. They do not contain provider secrets, image bytes,
base64 data URLs, or the raw provider response. Fake runtime selection neither
reads nor writes these checkpoints. A resumed result is marked
`PersistedCheckpoint`; it is reuse of an earlier parsed response, not a new live
provider acceptance event.

## Same-Key Providers

Some official or full OpenAI-compatible providers may license the same key for text, vision, and image operations.

Supported configuration shapes are:

- default single-key mode: configure `TEXT_PROVIDER_API_KEY`; image generation reuses it automatically when no `IMAGE_PROVIDER_API_KEY*` is present
- explicit dual-key mode: configure `TEXT_PROVIDER_API_KEY` plus `IMAGE_PROVIDER_API_KEY` or `IMAGE_PROVIDER_API_KEY_1..N`; image generation uses the explicit image key pool

When a provider contract explicitly permits both roles, dual-key mode may still reuse the same underlying secret value under both names. The runtime now no longer requires that duplication for the default single-key path.

Do not put an image-only merchant key under `TEXT_PROVIDER_API_KEY` or generic `OPENAI_API_KEY`. That bypasses the user's license intent and should be treated as misconfiguration.

Implementation guardrail:

- `ProviderEnvironmentConfiguration.Image.UsesSharedTextApiKeyFallback` is true only when no `IMAGE_PROVIDER_API_KEY*` value is present and `TEXT_PROVIDER_API_KEY` is present.
- `OpenAiProviderGuard` allows image generation with `TEXT_PROVIDER_API_KEY` only when `OpenAiProviderOptions.UsesSharedTextApiKeyFallback` is true.
- Adding any explicit `IMAGE_PROVIDER_API_KEY` or `IMAGE_PROVIDER_API_KEY_1..N` disables the fallback and restores the image key pool as the image-generation credential source.

## Statefulness Reminder

Credential placement does not decide whether a workflow should use remote retained state. The V1 default remains `store: false` unless the provider routing policy explicitly allows a stateful workflow.

## OpenAI Launch Preflight

The repository now includes a read-only OpenAI launch preflight path that evaluates whether the current local provider configuration is ready for a live V1 sample run.

Current behavior:

- reads role-scoped provider configuration from the selected `.env` path
- checks text-planning, vision-review, and image-generation readiness separately
- applies the real-provider smoke gate before any live smoke path is considered runnable
- writes local `json` and `md` reports under a diagnostics folder

Default smoke opt-in gate:

```env
IMAGE_SERIES_STUDIO_OPENAI_REAL_API_SMOKE=1
```

If this variable is missing or set to a different value, the preflight remains a dry-run readiness check and records the blocking reason instead of treating the smoke path as enabled.

The preflight is intentionally low-risk:

- no secret values are exported
- no provider dashboard state is required
- normal use stays on the local diagnostics path

This preflight is a readiness and evidence tool, not a replacement for the eventual live `2-item` V1 provider run recorded in [V1_LAUNCH_EVIDENCE.md](./V1_LAUNCH_EVIDENCE.md).

## Health Checks

Provider Center health checks use non-generating `/v1/models` requests. They validate connectivity and authentication without creating text completions or images. If a merchant forbids all non-image endpoints, skip Provider Center health checks for that image provider and rely on opt-in image smoke tests.
