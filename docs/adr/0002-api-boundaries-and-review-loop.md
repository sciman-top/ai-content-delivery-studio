# ADR 0002: API Boundaries And Review Loop

Date: 2026-05-31

## Status

Accepted.

## Context

The product needs AI conversation, planning, prompt generation, image generation, image editing, image review, and iterative prompt repair. A single API abstraction would hide important differences between text, image, and vision workloads.

OpenAI official guidance distinguishes direct Image API generation from conversational Responses API workflows. Vision review also has known limits, especially around small text, spatial reasoning, counting, and metadata.

## Decision

Use three provider contracts:

- `ITextPlanningProvider`
- `IImageGenerationProvider`
- `IVisionReviewProvider`

Treat AI review as advisory. Human approval remains the final delivery decision.

## Rationale

This keeps the app provider-neutral and makes capability differences explicit. It also prevents review confidence from being confused with final truth.

## Consequences

- Provider settings must be stored per capability.
- The UI can show which provider and model produced each plan, image, and review.
- Tests can use fake providers independently.
- Delivery metadata can trace every final image to the exact prompt, model, settings, and review loop.
