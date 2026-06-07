# V1 Launch Evidence

This report captures the current V1 launch evidence against the readiness checks in `docs/PRD_V1.md` and `docs/ROADMAP.md`.

Generated on: 2026-06-07

## Gate Evidence

| Gate | Command | Result | Evidence |
| --- | --- | --- | --- |
| Build | `dotnet build` | Pass | 0 warnings, 0 errors on 2026-06-07. |
| Test | `dotnet test` | Pass | 271 tests passed, 0 failed on 2026-06-07. |
| Format | `dotnet format --verify-no-changes` | Pass | Exit code 0 on 2026-06-07. |

## Launch Metrics

| Metric | Current status | Evidence | Gap or next action |
| --- | --- | --- | --- |
| Fake-first primary workflow completes from planning through delivery. | Pass | `tests/ImageSeriesStudio.Tests/FakeWorkflowTests.cs` covers fake planning, generation, review, approval, delivery package export, and manifest output. | The fuller brief -> blueprint promotion path is covered by focused application tests, but the UI-level golden path still needs a launch rehearsal. |
| Article or plain-text supporting route creates evidence-backed illustration targets without paid providers by default. | Pass | `tests/ImageSeriesStudio.Tests/DocumentIllustrationWorkflowTests.cs` and persistence tests cover fake-provider document planning, evidence anchors, approved target promotion, and stored plans. | Binary document extraction remains outside V1 launch scope. |
| Educational poster proof path exports deterministic text-composition provenance and human approval evidence. | Pass | `SkiaDeterministicTextComposerTests`, `PostRenderTextCompositionServiceTests`, `CompositionReadabilityCheckServiceTests`, delivery manifest tests, and final approval persistence/export tests cover composition output, layout report, readability findings, and approval evidence. | UI workflow wiring for invoking composition remains a later hardening opportunity. |
| One real low-risk operator action executes end-to-end with audit evidence. | Pass | `ArtifactValidationToolAdapterTests` covers additive local artifact validation, validation report output, `LowRiskAutoRepairService`, and diagnostics audit export. | No rollback action is needed for the current additive validation output; deleting the generated validation report is sufficient cleanup. |
| OpenAI provider routing defaults are locked. | Pass | `OpenAiProviderContractTests` and `V1LockedDefaultsTests` cover Responses for planning/review, Images for single-shot image generation, strict structured output schemas, and `store: false`. | SDK adoption remains a separate open task. |
| Opt-in real OpenAI 2-item sample series is verified with provenance, redaction, and approval evidence. | Not captured | Real paid API smoke tests require explicit user opt-in and credentials. | Run only after explicit approval; fake-first evidence remains the default regression path. |

## Remaining Launch Risks

- Primary and supporting routes are covered by automated tests, but still need a manual UI rehearsal before declaring the release experience polished.
- Official OpenAI .NET SDK evaluation remains open; current runtime keeps raw `HttpClient` for implemented provider surfaces.
- Responses API multi-turn image state remains intentionally deferred until it improves provenance, revision loops, or preview UX.
