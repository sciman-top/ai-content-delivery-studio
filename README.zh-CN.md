# AI Content Delivery Studio

以 Windows 为先的 AI 内容交付工作台，当前以图像序列生产作为核心发布路径。

> 英文原文: [README.md](./README.md)
> 中文文档中心: [docs/zh-CN/README.md](./docs/zh-CN/README.md)
> 说明: 本文件是中文入口版。若与英文原文、代码、测试、ADR、脚本或证据文件冲突，以仓库事实和英文正式文档为准。

本仓库是产品的活动实现主仓。它与 `D:\CODE\physicist_chinese_poster_batch_tool` 明确分离；后者是生产案例和迁移样本，不是当前实现根目录。当前产品名、仓库名、解决方案名和命名空间已经统一为 `AI Content Delivery Studio` / `ai-content-delivery-studio` / `ContentDeliveryStudio`，旧的 `ImageSeriesStudio` 仅保留在历史证据和有限兼容说明中。

## 当前读数

- 活动实现根目录: `D:\CODE\ai-content-delivery-studio`
- 当前本地根目录: `D:\CODE\ai-content-delivery-studio`
- 最近一次本地仓库全量验证: `2026-06-23`，命令为 `.\scripts\verify-repo.ps1 -NoRestore`
- 最近一次更强的收口门禁: `2026-06-23`，命令为 `.\scripts\preflight-release.ps1 -NoRestore`
- 最近一次记录在册的 V1 发布验证快照日期: `2026-06-23`
- 最近一次记录在册的真实 OpenAI 样本仍为: `artifacts/live-openai-v1-sample/20260611-132947`
- 当前 V1 发布验证结论: [docs/V1_LAUNCH_EVIDENCE.md](./docs/V1_LAUNCH_EVIDENCE.md) 的 `2026-06-23` 快照维持 `5 / 5` 指标全部已验证，其中最新的真实 provider 证据仍锚定在 `2026-06-11` 的样本资产
- 当前 repo-side 执行队列: 对已记录的 V1 和实施计划表面已收口；剩余内容明确属于未来触发通道或延后车道，而不是活动中的开放积压
- 当前最强的用户可见路径:
  - 短需求 -> brief -> blueprint -> series -> review -> delivery
  - 纯文本或文章 -> 证据锚点 -> 插图目标 -> 提升进下游工作流
  - 文本密集型教学海报 -> 确定性文本后合成 -> 审批证据导出
- 当前桌面宿主内置且边界受控的本地工具适配器:
  - `artifact-validation`
  - `deterministic-text-composition`
  - 只读 `openai-launch-preflight`

## 真值边界

- `README.md` / `README.zh-CN.md` 是仓库概览与本地启动入口，不负责单独定义发布真值。
- 当前 V1 发布声明的正式真值仍以 [docs/V1_LAUNCH_EVIDENCE.md](./docs/V1_LAUNCH_EVIDENCE.md) 为准；中文版 [docs/zh-CN/V1_LAUNCH_EVIDENCE.md](./docs/zh-CN/V1_LAUNCH_EVIDENCE.md) 主要用于中文阅读。
- 产品承诺与发布边界以 [docs/PRD_V1.md](./docs/PRD_V1.md) 为准；中文版入口见 [docs/zh-CN/PRD_V1.md](./docs/zh-CN/PRD_V1.md)。

## 产品目标

帮助用户把一条需求、一份源文件或一个草稿，转成经过评审的交付包。当前发布主干仍然是“图像序列优先”：

1. 采集目标、受众、约束、参考资料和质量标准。
2. 产出可复用的 brief、blueprint 候选以及被提升的路线。
3. 以受控批次生成候选视觉结果。
4. 基于结构化 rubric 进行评审，并保留人工最终审批。
5. 在正确层修复问题：brief、blueprint、prompt、参数、确定性合成或源证据。
6. 导出包含图像、prompt 快照、来源、评审证据和 manifest 的交付包。

## 快速开始

### 验证仓库

使用规范的本地全量门禁：

```powershell
.\scripts\verify-repo.ps1
```

该门禁先通过 `.\scripts\sync-reference-governance.ps1 -Check` 检查参考治理一致性，再运行参考证据、构建、测试和格式校验。

### 运行更强的发布式预检

```powershell
.\scripts\preflight-release.ps1
```

这个预检会在参考治理一致性检查之后，补充 placeholder、merge-conflict、publish dry-run 以及 diff hygiene 检查。

### 发布本地 Windows 构建

```powershell
.\scripts\publish-app.ps1 -Configuration Release -Runtime win-x64
```

只做预演：

```powershell
.\scripts\publish-app.ps1 -WhatIfOnly
```

### 仅文档卫生检查

```powershell
rg -n "(TB[D]|TO[D]O|PLACE''HOLDER)" .
git status --short
```

## 仓库结构

### 源项目

