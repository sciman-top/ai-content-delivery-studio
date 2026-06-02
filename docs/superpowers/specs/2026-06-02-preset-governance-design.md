# Preset Governance Design

## Current Landing And Target

- Current landing: `D:\CODE\ai-image-series-studio`
- Target: define a durable preset governance layer for image type, style, recipe, and review recommendations before expanding the current `Brief Studio` implementation.
- Scope: design only. No real image API calls, no provider integration changes, and no code changes in this slice.

## Problem

The product needs built-in executable presets, but poor preset design would make the app feel either rigid or unreliable:

- Too many hard-coded values would turn creative work into form filling.
- Too few controlled values would let AI invent settings that providers cannot execute.
- Mixing image type, visual style, provider settings, and review criteria would make later migrations expensive.
- A preset catalog copied from community projects or current trends would drift quickly and may not match this app's education, article, and batch-production workflows.

The preset layer must therefore separate stable execution contracts from flexible creative guidance.

## Evidence Policy

External references are allowed, but each source has a different authority level.

| Source type | Allowed use | Not allowed use |
| --- | --- | --- |
| Official provider docs | Determine real capabilities, parameters, limits, cost drivers, and unsupported settings. | Treat provider-specific names as domain model names without abstraction. |
| Local code and tests | Decide what already exists and what compatibility must be preserved. | Ignore dirty worktree state or unrelated workflow changes. |
| Community projects | Extract workflow patterns, UI grouping, queue/review ideas, and failure modes. | Copy catalog values, code, prompts, or defaults without license and maintenance review. |
| `skills.sh` and similar skill catalogs | Discover reusable agent workflow structures and prompt patterns. | Trust arbitrary skill instructions as safe or authoritative. |
| GitHub Trending monthly | Discover current project categories and emerging UX patterns. | Treat popularity as proof of long-term correctness. |
| Best-practice articles | Collect candidate dimensions and decision heuristics. | Replace official docs, local tests, or project rules. |

Evidence captured for implementation should use this shape:

```text
dimension | candidate values | source | source_type | confidence | provider_specific | fixed_or_flexible | test_gate
```

## Design Options

### Option 1: Small Hard-Coded Catalog

Keep a compact list of image type presets and expose only a few defaults.

Strengths:

- Easy to implement and test.
- Predictable for users.
- Low migration risk at first.

Weaknesses:

- Can feel limiting when the user wants a nuanced visual direction.
- Encourages adding many special-case presets later.
- Does not capture why AI chose a preset.

### Option 2: Dimension-Only AI Recommendations

Define abstract dimensions and let the text planning model generate specific values at runtime.

Strengths:

- Very flexible.
- Better for brainstorming unfamiliar visual directions.
- Low initial catalog work.

Weaknesses:

- AI can invent non-executable provider values.
- Hard to validate, compare, or reproduce.
- UI and tests cannot rely on stable IDs.

### Option 3: Versioned Core Catalog With AI Recommendation Overlay

Keep a small versioned catalog of executable IDs, let AI choose from it, and allow free-form creative overlays.

AI 推荐: choose Option 3 because it keeps provider execution safe while preserving creative flexibility.

Strengths:

- Stable IDs support persistence, review, tests, and migration.
- AI can still recommend style, composition, and trade-offs.
- Unsupported or unknown settings become visible warnings instead of silent failures.
- The catalog can grow evidence-first instead of trend-first.

Weaknesses:

- Requires a governance spec and invariant tests.
- Needs clear UI copy so users understand recommendations are editable.

## Core Boundary Decisions

### ImageTypePreset

`ImageTypePreset` is a delivery and intent preset, not a visual style preset.

It should answer:

- What is this image for?
- What delivery shape does it usually need?
- How should text be handled?
- Which review rubric should judge success?
- What workflow and failure modes are common?

It should not answer:

- The full artistic style.
- The final prompt wording.
- Every provider-specific parameter.
- The exact generated subject.

### GenerationRecipe

`GenerationRecipe` is an executable provider-neutral recipe.

It should hold:

