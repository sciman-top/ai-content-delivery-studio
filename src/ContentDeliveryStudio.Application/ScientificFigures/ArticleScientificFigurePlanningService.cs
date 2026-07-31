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
