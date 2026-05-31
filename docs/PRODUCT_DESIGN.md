# Product Design

## Product Thesis

AI Image Series Studio is a Windows desktop workbench for producing coherent image series, not a single-image prompt toy. It helps users move from vague intent to planned series, generated candidates, reviewed iterations, and clean delivery.

The first target user is a power user creating educational posters, article illustrations, social image sets, courseware visuals, product concept boards, visual storyboards, or themed image packs.

## Core Workflow

```mermaid
flowchart LR
    A["Idea / Goal"] --> B["AI discussion and constraints"]
    B --> C["Series plan"]
    C --> D["Item list"]
    D --> E["Prompt versions"]
    E --> F["Generation queue"]
    F --> G["Candidate gallery"]
    G --> H["Structured review"]
    H --> I{"Approved?"}
    I -->|No| J["Revise prompt or settings"]
    J --> F
    I -->|Yes| K["Delivery package"]
```

## Main Personas

- Solo creator: wants fast, controlled image sets with clear output folders.
- Teacher or content author: cares about factual correctness, readable text, and consistent style.
- Designer/operator: wants candidate comparison, batch controls, metadata, and repeatability.
- Developer/power user: wants provider configuration, workflow export, and auditability.

## First-Class Objects

- Workspace: local root folder containing projects and assets.
- Project: one user goal, such as a poster series or article image set.
- Series: a coherent visual set within a project.
- Item: one planned image target in a series.
- PromptVersion: versioned prompt text and generation settings for one item.
- GenerationTask: queued execution attempt.
- CandidateImage: one generated output plus metadata.
- ReviewRubric: user and AI-readable quality standard.
- ReviewResult: structured scores, pass/fail flags, comments, and suggested fixes.
- DeliveryPackage: final folder with images, prompts, metadata, and manifest.

## MVP Scope

The MVP must support:

- Multi-turn planning chat.
- Series plan and item list editing.
- Prompt generation and manual prompt editing.
- Queue-based batch generation using fake providers first, then OpenAI.
- Candidate gallery with side-by-side prompt, metadata, and review state.
- Structured AI-assisted review using a rubric.
- Prompt revision loop and regeneration history.
- Final delivery export with manifest.
- Import of the physics poster project as a sample migration.

The MVP excludes:

- Multi-user collaboration.
- Cloud sync.
- Marketplace plugins.
- Full node-graph editor.
- In-app pixel painting.
- Real API calls by default in tests.

## UI Structure

The window uses a workbench layout:

- Left rail: Workspaces, Projects, Settings.
- Main tabs: Brief, Plan, Prompts, Queue, Gallery, Review, Delivery.
- Right inspector: selected item metadata, prompt version, review summary, and actions.
- Bottom activity panel: queue status, cost estimate, logs, warnings, and errors.

## Review Model

Review is hybrid:

- AI review checks visible content against rubric.
- Programmatic checks validate files, dimensions, naming, metadata, and manifest.
- Human approval decides final status.

Hard-fail examples:

- Missing required subject.
- Wrong count of final images per item.
- Unsafe or disallowed content.
- Text-heavy output with unreadable or hallucinated text.
- Factual or brand-critical mismatch.
- Image does not match the selected item.

## Delivery Model

Each delivery package contains:

- Final images.
- Optional alternates.
- Prompt snapshots.
- Candidate metadata.
- Review report.
- Project manifest in JSON and CSV.
- Provider settings summary with secrets redacted.

Delivery folders are content-oriented and stable. Temporary generation batches are not the final structure.
