using System.Xml.Linq;

namespace ContentDeliveryStudio.Application.ScientificFigures;

/// Deterministic, fail-closed checks for the three article profiles added from the paper corpus.
public sealed class ArticleMechanicsScientificReviewer : IArticleScientificFigureReviewer
{
    private static readonly XNamespace Svg = "http://www.w3.org/2000/svg";
    public ArticleOpticalScientificReviewReport Review(ArticleScientificFigureCandidate c, ScientificSvgArtifact? artifact, ArticleSourceFigureAudit audit, ArticleSourceEvidenceBoard? board)
    {
        var findings = new List<ArticleOpticalScientificFinding>();
        if (c.Evidence.Count == 0) findings.Add(new("article-evidence-missing", c.CandidateId, "Located source evidence is required."));
        if (c.GateOneStatus != ArticleScientificFigureGateStatus.PendingHumanApproval) findings.Add(new("article-gate-one-boundary-invalid", c.CandidateId, "Human Gate 1 remains pending."));
        if (c.Kind == ArticleScientificFigureCandidateKind.SourceEvidenceBoard)
        {
            if (board is null || board.PngBytes.Length == 0) findings.Add(new("source-board-invalid", c.CandidateId, "Source board pixels are missing."));
        }
        else if (artifact is null) findings.Add(new("article-svg-missing", c.CandidateId, "Deterministic SVG is required."));
        else
        {
            var text = string.Join("\n", XDocument.Parse(artifact.Svg).Descendants(Svg + "text").Select(x => x.Value));
            foreach (var required in Required(c.Kind)) if (!text.Contains(required, StringComparison.Ordinal)) findings.Add(new("required-label-missing", c.CandidateId, required));
        }
        var regions = BuildRegions(c);
        return new ArticleOpticalScientificReviewReport(Profile(c), "located source evidence and deterministic profile invariants; human Gate 1 remains pending", findings, regions.Select(r => r.ExpectedCheck).ToArray());
    }
    public IReadOnlyList<ArticleOpticalVisualRegion> BuildRegions(ArticleScientificFigureCandidate c)
    {
        var ids = c.Evidence.Select(e => e.SourceBlockId).Distinct().ToArray();
        var label = c.Kind.ToString();
        var check = new ScientificExpectedVisualCheck($"expected-{label}", label, "Deterministic geometry and labels match the source-grounded objective.", string.Join("; ", Required(c.Kind)), null, ["SVG-first authoritative geometry"], ["unsupported scientific overclaim"], ids, ScientificExpectedVisualAuthority.LocatedSourceEvidencePendingGateOne);
        return [new ArticleOpticalVisualRegion(ScientificVisualRegionKind.Relation, new ScientificPixelRegion(40, 120, 1120, 580), check)];
    }
    private static string Profile(ArticleScientificFigureCandidate c) => c.Kind switch { ArticleScientificFigureCandidateKind.BernoulliFanEnergy or ArticleScientificFigureCandidateKind.BernoulliFanZones or ArticleScientificFigureCandidateKind.BernoulliStreamlineBoundary => "article-bernoulli-v1", ArticleScientificFigureCandidateKind.PinholeGeometry or ArticleScientificFigureCandidateKind.PinholeFocusPlane or ArticleScientificFigureCandidateKind.PinholeObservation => "article-pinhole-v1", ArticleScientificFigureCandidateKind.SuperconductingEnergy or ArticleScientificFigureCandidateKind.SuperconductingPersistentCurrent or ArticleScientificFigureCandidateKind.SuperconductingExcitation => "article-superconducting-v1", _ when c.ArticleTitle.Contains("伯努利", StringComparison.Ordinal) => "article-bernoulli-v1", _ when c.ArticleTitle.Contains("小孔成像", StringComparison.Ordinal) => "article-pinhole-v1", _ => "article-superconducting-v1" };
    private static string[] Required(ArticleScientificFigureCandidateKind k) => k switch
    {
        ArticleScientificFigureCandidateKind.BernoulliFanEnergy => ["风机做功", "电功", "总能"],
        ArticleScientificFigureCandidateKind.BernoulliFanZones => ["吸风区", "压缩区", "风机"],
        ArticleScientificFigureCandidateKind.BernoulliStreamlineBoundary => ["同一流线", "大气压"],
        ArticleScientificFigureCandidateKind.PinholeGeometry => ["小孔", "倒立实像", "可视范围"],
        ArticleScientificFigureCandidateKind.PinholeFocusPlane => ["小孔处", "光源处", "像面"],
        ArticleScientificFigureCandidateKind.PinholeObservation => ["近距", "远距", "全景"],
        ArticleScientificFigureCandidateKind.SuperconductingEnergy => ["电能", "磁能", "电流变化"],
        ArticleScientificFigureCandidateKind.SuperconductingPersistentCurrent => ["闭合通路", "撤去励磁电源", "恒定电流"],
        ArticleScientificFigureCandidateKind.SuperconductingExcitation => ["heater", "超导开关", "励磁电源", "液氦"],
        _ => []
    };
}
