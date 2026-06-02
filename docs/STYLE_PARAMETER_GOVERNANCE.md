# Image Type, Style, And Parameter Governance

Last reviewed: 2026-06-01.

## Recommendation

AI 推荐: use a layered governance model instead of a single flat prompt or parameter form. Image type, style, provider settings, references, experiments, review rules, and delivery metadata should be separate but linked objects.

This keeps common workflows simple while still leaving room for advanced image-generation scenarios. The app should start with presets and controlled parameter grids, then evolve toward workflow import/export and optional graph views only after the simpler contracts are stable.

## What Mature Tools Do

Official API documentation and mature community tools converge on the same pattern:

- Provider APIs expose capability-specific settings such as `size`, `quality`, `format`, `compression`, `background`, moderation level, image inputs, and edit masks.
- Workflow tools save the executable workflow or the generation metadata with the image, so an output can be inspected, repeated, remixed, or audited later.
- Experiment tools vary a small number of axes at a time, then show the result as a grid or comparable set.
- Creator workbenches separate image organization, reference assets, prompt reuse, generation settings, and review decisions.
- Vision review is useful but limited; it must be structured, auditable, and finally approved by a human.

## Core Objects

### ImageTypePreset

Defines what the image is for. Examples:

- Educational poster
- Article cover
- Article inline illustration
- Courseware slide visual
- Social media square
- Storyboard frame
- Product concept board
- Icon or sticker
- Background plate for deterministic text composition

Fields:

- `id`
- `display_name`
- `description`
- `default_aspect_ratio`
- `default_output_format`
- `text_policy`
- `review_rubric_id`
- `delivery_naming_policy`

### StyleGuide

Defines the repeatable visual language for a series. It should not replace per-item prompts.

Fields:

- `id`
- `name`
- `series_id`
- `visual_principles`
- `palette`
- `lighting`
- `composition_rules`
- `line_or_texture_rules`
- `negative_constraints`
- `reference_image_set_ids`
- `version`

### GenerationRecipe

Defines provider-neutral generation intent plus provider-specific mapping.

Fields:

- `id`
- `provider_profile_id`
- `model_id`
- `image_type_preset_id`
- `width`
- `height`
- `quality`
- `output_format`
- `background`
- `compression`
- `moderation`
- `seed`
- `supports_transparency`
- `estimated_cost_band`
- `capability_warnings`

Unsupported or provider-specific settings must be recorded as warnings instead of silently dropped.

### ReferenceImageSet

Groups reusable reference assets by role. The current domain model already started this direction with `Style`, `Subject`, `Composition`, `Palette`, `Mask`, and `Negative` roles.

Rules:

- Store workspace-relative paths only.
- Keep asset binaries outside git.
- Record role and description per image.
- Link reference sets to a style guide, edit recipe, or item-level prompt version.

### ParameterExperiment

Controls systematic exploration. The current domain model already started this with provider-neutral Cartesian-product variants.

Rules:

- Vary one to three axes per experiment.
- Keep a fixed base prompt and fixed baseline settings unless the axis explicitly varies them.
- Generate stable slugs and preserve all axis values in metadata.
- Use low or draft quality for exploration, then promote selected recipes to medium or high quality.

### WorkflowPackage

Exports and imports reusable workflow planning data without bundling generated assets, local databases, or secrets.

Included:

- style guides
- generation recipes
- reference image set metadata with workspace-relative paths
- parameter experiment definitions

Rules:

- Use a versioned JSON schema.
- Re-validate imported packages before accepting them.
- Reject reference image paths that are absolute or escape the workspace.
- Keep binary reference assets, generated images, local SQLite files, and API keys outside workflow packages.

### ReviewRubric

Turns subjective quality into explicit checks.

Dimensions should include:

- Requirement match
- Series consistency
- Subject accuracy
- Style-guide compliance
- Text readability
- Layout/composition
- Unsafe or disallowed content
- Delivery readiness

For text-heavy posters, the preferred production path is still deterministic post-render text composition, then review the combined image.

## Layering Rules

Use this precedence when building a prompt or task:

1. Project brief
2. Series style guide
3. Image type preset
4. Item brief
5. Prompt version
6. Reference image set
7. Generation recipe
8. Parameter experiment overrides
9. Provider capability validation
10. Review rubric

The app should never depend on one provider's request schema as the domain model. Provider adapters translate the neutral contract into API-specific request shapes and report unsupported options.

## Roadmap Implications

Yes, this needs a route map, implementation plan, and task checklist. The reason is not bureaucracy; it is blast-radius control. Image-generation options grow quickly, and without a plan the product will become a large prompt form with no reproducibility.

The recommended sequence:

1. Foundation: add durable docs, ADR, and task plan for style/parameter governance.
2. Domain: add `ImageTypePreset`, `StyleGuide`, and `GenerationRecipe` as provider-neutral core objects.
3. Capability validation: extend provider capabilities so invalid settings fail before queue execution.
4. Experiment workflow: connect parameter experiments to queue tasks, candidate metadata, and comparison review.
5. UI: add style library and recipe inspector after the domain contract is tested.
6. Export/import: include style guides, recipes, references, and experiments in workflow packages.
7. Advanced mode: add graph import/export or optional graph view only after workflow packages are stable.

## Verification Policy

Documentation-only changes use the pre-code gate defined in `AGENTS.md`: scan for unresolved placeholders, then inspect `git status --short`.

After core object changes, run:

```powershell
dotnet build
dotnet test
dotnet format --verify-no-changes
```

Real image API calls remain opt-in only.
