using System.Security.Cryptography;
using System.Text;
using ContentDeliveryStudio.Core.ScientificFigures;

namespace ContentDeliveryStudio.Application.ScientificFigures;

/// <summary>
/// Produces evidence-bound, non-authoritative figure proposals from a scientific article.
/// A proposal is deliberately not a <see cref="ScientificFigureSpec"/>: the latter still
/// requires an explicit Gate 1 decision before it may enter the render-and-delivery path.
/// </summary>
public sealed class ArticleScientificFigurePlanningService
{
    public IReadOnlyList<ArticleScientificFigureCandidate> Plan(
        ScientificDocumentExtraction extraction,
        string articleTitle,
        string audience)
    {
        ArgumentNullException.ThrowIfNull(extraction);
        if (extraction.Status != ScientificExtractionStatus.Ready)
        {
            throw new InvalidOperationException(
                "A blocked extraction cannot produce article figure candidates.");
        }

        var normalizedTitle = RequireText(articleTitle, nameof(articleTitle));
        var normalizedAudience = RequireText(audience, nameof(audience));
        if (IsGravityArticle(normalizedTitle, extraction))
        {
            return PlanGravity(extraction, normalizedTitle, normalizedAudience);
        }

        if (IsThermalArticle(normalizedTitle, extraction))
        {
            return PlanThermal(extraction, normalizedTitle, normalizedAudience);
        }

        if (IsThermistorArticle(normalizedTitle, extraction))
        {
            return PlanThermistor(extraction, normalizedTitle, normalizedAudience);
        }

        if (IsArchimedesArticle(normalizedTitle, extraction))
        {
            return PlanArchimedes(extraction, normalizedTitle, normalizedAudience);
        }

        if (IsBernoulliArticle(normalizedTitle, extraction))
            return PlanBernoulli(extraction, normalizedTitle, normalizedAudience);
        if (IsPinholeArticle(normalizedTitle, extraction))
            return PlanPinhole(extraction, normalizedTitle, normalizedAudience);
        if (IsSuperconductingArticle(normalizedTitle, extraction))
            return PlanSuperconducting(extraction, normalizedTitle, normalizedAudience);

        if (!IsOpticalArticle(normalizedTitle, extraction))
        {
            throw new InvalidOperationException(
                "The scientific article domain is unsupported; no figure profile was selected.");
        }

        var candidates = new List<ArticleScientificFigureCandidate>();
        AddIfEvidenceFound(
            candidates,
            "secondary-lens-imaging-path",
            ArticleScientificFigureCandidateKind.Mechanism,
            "二次凸透镜成像的光路关系",
            "把主凸透镜形成的中间像 S 作为眼睛的物体，展示观察光路的二次成像关系。",
            "用一张非按比例的光路示意图区分主凸透镜、中间像 S、眼睛和视网膜；仅作为待核验的候选规格。",
            ScientificFigureRiskLevel.High,
            "二次凸透镜成像",
            extraction.Blocks,
            ["图1", "图2"],
            ArticleScientificFigureDisposition.ReplaceExisting,
            "替换低清手绘光路，并把中间像、眼睛晶状体与视网膜方向分层标注。");
        AddIfEvidenceFound(
            candidates,
            "thin-lens-equation-graph",
            ArticleScientificFigureCandidateKind.LensEquationGraph,
            "薄透镜公式与共轭关系图",
            "在明确符号约定的前提下呈现文章使用的薄透镜关系和函数分支。",
            "用清晰坐标、公式和适用区域替换第 3 页难以辨认的函数图，不把文章的数值示例自动确认为人眼参数。",
            ScientificFigureRiskLevel.High,
            "函数图象",
            extraction.Blocks,
            ["第3页函数图象"],
            ArticleScientificFigureDisposition.ReplaceExisting,
            "重绘坐标、渐近关系和符号约定，避免原图尺寸过小与变量定义不清。");
        AddIfEvidenceFound(
            candidates,
            "retina-screen-experiment-comparison",
            ArticleScientificFigureCandidateKind.ExperimentalComparison,
            "实验中的光屏与视网膜成像对照",
            "用实验装置解释文章对光屏成像和眼睛视网膜成像的对比。",
            "将实验目的、代表眼睛的第二凸透镜、光屏/视网膜和清晰度对比拆开表示；实验结论仍须 Gate 1 核验。",
            ScientificFigureRiskLevel.High,
            "实验",
            extraction.Blocks,
            ["图3", "图4", "图5"],
            ArticleScientificFigureDisposition.ReplaceExisting,
            "将现场照片和手绘装置拆成可追踪的光屏、第二透镜和固定接收面关系。");
        AddIfEvidenceFound(
            candidates,
            "eye-position-visibility-comparison",
            ArticleScientificFigureCandidateKind.Comparison,
            "观察位置与可见性/清晰度的比较",
            "围绕中间像 S 比较眼睛位于不同相对位置时，文章提出的可见性和清晰度现象。",
            "把“能否看见”“是否清晰”与文章中的观察位置分栏呈现，不把近点、焦距或视觉结论自动确认为事实。",
            ScientificFigureRiskLevel.High,
            "眼睛在",
            extraction.Blocks,
            ["图6", "图7", "图8"],
            ArticleScientificFigureDisposition.AddExplanatoryReplacement,
            "用位置和光束会聚状态解释原照片条件；视觉清晰度结论仍待逐项 Gate 1。");
        AddIfEvidenceFound(
            candidates,
            "corrective-lens-control",
            ArticleScientificFigureCandidateKind.CorrectiveLensControl,
            "附加透镜改变入眼光束会聚状态的对照",
            "比较加入附加透镜前后进入相机或眼睛模型的光束会聚状态。",
            "只表示附加透镜改变光束会聚度的控制变量，不自动接受度数、清晰度或医学结论。",
            ScientificFigureRiskLevel.High,
            "近视眼镜",
            extraction.Blocks,
            ["图9"],
            ArticleScientificFigureDisposition.AddExplanatoryReplacement,
            "保留图9为来源照片，另加变量受控的确定性光路示意。");
        AddIfEvidenceFound(
            candidates,
            "source-observation-evidence-board",
            ArticleScientificFigureCandidateKind.SourceEvidenceBoard,
            "原文实验照片证据板",
            "从 PDF 内嵌像素提取并整理由原文提供的装置与观察照片。",
            "只做来源保真的裁切、排版和编号，不生成或修改实验现象。",
            ScientificFigureRiskLevel.High,
            "照片",
            extraction.Blocks,
            ["图3", "图4", "图6", "图7", "图8", "图9"],
            ArticleScientificFigureDisposition.ConsolidateSourceEvidence,
            "把分散照片整理成可比较证据板；照片本身不是因果证明。");

        if (candidates.Count < 6)
        {
            throw new InvalidOperationException(
                "The article did not expose enough located evidence for the complete optical figure set.");
        }

        return candidates.Select((candidate, index) => candidate with
        {
            CandidateId = $"article-{StableSlug(normalizedTitle)}-{index + 1:D2}-{candidate.Kind.ToString().ToLowerInvariant()}",
            ArticleTitle = normalizedTitle,
            Audience = normalizedAudience,
        }).ToArray();
    }

