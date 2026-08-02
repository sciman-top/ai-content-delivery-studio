# 文章图组修复后的盲视觉问答证据（2026-08-01）

## 目的

修复或优化渲染、科学关系和审查代码后，必须重新做一次不读取审查元数据的视觉问答。这个探针用于验证真实呈现是否仍能被独立读出，不能替代确定性物理校验、物理专家 Gate 1 或 live provider acceptance。

## Fresh run

- 源文章：`paper/眼睛直接观察凸透镜成像时的各种问题（王耀强）.pdf`
- 当前代码 fresh 图组：`%LOCALAPPDATA%/ContentDeliveryStudio/workspace/article-figure-runs/20260801-120417-complete-set/`
- 盲测目录：`%LOCALAPPDATA%/ContentDeliveryStudio/workspace/article-figure-runs/20260801-120417-complete-set/blind-review/`
- 盲测输入：6 个按内容 SHA-256 前缀匿名命名的 PNG；没有加载 `article-figure-set-report.json`、`*.visual-review.json`、计划 JSON 或源 PDF。
- 盲测记录：`blind-questionnaire.json`、`blind-answers.json`
- provider 边界：交互式系统视觉检查；没有 live/付费 provider 调用，也没有读取 secret。

## Blind questions and observed result

统一问题是：

1. 只根据图像描述对象、文字、箭头方向和空间顺序；
2. 检查文字裁切、标签重叠、空白输出、素材伪造或明显布局问题；
3. 说明最重要的可见科学关系，无法由图像确定的内容标记 `Uncertain`；
4. 标记可能被误读为定量、医学或因果结论的内容。

结果：

- 6/6 图的对象、标签、方向和关键空间关系可直接读出；
- 6/6 图的 presentation 与 visible-relationship 结果为 `Pass`；
- 可见裁切、标签重叠、空白输出和来源照片缺失：0 项；
- 公式图直接显示 `y=x/(x+1), x>0` 与 `y=x/(1-x), 0<x<1`，并显示正距离约定；
- 位置比较图直接显示 `L2` 在 `S` 右侧、共面、左侧三种状态及到达 `L2` 前的光束状态；
- 附加透镜图直接显示凹透镜、到达 `L2` 前更发散、焦点 B 相对焦点 A 后移且不重合；
- 来源证据板保留 6 个来源照片和页码，不将生成图冒充实验照片。

## Authority boundary

- 盲测只证明当前 PNG 在没有 expected-check 元数据帮助时仍然可读，并暴露明显呈现缺陷。
- `scientificAcceptance=PendingHumanApproval`：图中公式分支、符号约定、透镜类型、平面顺序和焦点移动仍须物理专家 Gate 1 逐项签字。
- `visualReviewProvider=fake-scientific-visual`：fake provider 只证明应用合同路径，不是 live 多模态模型判定。
- `gateTwo=NotRun`、`deliveryStatus=not-created`：候选不是最终交付图片。

## Re-run rule

以下任一变化后都应再次生成匿名盲测包并回答同一组问题：渲染器、公式/符号、光路/关系、透镜类型、来源照片映射、文字布局或视觉审查 payload。仅改变输出路径时可复用内容哈希，但仍应验证路径和报告身份未漂移。
