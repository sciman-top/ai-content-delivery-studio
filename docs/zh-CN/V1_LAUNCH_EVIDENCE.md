# V1 发布证据

> 英文原文: [../V1_LAUNCH_EVIDENCE.md](../V1_LAUNCH_EVIDENCE.md)
> 中文文档中心: [./README.md](./README.md)
> 说明: 本文件是中文说明版。若与英文原文、代码、测试、脚本或证据文件冲突，以仓库事实和英文正式文档为准。

## 目的

本文件用于跟踪当前仓库对 [../PRD_V1.md](../PRD_V1.md) 中显式 V1 发布指标的证据状态。

在核心文档集中，它是当前 V1 发布验证状态的正式文件。`ROADMAP.md` 和 `TASKS.md` 可以描述执行顺序或实现进展，但对当前发布声明必须让位于本文件。详见 [../DOCUMENTATION_GOVERNANCE.md](../DOCUMENTATION_GOVERNANCE.md)。

这里对证据类型保持严格区分：

- `Automated repo evidence`: 仓库内的本地测试、构建门禁和确定性产物校验。
- `Live operator evidence`: 一次真实的本地工具动作或工作流执行，并写出产物或审计输出。
- `Live provider evidence`: 使用当前凭据执行的一次 opt-in 真实 provider 运行，并通过脱敏检查。

目标是让发布就绪判断保持诚实。实现切片完成，不等于发布指标完成。

## 快照

- 快照日期: `2026-06-23`
- 仓库根目录: `D:\CODE\ai-content-delivery-studio`
- 最近一次自动化仓库验证: `2026-06-23`，命令为 `.\scripts\verify-repo.ps1 -NoRestore`
- 最近一次真实 provider 样本运行: `artifacts/live-openai-v1-sample/20260611-132947`
- 本快照使用的最新门禁:
  - `.\scripts\verify-repo.ps1 -NoRestore`
- 结果:
  - Reference governance parity passed
  - Reference evidence gate passed
  - Build passed
  - Tests passed: `433 / 433`
  - Format check passed

## 指标台账

