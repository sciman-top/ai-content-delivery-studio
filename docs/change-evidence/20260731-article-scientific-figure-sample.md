# 文章级科研配图候选实跑证据（2026-07-31）

> 本文件记录最初的单图/三候选切片。完整 6 项图组、来源照片替换策略和两轮系统视觉审查见 `docs/change-evidence/20260801-article-figure-set-reconstruction.md`；不要把这里的历史候选数当作当前样例状态。

## 目标与范围

将本地文章 PDF 连接到科研绘图主链：`PDF 提取 -> 多个证据绑定候选 -> Gate 1 -> 确定性 SVG/PNG/PDF -> 合同、语义、视觉审查`。本切片不替代科学审校，也不将机器审查或候选预览写成最终交付。

## 依据

- 文章：`paper/眼睛直接观察凸透镜成像时的各种问题（王耀强）.pdf`（用户本地资料，Git 忽略）。
- 权威链：`source article -> located evidence -> human-approved claims/specification -> render/review/delivery`。
- 首图以确定性 SVG 表示主透镜、中间像 S、眼睛晶状体、视网膜和两组光线；图中明确声明为非按比例的 Gate 1 候选预览。

## 实跑命令与结果

```powershell
pwsh -NoProfile -ExecutionPolicy Bypass -File scripts/run-article-scientific-figure.ps1 `
  -SourcePath 'paper/眼睛直接观察凸透镜成像时的各种问题（王耀强）.pdf' `
  -OutputDirectory outputs/article-scientific-figure-runs/20260731-eye-lens-article
```

结果：

- `ScientificExtractionStatus.Ready`；8 页、8 个可定位文本块。
- 源文件 SHA-256：`sha256:3b5d699a402e7600d87e2ebf17189ef69535cedbd3ee00b35e94619ad50a51b8`。
- 产生 3 个不同且带页码/块标识的候选：二次凸透镜成像光路、观察位置与可见性/清晰度比较、光屏与视网膜实验对照。
- 产生 `candidate-01-secondary-lens-imaging-path.svg`，用于 Gate 1 前的候选讨论。
- 指定审阅人 `sciman` 在当前交互中确认 Gate 1：仅批准“主凸透镜形成中间像 S；S 作为眼睛晶状体的物体；光线朝视网膜方向传播”的非按比例示意关系。
- 持久化正式工作流 `approved-scientific-workflow.json`，状态为 `ReviewPassed`，并保留 Gate 1 审批、来源哈希、页码/块证据、元素/关系溯源和两项下游机器审批。
- 产生同一权威 SVG 导出的 `approved-mechanism.svg`、`approved-mechanism.png` 和 `approved-mechanism.pdf`。
- 确定性合同审查通过；语义审查和全分辨率视觉审查均被调用。视觉审查准备了完整 PNG 和 7 个逐元素/关系裁剪区。
- 人工视觉复核曾发现两条关键关系标签的底板重叠，导致 `forms S` 被遮挡。渲染器现以确定性标签占位碰撞检测选择不重叠的标签带；合同审查新增 `critical-relation-label-overlap` 硬失败，任何关键关系标签重叠的 SVG 都不能进入机器审查通过状态。
- 本次为 fake-first：`fake-scientific-semantic` 与 `fake-scientific-visual` 均明确记录在 `machine-review.json`，不等同于真实 provider 或专家审查。
- 聚焦测试 `ArticleScientificFigurePlanningTests`：5/5 通过；其中一项用该 PDF 执行了真实提取、Gate 1、导出和机器审查输出，另一项覆盖三个关键关系标签的非重叠可读性。

本地输出位于 Git 忽略的 `outputs/article-scientific-figure-runs/20260731-eye-lens-article/`，包括文章报告、候选 SVG、正式工作流、正式 SVG/PNG/PDF 与机器审查记录。正式 PNG 已做人工视觉复核；中文缺字问题在复核中被发现后修复。导出器现在在文本无法由主字体或系统回退字体完整覆盖时 fail-closed，不会将缺字图交给机器审查或交付。

## 审批与风险边界

- Gate 1 已批准且其范围已持久化；它只覆盖非按比例的几何关系。
- 文章中的眼睛近点/焦距、视网膜成像位置、视觉正倒和清晰度等论断仍保留为未批准内容；系统没有自动把它们宣称为科学事实。
- 机器审查是 fake-first 验证，不能替代真实 provider、科学专家或最终人工验收。
- Gate 2、自动修复记录和 ZIP 交付包尚未创建；Gate 2 仍需新的明确人工决定。自动修复只可修改布局、样式和非证据性素材，不可改写本次 Gate 1 的科学关系。

## 回滚

删除本切片新增的应用服务、测试、脚本和本证据文件即可；`outputs/` 与 `paper/` 均为 Git 外本地资料，不使用 Git 回滚伪装恢复。
