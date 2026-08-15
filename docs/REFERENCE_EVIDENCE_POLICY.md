# Reference Evidence Policy

## Scope

Reference evidence is an explicit fail-closed option for four external-contract risk areas. The default repository gate checks manifest parity and reports touched mappings; it does not force a receipt for routine implementation, refactoring, tests, or documentation.

The machine-readable truth is [scripts/reference-basis.json](../scripts/reference-basis.json). [REFERENCE_BASIS.md](./REFERENCE_BASIS.md) contains its generated human-readable summary.

| Area | Enforced source seam | Why it remains strict |
| --- | --- | --- |
| `openai-provider` | OpenAI adapters and provider contracts | Paid/live requests, response shape, routing, and secret boundaries |
| `persistence-and-schema` | Infrastructure persistence implementation | SQLite schema, migrations, serialization, reload, backup compatibility |
| `delivery-package` | Application and infrastructure delivery package code | Manifest/hash/path-containment and durable package compatibility |
| `scientific-figure-workflow` | Scientific domain, application, and infrastructure code | Claim/evidence authority and deterministic scientific rendering |

Directory breadth alone is not a reason to add another area. Add a source rule only when a concrete external contract or recurring high-risk failure requires it.

## Explicit Decision Mode

Use `-RequireDecision` only when external source actually changes or adjudicates the current decision. In that mode, at least one changed accepted evidence file must contain a fenced `reference-decision` JSON block for that area:

```reference-decision
{
  "schemaVersion": 1,
  "area": "openai-provider",
  "trigger": "request-response-shape",
  "consultedSources": [
    {
      "path": "D:/CODE/external/ai-content-delivery-studio-references/01-openai/openai-dotnet",
      "revision": "<fixed-revision>"
    }
  ],
  "observedBehavior": "<relevant source behavior>",
  "decision": "adapt",
  "affectedContract": "<repository contract affected by the decision>",
  "focusedVerification": [
    "<command or probe>"
  ]
}
```

Required fields are validated. `decision` is `adopt`, `adapt`, or `reject`. If no source is available, `consultedSources` may be empty only with `unavailableEvidence.reason`, `expiresAt`, and `recoveryCondition`.

Use an existing durable provider, architecture, launch, research, support-matrix, or change-evidence document listed for the area. A new spec, plan, or evidence file is not required when an existing document is the correct durable home.

## Commands

Default current-change check (parity plus mapped-area advice):

```powershell
.\scripts\verify-reference-evidence.ps1
```

Explicit fail-closed decision check:

```powershell
.\scripts\verify-reference-evidence.ps1 -RequireDecision -Paths src/path-one.cs,docs/path-two.md
```

For `pwsh -File`, pass multiple paths as one comma-separated argument. In explicit decision mode, a mapped source path with no decision returns actionable fail-closed guidance; an empty evidence list must never cause a PowerShell parameter-binding failure.

Manifest/document parity only:

```powershell
.\scripts\verify-reference-evidence.ps1 -ParityOnly
```

Full and Release invoke the default check once through `verify-repo.ps1`. Quick does not invoke it. An implementation task invokes `-RequireDecision` separately only when its risk analysis requires external-source adjudication.

## Boundaries

- In explicit decision mode, a changed evidence path is only a carrier; prose alone does not satisfy the gate.
- A previous decision does not automatically justify a new source change.
- Repository evidence does not prove a live provider call, migration on user data, publication, manual review, or hardware acceptance.
- External source is read-only input. Pin revision and check license before copying implementation.
