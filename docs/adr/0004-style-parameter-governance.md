# ADR 0004: Style And Parameter Governance

Status: accepted

Date: 2026-06-01

## Context

Image-series production has many variables: image type, aspect ratio, style, model, quality, output format, references, masks, seeds, moderation mode, cost, review rubric, and delivery target. If these are stored only inside free-form prompts or provider-specific request objects, the app will lose reproducibility and become hard to test.

The current codebase already has useful foundations:

- `GenerationSettings` for size, quality, output format, and seed.
- Provider-neutral text, image, and vision provider contracts.
- Provider capability validation.
- `ParameterGridExperiment` for controlled variants.
- `ReferenceImageSet` for role-based reference assets.

## Decision

Use a layered provider-neutral governance model:

- `ImageTypePreset` describes the intended output category and delivery defaults.
- `StyleGuide` describes repeatable series-level visual language.
- `GenerationRecipe` describes generation settings and provider mapping.
- `ReferenceImageSet` groups reusable reference assets by role.
- `ParameterExperiment` explores a bounded matrix of prompt or setting variants.
- `ReviewRubric` validates quality, consistency, safety, and delivery readiness.

Provider adapters must translate this neutral model into API-specific request shapes and return warnings for unsupported options. Unsupported settings must not be silently ignored.

## Consequences

- The app can support OpenAI first without turning OpenAI request fields into the domain model.
- Series consistency can be reviewed against explicit style guides instead of implicit prompt wording.
- Experiments can be reproduced because axes, seeds, settings, references, and output metadata are preserved.
- UI can start simple with presets and inspectors, then later add workflow export/import or optional graph views.
- New provider settings require capability mapping and tests before they become user-facing controls.

## Rejected Alternatives

- Flat prompt-only approach: fastest initially, but weak for repeatability, review, and delivery manifests.
- Provider-request-as-domain approach: simpler for OpenAI MVP, but blocks future providers and makes tests brittle.
- Full node graph immediately: powerful, but too much product and UI surface before presets, recipes, and metadata are stable.

AI 推荐: layered presets and recipes first, graph later. This matches the current MVP and avoids building an advanced UI before the core contracts prove themselves.
