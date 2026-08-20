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
旧目录可先预演、再安全迁移：

```powershell
pwsh -NoProfile -ExecutionPolicy Bypass -File scripts/organize-local-outputs.ps1 -WhatIf
pwsh -NoProfile -ExecutionPolicy Bypass -File scripts/organize-local-outputs.ps1
```

迁移脚本不会删除或合并历史；目标已存在时会停止。机器可读索引位于
`outputs/OUTPUT-CATALOG.json`。英文正式合同见 [../LOCAL_OUTPUTS.md](../LOCAL_OUTPUTS.md)。
