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
        if (IsThermalArticle(normalizedTitle, extraction))
        {
            return PlanThermal(extraction, normalizedTitle, normalizedAudience);
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
            "用地形剖面表示冷空气下沉受阻和地面仍较暖的特例，不把特例推广为一般规律。",
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

    private static bool IsThermalArticle(
        string articleTitle,
        ScientificDocumentExtraction extraction) =>
        articleTitle.Contains("下雪", StringComparison.Ordinal)
        || articleTitle.Contains("融雪", StringComparison.Ordinal)
        || extraction.Blocks.Any(block =>
            block.OriginalText?.Contains("相对湿度", StringComparison.Ordinal) == true
            && block.OriginalText.Contains("传热", StringComparison.Ordinal));

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
    ArticleScientificFigureDeliveryStatus DeliveryStatus);

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
