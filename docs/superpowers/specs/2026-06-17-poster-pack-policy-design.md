# Poster Report Delivery Pack Policy Design

## Goal

Extend the stronger pack-and-policy contract to the built-in `poster-report-delivery` workflow pack.

This slice should prove the scenario-policy pattern also fits a delivery-oriented workflow without widening into general packaging behavior or UI changes.

## Why Now

- Generic image-series, article-illustration, document-review-translation, and courseware-visual scenarios already use the stronger contract and pass repository gates.
- `poster-report-delivery` is the last built-in scenario in the current starter catalog and its blueprint set already signals a distinct workflow family: poster packages, infographic reports, and review-backed delivery.
- The task list still leaves additional scenario-specific pack/policy hardening open behind bounded spec/plan slices.

## Scope

This slice covers:

- explicit scenario and policy-pack references on the built-in `poster-report-delivery` workflow pack
- built-in poster industry, renderer, and review-rubric packs
- focused catalog/export tests proving the poster scenario uses the stronger contract

## Non-Goals

This slice does not include:

- new poster-generation behavior
- new UI or workflow execution behavior
- additional scenario rollout beyond poster in the same patch

## Acceptance Criteria

- The built-in `poster-report-delivery` workflow pack declares scenario, industry, renderer, and review-rubric policy references.
- The starter registry contains the referenced poster policy packs.
- Focused tests and the repo gate pass after the slice lands.
