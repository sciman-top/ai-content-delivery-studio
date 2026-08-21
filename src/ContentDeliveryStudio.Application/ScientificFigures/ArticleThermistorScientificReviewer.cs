using System.Globalization;
using System.Xml;
using System.Xml.Linq;

namespace ContentDeliveryStudio.Application.ScientificFigures;

/// <summary>
/// Deterministic checks for the thermistor-divider article profile. These checks
/// validate renderer-owned labels and relationships; they do not approve the
/// source article or replace human Gate 1.
/// </summary>
public sealed class ArticleThermistorScientificReviewer : IArticleScientificFigureReviewer
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
                "Thermistor candidates must remain pending explicit human Gate 1 approval."));
        }

        if (candidate.Evidence.Count == 0
            || candidate.Evidence.Any(item => string.IsNullOrWhiteSpace(item.SourceBlockId)))
        {
            findings.Add(Finding("article-thermistor-evidence-missing", candidate.CandidateId,
                "Thermistor review requires located source evidence."));
        }

        if (candidate.Kind == ArticleScientificFigureCandidateKind.SourceEvidenceBoard)
        {
            if (board is null || board.PngBytes.Length == 0 || board.SourceAssetIds.Count == 0)
            {
                findings.Add(Finding("thermistor-source-board-invalid", candidate.CandidateId,
                    "Thermistor source evidence requires non-empty source pixels."));
            }
        }
        else
        {
            ReviewSvg(candidate, artifact, findings);
        }

        return new ArticleOpticalScientificReviewReport(
            "article-thermistor-v1",
            "located source evidence and deterministic thermistor-divider invariants; human Gate 1 remains pending",
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
            new ScientificPixelRegion(60, 135, 1080, 560),
            new ScientificExpectedVisualCheck(
                $"expected-{id}", id, meaning, exact, direction, conditions, forbidden,
                evidenceIds, ScientificExpectedVisualAuthority.LocatedSourceEvidencePendingGateOne));

        return candidate.Kind switch
        {
            ArticleScientificFigureCandidateKind.ThermistorCircuitDivider =>
                [Region("thermistor-circuit", ScientificVisualRegionKind.Relation,
                    "The voltmeter measures thermistor voltage and series current changes with resistance.",
                    "R0; R1; 电压表; I=U总/(R0+R1)", "current varies with R1",
                    ["series circuit"], ["current constant"])],
            ArticleScientificFigureCandidateKind.ThermistorCurvature =>
                [Region("thermistor-curvature", ScientificVisualRegionKind.Formula,
                    "The divider curve is increasing and concave in R1; equal voltage intervals have unequal resistance intervals.",
                    "U=U总R1/(R0+R1); 凹函数", "slope decreases as R1 grows",
                    ["same voltage interval"], ["linear U-R relation"])],
            ArticleScientificFigureCandidateKind.ThermistorError =>
                [Region("thermistor-error", ScientificVisualRegionKind.Relation,
                    "The ΔR=ΔU/I shortcut is conditional on constant current and is blocked for this series divider.",
                    "ΔR=ΔU/I; I不恒定", "condition before formula",
                    ["error and correction shown"], ["I恒定 asserted"])],
            ArticleScientificFigureCandidateKind.ThermistorSpecialValues =>
                [Region("thermistor-limits", ScientificVisualRegionKind.Formula,
                    "Special values and limits validate direction without becoming original measured parameters.",
                    "R1→0; R1→∞; 示例参数", "U→0; U→U总",
                    ["example marked illustrative"], ["example treated as given"])],
            ArticleScientificFigureCandidateKind.SourceEvidenceBoard =>
                [Region("thermistor-source", ScientificVisualRegionKind.Element,
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
            findings.Add(Finding("article-thermistor-svg-missing", candidate.CandidateId,
                "Thermistor review requires SVG authority."));
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
            findings.Add(Finding("article-thermistor-svg-invalid", candidate.CandidateId, exception.Message));
            return;
        }

        var joined = string.Join("\n", document.Descendants(Svg + "text").Select(item => item.Value));
        void Require(string value, string code)
        {
            if (!joined.Contains(value, StringComparison.Ordinal))
            {
                findings.Add(Finding(code, candidate.CandidateId, $"Required thermistor content is absent: {value}"));
            }
        }

        void Forbid(string value, string code)
        {
            if (joined.Contains(value, StringComparison.Ordinal))
            {
                findings.Add(Finding(code, candidate.CandidateId, $"Forbidden thermistor overclaim is present: {value}"));
            }
        }

        switch (candidate.Kind)
        {
            case ArticleScientificFigureCandidateKind.ThermistorCircuitDivider:
                Require("热敏电阻 R1", "thermistor-r1-missing");
                Require("定值电阻 R0", "thermistor-r0-missing");
                Require("电压表测 R1", "thermistor-voltmeter-target-missing");
                Require("电流随 R1 变化", "thermistor-current-variation-missing");
                Require("R1/(R0+R1)", "thermistor-divider-formula-missing");
                Forbid("电流不变", "thermistor-constant-current-overclaim");
                ReviewVoltmeterLeads(document, candidate, findings);
                break;
            case ArticleScientificFigureCandidateKind.ThermistorCurvature:
                Require("U-R1", "thermistor-curve-axis-missing");
                Require("凹函数", "thermistor-concavity-missing");
                Require("斜率递减", "thermistor-slope-boundary-missing");
                Require("相同 ΔU", "thermistor-equal-voltage-interval-missing");
                Require("ΔR后段更大", "thermistor-delta-r-direction-missing");
                Forbid("线性关系成立", "thermistor-linear-overclaim");
                break;
            case ArticleScientificFigureCandidateKind.ThermistorError:
                Require("ΔR=ΔU/I", "thermistor-shortcut-missing");
                Require("仅在 I 恒定时成立", "thermistor-shortcut-condition-missing");
                Require("本题 I 会变化", "thermistor-error-correction-missing");
                Forbid("近似成立", "thermistor-invalid-approximation-overclaim");
                break;
            case ArticleScientificFigureCandidateKind.ThermistorSpecialValues:
                Require("R1→0", "thermistor-zero-limit-missing");
                Require("R1→∞", "thermistor-infinite-limit-missing");
                Require("示例参数，仅作方向验证", "thermistor-example-boundary-missing");
                Require("不等同题设实测", "thermistor-example-not-measurement-missing");
                break;
        }
    }

    private static void ReviewVoltmeterLeads(XDocument document,
        ArticleScientificFigureCandidate candidate,
        ICollection<ArticleOpticalScientificFinding> findings)
    {
        var leads = document.Descendants(Svg + "path")
            .Where(path => (string?)path.Attribute("data-thermistor-role") is
                "voltmeter-left-lead" or "voltmeter-right-lead")
            .Select(path => ParseLine((string?)path.Attribute("d")))
            .Where(line => line is not null)
            .Select(line => line!.Value)
            .ToArray();
        var expected = new[]
        {
            new ThermistorLine(330, 360, 360, 320),
            new ThermistorLine(460, 360, 550, 320),
        };
        if (leads.Length != 2 || expected.Any(item => !leads.Contains(item)))
        {
            findings.Add(Finding("thermistor-voltmeter-connection-invalid", candidate.CandidateId,
                "The voltmeter leads must connect across R1, not across R0 or the full series loop."));
        }
    }

    private static ThermistorLine? ParseLine(string? value)
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

        return new ThermistorLine(parsedX1, parsedY1, parsedX2, parsedY2);
    }

    private readonly record struct ThermistorLine(double X1, double Y1, double X2, double Y2);

    private static ArticleOpticalScientificFinding Finding(string code, string id, string evidence) =>
        new(code, id, evidence);
}
