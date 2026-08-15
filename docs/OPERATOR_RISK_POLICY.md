# External Action Safety

## Current Status

The generic `OperatorAction` / `OperatorRun` / ToolAdapter runtime is retired because it had no current desktop product consumer. This file now preserves only the safety boundary for concrete external or destructive actions. It is not an extension-platform contract.

The locked V1 snapshot contains historical evidence for an earlier low-risk validation slice. That evidence does not mean the current application exposes a generic operator runtime.

## Rules

- Read-only inspection and additive outputs are preferred.
- Live or paid provider dispatch requires explicit current authorization for the exact request set and cost boundary.
- External publishing, third-party account changes, destructive filesystem work, credential changes, and writes outside declared roots require explicit current confirmation.
- Prepared queues, configured credentials, prior approvals, old evidence, or a passing repository gate never imply permission to act.
- Secrets and private source content must not be copied into logs, prompts, diagnostics, or Git.
- User project state, SQLite data, `workspace/`, and `outputs/` need their own backup or containment plan; Git rollback is not data recovery.
- Browser or desktop automation is used only for a concrete user-visible workflow when no safer API or CLI exists.

## Product Implementation

Safety belongs at the concrete use-case boundary:

- provider guards and immutable execution receipts for paid calls;
- path containment and approval checks for delivery;
- explicit save/publish commands for external effects;
- task-scoped authorization and rollback for local maintenance.

Do not restore a registry, adapter hierarchy, risk-level domain model, or generic audit package merely to represent these rules. Reintroduction requires two real product consumers and an accepted migration decision.

## Truth Boundary

Repository tests can prove guards, request identity, path safety, redaction, and deterministic packaging. They cannot prove a paid call, manual review, hardware behavior, third-party publication, or field acceptance.
