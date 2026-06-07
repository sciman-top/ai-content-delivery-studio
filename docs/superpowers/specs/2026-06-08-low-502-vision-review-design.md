# Low-502 Vision Review Design

## Current Landing And Target

- Current landing: `D:\CODE\ai-image-series-studio`
- Target: prevent the product from inheriting the same long-session, image-heavy review fragility seen in the physics poster production workflow.
- Scope: provider routing, bounded review requests, local review-prep artifacts, queue thresholds, and product-runtime guidance.

## Problem

The product already has the right high-level boundaries:

- separate planning, image generation, and vision review providers
- fake-first development
- `store: false` by default
- operator and review audit records

But those boundaries are not yet explicit enough about one recurring operational risk: a review workflow can still become too large if it tries to combine too many images, too much prior context, and too many repair rounds into one remote model thread.

That is especially dangerous for:

- article-illustration review
- candidate gallery triage
- repair routing across many items
- future multi-turn image state experiments

## Goals

- Make structured vision review work through bounded, local-direct provider calls rather than long conversational review chains.
- Keep the product's local project state as the system of record for review history.
- Require local deterministic pre-review artifacts before remote vision review scales up.
- Prevent `previous_response_id` and multi-turn review state from becoming the default path.
- Keep the product ready for image review with cloud APIs, but without making review requests unnecessarily large.

## Non-Goals

- Do not ban cloud vision review.
- Do not force the product back to human-only review.
- Do not make Responses multi-turn image state a default launch requirement.
- Do not widen the launch scope into fully autonomous cross-batch review orchestration.

## Recommended Approach

AI 推荐: treat vision review as a local-orchestrated, small-batch cloud service call, not as a long-lived remote conversation.

That means:

- the app prepares compact local review artifacts first
- review requests are sent directly by the app with dedicated review credentials
- each request is bounded by item count and image count
- request state is stateless by default: `store: false`, no `previous_response_id`
- the result is persisted locally as structured review records and repair routes

## Local Review-Prep Artifacts

Before asking a remote model to review images, the product should be able to build:

- contact sheets or thumbnail grids
- item-level candidate manifests
- prompt and setting summaries
- selected evidence anchors
- compact review instructions

These local artifacts should shrink what the remote model sees. The product should not default to sending entire historical review chains or broad workspace context.

## Routing Policy Decision

For image review in this product:

- direct local provider call is preferred runtime behavior
- Responses API remains the best official surface for structured multimodal review
- but the product should use it as a fresh, bounded request rather than as a chained review transcript unless a specific workflow proves the benefit

In other words, the question is not "Responses or not". The real decision is "long chained state or bounded local-direct request". The recommended default is the second one.

## Batch And Threshold Rules

Recommended defaults:

- one review batch should usually cover `3` to `6` items
- high-risk review batches should usually cover `2` to `4` items
- the app should calculate estimated image count per batch before dispatch
- if a batch exceeds a configured threshold, the app should require split or staged execution

The thresholds belong in application options and operator descriptors, not in user memory.

## Statefulness Rules

Default review path:

- `store: false`
- no `previous_response_id`
- no hidden dependence on a remote conversation object

Stateful review is allowed only when:

- the workflow clearly benefits from continuity
- the batch stays small
- local provenance still captures enough evidence
- the product explicitly records why stateful review was used

## Credential Boundary

Vision review must keep dedicated review credentials separate from image-generation credentials, matching the existing role-scoped provider direction.

This protects:

- image-only merchant keys
- cost attribution
- incident isolation
- fail-closed routing

## Acceptance Direction

The next hardening slice should prove:

- the product can prepare local compact review artifacts
- structured review requests can run as bounded local-direct cloud calls
- the review path stays stateless by default
- repair routing still works without long chained review history
- queue/options/task docs encode the thresholds so the product does not drift back into oversized review sessions
