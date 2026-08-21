using System.Xml;
using System.Xml.Linq;

namespace ContentDeliveryStudio.Application.ScientificFigures;

/// <summary>
/// Deterministic checks for the Archimedes/liquid-pressure profile. The article
/// contains model-sensitive claims, so the reviewer deliberately requires the
/// renderer to show conditions and caveats instead of accepting a bare formula.
/// </summary>
public sealed class ArticleArchimedesScientificReviewer : IArticleScientificFigureReviewer
{
    private static readonly XNamespace Svg = "http://www.w3.org/2000/svg";

    public ArticleOpticalScientificReviewReport Review(
        ArticleScientificFigureCandidate candidate,
        ScientificSvgArtifact? artifact,
        ArticleSourceFigureAudit audit,
        ArticleSourceEvidenceBoard? board)
    {
        ArgumentNullException.ThrowIfNull(candidate);
        ArgumentNullException.ThrowIfNull(audit);
        var findings = new List<ArticleOpticalScientificFinding>();
        if (!candidate.RequiresGateOneApproval
            || candidate.GateOneStatus != ArticleScientificFigureGateStatus.PendingHumanApproval)
        {
            findings.Add(Finding("article-gate-one-boundary-invalid", candidate.CandidateId,
                "Archimedes candidates must remain pending explicit human Gate 1 approval."));
        }

        if (candidate.Evidence.Count == 0
            || candidate.Evidence.Any(item => string.IsNullOrWhiteSpace(item.SourceBlockId)))
        {
            findings.Add(Finding("article-archimedes-evidence-missing", candidate.CandidateId,
                "Archimedes review requires located source evidence."));
        }

        if (candidate.Kind == ArticleScientificFigureCandidateKind.SourceEvidenceBoard)
        {
            if (board is null || board.PngBytes.Length == 0 || board.SourceAssetIds.Count == 0)
            {
                findings.Add(Finding("archimedes-source-board-invalid", candidate.CandidateId,
                    "Archimedes source evidence requires non-empty source pixels."));
            }
        }
        else
        {
            ReviewSvg(candidate, artifact, findings);
        }

        return new ArticleOpticalScientificReviewReport(
            "article-archimedes-v1",
            "located source evidence and deterministic liquid-pressure invariants; human Gate 1 remains pending",
            Array.AsReadOnly(findings.ToArray()),
            Array.AsReadOnly(BuildRegions(candidate).Select(item => item.ExpectedCheck).ToArray()));
    }

    public IReadOnlyList<ArticleOpticalVisualRegion> BuildRegions(
        ArticleScientificFigureCandidate candidate)
    {
        ArgumentNullException.ThrowIfNull(candidate);
        var evidenceIds = candidate.Evidence.Select(item => item.SourceBlockId)
            .Distinct(StringComparer.Ordinal).ToArray();
        ArticleOpticalVisualRegion Region(string id, ScientificVisualRegionKind kind, string meaning,
            string? exact, string? direction, string[] conditions, string[] forbidden) => new(
            kind,
            new ScientificPixelRegion(55, 130, 1090, 575),
            new ScientificExpectedVisualCheck(
                $"expected-{id}", id, meaning, exact, direction, conditions, forbidden,
                evidenceIds, ScientificExpectedVisualAuthority.LocatedSourceEvidencePendingGateOne));

        return candidate.Kind switch
        {
            ArticleScientificFigureCandidateKind.ArchimedesDefinition =>
                [Region("archimedes-definition", ScientificVisualRegionKind.Relation,
                    "V浸 and V排 are distinguished from the condition that allows a displaced-fluid formula.",
                    "V排=V浸; 表面与流体接触", "volume definition differs from pressure boundary",
                    ["contact state shown"], ["blue coloring alone defines V排"])],
            ArticleScientificFigureCandidateKind.ArchimedesWaterModel =>
                [Region("archimedes-water-model", ScientificVisualRegionKind.Relation,
                    "A same-volume ideal water body explains the buoyant pressure resultant under full fluid contact.",
                    "F液合=G水体=ρ液V排g", "pressure resultant upward under stated conditions",
                    ["hydrostatic fluid", "all surfaces contacted"], ["formula without conditions"])],
            ArticleScientificFigureCandidateKind.ArchimedesBottomContact =>
                [Region("archimedes-bottom-contact", ScientificVisualRegionKind.Relation,
                    "Bottom contact changes the pressure boundary and requires a conditional force model.",
                    "F液合=ρ液V排g-F底", "support reaction closes the force balance",
                    ["contact interface explicit"], ["unconditional Archimedes formula"])],
            ArticleScientificFigureCandidateKind.ArchimedesDepthDependence =>
                [Region("archimedes-depth", ScientificVisualRegionKind.Formula,
                    "Under the stated contact model, the omitted pressure term may vary with depth.",
                    "浮力与深度无关需条件", "depth changes omitted pressure term",
                    ["model-sensitive"], ["depth independence universal"])],
            ArticleScientificFigureCandidateKind.ArchimedesTopContact =>
                [Region("archimedes-top-contact", ScientificVisualRegionKind.Relation,
                    "Top and bottom contact produce different missing pressure terms and directions.",
                    "顶部/底部接触分开", "direction depends on contact boundary",
                    ["two contact states"], ["same correction sign always"])],
            ArticleScientificFigureCandidateKind.ArchimedesPier =>
                [Region("archimedes-pier", ScientificVisualRegionKind.Relation,
                    "Inclined pier claims require side-pressure integration and boundary conditions.",
                    "侧压力分量; 积分核验", "opposing side components may cancel",
                    ["same cross-section condition"], ["blue volume alone proves force"])],
            ArticleScientificFigureCandidateKind.ArchimedesPressureCaveat =>
                [Region("archimedes-pressure-caveat", ScientificVisualRegionKind.Relation,
                    "Absolute pressure, gauge pressure, and contact reaction must be separated before numeric claims.",
                    "表压/绝对压强; 接触界面", "model boundary before p0S",
                    ["system boundary explicit"], ["p0S universal correction"])],
            ArticleScientificFigureCandidateKind.SourceEvidenceBoard =>
                [Region("archimedes-source", ScientificVisualRegionKind.Element,
                    "Hash-bound source pixels remain represented.", string.Join("; ", candidate.SourceFigureReferences), null,
                    ["source-faithful pixels"], ["generated source image"])],
            _ => throw new ArgumentOutOfRangeException(nameof(candidate), candidate.Kind, null),
        };
    }