    private static IReadOnlyList<ArticleScientificFigureCandidate> PlanThermal(
        ScientificDocumentExtraction extraction,
        string articleTitle,
        string audience)
    {
        var candidates = new List<ArticleScientificFigureCandidate>();
        AddIfEvidenceFound(candidates, "thermal-snow-front", ArticleScientificFigureCandidateKind.ThermalFrontMechanism,
            "暖湿空气与寒冷空气相遇形成降雪", "解释锋面抬升、凝华成雪与高空放热的位置关系。",
            "用剖面示意图区分高空成雪与地面气温，避免把高空放热直接等同于地面升温。",
            ScientificFigureRiskLevel.High, "凝华成雪", extraction.Blocks, ["图1"],
            ArticleScientificFigureDisposition.ReplaceExisting, "重绘原文图1的锋面和气流关系。");
        AddIfEvidenceFound(candidates, "thermal-basin-exception", ArticleScientificFigureCandidateKind.ThermalBasinException,
            "盆地地形下的下雪特例", "解释盆地、高山南坡或大峡谷中寒冷空气滞留时的局地例外。",
            "用地形剖面表示冷空气越过高山后未快速下沉、地面仍较暖的特例，不把特例推广为一般规律。",
            ScientificFigureRiskLevel.High, "盆地", extraction.Blocks, ["图2"],
            ArticleScientificFigureDisposition.ReplaceExisting, "重绘原文图2的山谷地形与冷空气滞留关系。");
        AddIfEvidenceFound(candidates, "thermal-conductivity-data", ArticleScientificFigureCandidateKind.ThermalConductivityComparison,
            "空气、水蒸气、液态水与棉毛的导热系数比较", "将文章给出的导热系数数据转成可读的比较图。",
            "保留原文数据与单位；突出水蒸气与空气接近、液态水更强以及潮湿棉毛导热增大的条件链。",
            ScientificFigureRiskLevel.High, "导热系数", extraction.Blocks, ["表1"],
            ArticleScientificFigureDisposition.ReplaceExisting, "把原文小表格重绘为带单位的水平条形图。");
        AddIfEvidenceFound(candidates, "thermal-transfer-modes", ArticleScientificFigureCandidateKind.ThermalTransferModes,
            "冬夏散热方式的主导差异", "比较热传导、热对流、热辐射和相变潜热在冬夏语境中的作用。",
            "用四种传热方式与季节场景的关系图组织文章小结，不把示意强度当作定量测量。",
            ScientificFigureRiskLevel.High, "热对流", extraction.Blocks, ["第4节"],
            ArticleScientificFigureDisposition.AddExplanatoryReplacement, "把分散的传热文字组织成四模块图。");
        AddIfEvidenceFound(candidates, "thermal-humidity-clothing", ArticleScientificFigureCandidateKind.ThermalHumidityClothing,
            "相对湿度、潮湿衣物与湿冷体感", "解释融雪和湿冷中衣物潮湿导致保温性下降的链条。",
            "只表达空气湿度、衣物含水、导热增大和人体散热加快之间的文章主张，不伪造体感测量。",
            ScientificFigureRiskLevel.High, "相对湿度", extraction.Blocks, ["第五节"],
            ArticleScientificFigureDisposition.AddExplanatoryReplacement, "将湿冷与融雪的因果链拆成闭环箭头图。");
        AddIfEvidenceFound(candidates, "thermal-dry-wet-heat", ArticleScientificFigureCandidateKind.ThermalDryWetHeat,
            "干热与湿热的汗液蒸发对照", "解释相对湿度如何改变汗液蒸发和夏季体感。",
            "用同一人体散热入口对照干热快速蒸发与湿热蒸发受阻，避免把温度本身改写成湿度结论。",
            ScientificFigureRiskLevel.High, "汗液的蒸发", extraction.Blocks, ["第六节"],
            ArticleScientificFigureDisposition.AddExplanatoryReplacement, "将文章末节的干热/湿热例子转为对照图。");

        var sourceEvidence = extraction.Blocks
            .Where(block => !string.IsNullOrWhiteSpace(block.OriginalText))
            .Take(3)
            .Select(block => ArticleScientificFigureEvidence.Create(block, excerptLength: 240))
            .ToArray();
        if (sourceEvidence.Length > 0)
        {
            candidates.Add(new ArticleScientificFigureCandidate(
                "candidate-thermal-source-evidence-board",
                articleTitle,
                ArticleScientificFigureCandidateKind.SourceEvidenceBoard,
                "原文热学图表证据板",
                "保留文章中的原始手绘图和导热系数表作为来源证据。",
                "只做来源保真的提取与排版，不把原始图表当成新的科学证明。",
                audience,
                ScientificFigureRiskLevel.High,
                sourceEvidence,
                ["图1", "图2", "表1"],
                ArticleScientificFigureDisposition.ConsolidateSourceEvidence,
                "保留原文热学图表的像素证据，确定性重绘另行交付。",
                RequiresGateOneApproval: true,
                GateOneStatus: ArticleScientificFigureGateStatus.PendingHumanApproval,
                DeliveryStatus: ArticleScientificFigureDeliveryStatus.NotCreated));
        }

        if (candidates.Count < 6)
        {
            throw new InvalidOperationException(
                "The thermal article did not expose enough located evidence for the complete figure set.");
        }

        return candidates.Select((candidate, index) => candidate with
        {
            CandidateId = $"article-{StableSlug(articleTitle)}-{index + 1:D2}-{candidate.Kind.ToString().ToLowerInvariant()}",
            ArticleTitle = articleTitle,
            Audience = audience,
        }).ToArray();
    }

