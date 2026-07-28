# 科研绘图 Agent 操作的 Native WPF 试运行证据

英文原文：[20260728-scientific-figure-agent-operated-trial.md](../../change-evidence/20260728-scientific-figure-agent-operated-trial.md)

日期：2026-07-28

## 状态

`agent-operated evidence complete / human operator trial pending`

## 用途与边界

本次补充探针在真实 native WPF 应用中，以确定性 fake provider 走完已经 accepted 的科研绘图工作流。它补充了自动化测试之外的用户可见操作证据，但不是人类操作员决定，也不满足 `operator/manual evidence`。

本次探针没有重新打开科研绘图 Tasks 1-30 或 Checkpoints 0-5，没有调用付费 provider、刷新 live evidence、修改 accepted artifact，也没有调用试运行 harness 的 `Finalize` 模式。

## 会话证据

- Run ID：`agent-native-20260728-01`
- 启动时仓库提交：`7046a8dec82a9a5af961c8c9f567c411467cf2da`
- 被 Git 忽略的会话根：`outputs/scientific-figure-operator-trials/agent-native-20260728-01`
- WPF 退出后的生命周期：`status=awaiting_finalize`、`evidenceLevel=pending_operator`、`providerMode=fake`、`liveAccepted=false`
- 运行数据只写入会话内 `data/studio.sqlite`，没有使用正常用户 workspace 或 accepted evidence 路径
- WPF 和 harness 进程都已退出，没有执行 finalization

SQLite 和交付 ZIP 都是位于 `outputs/` 下的本地运行证据，不进入 Git。

## Native WPF 观察结果

应用标题栏显示 fake provider 模式；活动面板明确说明文本、图像和视觉评审 provider 均为 fake 实现，且未启用任何真实 API 调用。

Agent 依次检查了五个可见科研工作区：

1. Source 显示 `block-dynamics`、第 `4` 页、`2.1 Dynamics`，以及原文 `Net force causes acceleration for constant mass.`。
2. Understanding 显示已接受主张 `claim-newton-second-law`，置信度 `0.96`，没有缺失证据。
3. Figure Spec 显示 `element-force`、`element-acceleration`、`relation-force-acceleration`，关系为 `Causes`，Gate 1 权威冻结在规格版本 `1`。
4. Render & Review 显示确定性 SVG 和三个权威元素；合同硬失败、科学语义问题、视觉问题均为空，授权修复保持禁用。
5. Delivery 显示 SVG、PNG、PDF、三条 claim-evidence 映射、`Contract review: True`、`Machine review: True`、`repair count: 0`，以及确定性 fake semantic/visual provider 元数据。

Gate 2 reviewer 被刻意填写为 `codex-agent`，备注为 `Agent-operated fake-first native WPF probe; not human operator/manual acceptance.`。这个 approval 只存在于本地包内，不能解释为人类 approval。

## 交付包核验

WPF 导出的本地包为：

`outputs/scientific-figure-operator-trials/agent-native-20260728-01/delivery/scientific-figure.zip`

- ZIP 大小：`548433` 字节
- ZIP SHA-256：`C3E09F21374C48B0B74979F9A5A0B5E97CD7BF0F7A3B040E7D46C47551F6B959`
- 10 个必需条目全部存在，包括 SVG、PNG、PDF、specification、证据映射、reviews、repairs、providers、approvals 和 manifest
- 合同评审通过且没有 hard failure；机器评审没有 blocker
- provider 为 `fake-scientific-semantic / deterministic-fake-v1` 和 `fake-scientific-visual / deterministic-fake-v1`
- manifest 中的 SVG、PNG、PDF hash 与导出文件一致

只进行了只读结构和 hash 核验。人类专用 `Finalize` 没有被调用，因此 `trial.json` 仍是 `pending_operator`，不是 `operator/manual evidence`。

## 真值读数

- repo-side operator-trial kit：完成且未改变。
- agent-operated native WPF evidence：当前确定性 fake 样本已完成。
- human operator/manual evidence：仍待完成；真人必须检查五个工作区并显式 finalize 一个新会话。
- 既有 live accepted evidence：未改变；provider 行为没有变化，也没有刷新 live evidence。
- OCR-heavy 来源、实测或伪造数据图、显微镜式观测、自动改变科学含义，以及把生成图像表述为真实观测，仍在已验收边界之外。

## 仓库门禁

英文证据原文记录了 2026-07-28 的固定顺序门禁：build 为 `0` warning / `0` error，tests 为 `672 / 672`，contract/invariant 与 hotspot 全部通过。中文伴随页不单独扩大该结论。