| 发布指标 | 当前状态 | 当前证据 | 剩余缺口 |
| --- | --- | --- | --- |
| 主路径在不使用付费 API、且不手工改数据库的前提下，连续三次完成 fake-first 端到端运行。 | 已由 automated repo evidence 验证 | `PrimaryLaunchRouteVerificationTests.PrimaryLaunchRoute_CompletesThreeConsecutiveFakeFirstRunsWithoutManualDatabaseEdits` 证明了三次连续的短需求 -> brief -> blueprint -> series -> review -> delivery 运行，并使用 fake provider 与持久化本地状态检查。支撑切片覆盖仍在 `FakeWorkflowTests`、`ProjectApplicationServiceTests` 和 `BriefWorkflowApplicationServiceTests` 中。 | 未来仍可补一条面向用户的脚本来镜像这组测试，但发布指标已经有自动化证明。 |
| 一组 `2-item` 样本序列能通过 opt-in OpenAI 路径完成，并验证 request provenance、review evidence 和 secret redaction。 | 已由 live provider evidence 验证 | `artifacts/live-openai-v1-sample/20260611-132947/live-v1-sample-summary.json` 记录了当前可用的最新 opt-in OpenAI 运行。该样本通过了 launch preflight，完成了真实 text planning、真实 image generation、真实 image review、final approval、delivery export 和 diagnostics export。`outputs/delivery/manifest.json` 显示两个 item 都在首次批准尝试中完成，并保留了 prompt snapshot 与 metadata；`diagnostics/diagnostics.json` 与 `openai-launch-preflight.json` 保持了 secret 值脱敏。该样本还验证了当前紧凑 visual-review 路径，以及官方 SDK Images 路径上受控的瞬时 `502 upstream_error` 重试。自动化护栏仍在 `OpenAiProviderContractTests`、`OpenAiProviderConfigurationTests`、`OpenAiProviderSmokeTests`、`OpenAiLaunchPreflightTests`、`OpenAiLaunchPreflightReportWriterTests`、`OpenAiLaunchPreflightToolAdapterTests`、`ToolAdapterServiceCollectionExtensionsTests`、`DiagnosticsPackageTests`、`OpenAiOfficialSdkImageGenerationProviderTests` 与 `OpenAiLiveV1SampleRouteTests` 中。 | 只有当 provider 行为发生实质变化，或需要更新的 live snapshot 时，才需要刷新这组证据。 |
| 文章或纯文本规划在默认情况下不依赖真实 provider，也能生成并提升已批准的插图目标。 | 已由 automated repo evidence 验证 | `SupportingValidationRouteVerificationTests.SupportingValidationRoute_CompletesFakeFirstDocumentPlanningThroughDelivery` 证明了文章/纯文本规划、已批准目标提升、fake-first 生成、review、approval 和 delivery export 的整条路径。`DocumentIllustrationWorkflowTests` 继续覆盖更窄的规划与 oversize-guard 边界。 | 以后仍值得补一条面向用户的脚本，但该指标已经具备自动化证明。 |
| 教学海报证明路径能够导出确定性文本合成 provenance 和人工 approval evidence。 | 已由 automated repo evidence 验证 | `EducationalPosterLaunchProofTests.EducationalPosterProofPath_ExportsCompositionProvenanceAndApprovalEvidence` 证明了确定性合成、composition-report provenance 复制和 final approval evidence 在一次 delivery export 中全部成立。支撑组件覆盖仍在 `SkiaDeterministicTextComposerTests`、`DeterministicTextCompositionToolAdapterTests` 和 `DeliveryPackageTests` 中。 | 未来真实样本导出仍然有价值，但该指标已经具备自动化证明。 |
| 第一条真实低风险 operator 动作能端到端跑通，并写出审计输出与 rollback 或 cleanup 说明。 | 已由 automated repo evidence 验证 | `ArtifactValidationToolAdapterTests.LowRiskAutoRepairService_RunsArtifactValidationAdapterAndWritesDiagnosticsReport` 证明了本地验证输出能写入 diagnostics 目录，并带有 cleanup guidance。`LowRiskAutoRepairServiceTests` 证明了“仅低风险”执行边界。 | 未来可以补一个用户可见的 launch bundle，但该指标已经具备自动化证明。 |

## 当前结论

- `5 / 5` 个发布指标现在都已经具备足以计为当前已验证的自动化仓库证据或记录在册的真实 provider 证据。
- 对于 `2026-06-23` 这次快照，live OpenAI 发布缺口仍然处于关闭状态。
- 本快照使用的最新自动化门禁已经通过规范的仓库验证路径，并保持 `433 / 433` 测试通过。
- 本快照中的最新真实 OpenAI 证据仍来自 `artifacts/live-openai-v1-sample/20260611-132947`；这次读数刷新不需要新增付费 provider 重跑。
- 仓库现在同时具备只读 preflight 路径，以及一组与同一发布声明对应的已记录真实 provider 证据。

## Post-V1 科研工作流验收

本节是独立的 post-V1 台账，不改变历史 `2026-06-23` V1 快照，也不扩大其 `5 / 5` 发布指标。