- `src/ContentDeliveryStudio.App`: WPF 外壳、本地化、工作台 UI、诊断和面向操作员的流程。
- `src/ContentDeliveryStudio.Application`: 应用服务、工作流协调器、修复路由和交付编排。
- `src/ContentDeliveryStudio.Core`: 领域模型、provider 契约、工作流记录、评审与交付不变量。
- `src/ContentDeliveryStudio.Infrastructure`: EF Core 持久化、provider 适配器、本地工具适配器、诊断和合成服务。

### 支撑区域

- `tests/ContentDeliveryStudio.Tests`: 单测、SQLite reload、workflow、provider、launch route 和交付验证。
- `docs/`: 产品、架构、路线图、任务、发布证据、provider policy、operator policy、ADR 与参考治理。
- `scripts/`: 仓库验证、发布预检、发布脚本、参考证据和相关本地自动化。
- `artifacts/`: 已提交的证据包，例如真实 OpenAI 样本输出和诊断复跑结果。
- `workspace/`: 本地项目状态与运行数据，已被 git 忽略。
- `outputs/`: 生成的交付输出，已被 git 忽略。

## 当前 V1 边界

V1 明确比长期的多模态愿景更窄。

- 主发布路径: 短需求 -> `CreativeBrief` -> `DesignBlueprint` -> 图像序列工作流 -> review -> 已批准的 `DeliveryPackage`
- 支撑验证路径: 文章或纯文本 -> 证据锚点 -> 插图目标 -> 提升到同一条下游交付流
- 证明路径: 生成背景图 -> 确定性文本、公式和标签合成 -> 审批证据导出

当前正式边界文档：

- [docs/PRD_V1.md](./docs/PRD_V1.md)
- [docs/V1_LAUNCH_EVIDENCE.md](./docs/V1_LAUNCH_EVIDENCE.md)
- [docs/ROADMAP.md](./docs/ROADMAP.md)
- [docs/TASKS.md](./docs/TASKS.md)

## 当前工程姿态

- 默认仍是 fake-first 和 local-first。
- 真实 OpenAI 行为必须显式启用，并经过 provider 配置、preflight 与证据审查。
- 对文本密集型教学或海报输出，确定性文本合成已是当前被证明的 V1 路径组成部分。
- 操作员自动化保持边界受控: 先做低风险本地验证与准备，再考虑更强副作用自动化。
- 当前传输层拆分是有意的: 稳定的单次图像生成走官方 OpenAI .NET SDK Images 路径；部分需要状态化规划和评审的 Responses 流程，在 SDK 表面尚未满足契约时仍保留原始 `HttpClient`。
- 非 trivial 的工程切片应遵循 [docs/AI_CODING_WORKFLOW.md](./docs/AI_CODING_WORKFLOW.md): repo-owned spec/plan/evidence、契约优先的公共边界、按需使用 subagent/worktree，以及分层自动执行。

## 中文文档导航

### 建议先读

- [docs/zh-CN/README.md](./docs/zh-CN/README.md)
- [docs/zh-CN/PRD_V1.md](./docs/zh-CN/PRD_V1.md)
- [docs/zh-CN/V1_LAUNCH_EVIDENCE.md](./docs/zh-CN/V1_LAUNCH_EVIDENCE.md)
- [docs/zh-CN/USER_GUIDE.md](./docs/zh-CN/USER_GUIDE.md)
- [docs/zh-CN/PRODUCT_DESIGN.md](./docs/zh-CN/PRODUCT_DESIGN.md)

### 暂以英文版为准的深层工程文档

- [docs/ROADMAP.md](./docs/ROADMAP.md)
- [docs/TASKS.md](./docs/TASKS.md)
- [docs/AI_CODING_WORKFLOW.md](./docs/AI_CODING_WORKFLOW.md)
- [docs/ARCHITECTURE.md](./docs/ARCHITECTURE.md)
- [docs/PROVIDER_CONFIGURATION.md](./docs/PROVIDER_CONFIGURATION.md)
- [docs/PROVIDER_ROUTING_POLICY.md](./docs/PROVIDER_ROUTING_POLICY.md)
- [docs/OPERATOR_RISK_POLICY.md](./docs/OPERATOR_RISK_POLICY.md)
- [docs/REFERENCE_EVIDENCE_POLICY.md](./docs/REFERENCE_EVIDENCE_POLICY.md)
- [docs/REFERENCE_BASIS.md](./docs/REFERENCE_BASIS.md)

## 本地参考资料架

本地外部参考资料架位于：

`D:\CODE\external\ai-content-delivery-studio-references`

仓库会在 `scripts/external-reference-shelf.snapshot.json` 中保留一份 repo-side 清单快照。刷新该清单与 `docs/REFERENCE_BASIS.md` 时，请运行：

```powershell
.\scripts\sync-reference-governance.ps1
```

它用于本地快速查阅官方 SDK、WPF 与 host 模式、EF Core 和 SQLite、弹性与可观测性实现、确定性文档工具以及图像工作流架构参考。这些材料会影响工程决策，但不会覆盖本仓代码、测试、ADR 或项目规则。
