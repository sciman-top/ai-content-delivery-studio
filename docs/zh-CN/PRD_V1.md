# PRD V1

> 英文原文: [../PRD_V1.md](../PRD_V1.md)
> 中文文档中心: [./README.md](./README.md)
> 说明: 本文件是中文说明版。若与英文原文、代码、测试、ADR 或证据文件冲突，以仓库事实和英文正式文档为准。

## 标题

AI Content Delivery Studio V1 是一个以 Windows 为先、在本地运行的工作台，用于把一条短需求转成一个经过评审、可复现的图像序列交付包。

## 文档角色

这份 PRD 定义的是 V1 的发布承诺、发布边界和发布门槛。

它不声明当前仓库已经满足这些发布指标。当前证明应看 [../V1_LAUNCH_EVIDENCE.md](../V1_LAUNCH_EVIDENCE.md)。更广的文档角色映射见 [../DOCUMENTATION_GOVERNANCE.md](../DOCUMENTATION_GOVERNANCE.md)。

## 发布论点

长期产品愿景是一个多模态内容交付工作台，但 V1 不会一次性发布全部愿景。

V1 的承诺更窄：

- 帮助重度用户从一条短需求出发，而不是从零散 prompt 开始。
- 在付费生成之前，先产出并比较若干 blueprint 或 direction 方案。
- 在交付导出中保留 prompt、provider、review 和 approval 的证据。
- 通过确定性文本合成，证明一条文本密集型教学输出路径。

V1 的核心产品价值不是“再多几个图像生成旋钮”，而是一个可追溯的本地工作台，用来规划、生产、评审、修复并交付一致的视觉结果。

## 主要问题

当前产品方向是成立的，但这些问题仍需要被主动控制：

- 长期多模态愿景大于 V1 可以安全发布的范围。
- 如果 `Images API` 与 `Responses API` 的职责不由政策强约束，provider 路由就会漂移。
- 文本密集型输出如果没有确定性渲染通道，很难具备可信度。
- Operator 自动化建模的速度，已经快于真实验证速度。
- 如果没有治理，共享 reference shelf 很容易变成维护噪音。

这些问题不会推翻 V1 策略，但它们解释了为什么发布边界必须保持收窄。

## 发布决策

AI 推荐: 发布一条主路径、一条支撑验证路径和一条证明路径。

- 主发布路径: 短需求 -> brief -> blueprint -> series plan -> prompts -> generation -> review -> delivery
- 支撑验证路径: 文章或纯文本 -> evidence anchors -> illustration targets -> promoted plan -> 同一条 review 与 delivery 流
- 证明路径: 文本密集型教学海报 -> 生成背景底板 -> 确定性文本、公式与标签合成 -> 审批证据导出

这样的顺序能让发布始终围绕一条可复用的产品闭环，同时验证架构确实能支撑“文档导出的规划”和“文本密集型交付”。

## 已锁定的 V1 决策

除非后续 PRD 或 ADR 明确重开，下列决策都应视为 V1 默认值：

1. V1 首要目标用户: 独立创作者或类似教师的重度用户，而不是广义文档自动化用户。
2. V1 主发布工作流: 短需求 -> 图像序列，是唯一的主发布工作流。
3. 第一条真实 operator 切片: 叠加式的本地验证或诊断生成，而不是浏览器或桌面副作用自动化。
4. 确定性合成的实现选择: `SkiaSharp`。
5. V1 中 packs 的姿态: 仅限内部可复用配置，不做公开模板分享或 marketplace 能力。

这些默认值的目的是降低实现和硬化阶段的歧义。

## 目标用户

V1 的主要用户：

- 希望使用受控图像序列工作流，而不是零散 prompt 文件的独立创作者。
- 关注事实贴合、文本可读性和交付可追溯性的教师或课件作者。
- 希望掌握本地文件、显式 provider 设置和可审计导出的重度用户或开发者。

对 V1 很重要但不是首要对象的用户：

