# 中文文档中心

> 英文治理原文: [../DOCUMENTATION_GOVERNANCE.md](../DOCUMENTATION_GOVERNANCE.md)
> 根入口: [../../README.zh-CN.md](../../README.zh-CN.md)
> 说明: 这里提供中文优先的阅读入口与导航。若中文说明与英文正式文档、代码、测试、ADR 或证据文件冲突，以仓库事实和英文正式文档为准。

## 用这套文档解决什么问题

本目录不试图立刻复制整套英文工程文档，而是先为中文读者补齐“入口型说明文档”：

- 这个仓库是什么、应该从哪里读起
- V1 到底承诺了什么
- 当前已经被验证到什么程度
- 用户怎样使用当前最强路径
- 产品设计的核心对象和工作流是什么

对更深的工程治理、路线图、架构细节、provider policy 和 reference governance，本目录会明确把你导向英文正式文档，而不是制造第二套互相漂移的真值面。

## 建议阅读顺序

| 你现在想回答的问题 | 建议先读 |
| --- | --- |
| 这个仓库是什么、当前根目录在哪里、怎么本地开始？ | [../../README.zh-CN.md](../../README.zh-CN.md) 然后对照 [../../README.md](../../README.md) |
| V1 的发布承诺是什么？ | [PRD_V1.md](./PRD_V1.md) 然后对照 [../PRD_V1.md](../PRD_V1.md) |
| 目前到底验证到了什么程度？ | [V1_LAUNCH_EVIDENCE.md](./V1_LAUNCH_EVIDENCE.md) 然后对照 [../V1_LAUNCH_EVIDENCE.md](../V1_LAUNCH_EVIDENCE.md) |
| 作为用户或操作者，应当如何走当前最强路径？ | [USER_GUIDE.md](./USER_GUIDE.md) |
| 产品工作流、对象模型和 V1 设计边界是什么？ | [PRODUCT_DESIGN.md](./PRODUCT_DESIGN.md) |
| 还要做什么、下一步车道如何划分？ | 先读英文 [../ROADMAP.md](../ROADMAP.md) 和 [../TASKS.md](../TASKS.md) |
| 非 trivial 工程切片应当如何执行？ | 先读英文 [../AI_CODING_WORKFLOW.md](../AI_CODING_WORKFLOW.md) 和 [../../AGENTS.md](../../AGENTS.md) |
| provider、operator、reference evidence 的强治理边界是什么？ | 先读英文 [../PROVIDER_ROUTING_POLICY.md](../PROVIDER_ROUTING_POLICY.md)、[../OPERATOR_RISK_POLICY.md](../OPERATOR_RISK_POLICY.md)、[../REFERENCE_EVIDENCE_POLICY.md](../REFERENCE_EVIDENCE_POLICY.md)、[../REFERENCE_BASIS.md](../REFERENCE_BASIS.md) |

## 当前已提供中文版的说明文档

- [../../README.zh-CN.md](../../README.zh-CN.md): 仓库总入口、本地启动、当前读数、文档地图。
- [PRD_V1.md](./PRD_V1.md): V1 发布承诺、边界、指标和 blocker。
- [V1_LAUNCH_EVIDENCE.md](./V1_LAUNCH_EVIDENCE.md): 当前 V1 指标验证状态与证据快照。
- [USER_GUIDE.md](./USER_GUIDE.md): 当前最强支持路径的用户使用说明。
- [PRODUCT_DESIGN.md](./PRODUCT_DESIGN.md): 产品定位、工作流、对象模型与 MVP 范围。

## 当前仍以英文版为准的工程文档

- [../ROADMAP.md](../ROADMAP.md): 长中短期车道、阶段定义和当前 baseline。
- [../TASKS.md](../TASKS.md): 可执行任务清单与 deferred trigger。
- [../ARCHITECTURE.md](../ARCHITECTURE.md): 分层架构、数据模型、provider 边界和质量门禁。
- [../AI_CODING_WORKFLOW.md](../AI_CODING_WORKFLOW.md): repo-owned spec/plan/evidence 驱动的工程执行方式。
- [../PROVIDER_CONFIGURATION.md](../PROVIDER_CONFIGURATION.md): provider 配置、预检与健康检查。
- [../PROVIDER_ROUTING_POLICY.md](../PROVIDER_ROUTING_POLICY.md): `Images API` / `Responses API` 路由策略。
- [../OPERATOR_RISK_POLICY.md](../OPERATOR_RISK_POLICY.md): operator 风险分级和审计要求。
- [../REFERENCE_EVIDENCE_POLICY.md](../REFERENCE_EVIDENCE_POLICY.md): 高漂移工程变更的证据要求。
- [../REFERENCE_BASIS.md](../REFERENCE_BASIS.md): `任务/代码区域 -> 本地参考资料架` 的长期映射。

## 中英文对齐规则

1. 中文入口文档负责可读性和导航，不单独发明新的状态结论。
2. 已建立镜像的说明文档，只要英文版发生用户可感知语义变化，就应在同一切片内同步中文版本，或在中文文档中显式标注待同步漂移。
3. 如果中文说明与英文正式文档冲突，以英文正式文档为准；如果英文文档与代码、测试、ADR、脚本或证据文件冲突，以仓库事实为准。
4. 深层工程治理文档没有中文镜像时，中文中心必须明确告知读者“应跳到哪个英文正式面”，而不是含混转述。

## 这一轮的边界

这一轮补的是“中文可进入、可理解、可持续维护”的说明层，而不是一次性把全部深层工程文档逐字翻译一遍。这样能先解决中文读者进入门槛，同时避免两套长文档在后续维护中迅速失真。