    private static IReadOnlyList<ArticleScientificFigureExternalReference> GravityExternalReferences() =>
    [
        new(
            "NASA",
            "What is Microgravity?",
            "https://www.nasa.gov/centers-and-facilities/glenn/what-is-microgravity/",
            "2026-08-20",
            "Adopted for the non-zero orbital gravity, shared free-fall, and near-zero scale-reading boundary."),
        new(
            "NIST",
            "NIST Guide to the SI, Chapter 8.3 Weight",
            "https://www.nist.gov/pml/special-publication-811/nist-guide-si-chapter-8",
            "2026-08-20",
            "Adopted for the ISO 80000-4-aligned reference-frame definition of weight and the Earth-rotation centrifugal term."),
    ];

    private static IReadOnlyList<ArticleScientificFigureCandidate> PlanGravity(
        ScientificDocumentExtraction extraction,
        string articleTitle,
        string audience)
    {
        var candidates = new List<ArticleScientificFigureCandidate>();
        AddIfEvidenceFound(candidates, "gravity-terms", ArticleScientificFigureCandidateKind.GravityTerminology,
            "引力、有效重力与秤读数的术语边界", "先声明参考系和术语约定，再比较 gravitation、gravity、weight 与秤读数。",
            "地球引力、给定参考系中的有效重力和支持力/拉力读数是不同物理量；不能只凭英文单词互相替代。",
            ScientificFigureRiskLevel.High, "Gravitation", extraction.Blocks, ["第二部分"],
            ArticleScientificFigureDisposition.AddExplanatoryReplacement, "用标准化术语卡片替代把 weight 直接等同于秤读数的混合定义。");
        AddIfEvidenceFound(candidates, "gravity-orbit", ArticleScientificFigureCandidateKind.GravityOrbitFreeFall,
            "轨道中的共同自由落体与失重", "解释空间站内秤读数接近零而地球引力和轨道加速度仍不为零。",
            "轨道器、物体和秤共同自由落体；地球引力提供向心加速度，支持力接近零。",
            ScientificFigureRiskLevel.High, "空间站", extraction.Blocks, ["题例 1"],
            ArticleScientificFigureDisposition.AddExplanatoryReplacement, "纠正以秤读数为零推断轨道 g 为零的概念跳跃。");
        AddIfEvidenceFound(candidates, "gravity-elevator", ArticleScientificFigureCandidateKind.GravityElevatorFreeFall,
            "自由落体电梯中的力与秤读数", "在同一图中区分地球引力、物体加速度和秤的支持力。",
            "当电梯与物体以 a 约等于 g 共同下落时，地球引力仍存在，而支持力 N 约等于零。",
            ScientificFigureRiskLevel.High, "自由下落的电梯", extraction.Blocks, ["题例 2"],
            ArticleScientificFigureDisposition.AddExplanatoryReplacement, "把真实力和随动参考系中的惯性力分栏，避免混用。");
        AddIfEvidenceFound(candidates, "gravity-earth-rotation", ArticleScientificFigureCandidateKind.GravitySurfaceRotation,
            "地表自转与有效重力", "表示地球引力、自转离心项、有效重力和地面支持力之间的关系。",
            "在地球固连旋转参考系中，有效重力由引力场与离心项合成；向心加速度不是额外的一种相互作用力。",
            ScientificFigureRiskLevel.High, "地球表面的物体", extraction.Blocks, ["题例 3"],
            ArticleScientificFigureDisposition.AddExplanatoryReplacement, "重绘原文地球图并明确旋转轴、纬度和矢量角色。");
        AddIfEvidenceFound(candidates, "gravity-case-comparison", ArticleScientificFigureCandidateKind.GravityCaseComparison,
            "三种场景中的引力、加速度与秤读数", "并排比较轨道、自由落体电梯和地表静止三个场景。",
            "三种场景都受地球引力；是否失重由支持力/秤读数判断，不能由引力是否存在判断。",
            ScientificFigureRiskLevel.High, "重量为零", extraction.Blocks, ["题例 1", "题例 2", "题例 3"],
            ArticleScientificFigureDisposition.AddExplanatoryReplacement, "用统一列名消除同词异义和跨参考系比较。");
        AddIfEvidenceFound(candidates, "gravity-frame-rules", ArticleScientificFigureCandidateKind.GravityReferenceFrames,
            "惯性系与非惯性系的受力记账规则", "给出两套不可混用的受力分析规则。",
            "惯性系只画真实相互作用力；随动或旋转非惯性系可加入惯性力，但必须声明参考系。",
            ScientificFigureRiskLevel.High, "非惯性系", extraction.Blocks, ["第三部分"],
            ArticleScientificFigureDisposition.AddExplanatoryReplacement, "把参考系选择变成显式步骤，阻止重复计入向心力或离心力。");

        var sourceEvidence = extraction.Blocks
            .Where(block => !string.IsNullOrWhiteSpace(block.OriginalText))
            .Take(3)
            .Select(block => ArticleScientificFigureEvidence.Create(block, excerptLength: 240))
            .ToArray();
        if (sourceEvidence.Length > 0)
        {
            candidates.Add(new ArticleScientificFigureCandidate(
                "candidate-gravity-source-evidence-board",
                articleTitle,
                ArticleScientificFigureCandidateKind.SourceEvidenceBoard,
                "原文重力图表证据板",
                "保留题图、地球自转受力图和 ISO 摘录作为来源证据。",
                "只做来源保真的提取与排版，不把原图中的术语或箭头自动确认为科学事实。",
                audience,
                ScientificFigureRiskLevel.High,
                sourceEvidence,
                ["开篇题图", "地球自转图", "ISO 80000-4 摘录"],
                ArticleScientificFigureDisposition.ConsolidateSourceEvidence,
                "保留原文像素证据；科学性更正由确定性重绘图承担。",
                RequiresGateOneApproval: true,
                GateOneStatus: ArticleScientificFigureGateStatus.PendingHumanApproval,
                DeliveryStatus: ArticleScientificFigureDeliveryStatus.NotCreated));
        }

        if (candidates.Count < 7)
        {
            throw new InvalidOperationException(
                "The gravity article did not expose enough located evidence for the complete figure set.");
        }

        var externalReferences = GravityExternalReferences();
        return candidates.Select((candidate, index) => candidate with
        {
            CandidateId = $"article-{StableSlug(articleTitle)}-{index + 1:D2}-{candidate.Kind.ToString().ToLowerInvariant()}",
            ArticleTitle = articleTitle,
            Audience = audience,
            ExternalScientificReferences = externalReferences,
        }).ToArray();
    }

