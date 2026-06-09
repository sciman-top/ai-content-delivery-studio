# Provider Configuration

This file defines credential naming and role boundaries. Request routing, statefulness defaults, and surface-selection rules live in [PROVIDER_ROUTING_POLICY.md](./PROVIDER_ROUTING_POLICY.md).

This project treats provider credentials as role-scoped, not just vendor-scoped. A key that is licensed only for image generation must never be used for text planning or vision review.

## Role-Scoped `.env` Format

```env
TEXT_PROVIDER_KIND=openai_compatible
TEXT_PROVIDER_BASE_URL=https://text.example/v1
TEXT_PROVIDER_API_KEY=sk-text-provider-key
TEXT_PROVIDER_MODEL=gpt-5.5

IMAGE_PROVIDER_KIND=openai_compatible_image_only
IMAGE_PROVIDER_BASE_URL=https://image.example/v1
IMAGE_PROVIDER_MODEL=image-model
IMAGE_PROVIDER_API_KEY_1=sk-image-provider-key-1
IMAGE_PROVIDER_API_KEY_2=sk-image-provider-key-2
IMAGE_PROVIDER_API_KEY_3=sk-image-provider-key-3
IMAGE_PROVIDER_API_KEY_4=sk-image-provider-key-4
IMAGE_PROVIDER_APP_ID=image-app-id
IMAGE_PROVIDER_APP_SECRET=as-image-app-secret
IMAGE_PROVIDER_CONCURRENCY_PER_KEY=10
IMAGE_PROVIDER_TOTAL_CONCURRENCY=40
```

## Hard Boundaries

- `TEXT_PROVIDER_API_KEY` is reserved for text planning and vision review operations.
- `IMAGE_PROVIDER_API_KEY` and `IMAGE_PROVIDER_API_KEY_1..N` are reserved for image generation operations.
- `IMAGE_PROVIDER_API_KEY*` cannot be used by `OpenAiTextPlanningProvider` or `OpenAiVisionReviewProvider`, even if code manually sets broad provider permissions.
- `TEXT_PROVIDER_API_KEY` cannot be used by `OpenAiImageGenerationProvider`, even if code manually sets broad provider permissions.
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

## Same-Key Providers

Some official or full OpenAI-compatible providers may license the same key for text, vision, and image operations. In split environment profiles, duplicate the same secret value under the role-specific names only when the provider contract explicitly permits both roles.

Do not put an image-only merchant key under `TEXT_PROVIDER_API_KEY` or generic `OPENAI_API_KEY`. That bypasses the user's license intent and should be treated as misconfiguration.

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
