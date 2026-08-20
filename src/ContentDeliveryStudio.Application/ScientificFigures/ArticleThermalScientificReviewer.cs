using System.Globalization;
using System.Xml;
using System.Xml.Linq;

namespace ContentDeliveryStudio.Application.ScientificFigures;

/// <summary>
/// Deterministic, non-authoritative checks for the admitted thermal article profile.
/// It checks labels, units, and direction/topology that the renderer owns; it does
/// not approve the article's scientific claims or replace human Gate 1.
/// </summary>
public sealed class ArticleThermalScientificReviewer : IArticleScientificFigureReviewer
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
            findings.Add(Finding("article-gate-one-boundary-invalid", candidate.CandidateId,
                "Thermal article candidates must remain pending explicit human Gate 1 approval."));
        }

        if (candidate.Evidence.Count == 0
            || candidate.Evidence.Any(item => string.IsNullOrWhiteSpace(item.SourceBlockId)
                || string.IsNullOrWhiteSpace(item.Excerpt)))
        {
            findings.Add(Finding("article-thermal-evidence-missing", candidate.CandidateId,
                "Deterministic checks require located source evidence."));
        }

        if (candidate.Kind == ArticleScientificFigureCandidateKind.SourceEvidenceBoard)
        {
            if (board is null || board.PngBytes.Length == 0 || board.SourceAssetIds.Count == 0
                || board.SourceAssetIds.Any(id => !audit.Assets.Any(asset => asset.AssetId == id)))
            {
                findings.Add(Finding("thermal-source-board-invalid", candidate.CandidateId,
                    "Thermal source evidence requires non-empty hash-bound source assets."));
            }
        }
        else
        {
            ReviewSvg(candidate, artifact, findings);
        }

        return new ArticleOpticalScientificReviewReport(
            "article-thermal-v1",
            "located source evidence and deterministic thermal invariants; human Gate 1 remains pending",
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
            ArticleScientificFigureCandidateKind.ThermalFrontMechanism =>
            [Region("snow-front", ScientificVisualRegionKind.Relation, 80, 170, 1040, 430,
                "Warm moist air rises against cold air and snow forms aloft; the heat source is not the ground surface.",
                "暖湿空气; 寒冷空气; 凝华成雪; 高空放热", "warm moist air rises; cold air descends",
                ["cross-section schematic"], ["snow heat directly raises ground temperature"])],
            ArticleScientificFigureCandidateKind.ThermalBasinException =>
            [Region("basin-exception", ScientificVisualRegionKind.Relation, 80, 170, 1040, 430,
                "Terrain can retain cold air above a basin while snow occurs with a comparatively warmer surface.",
                "南下寒冷空气; 高山; 盆地; 地面仍较暖", "cold air movement is blocked by terrain",
                ["exception is local, not universal"], ["exception presented as the general rule"])],
            ArticleScientificFigureCandidateKind.ThermalConductivityComparison =>
            [Region("conductivity-data", ScientificVisualRegionKind.Formula, 80, 150, 1040, 500,
                "The article's stated conductivity values retain their units and relative ordering.",
                "空气 0.02; 水蒸气 0.02; 水 0.6; 棉毛 0.05; W/(m·K)", "水 > 棉毛 > 空气≈水蒸气",
                ["values are approximate article data"], ["water vapor treated as liquid water"])],
            ArticleScientificFigureCandidateKind.ThermalTransferModes =>
            [Region("transfer-modes", ScientificVisualRegionKind.Element, 70, 140, 1060, 520,
                "The four transfer modes and the article's winter/summer emphasis are all represented.",
                "热传导; 热对流; 热辐射; 相变潜热", "winter conduction/convection; summer evaporation",
                ["radiation contribution remains environment-dependent"],
                ["human infrared heat loss shown as negligible", "radiation shown as dominant winter loss"])],
            ArticleScientificFigureCandidateKind.ThermalHumidityClothing =>
            [Region("humidity-clothing-chain", ScientificVisualRegionKind.Relation, 70, 150, 1060, 500,
                "Higher relative humidity can make clothing damp, increasing conduction and heat loss.",
                "相对湿度较高; 衣物潮湿; 导热系数增大; 热量快速散去", "humidity -> damp clothing -> greater conduction",
                ["winter/snow-melt context"], ["snow directly absorbs body heat"])],
            ArticleScientificFigureCandidateKind.ThermalDryWetHeat =>
            [Region("dry-wet-heat", ScientificVisualRegionKind.Relation, 70, 150, 1060, 500,
                "Relative humidity changes sweat evaporation: dry heat allows faster evaporation than humid heat.",
                "干热; 湿热; 汗液蒸发", "dry air -> faster evaporation; humid air -> evaporation inhibited",
                ["summer context"], ["humidity increases evaporation rate"])],
            ArticleScientificFigureCandidateKind.SourceEvidenceBoard =>
            [Region("source-photo-evidence", ScientificVisualRegionKind.Element, 0, 0, 1600, 1200,
                "Selected source assets remain represented by hash-bound pixels.",
                string.Join("; ", candidate.SourceFigureReferences), null,
                ["source-faithful pixels", "layout only"], ["generated source photo", "missing asset"])],
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
            findings.Add(Finding("article-thermal-svg-missing", candidate.CandidateId,
                "Thermal review requires SVG authority."));
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
            findings.Add(Finding("article-thermal-svg-invalid", candidate.CandidateId, exception.Message));
            return;
        }

        var joined = string.Join("\n", document.Descendants(Svg + "text").Select(item => item.Value));
        void Require(string value, string code)
        {
            if (!joined.Contains(value, StringComparison.Ordinal))
            {
                findings.Add(Finding(code, candidate.CandidateId, $"Required thermal content is absent: {value}"));
            }
        }

        void Forbid(string value, string code)
        {
            if (joined.Contains(value, StringComparison.Ordinal))
            {
                findings.Add(Finding(code, candidate.CandidateId, $"Forbidden thermal content is present: {value}"));
            }
        }

        switch (candidate.Kind)
        {
            case ArticleScientificFigureCandidateKind.ThermalFrontMechanism:
                Require("暖湿空气", "thermal-warm-moist-air-missing");
                Require("寒冷空气", "thermal-cold-air-missing");
                Require("凝华成雪", "thermal-snow-formation-missing");
                Require("高空放热", "thermal-upper-air-heat-missing");
                Require("不等于地面升温", "thermal-ground-warming-boundary-missing");
                Forbid("直接使地面升温", "thermal-ground-warming-overclaim");
                break;
            case ArticleScientificFigureCandidateKind.ThermalBasinException:
                Require("南下寒冷空气", "thermal-cold-air-flow-missing");
                Require("高山", "thermal-mountain-missing");
                Require("盆地", "thermal-basin-missing");
                Require("地面仍较暖", "thermal-warm-ground-missing");
                Require("仍在高空，未快速下沉", "thermal-basin-aloft-condition-missing");
                ReviewBasinColdAirGeometry(document, candidate, findings);
                break;
            case ArticleScientificFigureCandidateKind.ThermalConductivityComparison:
                Require("导热系数 λ", "thermal-conductivity-axis-missing");
                Require("W/(m·K)", "thermal-conductivity-unit-missing");
                Require("空气", "thermal-air-value-missing");
                Require("水蒸气", "thermal-vapor-value-missing");
                Require("水", "thermal-water-value-missing");
                Require("棉毛", "thermal-cotton-value-missing");
                Require("0.6", "thermal-water-number-missing");
                break;
            case ArticleScientificFigureCandidateKind.ThermalTransferModes:
                Require("热传导", "thermal-conduction-mode-missing");
                Require("热对流", "thermal-convection-mode-missing");
                Require("热辐射", "thermal-radiation-mode-missing");
                Require("相变潜热", "thermal-latent-heat-mode-missing");
                Require("人体红外散热", "thermal-radiation-mechanism-missing");
                Require("占比随环境变化", "thermal-radiation-condition-missing");
                Require("冬季", "thermal-winter-context-missing");
                Require("夏季", "thermal-summer-context-missing");
                Forbid("人体红外：可忽略", "thermal-radiation-negligible-overclaim");
                Forbid("定性关系", "thermal-redundant-annotation-present");
                break;
            case ArticleScientificFigureCandidateKind.ThermalHumidityClothing:
                Require("相对湿度较高", "thermal-humidity-high-missing");
                Require("衣物潮湿", "thermal-damp-clothing-missing");
                Require("导热系数增大", "thermal-conduction-increase-missing");
                Require("热量快速散去", "thermal-heat-loss-missing");
                Forbid("直接吸收人体热量", "thermal-direct-body-heat-overclaim");
                Forbid("融雪时的湿度上升", "thermal-snowmelt-humidity-overclaim");
                ReviewForwardCausalLinks(document, candidate, findings);
                break;
            case ArticleScientificFigureCandidateKind.ThermalDryWetHeat:
                Require("干热", "thermal-dry-heat-missing");
                Require("湿热", "thermal-wet-heat-missing");
                Require("汗液蒸发", "thermal-sweat-evaporation-missing");
                Require("蒸发受阻", "thermal-evaporation-inhibited-missing");
                ReviewEvaporationRateGeometry(document, candidate, findings);
                break;
        }
    }

    private static void ReviewBasinColdAirGeometry(
        XDocument document,
        ArticleScientificFigureCandidate candidate,
        ICollection<ArticleOpticalScientificFinding> findings)
    {
        var segments = ReadRoleLines(document, "basin-cold-air-aloft");
        if (segments.Count != 4
            || segments.Any(line => Math.Max(line.Y1, line.Y2) > 430)
            || segments.Max(line => Math.Max(line.X1, line.X2)) < 700)
        {
            findings.Add(Finding(
                "thermal-basin-cold-air-altitude-invalid",
                candidate.CandidateId,
                "The cold-air path must cross the mountain while remaining clearly aloft above the basin ground."));
        }
    }

    private static void ReviewForwardCausalLinks(
        XDocument document,
        ArticleScientificFigureCandidate candidate,
        ICollection<ArticleOpticalScientificFinding> findings)
    {
        var links = ReadRoleLines(document, "humidity-causal-link");
        if (links.Count != 3 || links.Any(line => line.X2 <= line.X1 || line.Y2 != line.Y1))
        {
            findings.Add(Finding(
                "thermal-humidity-causal-direction-invalid",
                candidate.CandidateId,
                "Humidity, damp clothing, conductivity, and heat loss must form three forward causal links."));
        }
    }

    private static void ReviewEvaporationRateGeometry(
        XDocument document,
        ArticleScientificFigureCandidate candidate,
        ICollection<ArticleOpticalScientificFinding> findings)
    {
        var dry = ReadRoleLines(document, "dry-evaporation-rate");
        var humid = ReadRoleLines(document, "humid-evaporation-rate");
        if (dry.Count != 1
            || humid.Count != 1
            || Length(dry[0]) < 2 * Length(humid[0]))
        {
            findings.Add(Finding(
                "thermal-evaporation-rate-contrast-invalid",
                candidate.CandidateId,
                "The dry-heat evaporation arrow must be visibly at least twice the humid-heat arrow length."));
        }
    }

    private static IReadOnlyList<ThermalLineGeometry> ReadRoleLines(
        XDocument document,
        string role) =>
        document.Descendants(Svg + "path")
            .Where(path => string.Equals((string?)path.Attribute("data-thermal-role"), role, StringComparison.Ordinal))
            .Select(path => TryReadLine((string?)path.Attribute("d")))
            .Where(line => line is not null)
            .Select(line => line!.Value)
            .ToArray();

    private static ThermalLineGeometry? TryReadLine(string? value)
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

        return new ThermalLineGeometry(parsedX1, parsedY1, parsedX2, parsedY2);
    }

    private static double Length(ThermalLineGeometry line) =>
        Math.Sqrt(Math.Pow(line.X2 - line.X1, 2) + Math.Pow(line.Y2 - line.Y1, 2));

    private readonly record struct ThermalLineGeometry(double X1, double Y1, double X2, double Y2);

    private static ArticleOpticalScientificFinding Finding(string code, string id, string evidence) =>
        new(code, id, evidence);
}
