# AGENTS.md - ai-content-delivery-studio
**项目契约**: 2.0
**全局规则复核**: 9.77
**最后更新**: 2026-08-20

## 1. 当前落点与目标归宿
- 当前落点：`D:\CODE\ai-content-delivery-studio` 是 AI Content Delivery Studio 的实现仓，图像系列与科学图解是当前生产路径。
- 目标归宿：交付 Windows-first 桌面应用，覆盖素材理解、系列规划、生成、审查、修复、自动化和交付打包。
- 下一最小里程碑：直接修复当前代码中的有证据问题；`docs/TASKS.md` 只记录不能由仓库自主完成的四个外部阻断。
- `D:\CODE\physicist_chinese_poster_batch_tool` 仅是生产案例，不是实现根；仓库重命名以 `docs/adr/0008-product-identity-and-repository-rename.md` 的 gate 为准。
- 当前外部阻断从 `docs/TASKS.md` fresh read；provider/live 状态和交付批次仅从对应历史验收记录读取，不把旧快照当作当前门禁。

## A. 仓库事实与模块边界
- `ContentDeliveryStudio.sln` 是解决方案入口；`src/` 承载 WPF、应用服务、领域、provider、持久化、诊断与工具适配。
- `tests/` 承载单元/集成、fake-first 启动、provider preflight 与 operator/tool-adapter 回归。
- `docs/adr/` 只承载持久架构决策，`docs/research/` 只承载仍有消费者的参考证据；普通切片不新建 spec、plan、evidence receipt 或 machine queue。
- `workspace/` 与 `outputs/` 是本地运行/生成数据并由 Git 忽略；不得把它们当作源码真源。
- 代码、reference policy、reference basis、测试与 CI 事实冲突时先收口，不把过期规划叙述当成已实现能力。
- 真实主链是“素材输入 -> 系列规划 -> fake-first 生成 -> vision/文本审查 -> 人工批准 -> 交付打包”；先证明最薄可观察闭环，再扩 provider、自动修复或批处理枝节。

## B. 执行与风险边界
- fake provider 必须先于真实 provider；真实付费 API 调用、外部发布和持久化写入需要当前任务明确确认。
- API key、生成资产、本地 SQLite 与用户 workspace 不得提交；secret 使用 Windows Credential Manager 或 DPAPI-backed 本地配置。
- text planning、image generation、vision review 保持独立 provider contract；provider/自动化结果不能单独批准交付，最终 gate 由真人或具有逐图哈希回执和当前明确授权的 `authorized_agent` 决定。
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
- focused closeout：未触及 provider、observability、persistence/schema、document/image rendering、publish/package/release 的规则、文档、测试、verifier、script/config，运行 `git diff --check` 与受影响 verifier/test；需要 solution feedback 时才运行 `pwsh -NoProfile -ExecutionPolicy Bypass -File scripts/verify-repo.ps1 -Mode Quick -TestFilter <focused-filter> -NoRestore`，不机械叠加。
- contract/invariant：只有当前切片确需外部源码裁决时运行 `pwsh -NoProfile -ExecutionPolicy Bypass -File scripts/verify-reference-evidence.ps1 -RequireDecision`；Full 默认只做 parity 与映射提示，不强迫 evidence receipt。
- hotspot：publish/package/release 切片运行一次 `pwsh -NoProfile -ExecutionPolicy Bypass -File scripts/preflight-release.ps1 -NoRestore`；它只调用一次 Full，再追加 release-only tests、changed-C# format、scan 与 publish/package。
- full closeout：触及运行/交付风险，或 focused 发现跨面风险时运行一次 `pwsh -NoProfile -ExecutionPolicy Bypass -File scripts/verify-repo.ps1 -Mode Full`；它只执行一次 build、非 ReleaseOnly tests、reference contract 与 diff hygiene。
- 触及 provider、observability、persistence/schema 或 operator/tooling 边界时，reference evidence 失败即阻断。
- `docs/change-evidence/` 只用于无法由代码和 Git 历史重建的 live、人审、硬件、迁移、waiver 或 release acceptance；普通修复不写证据文件。
- 回滚只撤销本任务源码/规则/证据切片；生成输出和 workspace 需要时在 Git 外备份，不能用 Git 回滚伪装恢复。

## D. Git 与回滚
- Git baseline=`main`; upstream=`origin/main`; closeout=`proportional_focused_or_full`。
- 回滚只撤销本任务源码、规则或证据切片；生成输出与 workspace 需要时在 Git 外备份。
