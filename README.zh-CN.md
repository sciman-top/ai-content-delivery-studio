# AI Content Delivery Studio

面向 Windows 的桌面内容交付工作台，当前只维护两条正式产品主链：受控图像系列生产，以及证据约束的科学图解。

```text
素材或需求 -> 规划 -> fake-first 生成 -> 结构化审查 -> 人工批准 -> 交付包
```

## 边界

- 文档插画只是进入两条主链的输入方式，不是第三套平台。
- 文章、海报、课件、报告等名称是场景配置和交付分类，不是独立 workflow engine。
- fake provider 是默认路径；真实或付费 provider 必须显式启用并获得当前授权。
- `workspace/`、`outputs/`、本地 SQLite、凭据和生成资产不进入 Git。
- 仓库测试通过不代表已完成新一轮付费 provider、人工、硬件或现场验收。

历史发布与 live provider 证据边界见 [docs/V1_LAUNCH_EVIDENCE.md](docs/V1_LAUNCH_EVIDENCE.md)。

## 验证

```powershell
# 受影响测试
.\scripts\verify-repo.ps1 -Mode Quick -TestFilter <focused-filter> -NoRestore

# 仓库收口
.\scripts\verify-repo.ps1 -Mode Full

# 发布收口
.\scripts\preflight-release.ps1
```

Quick 只运行一次构建和指定测试；Full 只运行非发布测试、reference contract 与 diff hygiene；Release 只调用一次 Full，再追加 release-only 测试、变更 C# 格式、扫描和 publish/package 检查。

## 文档入口

- [docs/zh-CN/README.md](docs/zh-CN/README.md)
- [docs/PRD_V1.md](docs/PRD_V1.md)
- [docs/TASKS.md](docs/TASKS.md)
- [docs/ROADMAP.md](docs/ROADMAP.md)
- [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md)
- [docs/AI_CODING_WORKFLOW.md](docs/AI_CODING_WORKFLOW.md)
- [docs/REFERENCE_BASIS.md](docs/REFERENCE_BASIS.md)

工程默认不再为普通切片创建 spec、plan、evidence receipt、machine queue 或新治理门禁；优先具体产品路径、行为测试和一次最低充分验证。
