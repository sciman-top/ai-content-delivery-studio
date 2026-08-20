# 本地输出与最终交付目录

仓库采用两套权限含义不同的根目录：

```text
deliveries/                    只存放通过 Gate 2 的不可变最终交付包
outputs/
  review-ready/                机器检查完成、等待人工门禁的候选
  validation/                  回归测试和 agent 验证批次
  workspace-history/           历史生成及来源提取工作区
  operator-trials/             操作员或人工试运行证据
  diagnostics/                 smoke、探针和验证产物
```

`outputs/` 永远不是最终交付根目录。渲染成功、确定性科学审查通过、fake provider
通过或 agent 视觉检查，最多只能把候选放进 `review-ready/`，不能越过 Gate 1/Gate 2
写进 `deliveries/`。

若希望桌面应用也使用仓库内清晰可见的最终交付根目录，应在启动前设置：

```powershell
$env:CONTENT_DELIVERY_STUDIO_DELIVERY_ROOT = 'D:\CODE\ai-content-delivery-studio\deliveries'
```

文章图组脚本使用 `-OutputClass Workspace|Validation|ReviewReady` 选择非最终输出类别。
当每个候选都已通过声明的确定性及视觉检查后，经明确授权的操作员可同时批准
Gate 1/Gate 2，并通过唯一生产入口原子晋升：

```powershell
pwsh -NoProfile -ExecutionPolicy Bypass -File scripts/promote-article-scientific-figure-set.ps1 `
  -ReviewReadyDirectory outputs/review-ready/article-figure-sets/example/20260820-v1 `
  -ArticleSlug example `
  -PackageId 20260820-v1 `
  -Reviewer codex-agent-authorized-by-sciman `
  -OperatorKind authorized_agent `
  -AuthorizationReference "Codex task authorization: user message on 2026-08-20" `
  -ApproveGateOne `
  -GateOneNotes "已核准所有候选的科学含义、条件、方向、数值和例外。" `
  -ApproveGateTwo `
  -GateTwoNotes "已逐张视觉抽查并核准正式交付。"
```

报告不完整、未显式批准、授权 agent 缺少授权依据、sidecar 未通过、文件缺失或
哈希漂移、路径不安全、正式包已存在时都会 fail closed。正式 SVG/PNG/PDF 进入
`figures/`，来源证据板和原始提取资产保留在 `evidence/`；`manifest.json` 用
SHA-256 绑定所有文件且不泄露本机绝对路径。`authorized_agent` 决策会如实记录，
不会被改写成 live provider 或独立真人物理专家验收。

旧目录可先预演、再安全迁移：

```powershell
pwsh -NoProfile -ExecutionPolicy Bypass -File scripts/organize-local-outputs.ps1 -WhatIf
pwsh -NoProfile -ExecutionPolicy Bypass -File scripts/organize-local-outputs.ps1
```

迁移脚本不会删除或合并历史；目标已存在时会停止。机器可读索引位于
`outputs/OUTPUT-CATALOG.json`，其中 `finalDeliveryPackages` 会从有效 manifest
动态发现正式包；`deliveries/README.txt` 也会随当前正式包列表重建，不会再写回
过期的“无交付”状态。英文正式合同见 [../LOCAL_OUTPUTS.md](../LOCAL_OUTPUTS.md)。