    private static void ReviewSvg(ArticleScientificFigureCandidate candidate,
        ScientificSvgArtifact? artifact, ICollection<ArticleOpticalScientificFinding> findings)
    {
        if (artifact is null)
        {
            findings.Add(Finding("article-archimedes-svg-missing", candidate.CandidateId,
                "Archimedes review requires SVG authority."));
            return;
        }

        XDocument document;
        try
        {
            var settings = new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null };
            using var textReader = new StringReader(artifact.Svg);
            using var xmlReader = XmlReader.Create(textReader, settings);
            document = XDocument.Load(xmlReader, LoadOptions.None);
        }
        catch (XmlException exception)
        {
            findings.Add(Finding("article-archimedes-svg-invalid", candidate.CandidateId, exception.Message));
            return;
        }

        var joined = string.Join("\n", document.Descendants(Svg + "text").Select(item => item.Value));
        void Require(string value, string code)
        {
            if (!joined.Contains(value, StringComparison.Ordinal))
            {
                findings.Add(Finding(code, candidate.CandidateId, $"Required Archimedes content is absent: {value}"));
            }
        }

        void Forbid(string value, string code)
        {
            if (joined.Contains(value, StringComparison.Ordinal))
            {
                findings.Add(Finding(code, candidate.CandidateId, $"Forbidden Archimedes overclaim is present: {value}"));
            }
        }

        switch (candidate.Kind)
        {
            case ArticleScientificFigureCandidateKind.ArchimedesDefinition:
                Require("V浸", "archimedes-v-immersed-missing");
                Require("V排", "archimedes-v-displaced-missing");
                Require("表面与流体接触", "archimedes-contact-condition-missing");
                Require("模型边界", "archimedes-model-boundary-missing");
                break;
            case ArticleScientificFigureCandidateKind.ArchimedesWaterModel:
                Require("理想水体", "archimedes-water-model-missing");
                Require("F液合", "archimedes-resultant-force-missing");
                Require("ρ液V排g", "archimedes-displaced-weight-missing");
                Require("所有表面与流体接触", "archimedes-full-contact-missing");
                break;
            case ArticleScientificFigureCandidateKind.ArchimedesBottomContact:
                Require("底面完全贴合", "archimedes-bottom-contact-missing");
                Require("缺失底面压力", "archimedes-missing-bottom-pressure");
                Require("需按边界条件修正", "archimedes-conditional-correction-missing");
                Forbid("无条件成立", "archimedes-unconditional-overclaim");
                break;
            case ArticleScientificFigureCandidateKind.ArchimedesDepthDependence:
                Require("水深 h", "archimedes-depth-axis-missing");
                Require("底面压力项", "archimedes-depth-pressure-term-missing");
                Require("需要条件", "archimedes-depth-condition-missing");
                Forbid("永远与深度无关", "archimedes-depth-universal-overclaim");
                break;
            case ArticleScientificFigureCandidateKind.ArchimedesTopContact:
                Require("顶部贴合", "archimedes-top-contact-missing");
                Require("底部贴合", "archimedes-bottom-comparison-missing");
                Require("压力方向依接触面", "archimedes-contact-direction-missing");
                break;
            case ArticleScientificFigureCandidateKind.ArchimedesPier:
                Require("倾斜桥墩", "archimedes-pier-missing");
                Require("侧压力分量", "archimedes-pier-side-pressure-missing");
                Require("积分核验", "archimedes-pier-integration-boundary-missing");
                Forbid("蓝色体积直接证明", "archimedes-pier-volume-overclaim");
                break;
            case ArticleScientificFigureCandidateKind.ArchimedesPressureCaveat:
                Require("表压/绝对压强", "archimedes-pressure-type-missing");
                Require("接触界面", "archimedes-pressure-boundary-missing");
                Require("p0S 不是普遍修正", "archimedes-p0-universal-boundary-missing");
                Forbid("所有实验都等于同一数值", "archimedes-p0-overclaim");
                break;
        }
    }

    private static ArticleOpticalScientificFinding Finding(string code, string id, string evidence) =>
        new(code, id, evidence);
}
