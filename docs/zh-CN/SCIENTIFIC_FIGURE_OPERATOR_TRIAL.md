# 可信科研绘图 Fake-First 操作员试运行

英文原文：[SCIENTIFIC_FIGURE_OPERATOR_TRIAL.md](../SCIENTIFIC_FIGURE_OPERATOR_TRIAL.md)

## 用途

本 runbook 为已经 accepted 的科研绘图 WPF 工作流创建一个新的、本地隔离的操作员试运行。它只使用确定性 fake fixture，不调用付费 provider。

证据层级必须分开：

- 仓库实现和固定门禁通过属于 `repo-side done`。
- 仅准备或启动的会话属于 `pending_operator`。
- 由真人显式完成并 finalize 的会话才属于 `operator/manual evidence`。
- 本流程不会刷新或替换既有 `live accepted` 证据。

自动化测试、UI Automation 和交付包校验都不能等同于人工操作员试运行。

## 前置条件

- Windows、.NET 10 SDK 和 PowerShell 7
- `ContentDeliveryStudio.sln` 已完成干净构建
- 不需要 `.env`、API key 或 live provider 配置

## 运行试用

在仓库根目录执行：

```powershell
pwsh -NoProfile -ExecutionPolicy Bypass -File scripts/run-scientific-figure-operator-trial.ps1 -Mode Run
```

脚本会在 `outputs/scientific-figure-operator-trials/` 下创建唯一且被 Git 忽略的会话，强制设置 `PROVIDER_MODE=fake`，把数据库和应用本地文件重定向到该会话，并启动 WPF 应用。应用退出后，调用者原有环境变量会恢复。

如果只想准备会话文件而不打开 WPF：

```powershell
pwsh -NoProfile -ExecutionPolicy Bypass -File scripts/run-scientific-figure-operator-trial.ps1 -Mode Prepare
```

这种会话保持 `pending_operator`，不是人工证据。

## 人工检查清单

以会话内生成的 `operator-checklist.md` 为准：

1. 确认标题栏显示 fake provider 模式。
2. 打开“科研绘图”，检查 Source 的提取状态和证据。
3. 检查 Understanding 的主张、精确证据、冲突和限制。
4. 检查 Figure Spec 的元素、关系、provenance 和 Gate 1 权威。
5. 检查 Render & Review 的 SVG/PNG/PDF 预览和 contract、semantic、visual 三层审查。
6. 检查 Delivery 的格式、provider、repair、证据映射和两个人类门禁。
7. 输入真实 reviewer 和非空 notes，然后批准或拒绝 Gate 2。
8. 如果批准，把 ZIP 导出到 `trial.json` 中的精确 `expectedPackagePath`。
9. 关闭应用后再执行 finalize。

不要仅因为确定性 fixture 通过了自动化检查就批准。真人 reviewer 仍需对可见的科学含义、provenance、可读性和最终交付决定负责。

## 完成一个批准结果

把会话路径和人工字段替换为本次试运行的真实值：

```powershell
pwsh -NoProfile -ExecutionPolicy Bypass -File scripts/run-scientific-figure-operator-trial.ps1 `
  -Mode Finalize `
  -SessionPath outputs/scientific-figure-operator-trials/<run-id> `
  -Outcome accepted `
  -Reviewer "<真人审查人>" `
  -Notes "<检查内容和批准理由>" `
  -ConfirmFiveWorkspaces
```

accepted finalize 会校验 ZIP 必需条目、安全相对路径、Gate 2 reviewer 以及 SVG/PNG/PDF hash，随后在会话本地 `trial.json` 中记录 `operator/manual evidence`。

## 完成一个拒绝结果

```powershell
pwsh -NoProfile -ExecutionPolicy Bypass -File scripts/run-scientific-figure-operator-trial.ps1 `
  -Mode Finalize `
  -SessionPath outputs/scientific-figure-operator-trials/<run-id> `
  -Outcome rejected `
  -Reviewer "<真人审查人>" `
  -Notes "<阻断问题和所需纠正>" `
  -ConfirmFiveWorkspaces
```

rejected 试运行不会制造 accepted package。它是一次有效的人工拒绝记录，不是交付验收。

## 会话文件与清理

每个会话包含：

- `trial.json`：生命周期、声明路径、人工结果和校验摘要
- `operator-checklist.md`：人工操作步骤
- `data/`：隔离的 SQLite 和应用本地状态
- `delivery/`：操作员选择的 ZIP 目标目录

整个会话都被 Git 忽略。作为证据时应保留；废弃会话必须先关闭应用，再只删除该次会话的精确目录。不得用 Git 回滚来表述运行时数据的删除或恢复。

## 已验收边界

本试运行只覆盖现有确定性 fake 样本和 WPF 工作流。它不覆盖 OCR-heavy 来源、实测或伪造数据图、显微镜式观测、自动改变科学含义，或把生成图像表述为真实实验观测证据；也不授权 provider 调用、发布或对第三方系统执行桌面自动化。
