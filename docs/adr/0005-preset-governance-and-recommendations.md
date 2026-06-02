# ADR 0005: Preset Governance And Recommendations

Status: accepted

Date: 2026-06-02

## Context

ADR 0004 established layered style and parameter governance: image type presets, style guides, generation recipes, references, experiments, and review rubrics remain separate provider-neutral objects.

The `Brief Studio` workflow adds a new pressure point. The text planning provider can now generate multiple prompt directions from a creative brief, and those directions need to recommend image type, text policy, dimensions, quality, output format, and review rubric. If recommendations are free-form only, AI can invent non-executable settings. If every creative choice becomes a fixed enum, the app becomes rigid and weak for brainstorming.

The product therefore needs a stable preset governance rule that preserves both reproducibility and creative flexibility.

## Decision

Use a versioned core catalog with an AI recommendation overlay.

The stable catalog defines executable values:

- `ImageTypePresetCatalog`
- `ReviewRubricTemplateCatalog`
- `ImageTextPolicy`
- provider-neutral recipe fields such as size, quality, output format, background, moderation, and capability warnings

AI planning can recommend from those catalog-backed values, but non-catalog ideas stay in editable suggestion fields. Queue execution must not consume AI-invented executable IDs.

`PromptDirectionRecommendation` is the structured bridge between creative planning and executable generation. It records:

- recommended image type preset
- text policy
- style intent
- aspect ratio and exact draft size
- quality band
- output format
- background mode
- review rubric template
- draft and final counts
- recommendation reason
- confidence
- capability warnings
- non-executable suggestions

## Rationale

This keeps the right parts stable:

- Preset IDs can be persisted, tested, migrated, and shown in UI.
- Review rubrics remain structured and comparable.
- Provider adapters can map neutral quality, size, format, and background settings to real API capabilities.
- AI can still draft style, composition, palette, mood, and prompt language without forcing them into a rigid enum.

It also supports cost control. Text planning can produce several recommended directions before paid or slow image generation starts.

## Alternatives Considered

### Hard-coded preset catalog only

- Pros: simple to implement and test.
- Cons: too rigid for brainstorming and likely to cause catalog bloat.
- Rejected: the product must support exploratory creative work, not only fixed forms.

### AI-generated dimensions only

- Pros: flexible and fast to start.
- Cons: hard to validate, hard to persist safely, and likely to produce unsupported provider settings.
- Rejected: executable settings need stable IDs and capability checks.

### Provider-specific preset catalog

- Pros: easy to map to one provider.
- Cons: leaks provider semantics into the domain and makes future providers harder.
- Rejected: provider adapters should translate from neutral product intent.

## Consequences

- New executable preset IDs require evidence, review rubric mapping, default delivery shape, common failure modes, and invariant tests.
- `PromptDirection` can carry structured recommendation metadata, but prompt text remains editable prose.
- Fake providers must emit recommendation metadata before real providers do.
- Real provider planning must validate structured output before persistence.
- UI can display recommendations as editable decisions with reasons and warnings.
- Community and trending sources can inspire candidates, but official docs, local code, and tests decide executable behavior.

## Follow-Up

- Keep `docs/research/PRESET_GOVERNANCE_EVIDENCE.md` current when adding new executable preset IDs.
- Add UI display and editing for recommendation metadata in a later workflow slice.
- Keep real OpenAI brief-direction planning guarded until structured output validation and tests exist.
