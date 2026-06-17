# Courseware Visual Pack Policy Design

## Goal

Extend the stronger pack-and-policy contract to the built-in `courseware-visual` workflow pack.

This slice should prove the scenario-policy pattern also fits an education-first workflow whose outputs lean toward classroom visuals and slide-ready assets without widening into broader pack rollout or workflow behavior changes.

## Why Now

- Generic image-series, article-illustration, and document-review-translation scenarios already use the stronger contract and pass repository gates.
- `courseware-visual` is the next built-in scenario whose blueprint set already signals a distinct workflow family: lesson slide visuals, classroom diagrams, and worksheet visuals.
- The task list still leaves additional scenario-specific pack/policy hardening open behind bounded spec/plan slices.

## Scope

This slice covers:

- explicit scenario and policy-pack references on the built-in `courseware-visual` workflow pack
- built-in courseware industry, renderer, and review-rubric packs
- focused catalog/export tests proving the courseware scenario uses the stronger contract

## Non-Goals

This slice does not include:

- new slide export or deterministic composition behavior
- new UI or workflow execution behavior
- rollout to poster or other remaining workflow scenarios in the same patch

## Acceptance Criteria

- The built-in `courseware-visual` workflow pack declares scenario, industry, renderer, and review-rubric policy references.
- The starter registry contains the referenced courseware policy packs.
- Focused tests and the repo gate pass after the slice lands.