    private static IReadOnlyList<ArticleScientificFigureCandidate> PlanThermistor(
        ScientificDocumentExtraction extraction,
        string articleTitle,
        string audience)
    {
        var candidates = new List<ArticleScientificFigureCandidate>();
        AddIfEvidenceFound(candidates, "thermistor-circuit-divider", ArticleScientificFigureCandidateKind.ThermistorCircuitDivider,
            "热敏电阻分压电路与电流变化", "把电源、定值电阻、热敏电阻和电压表的测量对象画清楚。",
            "电压表测热敏电阻两端电压；串联电流随热敏电阻变化，不能把 ΔR=ΔU/I 当作恒流公式。",
            ScientificFigureRiskLevel.High, "热敏电阻", extraction.Blocks, ["图甲"],
            ArticleScientificFigureDisposition.ReplaceExisting, "重绘题图电路并标出分压关系与变电流边界。");
        AddIfEvidenceFound(candidates, "thermistor-curvature", ArticleScientificFigureCandidateKind.ThermistorCurvature,
            "分压函数的曲率与等电压间隔", "比较 U-R 凹函数和等电压间隔对应的电阻变化。",
            "U=U总R1/(R0+R1) 对 R1 是递增凹函数；相同电压变化对应的 ΔR 随 R1 增大而增大。",
            ScientificFigureRiskLevel.High, "凹函数", extraction.Blocks, ["图乙", "函数图象"],
            ArticleScientificFigureDisposition.AddExplanatoryReplacement, "用函数图替代原文小图并显式标注斜率递减。");
        AddIfEvidenceFound(candidates, "thermistor-error", ArticleScientificFigureCandidateKind.ThermistorError,
            "错误近似：把变电流当作恒流", "解释 ΔR=ΔU/I 只有在同一恒定电流下才可直接使用。",
            "电流 I=U总/(R0+R1) 会随 R1 变化；本题不能用变化前或变化后的电流代替全过程电流。",
            ScientificFigureRiskLevel.High, "错误原因", extraction.Blocks, ["错误解法"],
            ArticleScientificFigureDisposition.AddExplanatoryReplacement, "用两条电流状态和公式条件卡阻断恒流误用。");
        AddIfEvidenceFound(candidates, "thermistor-special-values", ArticleScientificFigureCandidateKind.ThermistorSpecialValues,
            "特殊值与极限法交叉验证", "用一个自洽参数例和两端极限验证选择题方向。",
            "R1→0 时分压趋近 0，R1→∞ 时分压趋近 U总；示例参数只能作为方向验证，不是原题实测参数。",
            ScientificFigureRiskLevel.High, "取特殊值", extraction.Blocks, ["解法 3", "解法 4"],
            ArticleScientificFigureDisposition.AddExplanatoryReplacement, "并列显示特殊值和极限边界，避免把示例参数当成题设。");
        AddIfEvidenceFound(candidates, "thermistor-source-evidence", ArticleScientificFigureCandidateKind.SourceEvidenceBoard,
            "原文热敏电阻题图证据板", "保留题图、电压温度图和原文推导作为来源证据。",
            "只保留来源像素与页码，不把原文结论或示例参数自动升级为事实。",
            ScientificFigureRiskLevel.High, "电路中", extraction.Blocks, ["图甲", "图乙"],
            ArticleScientificFigureDisposition.ConsolidateSourceEvidence, "保留原图证据，确定性重绘另行表达。");
        return CompleteProfile(candidates, articleTitle, audience, 5, "thermistor");
    }

