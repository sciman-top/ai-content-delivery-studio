# 科研绘图操作员试运行 CWD 修复与中文说明证据

英文原文：[20260728-scientific-figure-operator-trial-cwd-and-zh-cn.md](../../change-evidence/20260728-scientific-figure-operator-trial-cwd-and-zh-cn.md)

日期：2026-07-28

## 状态

`repo-side fix complete / human operator trial pending`

## 问题与根因

文档原来的相对命令只在 PowerShell 已位于仓库根目录时成立。用户从 `C:\Users\sciman` 执行后，PowerShell 会在该目录下寻找 `scripts/run-scientific-figure-operator-trial.ps1`，因此正确报告脚本不存在。

修复前，即使传入绝对脚本路径也不完整：脚本内部的 `Resolve-RepositoryRoot` 会针对调用者当前目录执行 `git rev-parse --show-toplevel`，从 Git 仓库外启动时仍会失败。

## 修复

- 使用 `git -C $PSScriptRoot`，让仓库发现绑定到已签入的脚本位置，而不是用户当前提示符。
- 新增从仓库外临时目录启动脚本的回归测试，并验证会话仍位于真实仓库根下。
- 同步更新英文和中文 runbook，提供可直接复制的绝对路径写法。
- 把科研绘图操作员试运行加入中文文档中心和双语治理清单。
- 为 agent-operated native WPF 证据补充中文伴随页，但不扩大英文 canonical evidence 的结论。

## 聚焦证据

新增回归测试在修复前以 exit code `1` 失败，修复后以 `1 / 1` 通过。

随后从 `C:\Users\sciman` 工作目录使用绝对脚本路径执行 `Mode Prepare`，命令 exit code 为 `0`，并创建：

`outputs/scientific-figure-operator-trials/external-cwd-probe-20260728-01`

该探针保持 `pending_operator`，没有启动 WPF、调用 provider 或制造人工证据。

## 真值边界

- repo-side 命令易用性：仓库根和仓库外工作目录都已修复。
- 中文操作说明：已提供，并从中文文档中心链接。
- human `operator/manual evidence`：仍待完成。
- 既有 `live accepted` evidence：未改变；provider 行为未变化，也没有刷新 live evidence。
- 生成的探针会话仍在被忽略的 `outputs/` 下，不进入 Git。

## 回滚与验证

Git 回滚只撤销本次脚本、测试、文档和证据切片；accepted artifact、provider contract、schema 和 live evidence 不受影响。ignored probe session 需要在 Git 之外单独保留或清理。

2026-07-28 的固定顺序门禁已通过：build 为 `0` warning / `0` error，tests 为 `673 / 673`，reference evidence、format、release preflight、嵌套仓库验证、publish WhatIf、placeholder/conflict scan 和 diff hygiene 均通过。写入该结果后，会对最终树复跑同一固定顺序，并确认 `.env`、accepted artifact、SQLite、workspace、output session 和 ZIP 均不进入 Git。
