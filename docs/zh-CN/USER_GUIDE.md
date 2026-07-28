# 用户指南

> 英文原文: [../USER_GUIDE.md](../USER_GUIDE.md)
> 中文文档中心: [./README.md](./README.md)
> 说明: 本文件是中文说明版。若与英文原文、代码、测试或证据文件冲突，以仓库事实和英文正式文档为准。

AI Content Delivery Studio 是一个 Windows 桌面工作台，用于规划、生成、评审、修复并交付内容包。当前核心工作流是图像序列生产。当前实现默认使用 fake provider，因此无需付费 API 调用也能验证端到端流程；而最新记录在册的 V1 发布快照把真实 OpenAI 路径保留为证据，而不是默认运行模式。

## 语言

通过标题栏中的语言选择器切换：

- `System`
- `中文`
- `English`

领域标识符、provider ID、model ID 和错误字符串仍保持英文原文；用户可见标签和工作流文本会做本地化。

## 基本工作流

1. 创建或选择一个项目。
2. 在 Brief 区域从一条短需求开始，或使用 document illustration 入口处理纯文本源材料。
3. 比较 prompt direction 或 blueprint 候选，再把选中的路线提升到常规的 Plan 和 Prompts 工作流。
4. 在启用付费 provider 之前，先运行 fake planning、queue、generation 和 review 动作来走通整条流程。
5. 比较候选结果，应用 repair guidance，并在评审指出问题时重新生成。
6. 只有在人工最终审批之后，才导出交付包。

## 当前最强支持路径

- 需求优先的图像序列: 当前最强的端到端路径，也是最新 V1 快照中最核心的主发布主干。
- 纯文本或文章插图规划: fake-first 路径，可将已批准的目标提升到现有图像序列工作流；在当前 V1 范围内已有自动化路线证明。
- 文本密集型教学或海报输出: 当前 V1 范围内已有自动化证明的路径。当标签、公式或标注必须清晰可读时，应把生成图像视为背景底板，再使用确定性后合成与单独的可读性评审。
- 可信科研绘图: 已验收的 post-V1 路径，面向含文本的物理与自然科学来源，覆盖证据约束主张、确定性 SVG/PNG/PDF、三层评审、两个人类门禁和可追溯交付。

## 文档插图

首个 document illustration 发布切片以 fake provider 为先。默认路径使用 fake provider，这样你可以在没有付费 API 调用和真实 provider 凭据的前提下验证整个流程。当前 OpenAI text-planning provider 在显式启用真实 provider 路径时，也已支持同一套 document-illustration 契约。

当你希望把粘贴文本或受控范围内的本地 `pdf` / `docx` 源文件先转成插图方向，再把批准后的目标提升到现有 Plan 和 Prompts 工作流时，应使用该入口。

- 输入支持粘贴草稿文本、纯文本内容，或导入受控范围内的本地 `pdf` / `docx` 文件。
- 当前受支持的 document-illustration 路径面向概念插图和 graphical abstract 草稿。
- 已批准的目标会被提升到现有 plan 结构，而不是创建一条完全独立的下游流水线。
- 源文本应被视为规划证据，而不是跳过评审或 provenance 追踪的许可证。
- 对于规划路径，真实 provider 执行现在已经具备契约级准备；本地 `pdf` / `docx` 文本提取也覆盖了当前受控 support-matrix 切片，但 OCR 较重和高保真二进制提取仍属后续工作。

推荐流程：

1. 将源段落、摘要、大纲或其他纯文本内容粘贴到 source text 框，或导入本地 `pdf` / `docx` 文件。
2. 选择与插图意图匹配的 draft mode。
3. 评估 fake 路径生成的 illustration target 和 prompt direction。
4. 只批准值得继续推进的目标。
5. 将已批准目标提升到常规的 Plan 和 Prompts 工作流，供后续编辑、生成与评审。

### 学术草稿模式

Scholarly draft mode 有更严格的安全边界。

- 适合: 示意概念、graphical abstract、解释性图示、背景底板。
- 不适合: 伪造数据图、实验结果图、显微镜式证据图，或任何可能被误认为真实观测证据的图像。
- 该工作流不得虚构证据图、模拟未发表结果，也不得暗示 AI 生成视觉结果是真实科学观测。

在通用 document-illustration 路径中，如果目标需要承载科学证据结构，应在规划层止步。对于含文本的物理和自然科学来源，应改用下述可信科研绘图路径。实测曲线、OCR-heavy 来源、显微镜式证据，以及任何被表述为真实实验观测的生成结果，仍不在已验收边界内。

## 可信科研绘图

post-V1 科研绘图工作流已经在受控范围内通过验收。状态必须分层理解：

- `Implemented`: 已具备提取、主张/证据理解、权威状态持久化、确定性 SVG/PNG/PDF、contract/semantic/visual review、受控修复、两个人类门禁、五个 WPF 工作区和交付打包。
- `Fake-first verified`: 12 项人类批准 corpus baseline 已全部重放，40 项声明的阻断 mutation 全部 fail closed。
- `Live verified`: run `20260727-150622` 已让一项机制图、一项概念比较图和一项图形摘要完成 OpenAI 理解、独立语义审查和全分辨率视觉审查。
- `Human accepted`: 审查人 `sciman` 于 `2026-07-27T23:57:09.9758439+08:00` 批准三张最终 live PNG，纠正记录为空。