    private static IReadOnlyList<ArticleScientificFigureCandidate> PlanArchimedes(
        ScientificDocumentExtraction extraction,
        string articleTitle,
        string audience)
    {
        var candidates = new List<ArticleScientificFigureCandidate>();
        AddIfEvidenceFound(candidates, "archimedes-definition", ArticleScientificFigureCandidateKind.ArchimedesDefinition,
            "V浸、V排与排开液体体积", "区分浸入体积、排开体积和底部贴合时的接触条件。",
            "V排通常等于物体浸入液体所占据的体积；但 ρ液V排g 等于液压合力需要所有相关表面与流体接触的条件。",
            ScientificFigureRiskLevel.High, "V排", extraction.Blocks, ["图2", "图3"],
            ArticleScientificFigureDisposition.AddExplanatoryReplacement, "用剖面和接触状态解释体积定义，不把蓝色涂色当成唯一 V排定义。");
        AddIfEvidenceFound(candidates, "archimedes-water-model", ArticleScientificFigureCandidateKind.ArchimedesWaterModel,
            "阿基米德原理的理想水体模型", "用同体积水体替换物体解释液压合力来源。",
            "在静止流体且表面均与流体接触的理想条件下，液压合力等于同体积流体的重力。",
            ScientificFigureRiskLevel.High, "理想模型", extraction.Blocks, ["图5"],
            ArticleScientificFigureDisposition.ReplaceExisting, "重绘压力分量与理想水体替换关系，并保留适用条件。");
        AddIfEvidenceFound(candidates, "archimedes-bottom-contact", ArticleScientificFigureCandidateKind.ArchimedesBottomContact,
            "底面贴合时的液压合力修正", "表示缺失底面液压力后，支持力与液压合力的受力平衡。",
            "底面完全贴合会改变流体压力边界；不能无条件套用 F浮=ρ液gV排，需明确接触界面和压力模型。",
            ScientificFigureRiskLevel.High, "贴合", extraction.Blocks, ["图1", "图2", "图3"],
            ArticleScientificFigureDisposition.AddExplanatoryReplacement, "把缺失压力项作为条件化修正，不把特例数值推广为普遍规律。");
        AddIfEvidenceFound(candidates, "archimedes-depth", ArticleScientificFigureCandidateKind.ArchimedesDepthDependence,
            "底面贴合下的深度依赖", "展示水深改变时底面压力项如何改变合力方向。",
            "在给定接触模型下，缺失的界面压力可能随深度变化；‘浮力与深度无关’不能脱离适用条件。",
            ScientificFigureRiskLevel.High, "水深", extraction.Blocks, ["第五部分"],
            ArticleScientificFigureDisposition.AddExplanatoryReplacement, "用参数轴表达条件化趋势，不把图示参数当成普遍实验定律。");
        AddIfEvidenceFound(candidates, "archimedes-top-contact", ArticleScientificFigureCandidateKind.ArchimedesTopContact,
            "顶部贴合时的压力方向", "对比顶部接触和底部接触时缺失压力项的方向差异。",
            "顶部或底部与固体贴合都会改变流体边界；压力方向必须依接触面和参考受力图判定。",
            ScientificFigureRiskLevel.High, "顶部", extraction.Blocks, ["第六部分"],
            ArticleScientificFigureDisposition.AddExplanatoryReplacement, "用边界条件标签避免把顶部修正与底部修正混为同一公式。");
        AddIfEvidenceFound(candidates, "archimedes-pier", ArticleScientificFigureCandidateKind.ArchimedesPier,
            "倾斜桥墩侧向压力抵消", "用同一高度截面的反向压力说明桥墩净竖直合力的几何原因。",
            "对称且各高度横截面相同的倾斜桥墩，侧面压力的竖直分量需通过积分和边界条件核验；不能只看一块蓝色体积。",
            ScientificFigureRiskLevel.High, "桥墩", extraction.Blocks, ["图1", "图2"],
            ArticleScientificFigureDisposition.AddExplanatoryReplacement, "重绘压力分量并把‘需积分核验’写入审查边界。");
        AddIfEvidenceFound(candidates, "archimedes-pressure-caveat", ArticleScientificFigureCandidateKind.ArchimedesPressureCaveat,
            "大气压与接触界面的实验边界", "区分表压模型、绝对压强和固体接触反力。",
            "大气压是否计入取决于完整边界和受力系统；不能仅凭一个 p0S 项断言所有实验支持力都变成同一数值。",
            ScientificFigureRiskLevel.High, "大气压", extraction.Blocks, ["第七部分"],
            ArticleScientificFigureDisposition.AddExplanatoryReplacement, "把文章的绝对压强修正标为待 Gate 1 核验的模型，而不是自动结论。");
        AddIfEvidenceFound(candidates, "archimedes-source-evidence", ArticleScientificFigureCandidateKind.SourceEvidenceBoard,
            "原文阿基米德图表证据板", "保留桥墩、石鼓、水体模型和接触图作为来源证据。",
            "来源板只保留原文像素和页码；争议公式由确定性重绘图分离表达。",
            ScientificFigureRiskLevel.High, "浮力", extraction.Blocks, ["图1", "图2", "图3", "图4", "图5", "图6"],
            ArticleScientificFigureDisposition.ConsolidateSourceEvidence, "保留原图证据，避免来源像素被误认为已验证结论。");
        return CompleteProfile(candidates, articleTitle, audience, 8, "archimedes");
    }

