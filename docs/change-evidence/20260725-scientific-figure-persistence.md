# Scientific Figure Persistence Evidence

Date: 2026-07-26

## Scope

This evidence records Task 9 of the trustworthy scientific-figure workflow.
The slice adds:

- a project-owned `ScientificFigureWorkflowAggregate`
- indexed relational authority and workflow-state fields
- a complete `scientific-figure-workflow.v1` JSON payload
- restoration through the Task 6-8 domain factories and approval transitions
- a dedicated EF repository with insert/update, single-record load, and
  project listing
- additive, idempotent SQLite initialization for databases created before the
  scientific workflow table existed
- desktop startup use of the schema initializer

It does not implement real document extraction, provider understanding,
rendering, review, repair, Gate 2, delivery, or WPF workflow surfaces.

## Persistence Contract

- Relational fields retain workflow, project, source, understanding, and
  specification identifiers and versions plus Gate 1 state.
- JSON retains extraction blocks and diagnostics, claims and evidence roles,
  Figure Spec elements and relations, reviewer snapshots, and downstream
  approvals.
- The payload carries an explicit schema version. Unknown versions block load.
- Reload reconstructs extraction, understanding, specification, Gate 1, and
  downstream state through validated domain methods.
- Reload compares the reconstructed aggregate with its indexed columns.
  Mismatch blocks load instead of accepting drift.
- Scientific revisions remain `FigureSpecDraft` with a higher spec version,
  no Gate 1 approval, and no downstream approvals after reload.

## Migration

The application previously called EF Core `EnsureCreatedAsync` at startup.
That API creates a new database but does not add tables to an existing one.
`AppDatabaseInitializer.InitializeAsync` now:

1. retains `EnsureCreatedAsync` for new databases
2. runs `CREATE TABLE IF NOT EXISTS` for `ScientificFigureWorkflows`
3. creates the project lookup and unique specification-version indexes

The DDL is additive: it changes no existing columns or project records. A
focused test creates the full current schema, removes only the scientific
table to represent a pre-Task-9 database, initializes it, reloads the existing
project, and observes an empty scientific-record list.

## Rollback

Code rollback restores the prior desktop startup call and removes the
scientific repository, configuration, codec, record, and aggregate.

Database rollback is intentionally separate and destructive:

1. stop application writes
2. back up `studio.sqlite`
3. export `ScientificFigureWorkflows` when records exist
4. run `DROP TABLE "ScientificFigureWorkflows";`
5. restore the pre-Task-9 application build

Dropping the table loses persisted scientific records and therefore must not
be automated as part of source rollback. Existing project tables are not
changed by either the forward or reverse operation.

## Test-First Evidence

The first focused run failed at compile time because
`ScientificFigureWorkflowAggregate` did not exist. After the minimal
implementation, the legacy fixture was corrected from an unrealistic
single-table database to a complete pre-Task-9 schema. A SQLite translation
failure then exposed `DateTimeOffset` ordering in the project-list query; the
repository now uses stable identifier ordering.

Focused command:

`dotnet test ContentDeliveryStudio.sln --filter "ScientificFigurePersistenceTests|PersistenceTests" --no-restore`

Focused result before final repository closeout: exit `0`, `14 / 14` passed.

## Compatibility And N/A

- Existing project schema: compatible; no existing table or column changes.
- Existing projects without scientific records: load normally and return an
  empty scientific workflow list.
- Runtime dependency/supply-chain change: `gate_na`; no package or executable
  was added.
- Paid/live provider: `gate_na`; Task 9 has no provider behavior, so explicit
  paid-call authorization was not consumed.
- UI/manual acceptance: `gate_na`; no UI changed. Recovery condition: later
  scientific workspace tasks must record human acceptance.

## Repository Gates

Final closeout uses the fixed repository order:

1. `dotnet build ContentDeliveryStudio.sln`
2. `dotnet test ContentDeliveryStudio.sln --no-build`
3. `pwsh -NoProfile -ExecutionPolicy Bypass -File scripts/verify-reference-evidence.ps1`
4. `dotnet format ContentDeliveryStudio.sln --verify-no-changes --no-restore`
5. `pwsh -NoProfile -ExecutionPolicy Bypass -File scripts/preflight-release.ps1 -NoRestore`

Fresh final outcomes:

- build: exit `0`, 0 warnings, 0 errors
- full test: exit `0`, `529 / 529` passed, 0 failed, 0 skipped
- reference evidence: exit `0`; `persistence-and-schema` and
  `scientific-figure-workflow` detected the repository-owned plan evidence
- format verification: exit `0`
- release preflight: exit `0`; canonical repository verification, publish
  WhatIf, placeholder/conflict scans, and diff hygiene passed
