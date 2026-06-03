# Provider Configuration

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

## Same-Key Providers

Some official or full OpenAI-compatible providers may license the same key for text, vision, and image operations. In split environment profiles, duplicate the same secret value under the role-specific names only when the provider contract explicitly permits both roles.

Do not put an image-only merchant key under `TEXT_PROVIDER_API_KEY` or generic `OPENAI_API_KEY`. That bypasses the user's license intent and should be treated as misconfiguration.

## Health Checks

Provider Center health checks use non-generating `/v1/models` requests. They validate connectivity and authentication without creating text completions or images. If a merchant forbids all non-image endpoints, skip Provider Center health checks for that image provider and rely on opt-in image smoke tests.
