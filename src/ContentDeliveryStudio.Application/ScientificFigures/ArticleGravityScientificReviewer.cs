using System.Globalization;
using System.Xml;
using System.Xml.Linq;

namespace ContentDeliveryStudio.Application.ScientificFigures;

/// <summary>
/// Deterministic checks for the admitted gravity-terminology article profile.
/// The checks protect reference-frame, force, acceleration, and scale-reading
/// boundaries owned by the renderer; human Gate 1 remains mandatory.
/// </summary>
public sealed class ArticleGravityScientificReviewer : IArticleScientificFigureReviewer
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
        var regions = BuildRegions(candidate);
        if (!candidate.RequiresGateOneApproval
            || candidate.GateOneStatus != ArticleScientificFigureGateStatus.PendingHumanApproval)
        {
            findings.Add(Finding(
                "article-gate-one-boundary-invalid",
                candidate.CandidateId,
                "Gravity article candidates must remain pending explicit human Gate 1 approval."));
        }

        if (candidate.Evidence.Count == 0
            || candidate.Evidence.Any(item => string.IsNullOrWhiteSpace(item.SourceBlockId)
                || string.IsNullOrWhiteSpace(item.Excerpt)))
        {
            findings.Add(Finding(
                "article-gravity-evidence-missing",
                candidate.CandidateId,
                "Deterministic gravity checks require located source evidence."));
        }


        var externalPublishers = candidate.ExternalScientificReferences
            .Select(reference => reference.Publisher)
            .ToHashSet(StringComparer.Ordinal);
        if (!externalPublishers.SetEquals(["NASA", "NIST"])
            || candidate.ExternalScientificReferences.Any(reference =>
                !Uri.TryCreate(reference.Url, UriKind.Absolute, out var uri)
                || uri.Scheme != Uri.UriSchemeHttps
                || string.IsNullOrWhiteSpace(reference.AdoptionDecision)))
        {
            findings.Add(Finding(
                "article-gravity-external-authority-missing",
                candidate.CandidateId,
                "Gravity corrections require explicit NASA and NIST scientific authority references."));
        }

        if (candidate.Kind == ArticleScientificFigureCandidateKind.SourceEvidenceBoard)
        {
            if (board is null
                || board.PngBytes.Length == 0
                || board.SourceAssetIds.Count == 0
                || board.SourceAssetIds.Any(id => !audit.Assets.Any(asset => asset.AssetId == id)))
            {
                findings.Add(Finding(
                    "gravity-source-board-invalid",
                    candidate.CandidateId,
                    "Gravity source evidence requires non-empty hash-bound source assets."));
            }
        }
        else
        {
            ReviewSvg(candidate, artifact, findings);
        }

        return new ArticleOpticalScientificReviewReport(
            "article-gravity-v1",
            "located source evidence and deterministic gravity/reference-frame invariants; human Gate 1 remains pending",
            Array.AsReadOnly(findings.ToArray()),
            Array.AsReadOnly(regions.Select(item => item.ExpectedCheck).ToArray()));
    }

    public IReadOnlyList<ArticleOpticalVisualRegion> BuildRegions(
        ArticleScientificFigureCandidate candidate)
    {
        ArgumentNullException.ThrowIfNull(candidate);
        var evidenceIds = candidate.Evidence.Select(item => item.SourceBlockId)
            .Distinct(StringComparer.Ordinal).ToArray();
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
            string[] forbidden) => new(
            kind,
            new ScientificPixelRegion(x, y, width, height),
            new ScientificExpectedVisualCheck(
                $"expected-{id}", id, meaning, exact, direction, conditions, forbidden,
                evidenceIds, ScientificExpectedVisualAuthority.LocatedSourceEvidencePendingGateOne));

        return candidate.Kind switch
        {
            ArticleScientificFigureCandidateKind.GravityTerminology =>
            [Region("gravity-terms", ScientificVisualRegionKind.Element, 60, 150, 1050, 540,
                "Gravitational interaction, effective gravity, and scale support/tension are distinct quantities.",
                "地球引力 F_g; 有效重力 g_eff; 支持力/拉力 N 或 T",
                null,
                ["reference frame and terminology must be declared"],
                ["weight is always identical to scale reading"])],
            ArticleScientificFigureCandidateKind.GravityOrbitFreeFall =>
            [Region("orbit-free-fall", ScientificVisualRegionKind.Relation, 120, 170, 1000, 520,
                "Earth gravity supplies non-zero inward orbital acceleration while the local support force is near zero.",
                "g(r) = GM/r² ≠ 0; 秤读数 N ≈ 0",
                "gravity and acceleration point toward Earth center",
                ["spacecraft, object, and scale share free fall"],
                ["gravity is zero in orbit", "orbital acceleration is zero"])],
            ArticleScientificFigureCandidateKind.GravityElevatorFreeFall =>
            [Region("elevator-free-fall", ScientificVisualRegionKind.Relation, 260, 140, 680, 570,
                "A freely falling elevator and object retain Earth gravity and downward acceleration while support is near zero.",
                "Fg = mg; a ≈ g; N ≈ 0",
                "gravity and acceleration point downward",
                ["idealized free fall"],
                ["Earth gravity disappears"])],
            ArticleScientificFigureCandidateKind.GravitySurfaceRotation =>
            [Region("surface-effective-gravity", ScientificVisualRegionKind.Relation, 160, 150, 970, 530,
                "In an Earth-fixed rotating frame, effective gravity combines gravitational and centrifugal accelerations.",
                "g_eff = g_grav + a_cf",
                "gravity points generally inward; centrifugal term points away from the rotation axis",
                ["centripetal acceleration is a kinematic role of the net force"],
                ["centripetal force is an additional interaction force"])],
            ArticleScientificFigureCandidateKind.GravityCaseComparison =>
            [Region("gravity-case-table", ScientificVisualRegionKind.Element, 60, 150, 1000, 550,
                "Orbit, free-fall elevator, and Earth surface all retain Earth gravity but differ in support-force reading.",
                "Fg ≠ 0; N ≈ 0; N ≈ m·g_eff",
                null,
                ["same quantity is compared in every column"],
                ["weightlessness proves no gravity"])],
            ArticleScientificFigureCandidateKind.GravityReferenceFrames =>
            [Region("gravity-frame-accounting", ScientificVisualRegionKind.Relation, 60, 150, 1020, 550,
                "Inertial and non-inertial force accounting are valid only when the selected frame is explicit.",
                "ΣF_real = m·a; ΣF_real + F_inertial = m·a_rel",
                "choose frame before listing forces",
                ["real and inertial forces are labeled separately"],
                ["mixing inertial-frame and co-moving-frame equations"])],
            ArticleScientificFigureCandidateKind.SourceEvidenceBoard =>
            [Region("source-gravity-evidence", ScientificVisualRegionKind.Element, 0, 0, 1600, 1200,
                "Selected source figures remain represented by hash-bound pixels.",
                string.Join("; ", candidate.SourceFigureReferences),
                null,
                ["source-faithful pixels", "layout only"],
                ["generated source figure", "missing source asset"])],
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
            findings.Add(Finding(
                "article-gravity-svg-missing",
                candidate.CandidateId,
                "Gravity review requires SVG authority."));
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
            findings.Add(Finding("article-gravity-svg-invalid", candidate.CandidateId, exception.Message));
            return;
        }

        var joined = string.Join("\n",
            document.Descendants(Svg + "text").Select(item => item.Value)
                .Concat(document.Descendants()
                    .Select(item => (string?)item.Attribute("data-math-tex"))
                    .Where(value => !string.IsNullOrWhiteSpace(value))!));
        void Require(string value, string code)
        {
            if (!joined.Contains(value, StringComparison.Ordinal))
            {
                findings.Add(Finding(code, candidate.CandidateId, $"Required gravity content is absent: {value}"));
            }
        }

        void Forbid(string value, string code)
        {
            if (joined.Contains(value, StringComparison.Ordinal))
            {
                findings.Add(Finding(code, candidate.CandidateId, $"Forbidden gravity content is present: {value}"));
            }
        }

        switch (candidate.Kind)
        {
            case ArticleScientificFigureCandidateKind.GravityTerminology:
                Require("地球引力", "gravity-force-term-missing");
                Require(@"\mathbf{F}_g", "gravity-force-symbol-missing");
                Require("有效重力", "gravity-effective-term-missing");
                Require(@"\mathbf{g}_{\mathrm{eff}}", "gravity-effective-symbol-missing");
                Require("支持力/拉力", "gravity-scale-force-term-missing");
                Require(@"\mathbf{N}\;\text{或}\;\mathbf{T}", "gravity-scale-force-symbol-missing");
                Forbid("Weight = 秤读数", "gravity-weight-scale-equivalence-overclaim");
                Forbid("先声明参考系与术语约定，再写公式和结论", "gravity-editorial-note-visible");
                break;
            case ArticleScientificFigureCandidateKind.GravityOrbitFreeFall:
                Require(@"g(r)=\frac{GM}{r^2}\ne0", "gravity-orbit-acceleration-missing");
                Require("共同自由落体", "gravity-orbit-free-fall-missing");
                Require(@"\mathbf{N}\approx0", "gravity-orbit-scale-reading-missing");
                Forbid("重力为零", "gravity-orbit-zero-overclaim");
                ReviewVector(document, candidate, "orbit-gravity", line => line.X2 < line.X1,
                    "gravity-orbit-vector-direction-invalid", findings);
                break;
            case ArticleScientificFigureCandidateKind.GravityElevatorFreeFall:
                Require(@"\mathbf{F}_g=m\mathbf{g}", "gravity-elevator-force-missing");
                Require(@"\mathbf{a}\approx\mathbf{g}", "gravity-elevator-acceleration-missing");
                Require(@"\mathbf{N}\approx0", "gravity-elevator-support-missing");
                Require("地球引力与自由落体加速度并未消失", "gravity-elevator-boundary-missing");
                ReviewVector(document, candidate, "elevator-gravity", line => line.Y2 > line.Y1,
                    "gravity-elevator-vector-direction-invalid", findings);
                break;
            case ArticleScientificFigureCandidateKind.GravitySurfaceRotation:
                Require("引力场", "gravity-surface-field-label-missing");
                Require(@"\mathbf{g}_{\mathrm{grav}}", "gravity-surface-field-missing");
                Require("离心项", "gravity-surface-centrifugal-label-missing");
                Require(@"\mathbf{a}_{\mathrm{cf}}=\omega^2r_\perp", "gravity-surface-centrifugal-missing");
                Require("有效重力", "gravity-surface-effective-label-missing");
                Require("地面合支持力 R", "gravity-surface-support-missing");
                Require(@"\mathbf{g}_{\mathrm{eff}}", "gravity-surface-effective-formula-missing");
                Require(@"\mathbf{R}+m\mathbf{g}_{\mathrm{eff}}=0", "gravity-surface-equilibrium-formula-missing");
                Require("不是额外的相互作用力", "gravity-centripetal-boundary-missing");
                Forbid("向心力与引力是两个力", "gravity-centripetal-extra-force-overclaim");
                ReviewVector(document, candidate, "surface-effective-gravity", line => Length(line) > 60,
                    "gravity-surface-effective-vector-invalid", findings);
                ReviewVector(document, candidate, "surface-support", line => line.Y2 < line.Y1,
                    "gravity-surface-support-vector-invalid", findings);
                ReviewSurfaceVectorComposition(document, candidate, findings);
                break;
            case ArticleScientificFigureCandidateKind.GravityCaseComparison:
                Require("绕地轨道", "gravity-case-orbit-missing");
                Require("自由落体电梯", "gravity-case-elevator-missing");
                Require("地表相对静止", "gravity-case-surface-missing");
                Require("失重判据：支持力/秤读数接近零", "gravity-weightlessness-criterion-missing");
                Require(@"\mathbf{a}_{\mathrm{orbit}}\ne0", "gravity-orbit-acceleration-comparison-missing");
                break;
            case ArticleScientificFigureCandidateKind.GravityReferenceFrames:
                Require(@"\sum\mathbf{F}_{\mathrm{real}}=m\mathbf{a}", "gravity-inertial-equation-missing");
                Require(@"\sum\mathbf{F}_{\mathrm{real}}+\mathbf{F}_{\mathrm{inertial}}=m\mathbf{a}_{\mathrm{rel}}", "gravity-noninertial-equation-missing");
                Require("不得与惯性系方程混用", "gravity-frame-mixing-boundary-missing");
                Require("不要再额外添加一支“向心力”箭头", "gravity-centripetal-double-count-boundary-missing");
                break;
        }
    }

    private static void ReviewVector(
        XDocument document,
        ArticleScientificFigureCandidate candidate,
        string role,
        Func<GravityLineGeometry, bool> predicate,
        string code,
        ICollection<ArticleOpticalScientificFinding> findings)
    {
        var vectors = document.Descendants(Svg + "path")
            .Where(path => string.Equals((string?)path.Attribute("data-gravity-role"), role, StringComparison.Ordinal))
            .Select(path => TryReadLine((string?)path.Attribute("d")))
            .Where(line => line is not null)
            .Select(line => line!.Value)
            .ToArray();
        if (vectors.Length != 1 || !predicate(vectors[0]))
        {
            findings.Add(Finding(code, candidate.CandidateId, $"Gravity vector role is invalid: {role}"));
        }
    }

    private static void ReviewSurfaceVectorComposition(
        XDocument document,
        ArticleScientificFigureCandidate candidate,
        ICollection<ArticleOpticalScientificFinding> findings)
    {
        var gravity = ReadSingleVector(document, "surface-gravity");
        var centrifugal = ReadSingleVector(document, "surface-centrifugal");
        var effective = ReadSingleVector(document, "surface-effective-gravity");
        var support = ReadSingleVector(document, "surface-support");
        var effectiveForce = ReadSingleVector(document, "surface-effective-force");
        if (gravity is null || centrifugal is null || effective is null
            || !SamePoint(gravity.Value.X1, gravity.Value.Y1, centrifugal.Value.X1, centrifugal.Value.Y1)
            || !SamePoint(gravity.Value.X1, gravity.Value.Y1, effective.Value.X1, effective.Value.Y1)
            || !SamePoint(
                effective.Value.X2,
                effective.Value.Y2,
                gravity.Value.X2 + centrifugal.Value.X2 - gravity.Value.X1,
                gravity.Value.Y2 + centrifugal.Value.Y2 - gravity.Value.Y1))
        {
            findings.Add(Finding(
                "gravity-surface-vector-sum-invalid",
                candidate.CandidateId,
                "Surface effective gravity must close the gravity-plus-centrifugal parallelogram."));
        }

        if (support is null || effectiveForce is null
            || !SamePoint(support.Value.X1, support.Value.Y1, effectiveForce.Value.X1, effectiveForce.Value.Y1)
            || !NearlyEqual(support.Value.X2 - support.Value.X1, -(effectiveForce.Value.X2 - effectiveForce.Value.X1))
            || !NearlyEqual(support.Value.Y2 - support.Value.Y1, -(effectiveForce.Value.Y2 - effectiveForce.Value.Y1)))
        {
            findings.Add(Finding(
                "gravity-surface-force-balance-invalid",
                candidate.CandidateId,
                "Static surface support must be equal and opposite to m times effective gravity."));
        }
    }

    private static GravityLineGeometry? ReadSingleVector(XDocument document, string role)
    {
        var vectors = document.Descendants(Svg + "path")
            .Where(path => string.Equals((string?)path.Attribute("data-gravity-role"), role, StringComparison.Ordinal))
            .Select(path => TryReadLine((string?)path.Attribute("d")))
            .Where(line => line is not null)
            .Select(line => line!.Value)
            .ToArray();
        return vectors.Length == 1 ? vectors[0] : null;
    }

    private static bool SamePoint(double x1, double y1, double x2, double y2) =>
        NearlyEqual(x1, x2) && NearlyEqual(y1, y2);

    private static bool NearlyEqual(double first, double second) => Math.Abs(first - second) <= 0.1;

    private static GravityLineGeometry? TryReadLine(string? value)
    {
        var parts = value?.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts is not ["M", var x1, var y1, "L", var x2, var y2]
            || !double.TryParse(x1, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsedX1)
            || !double.TryParse(y1, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsedY1)
            || !double.TryParse(x2, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsedX2)
            || !double.TryParse(y2, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsedY2))
        {
            return null;
        }

        return new GravityLineGeometry(parsedX1, parsedY1, parsedX2, parsedY2);
    }

    private static double Length(GravityLineGeometry line) =>
        Math.Sqrt(Math.Pow(line.X2 - line.X1, 2) + Math.Pow(line.Y2 - line.Y1, 2));

    private readonly record struct GravityLineGeometry(double X1, double Y1, double X2, double Y2);

    private static ArticleOpticalScientificFinding Finding(string code, string id, string evidence) =>
        new(code, id, evidence);
}
