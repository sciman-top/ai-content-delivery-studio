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

## Optical Scientific-Rigor Follow-Up

本轮加入 `article-optics-v1` 本地确定性审查包，视觉模型只检查可见缺陷，不承担科学真值定义：

- 公式区校验文章采用的 `y=x/(x+1), x>0` 与 `y=x/(1-x), 0<x<1`、反函数关系、`x=u/f`、`y=v/f`、距离正值约定及无量纲变量；
- 光学元件区校验主/眼睛透镜为凸透镜、控制变量使用附加凹透镜；
- 关系区校验 `L2` 位于 `S` 右侧/共面/左侧的三种顺序、光线从左向右、到达 `L2` 前的会聚/发散状态以及干预前后焦点不能相同；
- 来源板校验 6 个来源照片引用均有不同的已审计源资产，禁止生成实验照片或遗漏引用；
- 科学失败直接阻断且不进入自动修复；文字裁切、标签重叠、布局和非证据素材仍受三次 presentation-only 修复上限约束。

正式 `ScientificFigureSpec` 路径与文章路径的每个 crop 现在都携带 typed `ExpectedVisualCheck`：科学含义、exact content、关系方向、条件、禁止项、来源 block id 与 authority。正式路径标记 `ApprovedSpecification`；文章快捷路径因没有人工批准规格，只能标记 `LocatedSourceEvidencePendingGateOne`。

对抗测试覆盖错误公式 `x/(x-1)`、凸/凹透镜互换、干预前后焦点相同、光线反向、`L2/S` 顺序颠倒、来源照片遗漏、文字裁切和标签重叠。正确 6 图的确定性报告全部通过，但候选仍全部是 `PendingHumanApproval`。

OpenAI 官方图像视觉文档说明，精确小字和空间定位应在模型支持时使用 `detail: original`；GPT-5.4 及更新模型支持该模式，旧模型回退 `high`。本仓现在按模型能力选择 detail。根据用户新增要求与官方 GPT-5.6 指南，主动默认设置为 `gpt-5.6-sol` 与显式 `reasoning.effort=medium`；fake provider 仍是默认，未执行 live 调用。Scientific review provider 对 `408`、`429`、`5xx` 和瞬态网络错误最多尝试 3 次，`400/401/403` 不重试。

本轮又补齐 scientific review 的本地持久化 resume：checkpoint 身份绑定 schema、operation、endpoint、model、reasoning effort 与实际发送 JSON bytes 的 SHA-256，只有完全匹配才复用；损坏、超限、未知字段或身份不匹配均 fail closed。恢复结果继续校验当前责任项 ID，并显式标记 `PersistedCheckpoint`，不会把旧的 `Fail/Uncertain` 升级为 `Pass`。checkpoint 不保存 API key、图片 bytes/base64/data URL 或原始 provider response；`RealApiEnabled=false` 在读取 checkpoint 前阻断，因此 fake-first 不能被历史 live 状态绕过。

官方依据：

- <https://developers.openai.com/api/docs/guides/images-vision#choose-an-image-detail-level>
- <https://developers.openai.com/api/docs/guides/model-guidance?model=gpt-5.6>

## Persistence

实跑目录为 `outputs/article-scientific-figure-runs/20260801-eye-lens-complete-set/`，包含：

- `article-figure-set-plan.json`、`source-figure-audit.json`、`article-figure-set-report.json`；
- 5 组 SVG/PNG/PDF、1 张来源证据板 PNG；
- 6 份 `*.visual-review.json`、受限修复历史和 `system-visual-review.json`；
- `source-assets/` 下 12 张逐项哈希的来源图；
- `pdf-renders/` 下 5 张 PDF 复核渲染图。

本轮 fresh fake-first 实跑目录为 `outputs/article-scientific-figure-runs/20260801-eye-lens-optics-rigor/`。聚合报告记录 `deterministicReview=article-optics-v1`、6/6 完整、12 个来源资产、`gateOneStatus=pending for every candidate`；每份 `*.visual-review.json` 记录确定性 findings、typed crops 与 expected checks。`fresh-visual-contact-sheet.png` 仅是本轮交互式呈现复核，不是应用内 live provider 或物理专家证据。

实跑命令：

```powershell
pwsh -NoProfile -ExecutionPolicy Bypass -File scripts/run-article-scientific-figure-set.ps1 `
  -SourcePath 'paper/眼睛直接观察凸透镜成像时的各种问题（王耀强）.pdf' `
  -OutputDirectory 'outputs/article-scientific-figure-runs/20260801-eye-lens-complete-set'
```

## Authority Boundary

- 6 项候选均为 `PendingHumanApproval`；本轮没有自动批准任何新科学主张。
- 应用内 provider 记录是 `fake-scientific-visual`，只证明全分辨率请求、结果和修复路径可运行；不能称为 live OpenAI 视觉审查或专家验收。
- checkpoint/resume 的对抗测试只证明本地精确请求恢复、隐私边界和 fail-closed 行为；`PersistedCheckpoint` 不是本轮新的 live provider call 或 acceptance。
- `system-visual-review.json` 是本次交互中的系统视觉检查，覆盖视觉呈现和有界示意一致性；它不是逐图人类科学 Gate 1。
- Gate 2、ZIP 正式交付、WPF 图组入口与 SQLite 图组持久化不在本切片完成范围内。

