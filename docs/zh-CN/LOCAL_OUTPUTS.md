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

文章图组脚本使用 `-OutputClass Workspace|Validation|ReviewReady` 选择非最终输出类别，
并统一写入 `article-figure-sets/<article-slug>/<run-name>`。例如：

```powershell
pwsh -NoProfile -ExecutionPolicy Bypass -File scripts/run-article-scientific-figure-set.ps1 `
  -SourcePath paper/example.pdf `
  -OutputClass ReviewReady `
  -ArticleSlug example `
  -RunName 20260820-v1
```

省略 `-ArticleSlug` 时使用源 PDF 文件名作为文章目录；重跑批次不会再平铺到分类根目录。
当每个候选都已通过声明的确定性及视觉检查后，经明确授权的操作员可同时批准
Gate 1/Gate 2。`authorized_agent` 应先写入与每个候选精确哈希绑定的逐图视觉回执
和人审最小化评估：

```powershell
pwsh -NoProfile -ExecutionPolicy Bypass -File scripts/review-article-scientific-figure-set.ps1 `
  -ReviewReadyDirectory outputs/review-ready/article-figure-sets/example/20260820-v1 `
  -Reviewer codex-agent-authorized-by-sciman `
  -AuthorizationReference "Codex task authorization: user message on 2026-08-20" `
  -Notes "已逐张检查 PNG、PDF 返渲染、科学关系和来源证据板。" `
  -ConfirmEveryCandidateVisuallyInspected
```

评估路由为 `AuthorizedAgentAccept` 时，仓库交付不再要求用户逐图复审或现场人工
审核。随后才通过唯一生产入口原子晋升：

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

报告不完整、未显式批准、授权 agent 缺少授权依据或当前哈希回执、sidecar 未
通过、权威输入或候选文件漂移、路径不安全、正式包已存在时都会 fail closed。
正式 SVG/PNG/PDF 进入
`figures/`，来源证据板和原始提取资产保留在 `evidence/`；`manifest.json` 用
SHA-256 绑定所有文件且不泄露本机绝对路径。`authorized_agent` 决策会如实记录，
不会被改写成 live provider 或独立真人物理专家验收。

`High` 风险或 fake-first 批次仍要求当前任务明确授权并由 agent 实际逐图检查，
不得成为长期无人值守批准。只有低/中风险且独立视觉 provider 通过的批次，才会
标记为“未来可配置常驻自动化”，而启用该策略仍需另行授权。要求独立专家认证时
始终升级真人专家。计划、报告、来源审计、SVG、PNG、PDF 或 sidecar 任一字节
变化，都会使原回执失效并恢复审核门禁。

旧目录可先预演、再安全迁移：

```powershell
pwsh -NoProfile -ExecutionPolicy Bypass -File scripts/organize-local-outputs.ps1 -WhatIf
pwsh -NoProfile -ExecutionPolicy Bypass -File scripts/organize-local-outputs.ps1
```

迁移脚本不会删除或合并历史；目标已存在时会停止。机器可读索引位于
`outputs/OUTPUT-CATALOG.json`，其中 `finalDeliveryPackages` 会从有效 manifest
动态发现正式包，`reviewReadyAssessments` 则直接列出审核路由以及是否仍需现场、
用户逐图或独立专家审核；`deliveries/README.txt` 也会随当前正式包列表重建，不会再写回
过期的“无交付”状态。英文正式合同见 [../LOCAL_OUTPUTS.md](../LOCAL_OUTPUTS.md)。