按 Source、Understanding、Figure Spec、Render & Review、Delivery 的顺序使用五个科研工作区。Gate 1 在渲染前冻结已批准的主张、元素、关系、限制条件和 specification version。只有确定性 contract review 与两项机器审查都通过后，Gate 2 才可用。人工拒绝会路由回受控修复；自动修复不能改变科学含义，也不能静默替换 Gate 1 权威。

默认 provider 模式仍为 fake。刷新 live 证据必须先获得明确付费调用批准，再运行：

```powershell
.\scripts\run-scientific-figure-live-acceptance.ps1
```

不要为了重新读取已批准证据而重复付费运行。已批准的本地报告位于 `artifacts/scientific-figure-live-acceptance/20260727-150622`；该目录与 provider secret 均保持在 Git 外。提交到仓库的摘要见 [20260725-scientific-figure-live-acceptance.md](../change-evidence/20260725-scientific-figure-live-acceptance.md)。

科研交付包保留 SVG、PNG、PDF、已批准 specification、provenance map、review/repair 记录、provider metadata 和两个人类批准。每个包都应视为不可变快照。

如需在不调用付费 provider 的前提下进行可重复的本地操作员试运行，请使用
[可信科研绘图 Fake-First 操作员试运行](./SCIENTIFIC_FIGURE_OPERATOR_TRIAL.md)。
仅准备好的 harness 仍是 `pending_operator`；自动化检查本身不能构成结果。
真人或经用户明确授权的 agent 在检查五个可见工作区并通过交付包门禁后，
都可以 finalize 为等效的 `operator/manual evidence`；agent 身份和授权依据
必须显式记录。

迁移与回滚边界：

- 现有图像序列项目无需手工迁移；科研工作流持久化是增量式的，旧 workspace 仍可读取。
- 更改 provider mode 或回退代码前，应保留本地 SQLite/workspace 数据和已批准 artifact 目录；Git 回滚不能恢复本地运行数据。
- 停止 live 执行时应返回 fake provider mode。回退科研实现提交只会移除代码行为，不得被表述为删除或撤销已经记录的人类证据。

## 安全默认值

- fake provider 是开发和测试的默认路径。
- 真实 OpenAI 调用需要显式 opt-in 配置和用户批准。
- API key 不存储在仓库文件中。
- 本地 SQLite 数据库、workspace、生成输出、诊断和备份产物必须保持在 git 之外。
- Diagnostics export 只记录 secret 是否存在，不记录 secret 值本身。
- 安全备份默认排除 `.env`、本地 appsettings override、SQLite 数据库、`workspace/` 和 `outputs/`。

## OpenAI 发布预检

最近一次记录在册的真实 OpenAI V1 样本已经保存在 `artifacts/live-openai-v1-sample/20260611-132947`。在尝试刷新样本、重新验证 provider 行为，或生成新的发布证据快照之前，应先使用内置的只读 OpenAI launch preflight 路径。

它会检查：

- text planning 就绪性
- vision review 就绪性
- image generation 就绪性
- opt-in smoke-test 门禁
- 阻止真实 `2-item` 样本运行的原因

重要行为：

- 它会读取 provider 配置和 secret 就绪状态，但不会持久化 secret 值。
- 如果真实 provider smoke 路径没有被显式 opt-in，preflight 会保持 dry-run，并记录阻断原因，而不是尝试付费调用。
- 默认的 opt-in 环境变量是 `IMAGE_SERIES_STUDIO_OPENAI_REAL_API_SMOKE`，启用值为 `1`。

预期输出：

- `diagnostics/openai-launch-preflight.json`
- `diagnostics/openai-launch-preflight.md`

在把新的真实 provider 记录写入 [V1_LAUNCH_EVIDENCE.md](../V1_LAUNCH_EVIDENCE.md) 之前，应把 preflight 结果当作 readiness evidence。

## 诊断导出

Diagnostics export 是主要的本地支持包导出路径。

可包含内容：

- 应用和机器快照
- 项目与 provider 概要
- 不包含 secret 值的 secret presence flag
- routed repair-patch summary
- operator run audit summary
- 若已运行 OpenAI preflight，则可附带 OpenAI launch-preflight readiness snapshot

将生成的包分享给本机之外的对象前，应先人工审阅。

## 样本迁移

physics poster importer 可以读取 `D:\CODE\physicist_chinese_poster_batch_tool` 中选定的 prompt metadata 和 finalized delivery manifest。它只是样本迁移来源，不得修改那个仓库，不得默认复制大型生成二进制，也不得把 physics 特定词汇直接提升为通用产品概念。

## 交付包

Delivery export 会写出：

- 最终批准的图像
- prompt 快照
- 如存在则附带 metadata sidecar
- `manifest.json`
- `manifest.csv`
- `review-report.md`

交付包是不可变快照。内容有变化时，应重新构建一个新包，而不是原地改写旧包。

## 故障排查

在报告构建或工作流问题之前，先运行仓库规范门禁：

```powershell
.\scripts\verify-repo.ps1
```

如果需要更强的发布式校验，请使用：

```powershell
.\scripts\preflight-release.ps1
```

如果问题涉及真实 provider readiness，先运行 OpenAI launch preflight，并检查生成的 `json` 或 `md` 报告，再决定是否尝试刷新 live sample。
