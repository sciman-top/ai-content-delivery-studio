# Scientific Figure Authorized-Agent Operator Acceptance Evidence

Date: 2026-07-28

## Status

`authorized operator contract complete / operator-manual evidence accepted`

## Product Decision And Evidence Boundary

The user explicitly decided on 2026-07-28 that an agent which performs the
native WPF inspection under user authority replaces, and has the same acceptance
authority as, a human operator. The repository now models this as an
`authorized_operator_v1` policy.

Identity is not rewritten: the actor remains `authorized_agent`. Decision
authority is recorded separately as `equivalent_operator_acceptance`. Automated
tests, unattended UI automation, or ZIP validation alone still cannot create
this evidence; finalization requires an actual five-workspace inspection,
explicit user authorization, reviewer and notes, Gate 2 identity match, and all
package gates.

The earlier [agent-operated probe record](./20260728-scientific-figure-agent-operated-trial.md)
truthfully described the state at that time: the run had no policy granting
equivalent authority and remained pending. This document records the later user
decision and successful schema v2 finalization; it does not retroactively label
the actor human.

## Contract Change

`scripts/run-scientific-figure-operator-trial.ps1` now supports:

- `OperatorKind=human|authorized_agent`, with `human` as the compatible default;
- a required non-empty `AuthorizationReference` for `authorized_agent`;
- schema v2 `acceptancePolicy=authorized_operator_v1`;
- truthful `operatorKind`, traceable `authorizationReference`, and
  `decisionAuthority=equivalent_operator_acceptance` in finalized records;
- successful migration of schema v1 pending sessions and their relocated local
  paths during finalization.

All prior gates remain unchanged: explicit confirmation of Source,
Understanding, Figure Spec, Render & Review, and Delivery; non-empty reviewer
and notes; accepted/rejected outcome; immutable finalized records; Gate 2
reviewer match; safe unique ZIP paths; required entries; and SVG/PNG/PDF hashes.

## Runtime Acceptance Result

- Run ID: `20260728-204423-261`
- Repository commit at original launch:
  `ab67418ce701e36284bbf81f0ec6f94584b53b6a`
- Runtime record after finalization: schema `2`, `status=accepted`,
  `evidenceLevel=operator/manual evidence`
- Provider boundary: `providerMode=fake`, `liveAccepted=false`
- Reviewer: `codex-agent-authorized-by-sciman`
- Operator kind: `authorized_agent`
- Decision authority: `equivalent_operator_acceptance`
- Authorization reference: the user's explicit 2026-07-28 task instruction
  that agent-operated acceptance is equivalent to human-operated acceptance
- Confirmed workspaces: all five required workspaces
- Package entry count: `10`
- Package SHA-256:
  `25A345CD49FC66C5CE967F13BFF57CA7C2540EA8E014791BC561A7CE0FDD8F65`
- Package validation: reviewer matched; SVG, PNG, and PDF hashes matched

The pre-finalization record is retained next to the ignored session as
`trial.pre-authorized-finalize.json`. The complete session and ZIP remain under
ignored `outputs/` and are not committed.

Session `20260728-214821-253` remains
`awaiting_finalize / pending_operator / fake / liveAccepted=false`. It has no
Gate 2 decision or delivery ZIP and was not accepted merely because another run
qualified.

## Test Evidence

The script suite first failed in three places against the old contract: human
records lacked actor/authority fields, a schema v1 authorized-agent session did
not migrate, and an agent without an authorization reference could finalize.

After implementation:

`dotnet test tests/ContentDeliveryStudio.Tests/ContentDeliveryStudio.Tests.csproj --filter "FullyQualifiedName~ScientificFigureOperatorTrialScriptTests" --nologo`

Result: exit `0`, `9 / 9` passed. Coverage includes human compatibility,
authorized-agent schema migration and acceptance, missing-authorization
rejection, workspace attestation, reviewer/hash failures, rejected outcomes,
outside-repository launch, and lifecycle immutability.

## Fixed-Order Verification

The implementation and documentation tree passed:

1. Build: exit `0`; `0` warnings; `0` errors
2. Tests: exit `0`; `679 / 679` passed
3. Contract/invariant: reference governance/evidence passed; format had no
   changes
4. Hotspot: release preflight passed, including nested `679 / 679`, publish
   WhatIf, placeholder/conflict scans, and cached/uncached diff hygiene

The same fixed order is rerun after this evidence and the completed plan are on
the final tree.

## Unchanged Truth

- Scientific Tasks 1-30 and Checkpoints 0-5 remain accepted and closed.
- Existing live accepted evidence is unchanged; no paid provider was called.
- Repo-side implementation, operator/manual evidence, and live accepted remain
  separate evidence layers.
- OCR-heavy sources, measured or fabricated data plots, microscope-like
  observations, automatic scientific-meaning changes, and generated visuals
  represented as observations remain Future Trigger Lane exclusions.
- No accepted corpus artifact, `.env`, SQLite, workspace, outputs, screenshot,
  or ZIP enters Git.

## Review And Rollback

Five-axis review covers actor truth, authorization fail-closed behavior, legacy
compatibility, lifecycle immutability, package security, and unattended
automation boundaries. No dependency, provider, schema database, or domain
contract changed.

Revert this repository slice to remove authorized-agent finalization for future
runs. The already-finalized ignored session remains historical evidence and is
not changed by Git rollback. Its pre-finalization backup provides a local audit
record but must not be used to silently erase the accepted decision. Existing
live evidence and the incomplete pending session remain unchanged.
