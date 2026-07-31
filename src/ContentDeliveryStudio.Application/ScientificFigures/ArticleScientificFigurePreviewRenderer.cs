using System.Security.Cryptography;
using System.Text;

namespace ContentDeliveryStudio.Application.ScientificFigures;

/// <summary>
/// Renders an explicitly non-final, deterministic optical candidate preview.  It is useful
/// for Gate 1 review, but cannot substitute for a Gate-1-approved ScientificFigureSpec.
/// </summary>
public sealed class ArticleScientificFigurePreviewRenderer
{
    public ArticleScientificFigurePreview RenderOpticalPathPreview(
        ArticleScientificFigureCandidate candidate)
    {
        ArgumentNullException.ThrowIfNull(candidate);
        if (candidate.Kind != ArticleScientificFigureCandidateKind.Mechanism
            || !candidate.RequiresGateOneApproval)
        {
            throw new InvalidOperationException(
                "Only a pending mechanism candidate can produce the optical Gate 1 preview.");
        }

        var sourceIds = string.Join(',', candidate.Evidence.Select(item => item.SourceBlockId));
        var svg = $$"""
            <?xml version="1.0" encoding="utf-8"?>
            <svg xmlns="http://www.w3.org/2000/svg" width="1200" height="800" viewBox="0 0 1200 800" role="img" aria-labelledby="title desc" data-preview-kind="gate-one-candidate" data-candidate-id="{{candidate.CandidateId}}" data-source-block-ids="{{sourceIds}}">
              <title id="title">{{Escape(candidate.Title)}}（Gate 1 候选预览）</title>
              <desc id="desc">非按比例的二次凸透镜成像候选图。它说明待核验的几何关系，不声明文章中的数值、视觉结论或医学结论已经成立。</desc>
              <rect x="0" y="0" width="1200" height="800" fill="#FFFFFF" />
              <text x="600" y="54" text-anchor="middle" font-family="Segoe UI" font-size="28" fill="#0F172A">二次凸透镜成像的观察关系（候选示意，非按比例）</text>
              <text x="600" y="88" text-anchor="middle" font-family="Segoe UI" font-size="16" fill="#B45309">仅供 Gate 1 科学核验：不自动确认近点、焦距、清晰度或正倒视觉结论</text>
              <path d="M 80 420 L 1120 420" stroke="#64748B" stroke-width="2" />
              <text x="1100" y="446" text-anchor="end" font-family="Segoe UI" font-size="15" fill="#475569">主光轴</text>
              <g data-element="object">
                <path d="M 160 520 L 160 300" stroke="#0F766E" stroke-width="6" />
                <path d="M 160 300 L 146 324" stroke="#0F766E" stroke-width="6" />
                <path d="M 160 300 L 174 324" stroke="#0F766E" stroke-width="6" />
                <text x="160" y="550" text-anchor="middle" font-family="Segoe UI" font-size="18" fill="#0F172A">物体</text>
              </g>
              <g data-element="primary-lens">
                <rect x="398" y="210" width="8" height="420" rx="4" fill="#BFDBFE" stroke="#1D4ED8" stroke-width="2" />
                <text x="402" y="662" text-anchor="middle" font-family="Segoe UI" font-size="18" fill="#0F172A">主凸透镜 L₁</text>
              </g>
              <g data-element="intermediate-image">
                <path d="M 660 315 L 660 535" stroke="#DC2626" stroke-width="6" />
                <path d="M 660 535 L 646 511" stroke="#DC2626" stroke-width="6" />
                <path d="M 660 535 L 674 511" stroke="#DC2626" stroke-width="6" />
                <text x="660" y="572" text-anchor="middle" font-family="Segoe UI" font-size="18" fill="#991B1B">中间像 S</text>
              </g>
              <g data-element="eye-lens">
                <rect x="850" y="210" width="8" height="420" rx="4" fill="#DDD6FE" stroke="#6D28D9" stroke-width="2" />
                <text x="854" y="662" text-anchor="middle" font-family="Segoe UI" font-size="18" fill="#0F172A">眼睛晶状体 L₂</text>
              </g>
              <g data-element="retina">
                <rect x="1040" y="250" width="10" height="340" rx="5" fill="#FDE68A" stroke="#B45309" stroke-width="2" />
                <text x="1045" y="622" text-anchor="middle" font-family="Segoe UI" font-size="18" fill="#0F172A">视网膜</text>
              </g>
              <g data-relation="primary-ray-1"><path d="M 160 300 L 402 300" stroke="#2563EB" stroke-width="3" /><path d="M 402 300 L 660 535" stroke="#2563EB" stroke-width="3" /></g>
              <g data-relation="primary-ray-2"><path d="M 160 300 L 402 420" stroke="#2563EB" stroke-width="3" /><path d="M 402 420 L 660 535" stroke="#2563EB" stroke-width="3" /></g>
              <g data-relation="eye-ray-1"><path d="M 660 535 L 854 360" stroke="#7C3AED" stroke-width="3" /><path d="M 854 360 L 1045 450" stroke="#7C3AED" stroke-width="3" /></g>
              <g data-relation="eye-ray-2"><path d="M 660 535 L 854 480" stroke="#7C3AED" stroke-width="3" /><path d="M 854 480 L 1045 450" stroke="#7C3AED" stroke-width="3" /></g>
              <text x="530" y="250" text-anchor="middle" font-family="Segoe UI" font-size="16" fill="#1D4ED8">L₁ 形成中间实像 S</text>
              <text x="945" y="330" text-anchor="middle" font-family="Segoe UI" font-size="16" fill="#6D28D9">S 作为 L₂ 的物体</text>
              <text x="600" y="744" text-anchor="middle" font-family="Segoe UI" font-size="15" fill="#475569">来源证据：{{Escape(string.Join("；", candidate.Evidence.Select(item => $"第 {item.PageNumber} 页/{item.SourceBlockId}")))}}</text>
            </svg>
            """;
        var bytes = Encoding.UTF8.GetBytes(svg);
        var hash = Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
        return new ArticleScientificFigurePreview(
            candidate.CandidateId,
            "gate-one-candidate-preview",
            svg,
            $"sha256:{hash}",
            candidate.GateOneStatus);
    }

    private static string Escape(string value) => System.Security.SecurityElement.Escape(value) ?? string.Empty;
}

public sealed record ArticleScientificFigurePreview(
    string CandidateId,
    string PreviewKind,
    string Svg,
    string Sha256,
    ArticleScientificFigureGateStatus GateOneStatus);