## Historical Verification

按项目固定顺序执行并通过：

- `dotnet build ContentDeliveryStudio.sln`：0 警告、0 错误。
- `dotnet test ContentDeliveryStudio.sln --no-build`：747/747 通过，0 失败、0 跳过。
- `scripts/verify-reference-evidence.ps1`：reference governance 同步，scientific-figure workflow 证据命中并通过。
- `dotnet format --verify-no-changes`：exit 0。
- `scripts/preflight-release.ps1 -NoRestore`：repository verification、publish WhatIf/实际 preflight、84 文件 ZIP、placeholder/conflict 和 diff hygiene 全部通过；包 SHA-256 为 `8fc01b59b00da3e3c3aafede9abb2f08c84796ae3e6f71d0f01df43076a47ff8`。

以上 747/747 与包哈希属于上一轮重建基线，不能代表本轮 follow-up。

## Fresh Optical-Rigor Verification

本轮先通过 92 项 provider/article 合同测试与 29 项 optics/retry/prep 聚焦测试，再对最终实现按固定顺序刷新完整门禁：

- `dotnet build ContentDeliveryStudio.sln`：0 警告、0 错误；
- `dotnet test ContentDeliveryStudio.sln --no-build`：758/758 通过，0 失败、0 跳过；
- `scripts/verify-reference-evidence.ps1`：`openai-provider` 与 `scientific-figure-workflow` 两个触发领域均命中 authority evidence 并通过；
- `dotnet format --verify-no-changes`：exit 0；
- `scripts/preflight-release.ps1 -NoRestore`：repository verification、publish WhatIf/实际 preflight、placeholder/conflict 与 diff hygiene 全部通过；生成 84 文件 ZIP，SHA-256 为 `2cb2bd4c05b50ac49c7b18774cedfbbff7fed852ef43008585cf17b52a8544fd`。

以上均为 fake-first、本机 repo-side 验证。它们不等于物理专家 Gate 1、live OpenAI provider acceptance 或正式 Gate 2 交付。

## Checkpoint/Resume Focused Verification

在 full gate 前执行 scientific review、runtime DI、fake origin 和 checkpoint 聚焦测试，30/30 通过。覆盖跨 provider 实例精确请求恢复、恢复时零 secret 读取和零 HTTP 调用、model/payload/image hash 变化不命中、损坏或身份篡改阻断、fake-first 不读取历史 checkpoint、`Fail` verdict 原样恢复，以及 checkpoint 不含 secret 或图像 payload。该测试使用本地 fake handler，没有发起 live/付费调用；full gate 结果在本轮 fresh 验证后更新。

## Fresh Checkpoint/Resume Full Gate

checkpoint/resume 最终代码与文档按固定顺序完成本轮 full gate：

- `dotnet build ContentDeliveryStudio.sln`：0 警告、0 错误；
- `dotnet test ContentDeliveryStudio.sln --no-build`：764/764 通过，0 失败、0 跳过；
- `scripts/verify-reference-evidence.ps1`：`openai-provider` 与 `scientific-figure-workflow` 两个触发域均命中各自 authority evidence 并通过；
- `dotnet format --verify-no-changes`：exit 0；
- `scripts/preflight-release.ps1 -NoRestore`：repository verification、publish WhatIf/实际 preflight、placeholder/conflict 与 diff hygiene 全部通过；生成 84 文件 ZIP，SHA-256 为 `a4eedf0e9101d506980bed5a5dd9841d6e616fd060b1fd66921cc534c1d55a18`。

该结果只证明 repo-side 实现、fake handler 合同与本地 checkpoint 行为。没有读取 secret，没有发起 live/付费调用，也不构成物理专家 Gate 1、live provider acceptance 或 Gate 2 正式交付。

## Rollback

仅撤销本切片新增的 optical reviewer、typed expected-check 合同、provider model/reasoning/detail/retry 设置、测试、脚本、设计/计划和证据变更。原重建切片与 `paper/` 不回滚；`outputs/` 是 Git 外运行证据，不使用 Git 回滚伪装恢复。

## 2026-08-01 路径收口复核

统一最终图片路径切片后，使用同一份眼睛/凸透镜 PDF 重新执行：

```powershell
pwsh -NoProfile -ExecutionPolicy Bypass -File scripts/run-article-scientific-figure-set.ps1 `
  -SourcePath 'paper\眼睛直接观察凸透镜成像时的各种问题（王耀强）.pdf' `
  -OutputDirectory 'workspace\article-figure-runs\20260801-delivery-factory-rerun-complete-set'
```

结果为 exit 0、6/6、`complete=true`、`deterministicReview=article-optics-v1`；6 份
`*.visual-review.json` 均有 typed crops 与 expected checks，确定性科学审查通过，
且 `gateOneStatus=PendingHumanApproval`。这次运行没有 live/付费 provider 调用，
也没有把脚本候选复制到最终交付根。