- provider profile
- model id
- image type preset id
- width and height
- quality
- output format
- background mode
- compression
- moderation mode
- seed
- capability warnings

It should not hold:

- user intent
- brainstorming assumptions
- review reasoning
- long free-form style text

### PromptDirection

`PromptDirection` is a candidate strategy for satisfying a brief.

It should hold:

- prompt text
- negative prompt
- intended use
- strengths
- risks
- structured recommendation fields
- recommendation reason
- capability warnings

It should not silently create new executable IDs. If AI suggests a value outside the catalog, the result must be stored as a non-executable suggestion or warning.

### ReviewRubricTemplate

`ReviewRubricTemplate` is a reusable review contract.

It should be linked from image type presets and prompt direction recommendations. It should not be buried in prompt text because review must stay parseable and comparable.

## Fixed, Constrained, And Free Dimensions

### Fixed Executable Dimensions

These require stable IDs or enum-like values because they affect persistence, provider mapping, test assertions, and replay:

- `imageTypePresetId`
- `reviewRubricTemplateId`
- `textPolicy`
- `workflowMode`
- `outputFormat`
- `backgroundMode`
- `moderationMode`
- `qualityBand`
- `deliveryNamingPolicy`

Provider adapters may translate these into provider-specific values.

### Constrained Recommendation Dimensions

These should come from a known set, but users and AI can override them with warnings:

- `aspectRatio`
- `sizePolicy`
- `styleFamily`
- `compositionFamily`
- `paletteFamily`
- `referenceImageRole`
- `draftCount`
- `finalCount`
- `costStrategy`

The app should prefer common values and mark unusual choices as advanced.

### Free Creative Dimensions

These should remain text fields because enumerating them would reduce quality:

- `styleIntent`
- `compositionIntent`
- `paletteIntent`
- `lightingIntent`
- `textureIntent`
- `subjectNotes`
- `negativePrompt`
- `brandOrFactualConstraints`
- `failureDefinition`

AI can draft these values, but they are not executable contracts by themselves.

## Recommended Preset Record Shape

Future `ImageTypePreset` should grow by additive fields only:

```text
ImageTypePreset
- id
- catalogVersion
- displayName
- description
- deliveryFamily
- defaultAspectRatio
- supportedAspectRatios
- defaultOutputFormat
- defaultTextPolicy
- defaultBackgroundMode
- defaultQualityBand
- reviewRubricTemplateId
- deliveryNamingPolicy
- workflowModes
- styleDimensionHints
- requiredBriefFields
- commonFailureModes
- capabilityRequirements
- deprecation
```

`catalogVersion` records the catalog generation, not a breaking API version. Preset IDs must remain stable. Changing the meaning of an existing ID requires a new ID or a deprecation entry.

## Recommended Prompt Direction Recommendation Shape

Future `PromptDirection` and `PromptDirectionDraft` should add structured recommendation metadata:

```text
PromptDirectionRecommendation
- imageTypePresetId
- textPolicy
- styleIntent
- aspectRatio
- width
- height
- qualityBand
- outputFormat
- backgroundMode
- reviewRubricTemplateId
- draftCount
- finalCount
- recommendationReason
- confidence
- capabilityWarnings
- nonExecutableSuggestions
```

Rules:

- `imageTypePresetId` must match the catalog.
- `reviewRubricTemplateId` must match the catalog.
- `qualityBand` is product-level and maps later to provider `low`, `medium`, `high`, `auto`, or another supported value.
- `nonExecutableSuggestions` may contain AI-created style or workflow ideas, but queue execution must not consume them without user confirmation.
- `confidence` should guide UI display and clarification prompts, not hide options.

## Initial Catalog Strategy

Do not expand to a large preset list yet. Keep the current catalog as the first stable foundation and evaluate each addition through evidence.

Current foundation:

- `educational-poster`
- `article-cover`
- `article-inline-illustration`
- `concept-diagram`
- `graphical-abstract`
- `scholarly-schematic`
- `social-square`
- `background-plate`

Near-term candidates should stay outside the executable catalog until evidence is captured:

