# Scientific Figure Agent-Operated Native WPF Trial Evidence

Date: 2026-07-28

## Status

`agent-operated evidence complete / human operator trial pending`

## Purpose And Boundary

This supplemental probe exercised the accepted scientific-figure workflow in
the real native WPF application with deterministic fake providers. It adds
user-visible operational evidence beyond automated tests, but it is not a
human operator decision and does not satisfy `operator/manual evidence`.

The probe did not reopen scientific Tasks 1-30 or Checkpoints 0-5, call a paid
provider, refresh live evidence, modify an accepted artifact, or invoke the
trial harness `Finalize` mode.

## Session Evidence

- Run ID: `agent-native-20260728-01`
- Repository commit at launch: `7046a8dec82a9a5af961c8c9f567c411467cf2da`
- Ignored session root:
  `outputs/scientific-figure-operator-trials/agent-native-20260728-01`
- Lifecycle after the WPF process exited: `status=awaiting_finalize`,
  `evidenceLevel=pending_operator`, `providerMode=fake`, and
  `liveAccepted=false`
- Isolated runtime data: session-local `data/studio.sqlite`; no normal user
  workspace or accepted evidence path was used
- WPF and harness processes exited after the trial; no finalization command was
  run

The session remains under ignored `outputs/`. Its SQLite database and delivery
ZIP are local runtime evidence and are intentionally not committed.

## Native WPF Observations

The application title bar reported `Fake Provider`. The activity panel stated
that text, image, and visual-review providers were fake implementations and
that no real API call was enabled.

The agent inspected the five visible scientific workspaces in sequence:

1. Source showed `block-dynamics`, page `4`, section `2.1 Dynamics`, and the
   exact statement `Net force causes acceleration for constant mass.`
2. Understanding showed accepted claim `claim-newton-second-law` with
   confidence `0.96` and no missing evidence.
3. Figure Spec showed `element-force`, `element-acceleration`, and
   `relation-force-acceleration` with the `Causes` relation and frozen Gate 1
   authority at specification version `1`.
4. Render & Review displayed the deterministic SVG preview and all three
   authority items. Contract hard failures, scientific semantic issues, and
   visual issues were empty; authorized repair remained disabled.
5. Delivery displayed SVG, PNG, and PDF artifacts, three claim-evidence maps,
   `Contract review: True`, `Machine review: True`, `repair count: 0`, and the
   deterministic fake semantic and visual provider metadata.

The Gate 2 fields were deliberately entered as reviewer `codex-agent` with the
note `Agent-operated fake-first native WPF probe; not human operator/manual
acceptance.` The resulting approval exists only inside the local package and
must not be interpreted as a human approval. Native-window screenshots were
observed in the task transcript but were not promoted into repository or
accepted evidence artifacts.

## Delivery Package Inspection

The WPF export wrote the declared local package:

`outputs/scientific-figure-operator-trials/agent-native-20260728-01/delivery/scientific-figure.zip`

- ZIP size: `548433` bytes
- ZIP SHA-256:
  `C3E09F21374C48B0B74979F9A5A0B5E97CD7BF0F7A3B040E7D46C47551F6B959`
- Required entries present: `figure.svg`, `figure.png`, `figure.pdf`,
  `specification.json`, `claim-evidence-item-map.json`, `reviews.json`,
  `repairs.json`, `providers.json`, `approvals.json`, and `manifest.json`
- Package review record: contract passed with no hard failures; machine review
  allowed Gate 2 with no blockers
- Provider records: `fake-scientific-semantic / deterministic-fake-v1` and
  `fake-scientific-visual / deterministic-fake-v1`
- Manifest artifact hashes matched the exported SVG, PNG, and PDF values shown
  by the WPF Delivery workspace

Package structure and hashes were inspected read-only. The human-only
`Finalize` validator was intentionally not called, so `trial.json` remains
`pending_operator` rather than `operator/manual evidence`.

## Truth Readout

- Repo-side operator-trial kit: done and unchanged.
- Agent-operated native WPF evidence: complete for this deterministic fake
  sample.
- Human operator/manual evidence: pending; a real operator must inspect all five
  workspaces and explicitly finalize a new session.
- Existing live accepted evidence: unchanged; provider behavior did not change
  and no live refresh was performed.
- Future trigger boundaries: OCR-heavy inputs, measured or fabricated data
  plots, microscope-like observations, automatic scientific-meaning changes,
  and generated visuals represented as observations remain excluded.

## Rollback And Retention

Revert this evidence document and its `TASKS.md` line to remove only the
repository record. The ignored session can be retained for local inspection or
removed separately after resolving its exact path; Git rollback does not alter
runtime evidence. Existing accepted corpus, live evidence, and closeout
documents remain untouched.

## Repository Gates

The fixed-order closeout passed on 2026-07-28:

1. build: exit `0`, `0` warnings, `0` errors
2. test: exit `0`, `672 / 672` passed
3. contract/invariant: reference evidence and format passed
4. hotspot: release preflight, nested repository verification, publish WhatIf,
   placeholder/conflict scans, and diff hygiene passed; the nested suite also
   passed `672 / 672`

After recording these results, the same fixed order is rerun against the final
tree. Git status is also checked to confirm that `.env`, `artifacts/`, SQLite,
`workspace/`, `outputs/`, and generated ZIP files are not in the write set.
