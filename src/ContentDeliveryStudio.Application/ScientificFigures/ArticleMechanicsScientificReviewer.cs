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
            XDocument document;
            try
            {
                document = XDocument.Parse(artifact.Svg);
            }
            catch (System.Xml.XmlException exception)
            {
                findings.Add(new("article-svg-invalid", c.CandidateId, exception.Message));
                return Report(c, findings);
            }

            var text = string.Join("\n", document.Descendants(Svg + "text").Select(x => x.Value));
            foreach (var required in Required(c.Kind))
            {
                if (!text.Contains(required, StringComparison.Ordinal))
                    findings.Add(new("required-label-missing", c.CandidateId, required));
            }

            var graphics = document.Descendants()
                .Where(element => element.Name == Svg + "path" || element.Name == Svg + "rect")
                .Where(element => !element.Ancestors(Svg + "defs").Any())
                .ToArray();
            if (graphics.Length < 10)
                findings.Add(new("article-graphic-density-insufficient", c.CandidateId,
                    $"A scientific illustration requires at least 10 visible graphic primitives; found {graphics.Length}."));

            var roles = document.Descendants(Svg + "path")
                .Select(element => (string?)element.Attribute("data-article-role"))
                .Where(role => !string.IsNullOrWhiteSpace(role))
                .ToHashSet(StringComparer.Ordinal);
            foreach (var requiredRole in RequiredRoles(c.Kind))
            {
                if (!roles.Contains(requiredRole))
                    findings.Add(new("article-required-graphic-role-missing", c.CandidateId, requiredRole));
            }
        }
        return Report(c, findings);
    }

    private ArticleOpticalScientificReviewReport Report(
        ArticleScientificFigureCandidate candidate,
        IReadOnlyList<ArticleOpticalScientificFinding> findings)
    {
        var regions = BuildRegions(candidate);
        return new ArticleOpticalScientificReviewReport(
            Profile(candidate),
            "located source evidence, required apparatus/topology roles, and deterministic profile invariants; human Gate 1 remains pending",
            findings,
            regions.Select(region => region.ExpectedCheck).ToArray());
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
        ArticleScientificFigureCandidateKind.PinholeFocusPlane => ["小孔处", "光源处", "像位置"],
        ArticleScientificFigureCandidateKind.PinholeObservation => ["近距", "远距", "全景"],
        ArticleScientificFigureCandidateKind.SuperconductingEnergy => ["电能", "磁能", "电流变化"],
        ArticleScientificFigureCandidateKind.SuperconductingPersistentCurrent => ["闭合通路", "撤去励磁电源", "恒定电流"],
        ArticleScientificFigureCandidateKind.SuperconductingExcitation => ["heater", "超导开关", "励磁电源", "液氦"],
        _ => []
    };

    private static string[] RequiredRoles(ArticleScientificFigureCandidateKind kind) => kind switch
    {
        ArticleScientificFigureCandidateKind.BernoulliFanEnergy => ["intake-flow", "fan-blade", "electrical-work", "outlet-flow"],
        ArticleScientificFigureCandidateKind.BernoulliFanZones => ["duct-wall", "suction-flow", "compression-flow"],
        ArticleScientificFigureCandidateKind.BernoulliStreamlineBoundary => ["same-streamline", "free-jet", "comparison-boundary"],
        ArticleScientificFigureCandidateKind.PinholeGeometry => ["object", "barrier", "principal-ray", "image-plane", "inverted-image"],
        ArticleScientificFigureCandidateKind.PinholeFocusPlane => ["focus-plane", "ray", "camera-input-ray", "camera-focused-ray", "sensor"],
        ArticleScientificFigureCandidateKind.PinholeObservation => ["barrier", "aperture", "near-field", "far-field"],
        ArticleScientificFigureCandidateKind.SuperconductingEnergy => ["circuit", "switch", "magnetic-field"],
        ArticleScientificFigureCandidateKind.SuperconductingPersistentCurrent => ["charging-loop", "persistent-current"],
        ArticleScientificFigureCandidateKind.SuperconductingExcitation => ["excitation-circuit", "persistent-switch-branch", "heater-circuit"],
        _ => [],
    };
}
