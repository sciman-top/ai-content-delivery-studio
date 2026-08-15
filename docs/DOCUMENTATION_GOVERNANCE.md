# Documentation Governance

Documentation is intentionally small and role-based. Git history is the archive for completed implementation slices.

| Question | Source |
| --- | --- |
| What does V1 promise? | [PRD_V1.md](./PRD_V1.md) |
| What are the two current production lanes? | [PRD_POST_V1_PRODUCT_FOCUS.md](./PRD_POST_V1_PRODUCT_FOCUS.md) |
| What remains actionable? | [TASKS.md](./TASKS.md) |
| What direction should future work follow? | [ROADMAP.md](./ROADMAP.md) |
| What historical release/live claims are valid? | [V1_LAUNCH_EVIDENCE.md](./V1_LAUNCH_EVIDENCE.md) |
| How is the system organized? | [ARCHITECTURE.md](./ARCHITECTURE.md) |
| How should AI implement a change? | [AI_CODING_WORKFLOW.md](./AI_CODING_WORKFLOW.md) |
| When is external-source evidence required? | [REFERENCE_EVIDENCE_POLICY.md](./REFERENCE_EVIDENCE_POLICY.md) and [REFERENCE_BASIS.md](./REFERENCE_BASIS.md) |

## Rules

- Code, tests, runtime behavior, and current configuration outrank stale narrative status.
- Do not put changing test counts, transient dates, current branch state, or per-slice completion lists in README, TASKS, ROADMAP, or AGENTS.
- Do not create a spec, plan, evidence receipt, task queue entry, or governance check for an ordinary implementation slice.
- Use an ADR only for a durable architectural decision with meaningful alternatives and compatibility impact.
- Use `docs/change-evidence/` only for externally meaningful live, human, hardware, migration, waiver, or release acceptance that cannot be reconstructed from code and Git history.
- Update an existing document only when its durable contract or current external blocker changed.
- Keep historical snapshots historically truthful; never overwrite them to resemble the current repository baseline.
- English and Chinese companions should agree on user-facing meaning. A historical snapshot may retain its original wording when clearly marked as historical.

## Review Shortcut

For ordinary code work, read the affected code/tests and [TASKS.md](./TASKS.md). Read product, architecture, provider, operator, or reference documents only when that boundary is touched. There is no machine execution queue or mandatory spec/plan/evidence chain.
