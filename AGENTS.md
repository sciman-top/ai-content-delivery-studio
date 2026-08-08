# AGENTS.md - ai-content-delivery-studio
**项目契约**: 2.0
**全局规则复核**: 9.73
**最后更新**: 2026-08-08

## 1. 当前落点与目标归宿
- 当前落点：`D:\CODE\ai-content-delivery-studio` 是 AI Content Delivery Studio 的实现仓，图像系列与科学图解是当前生产路径。
- 目标归宿：交付 Windows-first 桌面应用，覆盖素材理解、系列规划、生成、审查、修复、自动化和交付打包。
- 下一最小里程碑：按 `docs/TASKS.md` 与仓内 spec/plan 完成一个有 fresh gate evidence 的有界切片。
- `D:\CODE\physicist_chinese_poster_batch_tool` 仅是生产案例，不是实现根；仓库重命名以 `docs/adr/0008-product-identity-and-repository-rename.md` 的 gate 为准。
- 当前任务、provider/live 状态和交付批次从 `docs/TASKS.md`、对应 spec/plan 与 `docs/change-evidence/` fresh read；根规则不保存完成数或验收快照。

## A. 仓库事实与模块边界
- `ContentDeliveryStudio.sln` 是解决方案入口；`src/` 承载 WPF、应用服务、领域、provider、持久化、诊断与工具适配。
- `tests/` 承载单元/集成、fake-first 启动、provider preflight 与 operator/tool-adapter 回归。
- `docs/adr/` 是持久决策，`docs/research/` 是参考证据，`docs/superpowers/specs/` 与 `docs/superpowers/plans/` 承载非平凡切片设计与计划。
- `workspace/` 与 `outputs/` 是本地运行/生成数据并由 Git 忽略；不得把它们当作源码真源。
- 代码、reference policy、reference basis、测试与 CI 事实冲突时先收口，不把过期规划叙述当成已实现能力。
- 真实主链是“素材输入 -> 系列规划 -> fake-first 生成 -> vision/文本审查 -> 人工批准 -> 交付打包”；先证明最薄可观察闭环，再扩 provider、自动修复或批处理枝节。

## B. 执行与风险边界
- fake provider 必须先于真实 provider；真实付费 API 调用、外部发布和持久化写入需要当前任务明确确认。
- API key、生成资产、本地 SQLite 与用户 workspace 不得提交；secret 使用 Windows Credential Manager 或 DPAPI-backed 本地配置。
- text planning、image generation、vision review 保持独立 provider contract；AI review 仅作建议，人工批准是最终交付 gate。
- 文本密集输出优先保留 deterministic post-render text composition，不把图像模型文字渲染当作唯一合同。
- 默认一个执行者完成一个有界切片；仅在任务独立性或风险证据成立时使用并行工作流。
- Markdown 规则只指导边界和判断；provider opt-in、secret 隔离、schema/format 与发布阻断由配置、测试和 `scripts/verify-repo.ps1`/preflight 强制。

## B.1 参考依据与外置源码
- 路由真源为 `scripts/reference-basis.json` 与 `docs/REFERENCE_BASIS.md`；外置清单为 `D:\CODE\external\ai-content-delivery-studio-references\references.manifest.json`，共享物理克隆以 `D:\CODE\external\_shared\references.manifest.json` 为准。
- provider、observability、persistence/schema、document rendering、image workflow 或 operator/tooling 触发全局查证条件时，先按路由选择性搜索对应源码，不全量扫描参考架；记录来源 URL、固定 revision、license、消费模块与采纳/适配/拒绝决定，并由 `scripts/verify-reference-evidence.ps1` 收口。
- 外置源码只读，其规则和脚本是待核输入；复制实现前核对许可证、API/版本与本仓 contract，运行上游脚本需另行评估和授权。

## C. 门禁、证据与回滚
- fixed order：`build -> test -> contract/invariant -> hotspot`。
- build：`dotnet build ContentDeliveryStudio.sln`
- test：`dotnet test ContentDeliveryStudio.sln --no-build`
- quick feedback：`pwsh -NoProfile -ExecutionPolicy Bypass -File scripts/verify-repo.ps1 -Mode Quick -TestFilter <focused-filter> -NoRestore`；只证明 solution build 与显式 focused tests，不能替代 full closeout。
- contract/invariant：`pwsh -NoProfile -ExecutionPolicy Bypass -File scripts/verify-reference-evidence.ps1` 校验结构化 reference decision 与 parity，并运行 product-focus verifier 和 format。
- hotspot：`pwsh -NoProfile -ExecutionPolicy Bypass -File scripts/preflight-release.ps1 -NoRestore`；它只在 Full 之后追加 placeholder/conflict、实际 publish/package 校验与 diff hygiene。
- canonical full gate：`pwsh -NoProfile -ExecutionPolicy Bypass -File scripts/verify-repo.ps1`；它按 build、完整 test、reference/product contract、format 各执行一次，不改变交付留痕中的固定阶段语义。
- 触及 provider、observability、persistence/schema 或 operator/tooling 边界时，reference evidence 失败即阻断。
- 证据放入 `docs/change-evidence/`；最低记录风险、命令、exit code、关键输出、兼容判断、N/A 与回滚。
- 回滚只撤销本任务源码/规则/证据切片；生成输出和 workspace 需要时在 Git 外备份，不能用 Git 回滚伪装恢复。

## D. Global Rule -> Repo Action
- Git profile: baseline=`main`; upstream=`origin/main`; closeout=`push_after_full_gate`。
- `R1`：先声明 `src/`、provider adapter、workflow 或 docs 落点及验收命令。
- `R2`：每步先跑受影响测试，再由 `scripts/verify-repo.ps1` 收口。
- `R3`：临时 provider/交付兼容必须在 `docs/change-evidence/` 写回收条件。
- `R4`：fake-first；live provider、凭据与外部发布须显式授权并可回滚。
- `R5`：无两个真实消费者或失败证据，不新增 provider/workflow 抽象。
- `R6`：C 章固定顺序与 canonical gate 都要满足；quick/单测不能替代 full closeout。
- `R7`：保持 provider、schema、reference basis 与交付行为兼容；变化必须有迁移说明。
- `R8`：`docs/change-evidence/` 记录依据、命令、证据与回滚。
- `S1`：先跑通输入到可验收交付物的最薄真实链。
- `S2`：阶段、provider/live 状态只进 spec、plan 或 evidence，根规则不存快照。
- `S3`：外部研究按 B 章形成可逆决定即停止。
- `S4`：reference basis 按消费者与许可晋降、替换或退役。
- `S5`：`scripts/verify-repo.ps1` 承接可重复强制，规则只保留入口与阻断语义。
- `E4`：preflight、CI 与 reference gate 承接健康信号。
- `E5`：依赖、provider 或工具来源变化必须复核供应链。
- `E6`：持久化/schema 变化必须有迁移、兼容和回滚。