    private static IReadOnlyList<ArticleScientificFigureCandidate> CompleteProfile(
        List<ArticleScientificFigureCandidate> candidates,
        string articleTitle,
        string audience,
        int minimumCount,
        string profile)
    {
        if (candidates.Count < minimumCount)
        {
            throw new InvalidOperationException(
                $"The {profile} article did not expose enough located evidence for the complete figure set: "
                + string.Join(", ", candidates.Select(candidate => candidate.Kind)));
        }

        return candidates.Select((candidate, index) => candidate with
        {
            CandidateId = $"article-{StableSlug(articleTitle)}-{index + 1:D2}-{candidate.Kind.ToString().ToLowerInvariant()}",
            ArticleTitle = articleTitle,
            Audience = audience,
        }).ToArray();
    }

    private static bool IsGravityArticle(
        string articleTitle,
        ScientificDocumentExtraction extraction) =>
        articleTitle.Contains("重力", StringComparison.Ordinal)
        || extraction.Blocks.Any(block =>
            block.OriginalText?.Contains("Gravitation", StringComparison.Ordinal) == true
            && block.OriginalText.Contains("Weight", StringComparison.Ordinal));

    private static bool IsThermalArticle(
        string articleTitle,
        ScientificDocumentExtraction extraction) =>
        articleTitle.Contains("下雪", StringComparison.Ordinal)
        || articleTitle.Contains("融雪", StringComparison.Ordinal)
        || extraction.Blocks.Any(block =>
            block.OriginalText?.Contains("相对湿度", StringComparison.Ordinal) == true
            && block.OriginalText.Contains("传热", StringComparison.Ordinal));

    private static bool IsOpticalArticle(
        string articleTitle,
        ScientificDocumentExtraction extraction) =>
        articleTitle.Contains("凸透镜", StringComparison.Ordinal)
        || articleTitle.Contains("光路", StringComparison.Ordinal)
        || extraction.Blocks.Any(block =>
            block.OriginalText?.Contains("二次凸透镜成像", StringComparison.Ordinal) == true
            && block.OriginalText.Contains("视网膜", StringComparison.Ordinal));

    private static bool IsThermistorArticle(
        string articleTitle,
        ScientificDocumentExtraction extraction) =>
        articleTitle.Contains("新疆物理中考", StringComparison.Ordinal)
        || (extraction.Blocks.Any(block => block.OriginalText?.Contains("热敏电阻", StringComparison.Ordinal) == true)
            && extraction.Blocks.Any(block => block.OriginalText?.Contains("电压表", StringComparison.Ordinal) == true));

    private static bool IsArchimedesArticle(
        string articleTitle,
        ScientificDocumentExtraction extraction) =>
        articleTitle.Contains("阿基米德", StringComparison.Ordinal)
        || extraction.Blocks.Any(block =>
            block.OriginalText?.Contains("V 排", StringComparison.Ordinal) == true
            && block.OriginalText.Contains("浮力", StringComparison.Ordinal));

    private static bool IsBernoulliArticle(string title, ScientificDocumentExtraction extraction) =>
        title.Contains("伯努利", StringComparison.Ordinal) || extraction.Blocks.Any(b => b.OriginalText?.Contains("电吹风", StringComparison.Ordinal) == true);
    private static bool IsPinholeArticle(string title, ScientificDocumentExtraction extraction) =>
        title.Contains("小孔成像", StringComparison.Ordinal) || extraction.Blocks.Any(b => b.OriginalText?.Contains("手动对焦", StringComparison.Ordinal) == true);
    private static bool IsSuperconductingArticle(string title, ScientificDocumentExtraction extraction) =>
        title.Contains("超导磁体", StringComparison.Ordinal) || extraction.Blocks.Any(b => b.OriginalText?.Contains("超导线圈", StringComparison.Ordinal) == true);

