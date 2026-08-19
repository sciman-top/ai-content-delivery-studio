using System.Globalization;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;

namespace ContentDeliveryStudio.Application.ScientificFigures;

public sealed record ArticleOpticalScientificFinding(
    string Code,
    string ResponsibleItemId,
    string Evidence);

public sealed record ArticleOpticalScientificReviewReport(
    string PackageId,
    string AuthorityBoundary,
    IReadOnlyList<ArticleOpticalScientificFinding> Findings,
    IReadOnlyList<ScientificExpectedVisualCheck> ExpectedVisualChecks)
{
    public bool Passed => Findings.Count == 0;
}

public sealed record ArticleOpticalVisualRegion(
    ScientificVisualRegionKind Kind,
    ScientificPixelRegion Region,
    ScientificExpectedVisualCheck ExpectedCheck);

public sealed class ArticleOpticalScientificReviewer : IArticleScientificFigureReviewer
{
    private static readonly XNamespace Svg = "http://www.w3.org/2000/svg";
    private static readonly Regex LinePath = new(
        @"^M\s+(?<x1>-?\d+(?:\.\d+)?)\s+(?<y1>-?\d+(?:\.\d+)?)\s+L\s+(?<x2>-?\d+(?:\.\d+)?)\s+(?<y2>-?\d+(?:\.\d+)?)$",
        RegexOptions.CultureInvariant | RegexOptions.Compiled);

    public ArticleOpticalScientificReviewReport Review(
        ArticleScientificFigureCandidate candidate,
        ScientificSvgArtifact? artifact,
        ArticleSourceFigureAudit audit,
        ArticleSourceEvidenceBoard? board)
    {
        ArgumentNullException.ThrowIfNull(candidate);
        ArgumentNullException.ThrowIfNull(audit);
        var findings = new List<ArticleOpticalScientificFinding>();
        var regions = BuildRegions(candidate);
        if (!candidate.RequiresGateOneApproval
            || candidate.GateOneStatus != ArticleScientificFigureGateStatus.PendingHumanApproval)
        {
            findings.Add(Finding(
                "article-gate-one-boundary-invalid",
                candidate.CandidateId,
                "Article optical candidates must remain pending explicit human Gate 1 approval."));
        }

        if (candidate.Evidence.Count == 0
            || candidate.Evidence.Any(item => string.IsNullOrWhiteSpace(item.SourceBlockId)
                || string.IsNullOrWhiteSpace(item.Excerpt)))
        {
            findings.Add(Finding(
                "article-optical-evidence-missing",
                candidate.CandidateId,
                "Deterministic checks require located source evidence."));
        }

        if (candidate.Kind == ArticleScientificFigureCandidateKind.SourceEvidenceBoard)
        {
            ReviewEvidenceBoard(candidate, audit, board, findings);
        }
        else
        {
            ReviewSvg(candidate, artifact, findings);
        }

        return new ArticleOpticalScientificReviewReport(
            "article-optics-v1",
            "located source evidence and deterministic optics invariants; human Gate 1 remains pending",
            Array.AsReadOnly(findings.ToArray()),
            Array.AsReadOnly(regions.Select(item => item.ExpectedCheck).ToArray()));
    }

