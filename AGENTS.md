# AGENTS.md - ai-content-delivery-studio
**项目契约**: 2.0
**全局规则复核**: 9.55
**最后更新**: 2026-07-10

## 1. 当前落点与目标归宿
- 当前落点：`D:\CODE\ai-content-delivery-studio` 是 AI Content Delivery Studio 的实现仓，图像系列生成是当前核心生产路径。
- 目标归宿：交付 Windows-first 桌面应用，覆盖素材理解、系列规划、生成、审查、修复、自动化和交付打包。
- 下一最小里程碑：按 `docs/TASKS.md` 与仓内 spec/plan 完成一个有 fresh gate evidence 的有界切片。
- `D:\CODE\physicist_chinese_poster_batch_tool` 仅是生产案例，不是实现根；仓库重命名以 `docs/adr/0008-product-identity-and-repository-rename.md` 的 gate 为准。

## A. 仓库事实与模块边界
- `ContentDeliveryStudio.sln` 是解决方案入口；`src/` 承载 WPF、应用服务、领域、provider、持久化、诊断与工具适配。
- `tests/` 承载单元/集成、fake-first 启动、provider preflight 与 operator/tool-adapter 回归。
- `docs/adr/` 是持久决策，`docs/research/` 是参考证据，`docs/superpowers/specs/` 与 `docs/superpowers/plans/` 承载非平凡切片设计与计划。
- `workspace/` 与 `outputs/` 是本地运行/生成数据并由 Git 忽略；不得把它们当作源码真源。
- 代码、reference policy、reference basis、测试与 CI 事实冲突时先收口，不把过期规划叙述当成已实现能力。

## B. 执行与风险边界
- fake provider 必须先于真实 provider；真实付费 API 调用、外部发布和持久化写入需要当前任务明确确认。
- API key、生成资产、本地 SQLite 与用户 workspace 不得提交；secret 使用 Windows Credential Manager 或 DPAPI-backed 本地配置。
- text planning、image generation、vision review 保持独立 provider contract；AI review 仅作建议，人工批准是最终交付 gate。
- 文本密集输出优先保留 deterministic post-render text composition，不把图像模型文字渲染当作唯一合同。
- 默认一个执行者完成一个有界切片；仅在任务独立性或风险证据成立时使用并行工作流。

## C. 门禁、证据与回滚
- fixed order：`build -> test -> contract/invariant -> hotspot`。
- agent-rule contract CI：`.github/workflows/agent-rule-contract.yml` 只验证规则契约，不替代本仓产品门禁。
- build：`dotnet build ContentDeliveryStudio.sln`
- test：`dotnet test ContentDeliveryStudio.sln --no-build`
- contract/invariant：`pwsh -NoProfile -ExecutionPolicy Bypass -File scripts/verify-reference-evidence.ps1`，并运行 `dotnet format --verify-no-changes`。
- hotspot：`pwsh -NoProfile -ExecutionPolicy Bypass -File scripts/preflight-release.ps1 -NoRestore`；它会追加 placeholder/conflict、publish WhatIf 与 diff hygiene。
- canonical full gate：`pwsh -NoProfile -ExecutionPolicy Bypass -File scripts/verify-repo.ps1`；它聚合 reference evidence、build、test 与 format，不改变交付留痕中的固定阶段语义。
- 触及 provider、observability、persistence/schema 或 operator/tooling 边界时，reference evidence 失败即阻断。
- 证据放入 `docs/change-evidence/`；最低记录风险、命令、exit code、关键输出、兼容判断、N/A 与回滚。
- 回滚只撤销本任务源码/规则/证据切片；生成输出和 workspace 需要时在 Git 外备份，不能用 Git 回滚伪装恢复。

## D. Global Rule -> Repo Action
- `R1-R5`：先声明模块落点/目标/验证，按 repo-owned spec/plan 小步推进；无证据不扩张 provider 或工作流抽象。
- `R6`：C 章固定顺序与 canonical gate 都要满足；quick/单测不能替代 full closeout。
- `R7`：保持 provider、schema、reference basis 与交付行为兼容；变化必须有迁移说明。
- `R8`：`docs/change-evidence/` 记录依据、命令、证据与回滚。
- `E4`：preflight、CI 与 reference gate 承接健康信号；`E5`：依赖/provider/tool 来源变化必须复核供应链；`E6`：持久化/schema 变化必须有迁移、兼容和回滚。