    private static IReadOnlyList<ArticleScientificFigureCandidate> PlanProfile(
        ScientificDocumentExtraction extraction, string title, string audience, string profile,
        params (string seed, ArticleScientificFigureCandidateKind kind, string heading, string objective, string message, string keyword, string[] refs)[] specs)
    {
        var candidates = new List<ArticleScientificFigureCandidate>();
        foreach (var s in specs)
            AddIfEvidenceFound(candidates, s.seed, s.kind, s.heading, s.objective, s.message, ScientificFigureRiskLevel.High, s.keyword, extraction.Blocks, s.refs, ArticleScientificFigureDisposition.ReplaceExisting, "用确定性 SVG 重绘原文关系并保留来源证据。");
        var source = extraction.Blocks.Where(b => !string.IsNullOrWhiteSpace(b.OriginalText)).Take(3).Select(b => ArticleScientificFigureEvidence.Create(b, 240)).ToArray();
        if (source.Length > 0) candidates.Add(new ArticleScientificFigureCandidate("candidate-source", title, ArticleScientificFigureCandidateKind.SourceEvidenceBoard, "原文来源证据板", "保留原文证据", "仅作来源保真排版，不新增科学结论", audience, ScientificFigureRiskLevel.High, source, ["原文页面"], ArticleScientificFigureDisposition.ConsolidateSourceEvidence, "保留原文像素证据。", true, ArticleScientificFigureGateStatus.PendingHumanApproval, ArticleScientificFigureDeliveryStatus.NotCreated));
        if (candidates.Count < 4) throw new InvalidOperationException($"The {profile} article did not expose enough located evidence for the complete figure set.");
        return candidates.Select((c, i) => c with { CandidateId = $"article-{StableSlug(title)}-{i + 1:D2}-{c.Kind.ToString().ToLowerInvariant()}", ArticleTitle = title, Audience = audience }).ToArray();
    }

    private static IReadOnlyList<ArticleScientificFigureCandidate> PlanBernoulli(ScientificDocumentExtraction e, string t, string a) => PlanProfile(e,t,a,"bernoulli",
        ("fan-energy", ArticleScientificFigureCandidateKind.BernoulliFanEnergy, "电吹风：风机对气流做功", "区分风机做功与同一流线上的伯努利比较", "风机输入电功，提高气流总能；不能跨流线套用流速越快压强越小", "外界做功", ["图1"]),
        ("fan-zones", ArticleScientificFigureCandidateKind.BernoulliFanZones, "吸风区、风机与压缩区", "标出吸风低压、风机和压缩高压区域", "风机位于能量输入边界，压力分区取决于结构", "吸风区", ["图2"]),
        ("streamline-boundary", ArticleScientificFigureCandidateKind.BernoulliStreamlineBoundary, "同一流线与静压边界", "说明静压比较的流线和出口边界条件", "A、C、D 不必在同一流线上；出口静压近似大气压", "同一流线", ["第2节"]));
    private static IReadOnlyList<ArticleScientificFigureCandidate> PlanPinhole(ScientificDocumentExtraction e, string t, string a) => PlanProfile(e,t,a,"pinhole",
        ("pinhole-geometry", ArticleScientificFigureCandidateKind.PinholeGeometry, "小孔成像几何关系", "展示倒立实像、物距像距与可视范围", "小孔筛选光线，物距较小时像面大且可视范围受限", "可视范围", ["图1"]),
        ("focus-plane", ArticleScientificFigureCandidateKind.PinholeFocusPlane, "相机手动对焦的成像平面", "区分对焦小孔、光源和像面", "相机对焦到哪里，清晰呈现的就是哪里的图像", "手动对焦", ["第3节"]),
        ("pinhole-observation", ArticleScientificFigureCandidateKind.PinholeObservation, "近距与远距观察对照", "对照光源距离改变时的可见范围", "相机靠近小孔且光源远离时更可能看到全景", "全景", ["图2", "图3"]));
    private static IReadOnlyList<ArticleScientificFigureCandidate> PlanSuperconducting(ScientificDocumentExtraction e, string t, string a) => PlanProfile(e,t,a,"superconducting",
        ("magnetic-energy", ArticleScientificFigureCandidateKind.SuperconductingEnergy, "励磁中的电能与磁能", "表示电流建立时能量进入磁场", "电流变化时磁场能建立；恒定电流不持续消耗电能来维持静磁场", "磁能", ["第1节", "第2节"]),
        ("persistent-current", ArticleScientificFigureCandidateKind.SuperconductingPersistentCurrent, "超导闭环持久电流", "区分撤去励磁电源与断开线圈回路", "撤去电源但线圈闭合，电流可持续；这不是把线圈断电", "闭合通路", ["第3节"]),
        ("mri-excitation", ArticleScientificFigureCandidateKind.SuperconductingExcitation, "MRI 超导磁体励磁过程", "重绘 heater、超导开关、励磁电源和线圈连接", "加热开关使其有电阻，励磁后冷却恢复超导，再切断励磁电源", "励磁电源", ["图1", "第4节"]));