    public IReadOnlyList<ArticleOpticalVisualRegion> BuildRegions(
        ArticleScientificFigureCandidate candidate)
    {
        ArgumentNullException.ThrowIfNull(candidate);
        var evidenceIds = candidate.Evidence.Select(item => item.SourceBlockId)
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        ArticleOpticalVisualRegion Region(
            string id,
            ScientificVisualRegionKind kind,
            int x,
            int y,
            int width,
            int height,
            string meaning,
            string? exact,
            string? direction,
            string[] conditions,
            string[] forbidden) =>
            new(
                kind,
                new ScientificPixelRegion(x, y, width, height),
                new ScientificExpectedVisualCheck(
                    $"expected-{id}",
                    id,
                    meaning,
                    exact,
                    direction,
                    conditions,
                    forbidden,
                    evidenceIds,
                    ScientificExpectedVisualAuthority.LocatedSourceEvidencePendingGateOne));

        return candidate.Kind switch
        {
            ArticleScientificFigureCandidateKind.Mechanism =>
            [
                Region("secondary-lenses", ScientificVisualRegionKind.Element, 270, 240, 660, 340,
                    "Both optical elements are convex lenses and S is the intermediate-image plane.",
                    "主凸透镜 L1; 眼睛晶状体 L2; 中间像 S", "L1 -> S -> L2 -> fixed receiver",
                    ["non-scale schematic", "light propagates left to right"], ["concave L1", "reversed rays"]),
                Region("secondary-ray-topology", ScientificVisualRegionKind.Relation, 100, 230, 1020, 330,
                    "Rays converge at S and then propagate through L2 to a later receiving plane.",
                    null, "left-to-right; converge at S; converge after L2",
                    ["S is the object for L2"], ["ray direction right-to-left"]),
            ],
            ArticleScientificFigureCandidateKind.LensEquationGraph =>
            [
                Region("lens-formula-branches", ScientificVisualRegionKind.Formula, 580, 210, 520, 360,
                    "The article branches use the stated positive-distance convention.",
                    "y = x / (x + 1), x > 0; y = x / (1 - x), 0 < x < 1",
                    "the two branches are inverse functions", ["x=u/f", "y=v/f", "u and v are distance magnitudes"],
                    ["y=x/(x-1)", "f approximately 2 cm is a universal eye constant"]),
                Region("lens-domain-graph", ScientificVisualRegionKind.Relation, 100, 210, 440, 430,
                    "The plotted domains and asymptotic boundaries match the two exact branches.",
                    "x=1; y=1; y=x", "branch one stays below y=1; branch two uses 0<x<1",
                    ["dimensionless x and y"], ["undefined values plotted as valid"]),
            ],
            ArticleScientificFigureCandidateKind.ExperimentalComparison =>
            [
                Region("screen-receiver-conditions", ScientificVisualRegionKind.Relation, 55, 145, 1090, 520,
                    "A movable screen and a fixed retina/sensor are distinct receiving conditions.",
                    "光屏位于像面; 固定接收面", "screen experiment != eye/camera condition",
                    ["L1 and L2 remain convex"], ["equating the two observation conditions"]),
            ],
            ArticleScientificFigureCandidateKind.Comparison =>
            [
                Region("plane-order-comparison", ScientificVisualRegionKind.Relation, 50, 150, 1100, 500,
                    "The three panels preserve the order L2 right of S, coplanar, and L2 left of S.",
                    "L2 位于 S 右侧; L2 与 S 平面重合; L2 位于 S 左侧",
                    "right panel state diverging; coplanar boundary; left panel state converging",
                    ["compare rays before reaching L2"], ["same S/L2 order in all panels"]),
            ],
            ArticleScientificFigureCandidateKind.CorrectiveLensControl =>
            [
                Region("corrective-lens-type", ScientificVisualRegionKind.Element, 320, 420, 390, 190,
                    "The added control element is a concave lens before the same eye/camera lens.",
                    "附加凹透镜（近视镜片）", "added concave lens -> L2",
                    ["same downstream lens"], ["added convex lens"]),
                Region("corrective-focus-shift", ScientificVisualRegionKind.Relation, 620, 190, 450, 420,
                    "The intervention rays are more divergent at L2 and the illustrative focus differs from control.",
                    "焦点 A（示意）; 焦点 B（示意）", "focus B is farther right than focus A",
                    ["no medical or acuity conclusion"], ["identical focus before and after intervention"]),
            ],
            ArticleScientificFigureCandidateKind.SourceEvidenceBoard =>
            [
                Region("source-photo-evidence", ScientificVisualRegionKind.Element, 0, 0, 1600, 1200,
                    "Every referenced source photo remains represented by a hash-bound extracted asset.",
                    string.Join("; ", candidate.SourceFigureReferences), null,
                    ["source-faithful pixels", "layout and numbering only"], ["generated experiment photo", "missing referenced photo"]),
            ],
            _ => throw new ArgumentOutOfRangeException(nameof(candidate), candidate.Kind, null),
        };
    }

