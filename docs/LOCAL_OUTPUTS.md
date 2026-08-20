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

Run the idempotent local migration when older top-level output names are present:

```powershell
pwsh -NoProfile -ExecutionPolicy Bypass -File scripts/organize-local-outputs.ps1 -WhatIf
pwsh -NoProfile -ExecutionPolicy Bypass -File scripts/organize-local-outputs.ps1
```

The organizer never deletes or merges histories. It fails if a destination already
exists, writes `outputs/OUTPUT-CATALOG.json` with stable managed mappings and the
last-run moves, and leaves current final-delivery status visible in each article report.