    private static void AddIfEvidenceFound(
        ICollection<ArticleScientificFigureCandidate> candidates,
        string seed,
        ArticleScientificFigureCandidateKind kind,
        string title,
        string objective,
        string centralMessage,
        ScientificFigureRiskLevel riskLevel,
        string requiredKeyword,
        IReadOnlyList<ScientificSourceBlock> blocks,
        IReadOnlyList<string> sourceFigureReferences,
        ArticleScientificFigureDisposition disposition,
        string replacementRationale)
    {
        var evidence = blocks
            .Where(block => block.OriginalText?.Contains(requiredKeyword, StringComparison.Ordinal) == true)
            .Select(block => ArticleScientificFigureEvidence.Create(block, excerptLength: 240))
            .Take(3)
            .ToArray();
        if (evidence.Length == 0)
        {
            return;
        }

        candidates.Add(new ArticleScientificFigureCandidate(
            $"candidate-{seed}",
            string.Empty,
            kind,
            title,
            objective,
            centralMessage,
            string.Empty,
            riskLevel,
            evidence,
            sourceFigureReferences,
            disposition,
            replacementRationale,
            RequiresGateOneApproval: true,
            GateOneStatus: ArticleScientificFigureGateStatus.PendingHumanApproval,
            DeliveryStatus: ArticleScientificFigureDeliveryStatus.NotCreated));
    }

    private static string RequireText(string value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Value cannot be empty.", parameterName);
        }

        return value.Trim();
    }

    private static string StableSlug(string value)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        return Convert.ToHexString(bytes)[..12].ToLowerInvariant();
    }
}

public sealed record ArticleScientificFigureCandidate(
    string CandidateId,
    string ArticleTitle,
    ArticleScientificFigureCandidateKind Kind,
    string Title,
    string Objective,
    string CentralMessage,
    string Audience,
    ScientificFigureRiskLevel RiskLevel,
    IReadOnlyList<ArticleScientificFigureEvidence> Evidence,
    IReadOnlyList<string> SourceFigureReferences,
    ArticleScientificFigureDisposition Disposition,
    string ReplacementRationale,
    bool RequiresGateOneApproval,
    ArticleScientificFigureGateStatus GateOneStatus,
    ArticleScientificFigureDeliveryStatus DeliveryStatus)
{
    public IReadOnlyList<ArticleScientificFigureExternalReference> ExternalScientificReferences { get; init; } = [];
}

public sealed record ArticleScientificFigureExternalReference(
    string Publisher,
    string Title,
    string Url,
    string AccessedOn,
    string AdoptionDecision);

public sealed record ArticleScientificFigureEvidence(
    string SourceBlockId,
    int PageNumber,
    string Section,
    string Excerpt)
{
    public static ArticleScientificFigureEvidence Create(
        ScientificSourceBlock block,
        int excerptLength)
    {
        ArgumentNullException.ThrowIfNull(block);
        var text = block.OriginalText
            ?? throw new InvalidOperationException("Candidate evidence must retain source text.");
        var compact = string.Join(' ', text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
        var excerpt = compact.Length <= excerptLength
            ? compact
            : $"{compact[..excerptLength].TrimEnd()}…";
        return new ArticleScientificFigureEvidence(
            block.BlockId,
            block.Location.PageNumber,
            block.Location.Section,
            excerpt);
    }
}

public enum ArticleScientificFigureCandidateKind
{
    Mechanism = 0,
    Comparison = 1,
    ExperimentalComparison = 2,
    LensEquationGraph = 3,
    CorrectiveLensControl = 4,
    SourceEvidenceBoard = 5,
    ThermalFrontMechanism = 6,
    ThermalBasinException = 7,
    ThermalConductivityComparison = 8,
    ThermalTransferModes = 9,
    ThermalHumidityClothing = 10,
    ThermalDryWetHeat = 11,
    GravityTerminology = 12,
    GravityOrbitFreeFall = 13,
    GravityElevatorFreeFall = 14,
    GravitySurfaceRotation = 15,
    GravityCaseComparison = 16,
    GravityReferenceFrames = 17,
    ThermistorCircuitDivider = 18,
    ThermistorCurvature = 19,
    ThermistorError = 20,
    ThermistorSpecialValues = 21,
    ArchimedesDefinition = 22,
    ArchimedesWaterModel = 23,
    ArchimedesBottomContact = 24,
    ArchimedesDepthDependence = 25,
    ArchimedesTopContact = 26,
    ArchimedesPier = 27,
    ArchimedesPressureCaveat = 28,
    BernoulliFanEnergy = 29, BernoulliFanZones = 30, BernoulliStreamlineBoundary = 31,
    PinholeGeometry = 32, PinholeFocusPlane = 33, PinholeObservation = 34,
    SuperconductingEnergy = 35, SuperconductingPersistentCurrent = 36, SuperconductingExcitation = 37,
}

public enum ArticleScientificFigureDisposition
{
    ReplaceExisting = 0,
    AddExplanatoryReplacement = 1,
    ConsolidateSourceEvidence = 2,
}

public enum ArticleScientificFigureGateStatus
{
    PendingHumanApproval = 0,
}

public enum ArticleScientificFigureDeliveryStatus
{
    NotCreated = 0,
}