    private static void ReviewSvg(
        ArticleScientificFigureCandidate candidate,
        ScientificSvgArtifact? artifact,
        ICollection<ArticleOpticalScientificFinding> findings)
    {
        if (artifact is null)
        {
            findings.Add(Finding("article-optical-svg-missing", candidate.CandidateId, "Optical review requires SVG authority."));
            return;
        }

        var document = Parse(artifact.Svg);
        var root = document.Root!;
        var text = root.Descendants(Svg + "text").Select(item => item.Value).ToArray();
        var joined = string.Join("\n", text);
        void Require(string value, string code, string itemId)
        {
            if (!joined.Contains(value, StringComparison.Ordinal))
            {
                findings.Add(Finding(code, itemId, $"Required exact optical content is absent: {value}"));
            }
        }

        void Forbid(string value, string code, string itemId)
        {
            if (joined.Contains(value, StringComparison.Ordinal))
            {
                findings.Add(Finding(code, itemId, $"Forbidden optical content is present: {value}"));
            }
        }

        foreach (var ray in root.Descendants(Svg + "path").Where(item => item.Attribute("marker-end") is not null))
        {
            var match = LinePath.Match((string?)ray.Attribute("d") ?? string.Empty);
            if (!match.Success)
            {
                findings.Add(Finding(
                    "optics-ray-geometry-invalid",
                    "optical-ray-topology",
                    "Arrowed optical ray must use a deterministic straight-line path."));
                break;
            }

            var x1 = double.Parse(match.Groups["x1"].Value, CultureInfo.InvariantCulture);
            var x2 = double.Parse(match.Groups["x2"].Value, CultureInfo.InvariantCulture);
            if (Math.Abs(x2 - x1) > 0.01 && x2 <= x1)
            {
                findings.Add(Finding(
                    "optics-ray-direction-reversed",
                    "optical-ray-topology",
                    $"Arrowed ray runs from x={x1} to x={x2}; article optics require left-to-right propagation."));
                break;
            }
        }

        switch (candidate.Kind)
        {
            case ArticleScientificFigureCandidateKind.Mechanism:
                Require("主凸透镜 L1", "optics-primary-lens-type-invalid", "secondary-lenses");
                Require("眼睛晶状体 L2", "optics-secondary-lens-type-invalid", "secondary-lenses");
                Require("S 是 L2 的物体", "optics-intermediate-image-role-invalid", "secondary-ray-topology");
                break;
            case ArticleScientificFigureCandidateKind.LensEquationGraph:
                Require("x = u/f，y = v/f；u、v 均表示相应距离的正值", "optics-symbol-unit-convention-invalid", "lens-formula-branches");
                Require("y = x / (x + 1)，x > 0", "optics-virtual-object-branch-invalid", "lens-formula-branches");
                Require("y = x / (1 - x)，0 < x < 1", "optics-real-object-branch-invalid", "lens-formula-branches");
                Require("两支互为反函数", "optics-inverse-branch-relationship-invalid", "lens-domain-graph");
                Forbid("x / (x - 1)", "optics-wrong-formula-branch", "lens-formula-branches");
                break;
            case ArticleScientificFigureCandidateKind.ExperimentalComparison:
                Require("A. 光屏接收中间实像", "optics-screen-panel-missing", "screen-receiver-conditions");
                Require("B. 眼睛/相机模型继续接收光束", "optics-fixed-receiver-panel-missing", "screen-receiver-conditions");
                Require("光屏位于像面", "optics-screen-condition-missing", "screen-receiver-conditions");
                Require("固定接收面", "optics-fixed-receiver-condition-missing", "screen-receiver-conditions");
                break;
            case ArticleScientificFigureCandidateKind.Comparison:
                Require("L2 位于 S 右侧", "optics-plane-order-right-invalid", "plane-order-comparison");
                Require("L2 与 S 平面重合", "optics-plane-order-coplanar-invalid", "plane-order-comparison");
                Require("L2 位于 S 左侧", "optics-plane-order-left-invalid", "plane-order-comparison");
                Require("光束已越过 S 并发散", "optics-diverging-topology-invalid", "plane-order-comparison");
                Require("光束仍在会聚", "optics-converging-topology-invalid", "plane-order-comparison");
                break;
            case ArticleScientificFigureCandidateKind.CorrectiveLensControl:
                Require("附加凹透镜", "optics-corrective-lens-type-invalid", "corrective-lens-type");
                Forbid("附加凸透镜", "optics-corrective-lens-type-invalid", "corrective-lens-type");
                Require("光束更发散", "optics-corrective-divergence-invalid", "corrective-focus-shift");
                ReviewFocusShift(root, findings);
                break;
        }
    }

    private static void ReviewFocusShift(
        XElement root,
        ICollection<ArticleOpticalScientificFinding> findings)
    {
        var focusA = root.Descendants(Svg + "text").SingleOrDefault(item => item.Value.Contains("焦点 A", StringComparison.Ordinal));
        var focusB = root.Descendants(Svg + "text").SingleOrDefault(item => item.Value.Contains("焦点 B", StringComparison.Ordinal));
        if (focusA is null || focusB is null || Number(focusB, "x") - Number(focusA, "x") < 20)
        {
            findings.Add(Finding(
                "optics-focus-shift-not-represented",
                "corrective-focus-shift",
                "Control and intervention must not use the same illustrative focus position."));
        }
    }

    private static void ReviewEvidenceBoard(
        ArticleScientificFigureCandidate candidate,
        ArticleSourceFigureAudit audit,
        ArticleSourceEvidenceBoard? board,
        ICollection<ArticleOpticalScientificFinding> findings)
    {
        var auditIds = audit.Assets.Select(item => item.AssetId).ToHashSet(StringComparer.Ordinal);
        if (board is null
            || board.SourceAssetIds.Count < candidate.SourceFigureReferences.Count
            || board.SourceAssetIds.Distinct(StringComparer.Ordinal).Count() != board.SourceAssetIds.Count
            || board.SourceAssetIds.Any(id => !auditIds.Contains(id)))
        {
            findings.Add(Finding(
                "optics-source-photo-coverage-invalid",
                "source-photo-evidence",
                "The evidence board must retain at least one distinct audited source asset for every referenced photo."));
        }
    }

    private static XDocument Parse(string svg)
    {
        var settings = new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null };
        using var text = new StringReader(svg);
        using var reader = XmlReader.Create(text, settings);
        return XDocument.Load(reader, LoadOptions.None);
    }

    private static double Number(XElement element, string attribute) =>
        double.Parse((string?)element.Attribute(attribute) ?? "NaN", CultureInfo.InvariantCulture);

    private static ArticleOpticalScientificFinding Finding(string code, string id, string evidence) =>
        new(code, id, evidence);
}
