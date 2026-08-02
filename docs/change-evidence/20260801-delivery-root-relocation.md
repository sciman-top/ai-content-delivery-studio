# 最终交付根迁移策略证据（2026-08-01）

## Decision

最终图片不再依赖 C 盘 app-local 目录作为唯一归宿。交付包现在支持独立的
`CONTENT_DELIVERY_STUDIO_DELIVERY_ROOT`，本机推荐值为：

```text
D:\CODE\classroom-answer-toolkit\正式交付\科学配图\文章图组
```

最终包结构为：

```text
<delivery-root>\<project-id>\<timestamp>\
  images\       # 只有 Gate 1/Gate 2 通过的最终图片
  prompts\
  metadata\
  composition\
  manifest.json
```

临时生成、编辑、crop、盲测和视觉审查仍留在：

```text
D:\CODE\ai-content-delivery-studio\workspace\
```

或由 `CONTENT_DELIVERY_STUDIO_DATA_ROOT` 指定的工作区；它们不能写入最终
`images/`。

本机启动前可显式设置两个非敏感路径变量（不写入 secret）：

```powershell
$env:CONTENT_DELIVERY_STUDIO_DATA_ROOT = 'D:\CODE\ai-content-delivery-studio'
$env:CONTENT_DELIVERY_STUDIO_DELIVERY_ROOT = 'D:\CODE\classroom-answer-toolkit\正式交付\科学配图\文章图组'
```

这样 SQLite、候选和审查临时文件进入生成仓库的忽略目录，只有通过正式审批的
交付包进入课堂答案工具仓的专用类别目录。

## Compatibility and safety

- 未设置 `CONTENT_DELIVERY_STUDIO_DELIVERY_ROOT` 时，行为保持兼容：最终包回落到
  当前数据根的 `deliveries/<project-id>/<timestamp>/`。
- `PushRootOverride` 仍用于测试/隔离运行，并优先保持临时根与 fallback 交付根在
  同一隔离树内；外部交付根只在真实部署配置中生效。
- 不迁移、不删除已有 C 盘或 `outputs/` 历史证据；候选图在 Gate 1/2 前不复制到
  目标仓库的正式交付目录。
- `D:\CODE\classroom-answer-toolkit` 当前工作树存在用户未提交资产，本切片不修改
  该仓库。

## Verification

本切片需要按仓库固定顺序执行 `build -> test -> contract/invariant -> hotspot`，并
用路径合同测试确认外部 delivery root 不会改变 workspace root。

本次显式 D 盘工作根实跑：

```text
D:\CODE\ai-content-delivery-studio\workspace\article-figure-runs\20260801-123604-complete-set
```

该运行生成 6 项候选、确定性 `article-optics-v1` 和 fake-first 视觉证据；目标仓的
`正式交付\科学配图\文章图组` 尚不存在，因此本次没有向目标仓写入任何文件，也没有
把候选误标为最终交付。

## Rollback

仅移除 `DeliveryRootEnvironmentVariable`、路径解析测试、架构说明和本证据文档；不
删除任何外部仓库文件或历史运行输出。
