# Blueprint-First Series Design

## Current Landing And Target

- Current landing: `D:\CODE\ai-image-series-studio`
- Target: extend the existing brief-first design into a generalized blueprint-first workflow for narrative and non-narrative image series.
- Scope: requirements, briefs, blueprint routes, panel or item planning, prompt generation, review routing, and delivery traceability.

## Problem

The product already supports planning, prompts, generation, review, and delivery. It still risks collapsing too much product value into prompt generation alone.

Users often think in one of these forms:

- "I need a clean image series for this topic."
- "Turn this article into illustrations."
- "Give me several visual routes before I spend on generation."
- "Break this idea into several coherent panels."
- "Keep the same style and subject logic across many images."

Those are not only prompt requests. They are design-route requests.

## Goals

- Keep the product domain-neutral across posters, diagrams, article illustrations, concept explainers, storyboards, panel sequences, and narrative image packs.
- Turn high-level requirements into reusable design blueprints before producing image prompts.
- Support both standard series items and multi-frame panel sequences through one generalized workflow.
- Preserve existing provider-neutral boundaries and fake-first development discipline.
- Keep human approval as the final delivery gate.

## Non-Goals

- Do not introduce full video generation.
- Do not build a full comic page editor in the first slice.
- Do not force every project into a storyboard or comic vocabulary.
- Do not duplicate the existing project model with a second disconnected narrative model.

## Recommended Approach

AI 推荐: add a reusable `DesignBlueprint` layer between `CreativeBrief` and prompt directions.

The blueprint layer should answer:

- What kind of image series is this?
- What is the visual reasoning route?
- What does each frame or item need to accomplish?
- What should stay consistent?
- What should vary across the sequence?
- What are the expected failure modes?

## DesignBlueprint

`DesignBlueprint` is a reusable high-level series strategy.

Recommended fields:

- `id`
- `project_id`
- `creative_brief_id`
- `key`
- `display_name`
- `category`
- `summary`
- `intended_use`
- `recommended_item_count_range`
- `supports_panel_sequence`
- `default_text_policy`
- `default_review_rubric_id`
- `consistency_rules`
- `variation_rules`
- `risk_notes`
- `created_at`
- `updated_at`

Example blueprint categories:

- `poster_series`
- `article_illustration_pack`
- `comparison_sequence`
- `timeline_sequence`
- `concept_explainer_sequence`
- `panel_narrative_sequence`
- `character_sheet`
- `scene_keyframe_set`

## Blueprint Output

A selected blueprint should generate a `NarrativePlan` or generalized `SeriesRoute`:

- summary
- item or panel list
- per-item purpose
- knowledge or message payload
- visual anchors
- consistency notes
- text policy
- risk notes

This route is what later feeds prompt directions and prompt versions.

## Panel Versus Item

The domain model should not split into two products.

Recommended approach:

- keep `Series` and `SeriesItem` as the base generic model
- allow a `SeriesItemKind` such as `Standard`, `Panel`, `Diagram`, `Keyframe`, `Cover`
- keep panel sequences as a specialized series, not a separate subsystem

This preserves interoperability with queue, review, gallery, delivery, and migration tooling.

## Prompt Direction Generation

The text planning provider should produce prompt directions after the blueprint has clarified the route.

Recommended direction families:

- conservative faithful
- visual impact
- minimal clean
- experimental alternate

For panel or narrative sequences, directions should also state:

- what remains fixed across panels
- what changes from panel to panel
- whether text belongs inside the image or in deterministic post-render composition

## Review Routing

The review loop should identify the correct layer to repair:

- `brief_problem`
- `blueprint_problem`
- `plan_problem`
- `prompt_problem`
- `settings_problem`
- `reference_problem`

This prevents the current common failure of trying to solve a wrong design route with endless prompt rewrites.

## UI Placement

The `Brief` tab should grow into a `Brief + Blueprints` workbench:

- structured brief
- assumptions
- blueprint cards
- direction comparison
- promotion actions
- low-cost trial strategy

The `Plan` tab should then show the promoted route as either:

- image series items
- panel sequence items
- mixed image package

## Best-Practice References

Official references worth following:

- OpenAI image generation and image prompting guidance
- OpenAI images and vision review limitations
- Google Vertex AI image generation, editing, deterministic generation, negative prompting, customization, and prompt rewriting guidance
- Stability AI image-to-image, inpainting, and multi-prompt parameter patterns

Community patterns worth borrowing:

- ComfyUI workflow as a first-class saved artifact
- InvokeAI workspace, library, and canvas thinking
- Diffusers pipeline abstraction and explicit configuration
- AUTOMATIC1111 parameter experiment and metadata traceability
- Label Studio, CVAT, and FiftyOne review-state and QA concepts

## Acceptance Direction

The first implementation slice should prove:

- a project can create a brief
- the system can generate multiple blueprint candidates
- the user can promote one blueprint
- the promoted blueprint can create a coherent image series plan
- the plan can enter the existing prompt/generation/review workflow without new parallel subsystems
