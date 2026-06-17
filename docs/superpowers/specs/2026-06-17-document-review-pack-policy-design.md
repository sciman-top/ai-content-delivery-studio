# Document Review Translation Pack Policy Design

## Goal

Extend the stronger pack-and-policy contract to the built-in `document-review-translation` workflow pack.

This slice should prove the scenario-policy pattern also fits a document-centric workflow without widening into broad source or renderer automation.

## Why Now

- Generic image-series and article-illustration scenarios already use the stronger contract and pass repository gates.
- `document-review-translation` is the next built-in scenario whose blueprint set already signals a distinct workflow family: translation review, paper review summary, and LaTeX cleanup.
- The task list still leaves additional scenario-specific pack/policy hardening open behind bounded spec/plan slices.

## Scope

This slice covers:

- explicit scenario and policy-pack references on the built-in `document-review-translation` workflow pack
- built-in document-review industry, renderer, and review-rubric packs
- focused catalog/export tests proving the document-review scenario uses the stronger contract

## Non-Goals

This slice does not include:

- binary extraction or OCR behavior changes
- new UI or workflow execution behavior
- rollout to courseware or poster scenarios in the same patch

## Acceptance Criteria

- The built-in `document-review-translation` workflow pack declares scenario, industry, renderer, and review-rubric policy references.
- The starter registry contains the referenced document-review policy packs.
- Focused tests and the repo gate pass after the slice lands.
