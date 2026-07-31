# 文章完整配图重建与系统视觉审查证据（2026-08-01）

## Goal

把《眼睛直接观察凸透镜成像时的各种问题（王耀强）》从单图样例扩展为完整的 6 项候选图组，同时覆盖新增解释图、低清原图替换和来源照片保真整理。图组必须执行确定性合同审查、全分辨率视觉审查、受限自动修复并落盘，但不得把候选视觉通过写成科学 Gate 1 或专家验收。

## Source And Plan

- 源 PDF：8 页，SHA-256 `sha256:3b5d699a402e7600d87e2ebf17189ef69535cedbd3ee00b35e94619ad50a51b8`。
- PdfPig 审计得到 12 个页面内嵌图像，逐张转为来源像素 PNG，记录页码、页面范围、像素尺寸和 SHA-256。
- 5 张解释/替换图采用确定性 SVG，再由同一 SVG 导出 PNG/PDF；实验照片不生成、不增强，只在证据板中等比缩放、排版和编号，12 张来源资产均单独保留。
- 6 项分别是：二次成像光路、薄透镜共轭函数、光屏/固定接收面对照、`L2/S` 位置比较、附加凹透镜控制变量、来源实验照片证据板。

## Scientific Corrections

- 原候选错误使用普通实物成像分支 `y=x/(x-1)`。现按文章符号条件分别显示 `y=x/(x+1)`（`x>0`）与反函数 `y=x/(1-x)`（`0<x<1`），明确 `u/v` 为相应距离正值，且不把 `f≈2 cm` 自动声明为人眼常数。
- 原位置比较三栏的 `S/L2` 横向关系相同。现分别绘制 `L2` 位于 `S` 右侧、共面和左侧，只比较到达 `L2` 前的发散/会聚状态。
- 附加透镜明确使用凹透镜符号；干预组到达 `L2` 的光束更发散，并以不同的示意焦点表示像面后移。图中不声明度数、清晰度或医学结论。
- 光屏与固定视网膜/传感器面被明确画成不同接收条件，避免把移动光屏实验直接等同于眼睛条件。

## System Visual Review

第一轮原分辨率检查发现并修复 4 项问题：Skia 导出器忽略 `text-anchor=end` 导致右侧文字裁切；`S/L2` 共面标签重叠；附加凹透镜前后焦点未分离；色彩阈值漏掉第 4 页低饱和度实验照片。导出器增加右对齐映射与像素级回归测试；图 4、图 5 和证据板均重绘。

第二轮分别检查 6 张 PNG，未发现文字裁切、标签重叠、空白输出或新的图意冲突。5 个 PDF 又用 `pypdfium2` 重渲染为 1200×800 PNG 并逐张检查；相对直接 PNG 的最大平均通道绝对差为 1.608，无可见布局漂移。实际两轮记录位于 `outputs/article-scientific-figure-runs/20260801-eye-lens-complete-set/system-visual-review.json`。

## Persistence

实跑目录为 `outputs/article-scientific-figure-runs/20260801-eye-lens-complete-set/`，包含：

- `article-figure-set-plan.json`、`source-figure-audit.json`、`article-figure-set-report.json`；
- 5 组 SVG/PNG/PDF、1 张来源证据板 PNG；
- 6 份 `*.visual-review.json`、受限修复历史和 `system-visual-review.json`；
- `source-assets/` 下 12 张逐项哈希的来源图；
- `pdf-renders/` 下 5 张 PDF 复核渲染图。

实跑命令：

```powershell
pwsh -NoProfile -ExecutionPolicy Bypass -File scripts/run-article-scientific-figure-set.ps1 `
  -SourcePath 'paper/眼睛直接观察凸透镜成像时的各种问题（王耀强）.pdf' `
  -OutputDirectory 'outputs/article-scientific-figure-runs/20260801-eye-lens-complete-set'
```

## Authority Boundary

- 6 项候选均为 `PendingHumanApproval`；本轮没有自动批准任何新科学主张。
- 应用内 provider 记录是 `fake-scientific-visual`，只证明全分辨率请求、结果和修复路径可运行；不能称为 live OpenAI 视觉审查或专家验收。
- `system-visual-review.json` 是本次交互中的系统视觉检查，覆盖视觉呈现和有界示意一致性；它不是逐图人类科学 Gate 1。
- Gate 2、ZIP 正式交付、WPF 图组入口与 SQLite 图组持久化不在本切片完成范围内。

## Verification

按项目固定顺序执行并通过：

- `dotnet build ContentDeliveryStudio.sln`：0 警告、0 错误。
- `dotnet test ContentDeliveryStudio.sln --no-build`：747/747 通过，0 失败、0 跳过。
- `scripts/verify-reference-evidence.ps1`：reference governance 同步，scientific-figure workflow 证据命中并通过。
- `dotnet format --verify-no-changes`：exit 0。
- `scripts/preflight-release.ps1 -NoRestore`：repository verification、publish WhatIf/实际 preflight、84 文件 ZIP、placeholder/conflict 和 diff hygiene 全部通过；包 SHA-256 为 `8fc01b59b00da3e3c3aafede9abb2f08c84796ae3e6f71d0f01df43076a47ff8`。

## Rollback

仅撤销本切片新增的图组规划、渲染、提取、测试、脚本、设计/计划和证据文件，以及 `ScientificFigureExporter` 的 `text-anchor=end` 修复。`paper/`、`outputs/` 和 `tmp/` 是 Git 外资料，不使用 Git 回滚伪装恢复。
