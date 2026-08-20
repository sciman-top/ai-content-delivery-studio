# Local output and delivery map

The repository uses two roots with different authority:

```text
deliveries/                    Gate-2-approved immutable delivery packages only
outputs/
  review-ready/                machine-complete candidates awaiting human gates
  validation/                  regression and agent validation runs
  workspace-history/           historical generation and extraction workspaces
  operator-trials/             operator/manual trial evidence
  diagnostics/                 smoke tests, probes, and verification artifacts
```

`outputs/` is never a final-delivery root. A render, deterministic scientific pass,
fake-provider visual pass, or agent visual inspection can move a candidate into
`review-ready/`, but cannot promote it into `deliveries/`.

For a repository-local desktop or scripted session, explicitly point the product
delivery resolver at the same visible final root before startup:

```powershell
$env:CONTENT_DELIVERY_STUDIO_DELIVERY_ROOT = 'D:\CODE\ai-content-delivery-studio\deliveries'
```

Without that override, the desktop product keeps its platform-local delivery root
under the configured studio data directory. The repository organizer does not
silently rewrite host environment variables.

For article figure sets, use the classified script interface rather than inventing
new top-level directory names:

```powershell
pwsh -NoProfile -ExecutionPolicy Bypass -File scripts/run-article-scientific-figure-set.ps1 `
  -SourcePath paper/example.pdf `
  -OutputClass ReviewReady `
  -RunName example-20260820-v1
```

`Workspace` remains the default. `Validation` and `ReviewReady` write under the
matching `outputs/` category. An explicit `-OutputDirectory` remains available for
compatibility, but callers then own its classification.

After every candidate has passed the declared deterministic and visual checks, an
explicitly authorized operator can approve both gates and atomically promote the
unchanged files into an immutable article-set package:

```powershell
pwsh -NoProfile -ExecutionPolicy Bypass -File scripts/promote-article-scientific-figure-set.ps1 `
  -ReviewReadyDirectory outputs/review-ready/article-figure-sets/example/20260820-v1 `
  -ArticleSlug example `
  -PackageId 20260820-v1 `
  -Reviewer codex-agent-authorized-by-sciman `
  -OperatorKind authorized_agent `
  -AuthorizationReference "Codex task authorization: user message on 2026-08-20" `
  -ApproveGateOne `
  -GateOneNotes "Approved scientific meaning, conditions, directions, values, and exceptions." `
  -ApproveGateTwo `
  -GateTwoNotes "Every candidate was visually spot-checked and approved for delivery."
```

Promotion fails closed for incomplete reports, missing explicit approvals,
authorized agents without an authorization reference, failed sidecars, missing or
hash-drifted files, unsafe paths, or an existing package destination. Publishable
SVG/PNG/PDF files go to `figures/`; source boards and extracted source assets remain
under `evidence/`. `manifest.json` binds every copied file to its review-ready SHA-256
without recording absolute machine paths. An authorized-agent decision is recorded
truthfully and does not become live-provider or independent-human-expert acceptance.

Run the idempotent local migration when older top-level output names are present:

```powershell
pwsh -NoProfile -ExecutionPolicy Bypass -File scripts/organize-local-outputs.ps1 -WhatIf
pwsh -NoProfile -ExecutionPolicy Bypass -File scripts/organize-local-outputs.ps1
```

The organizer never deletes or merges histories. It fails if a destination already
exists, writes `outputs/OUTPUT-CATALOG.json` with stable managed mappings and the
last-run moves, and discovers every valid article delivery manifest into
`finalDeliveryPackages`. `deliveries/README.txt` is regenerated with the current
package list, so running the organizer cannot overwrite final-delivery truth with a
stale "no delivery" message.