- courseware slide visual
- storyboard frame
- icon or sticker
- product concept board
- character sheet
- reference-based edit
- brand campaign visual

Each future preset needs:

- at least one concrete workflow example
- default text policy
- default review rubric
- default delivery shape
- common failure modes
- provider capability implications
- invariant tests

## Text Policy Rules

Text policy is one of the highest-impact preset decisions.

- `DeterministicPostRender`: required for text-heavy posters, formula labels, legends, slide overlays, and precise educational diagrams.
- `Hybrid`: acceptable for covers, social images, and editorial illustrations where small in-image text is optional or decorative.
- `ImageModelOnly`: only for cases where generated text is not critical to correctness.

If a brief contains required readable text, formulas, numbered labels, small captions, or brand-safe copy, the AI recommendation should prefer deterministic post-render text and warn against relying only on model-rendered text.

## Provider Capability Mapping

The neutral catalog must not assume one provider forever.

Provider adapters should map neutral values to supported provider settings:

- size and aspect ratio
- quality
- output format
- compression
- background mode
- moderation mode
- reference image support
- mask/edit support
- streaming support

Unsupported values must produce capability warnings before queue execution. They must not be dropped silently.

## AI Recommendation Flow

When planning prompt directions:

1. Read `CreativeBrief`.
2. Pick a catalog preset by delivery intent and failure risk.
3. Recommend text policy and review rubric from the preset.
4. Recommend size, ratio, quality band, output format, and workflow mode.
5. Generate two to four prompt directions with distinct strengths and risks.
6. Add warnings for text rendering, unsupported provider features, or low confidence.
7. Keep all non-catalog ideas as editable suggestions.

If confidence is low, the provider should return one clarifying question instead of forcing a false match.

## UI Guidance

The UI should present recommendations as editable decisions:

- show recommended preset, text policy, ratio, quality band, and rubric together
- show the recommendation reason
- show provider warnings before generation
- allow users to override with explicit advanced mode
- keep creative style text visible and editable
- avoid exposing provider-specific raw parameters in the simple path

The simple view should answer: "What will be generated, why these settings, and what might fail?"

## Data Compatibility

Implementation should be additive:

- Add optional fields to draft/result records.
- Keep current prompt direction fields intact.
- Keep existing preset IDs stable.
- Add invariant tests before expanding catalog values.
- Store unknown external suggestions separately from executable fields.

No migration should reinterpret an existing preset ID.

## Invariant Tests

The future implementation should add tests for:

- preset IDs are unique
- display names and descriptions are non-empty
- output formats are normalized
- default review rubric IDs exist
- text-heavy presets use `DeterministicPostRender`
- supported aspect ratios are valid
- deprecated presets remain readable
- fake planning provider emits only known executable IDs
- unknown AI suggestions become warnings or non-executable suggestions
- provider capability warnings are preserved through promotion

## Acceptance Criteria

- The product has a documented distinction between image type presets, generation recipes, prompt direction recommendations, and review rubrics.
- AI can recommend image type, style, and parameters without inventing executable IDs.
- Users can inspect and override recommended values.
- The initial catalog stays small and evidence-backed.
- Future catalog expansion has explicit evidence and invariant test requirements.
- No real image API call is required to validate this design.

## Verification Policy

Documentation-only changes must pass:

```powershell
rg -n "(TB[D]|TO[D]O|PLACE''HOL[D]ER)" docs/superpowers/specs/2026-06-02-preset-governance-design.md
git status --short
```

Code implementation later must pass:

```powershell
dotnet build
dotnet test
dotnet format --verify-no-changes
```

## Rollback

Revert this design document commit if the preset governance direction changes. Later implementation slices should remain separate so catalog and recommendation schema changes can be reverted independently.

## Reference Links

- OpenAI Image generation guide: https://developers.openai.com/api/docs/guides/image-generation
- OpenAI Images and vision guide: https://developers.openai.com/api/docs/guides/images-vision
- skills.sh docs: https://skills.sh/docs
- GitHub Trending monthly: https://github.com/trending?since=monthly