- 广义文档处理用户。
- 需要对第三方系统做浏览器或桌面自动化的 operator。
- 需要协作、派单或云同步的团队。

## 用户问题

V1 试图解决这些问题：

1. 用户往往从模糊需求开始，而不是从稳定 prompt 开始。
2. 纯 prompt 工作流太容易在错误视觉路线上的花费失控。
3. 文本密集型输出常常失败，因为图像模型的文字渲染不可靠。
4. 在重复生成过程中，review 和 approval 决策很容易丢失。
5. 最终输出目录通常缺少足够的 provenance，无法事后自证或复用。

## 产品承诺

如果 V1 成立，用户应该能够说：

- “我可以从需求开始，而不是从 prompt 工程琐事开始。”
- “我可以在花钱之前先比较几条视觉路线。”
- “我能够追踪最终交付物是如何生成、评审和批准的。”
- “我可以做文本密集型教学输出，而不必信任模型精确渲染最终文本。”

## 纳入范围

V1 必须支持：

- 基于 WPF 的本地 Windows 工作台。
- 主发布路径的 fake-first 端到端流程。
- 从需求出发的 brief 捕获。
- 在付费生成前先给出 2 到 4 条 blueprint 或 prompt-direction 候选。
- 从 blueprint 提升到可编辑 series plan。
- prompt 生成、prompt 编辑与 prompt 历史。
- 以 fake provider 为先、以 opt-in OpenAI 为后的队列式生成。
- 带 prompt 和 metadata 上下文的 candidate gallery。
- 结构化 AI 辅助 review，并保留人工最终审批。
- 带 manifest 与 approval evidence 的交付导出。
- 文章或纯文本插图规划，并能把批准的目标提升进同一条下游工作流。
- 面向文本密集型教学海报路径的确定性后合成，用于标签、公式、图例和 callout。
- 一条真实的低风险 operator 动作，端到端执行并保留审计证据。

## 明确不纳入范围

V1 不需要随发布一起提供：

- 面向 `pdf`、`docx`、`pptx`、`xlsx` 的广义高保真二进制提取。
- PDF、DOCX 或幻灯片作为一等发布输出。
- 面向终端用户的 pack marketplace、公开模板共享或外部 pack 生态。
- 作为必需运行路径的远程 workflow-engine 集成。
- 带副作用的广义浏览器自动化或 Windows 桌面自动化。
- 完整图编辑器或 workflow-node 编排界面。
- 云同步、多用户协作或任务分派工作流。
- 物理仓库更名或命名空间迁移。

## 主发布工作流

主路径如下：

1. 用户输入一条短需求。
2. 应用创建 `CreativeBrief`。
3. 应用生成多个 `DesignBlueprint` 候选。
4. 用户提升其中一个 blueprint。
5. 应用将被提升的 blueprint 转成 series plan 和 prompt direction。
6. 用户按需编辑 prompt。
7. 应用默认通过 fake provider 生成候选，再在 opt-in 条件下通过 OpenAI provider 生成真实结果。
8. 应用运行结构化 review。
9. 用户批准或路由 repair。
10. 应用导出带 provenance 与 approval evidence 的交付包。

这条工作流是 V1 的发布主干。其他路线应复用它，而不是创建并行产品模式。

## 计划收窄

AI 推荐: 即便架构已经支持更大的未来，也要保持 V1 边界收窄。

- 冻结 graph-first 工作流野心。
- 冻结 remote workflow-engine 野心。
- 冻结 support matrix 之外的广义二进制文档自动化。
- 冻结第一条低风险 operator slice 之外的广义浏览器和桌面自动化。
- 冻结文档和计划之外的 rename 执行。

这不是反架构，而是保护发布质量的方式。

## 支撑验证工作流

文章或纯文本路径被纳入 V1，只是为了证明“带证据的规划”能够接入同一条发布主干。

验收意图：

