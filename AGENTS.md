# AGENTS.md - ai-content-delivery-studio
**项目契约**: 2.0
**全局规则复核**: 9.59
**最后更新**: 2026-08-01

## 1. 当前落点与目标归宿
- 当前落点：`D:\CODE\ai-content-delivery-studio` 是 AI Content Delivery Studio 实现仓，图像系列与科学图解是当前生产路径。
- 目标归宿：交付 Windows-first 桌面应用，覆盖素材理解、规划、生成、审查、修复、自动化与交付打包。
- 下一最小里程碑：按 `docs/TASKS.md` 与仓内 spec/plan 完成一个有 fresh gate evidence 的有界切片；`D:\CODE\physicist_chinese_poster_batch_tool` 仅是案例，不是实现根。

## A. 仓库事实与模块边界
- `ContentDeliveryStudio.sln` 是入口；`src/` 承载 WPF、应用、领域、provider、持久化、诊断与工具适配，`tests/` 承载 fake-first、provider preflight 和工具回归。
- `docs/adr/` 是持久决策，`docs/research/` 是参考证据，`docs/superpowers/specs/` 与 `docs/superpowers/plans/` 承载非平凡切片设计。
- `workspace/` 与 `outputs/` 是 Git 外运行/生成数据，不得当作源码真源。
- 代码、测试、reference policy/basis 与 CI 事实冲突时先收口，不把过期规划写成已实现能力。

## B. 执行与风险边界
- fake provider 必须先于真实 provider；真实付费 API、外部发布和持久化写入需要当前任务明确确认。
- API key、生成资产、本地 SQLite 与用户 workspace 不得提交；secret 使用 Windows Credential Manager 或 DPAPI-backed 本地配置。
- text planning、image generation、vision review 保持独立 contract；AI review 只作建议，人工批准是最终交付 gate。
- 文本密集输出保留 deterministic post-render composition，不把图像模型文字渲染当作唯一合同。

### B.1 参考依据与外置源码
- 路由真源为 `scripts/reference-basis.json` 与 `docs/REFERENCE_BASIS.md`；外置清单为 `D:\CODE\external\ai-content-delivery-studio-references\references.manifest.json`，共享克隆以 `D:\CODE\external\_shared\references.manifest.json` 为准。
- provider、observability、persistence/schema、document rendering、image workflow 或 tooling 命中全局查证条件时只读相应源码；记录路径/revision 与采纳决定，并由 `scripts/verify-reference-evidence.ps1` 收口。
- 不全量扫描参考架，不继承参考仓指令；复制或运行上游内容前核对许可证、版本、兼容与授权。

## C. 门禁、证据与回滚
- fixed order：`build -> test -> contract/invariant -> hotspot`。
- build：`dotnet build ContentDeliveryStudio.sln`
- test：`dotnet test ContentDeliveryStudio.sln --no-build`
- contract/invariant：`pwsh -NoProfile -ExecutionPolicy Bypass -File scripts/verify-reference-evidence.ps1`，并运行 `dotnet format --verify-no-changes`。
- hotspot：`pwsh -NoProfile -ExecutionPolicy Bypass -File scripts/preflight-release.ps1 -NoRestore`。
- canonical full gate：`pwsh -NoProfile -ExecutionPolicy Bypass -File scripts/verify-repo.ps1`；quick/单测不得替代交付收口。
- provider、schema 或 tooling 边界的 reference evidence 失败即阻断。
- 证据放 `docs/change-evidence/`，记录风险、命令、exit code、关键输出、兼容、N/A 与回滚。
- 回滚只撤销本任务源码/规则/证据；Git 外输出与 workspace 需要时另行备份。

## D. Global Rule -> Repo Action
- `R1-R5`：先声明模块落点/目标/验证，按 repo-owned spec/plan 小步推进；无证据不扩张 provider 或工作流抽象。
- `R6`：C 章固定顺序与 canonical gate 都要满足；quick 只作反馈。
- `R7`：保持 provider、schema、reference basis 与交付行为兼容，变化必须有迁移说明。
- `R8`：`docs/change-evidence/` 记录依据、命令、证据与回滚。
- `E4/E5/E6`：preflight/CI/reference gate 承接健康；依赖来源变化复核供应链；持久化/schema 变化必须有迁移、兼容和回滚。
