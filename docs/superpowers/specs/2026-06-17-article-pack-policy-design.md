# Article Pack Policy Design

## Goal

Extend the new pack-and-policy contract from the generic image-series workflow to the `article-illustration` scenario.

This slice should prove the scenario-specific pattern is reusable without widening into a full multi-scenario migration in one pass.

## Why Now

- The repository has already landed and verified the generic scenario contract.
- `docs/TASKS.md` still leaves additional scenario-specific pack/policy hardening open.
- `article-illustration` is the most natural next scenario because it is already one of the core V1 supporting routes.

## Scope

This slice covers:

- explicit scenario and policy-pack references on the `article-illustration` workflow pack
- the built-in policy packs needed for the article scenario
- registry and catalog tests proving the article scenario uses the stronger contract

## Non-Goals

This slice does not include:

- policy-link rollout to every remaining workflow pack
- public pack editing
- shell/UI changes
- workflow execution changes

## Acceptance Criteria

- The built-in `article-illustration` workflow pack declares scenario, industry, renderer, and review-rubric policy references.
- The starter registry contains the referenced article policy packs.
- Focused tests and the repo gate pass after the slice lands.