- 能从粘贴文本、`.txt` 或 `.md` 开始。
- 默认不依赖付费 API，也能生成 illustration target 和 evidence anchor。
- 批准后的目标能提升到现有 plan / prompts 流程。
- 不要求完整的二进制文档高保真提取，也能算作 V1 完成。

## 教学海报证明路径

文本密集型教学海报路径的存在，是为了证明产品能够交付“精确文本很重要”的高信任输出。

验收意图：

- 图像生成用于场景或背景底板。
- 必需的标签、公式、图例和 callout 在渲染后以确定性方式合成。
- Review 能区分“视觉场景成功”和“文本布局失败”。
- 最终交付包会导出 approval evidence。

## V1 的 OpenAI 使用策略

V1 需要把 provider 分工说清楚：

- 直接单次生成或编辑走 Images API。
- 需要状态、多轮规划、结构化评审或多轮图像工作流的场景，只有在这些能力确实带来价值时才走 Responses API。
- 默认 `store: false`，除非某条工作流显式选择远端状态保留。
- fake provider 仍是默认开发与回归门禁。

细化路由规则见 [../PROVIDER_ROUTING_POLICY.md](../PROVIDER_ROUTING_POLICY.md)。

## V1 的 Operator 策略

V1 只纳入一条真实的低风险 operator 执行切片。

推荐的第一条切片：

- 对一个 staged 的项目或交付目录运行本地交付/产物验证。
- 将新的审计报告写入 diagnostics 或 validation 输出目录。
- 避免破坏性副作用，也避免直接修改第三方系统。

细化执行规则见 [../OPERATOR_RISK_POLICY.md](../OPERATOR_RISK_POLICY.md)。

## V1 的源与产物边界

受支持的输入与输出边界被有意收窄。发布支持聚焦于需求文本、粘贴/纯文本文章输入，以及图像序列交付产物。

详细支持状态见 [../SOURCE_ARTIFACT_SUPPORT_MATRIX.md](../SOURCE_ARTIFACT_SUPPORT_MATRIX.md)。

## 目标状态参考

两份策略文档定义了 V1 与长期工程方向的关系：

- [../TARGET_ENGINEERING_STATE.md](../TARGET_ENGINEERING_STATE.md)
- [../EXTERNAL_REFERENCE_STRATEGY.md](../EXTERNAL_REFERENCE_STRATEGY.md)

## 发布指标

仅当下列检查全部通过时，V1 才算 ready：

- 主路径能在不使用付费 API、且不手工改数据库的前提下，连续三次完成 fake-first 端到端运行。
- 一组 `2-item` 样本序列能通过 opt-in OpenAI 路径完成，并验证 request provenance、review evidence 和 secret redaction。
- 文章或纯文本规划在默认情况下不依赖真实 provider，也能生成并提升已批准的插图目标。
- 教学海报证明路径能导出确定性文本合成 provenance 和人工 approval evidence。
- 第一条真实低风险 operator 动作能端到端跑通，并写出审计输出与 rollback 或 cleanup 说明。

## 发布阻断项

若以下任一项仍未解决，则发布被阻断：

- 已批准输出无法在交付导出中保留 review 与 approval evidence。
- `Images API` 和 `Responses API` 之间的 provider 路由在实现或文档层存在歧义。
- 文本密集型教学输出仍依赖模型直接渲染必需标签或公式。
- operator 执行缺少第一条真实动作的可审计边界。
- 主发布路径相对手工编辑 prompt 文件没有明显优势。

## 本 PRD 采用的前提假设

- 当前活动实现根目录是 `D:\CODE\ai-content-delivery-studio`。更名前的历史文档在描述旧证据时，可能仍引用 `D:\CODE\ai-image-series-studio`。
- fake provider 仍是默认安全与回归路径。
- pack 基础设施已存在，但在 V1 中只作为内部可复用配置，而不是公开产品表面。
- 二进制文档提取质量很重要，但不是 V1 发布门槛。