| 证据层 | 状态 | 权威依据 |
| --- | --- | --- |
| Implemented | 完成至 Task 30 与 Checkpoint 5 | 科研提取、权威状态持久化、确定性 SVG/PNG/PDF、三层审查、受控修复、两个人类门禁、五个 WPF 工作区、交付打包和文档收口均已实现，并由 `664 / 664` 仓库测试覆盖。 |
| Fake-first verified | 已验收 | `artifacts/scientific-figure-corpus-acceptance/report.json` 记录 12 项人类批准 baseline 全部通过，40 项声明的阻断 mutation 全部被拒绝。 |
| Live verified | 已验收 | `artifacts/scientific-figure-live-acceptance/20260727-150622/report.json` 记录一项机制图、一项概念比较图和一项图形摘要完成 OpenAI 理解、语义审查、视觉审查、确定性 contract review 和导出。 |
| Human accepted | 已验收 | 审查人 `sciman` 于 `2026-07-27T23:57:09.9758439+08:00` 批准全部三项 Gate 2，纠正记录为空；提交到仓库的摘要见 [20260725-scientific-figure-live-acceptance.md](../change-evidence/20260725-scientific-figure-live-acceptance.md)。 |
| 最终文档收口 | 已验收 | Task 30 已同步用户指南、路线图、迁移、回滚、排除边界和中英文 companion。固定顺序收口通过无警告/错误 build、`664 / 664` tests、reference evidence、format 和 release preflight。 |

已验收边界不包括 OCR-heavy 来源、实测或伪造数据图、显微镜式观测、自动改变科学含义，或把生成图像表述为真实观测证据。provider 凭据、本地 SQLite 数据、workspace 与已批准 live artifact 均保持在 Git 外。

## 建议的下一批证据切片

1. 如果后续 provider 行为发生变化，重新执行 opt-in OpenAI `2-item` 样本路径，并刷新 live evidence 资产集。
2. 若后续需要，可增加一条面向用户的脚本，镜像当前自动化“三次 fake-first 发布套件”。
3. 若后续需要，可增加一份面向用户的样本导出包，镜像自动化的教学海报证明路径。
4. 只有当 provider 行为或科研契约发生实质变化时，才刷新已批准的科研 live run；不得只为重复不变的批准记录而消耗付费调用。

## 本快照使用的证据来源

- `tests/ContentDeliveryStudio.Tests/FakeWorkflowTests.cs`
- `tests/ContentDeliveryStudio.Tests/PrimaryLaunchRouteVerificationTests.cs`
- `tests/ContentDeliveryStudio.Tests/BriefWorkflowApplicationServiceTests.cs`
- `tests/ContentDeliveryStudio.Tests/SupportingValidationRouteVerificationTests.cs`
- `tests/ContentDeliveryStudio.Tests/DocumentIllustrationWorkflowTests.cs`
- `tests/ContentDeliveryStudio.Tests/OpenAiProviderContractTests.cs`
- `tests/ContentDeliveryStudio.Tests/OpenAiProviderConfigurationTests.cs`
- `tests/ContentDeliveryStudio.Tests/OpenAiLiveV1SampleRouteTests.cs`
- `tests/ContentDeliveryStudio.Tests/OpenAiProviderSmokeTests.cs`
- `tests/ContentDeliveryStudio.Tests/OpenAiLaunchPreflightTests.cs`
- `tests/ContentDeliveryStudio.Tests/OpenAiLaunchPreflightReportWriterTests.cs`
- `tests/ContentDeliveryStudio.Tests/OpenAiLaunchPreflightToolAdapterTests.cs`
- `tests/ContentDeliveryStudio.Tests/ToolAdapterServiceCollectionExtensionsTests.cs`
- `tests/ContentDeliveryStudio.Tests/DiagnosticsPackageTests.cs`
- `tests/ContentDeliveryStudio.Tests/SkiaDeterministicTextComposerTests.cs`
- `tests/ContentDeliveryStudio.Tests/DeterministicTextCompositionToolAdapterTests.cs`
- `tests/ContentDeliveryStudio.Tests/EducationalPosterLaunchProofTests.cs`
- `tests/ContentDeliveryStudio.Tests/DeliveryPackageTests.cs`
- `tests/ContentDeliveryStudio.Tests/ProjectApplicationServiceTests.cs`
- `tests/ContentDeliveryStudio.Tests/ArtifactValidationToolAdapterTests.cs`
- `tests/ContentDeliveryStudio.Tests/LowRiskAutoRepairServiceTests.cs`
- `scripts/verify-repo.ps1`
- `scripts/sync-reference-governance.ps1`
- `scripts/verify-reference-evidence.ps1`
