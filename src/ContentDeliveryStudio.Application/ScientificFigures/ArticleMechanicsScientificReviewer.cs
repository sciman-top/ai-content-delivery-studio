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

            var textNodes = document.Descendants(Svg + "text").ToArray();
            var text = string.Join("\n", textNodes.Select(x => x.Value));
            foreach (var required in Required(c.Kind))
            {
                if (!text.Contains(required, StringComparison.Ordinal))
                    findings.Add(new("required-label-missing", c.CandidateId, required));
            }

            var graphics = VisibleGraphicElements(document).ToArray();
            if (graphics.Length < 10)
                findings.Add(new("article-graphic-density-insufficient", c.CandidateId,
                    $"A scientific illustration requires at least 10 visible graphic primitives; found {graphics.Length}."));

            var roleCounts = VisibleGraphicElements(document)
                .Select(element => (string?)element.Attribute("data-article-role"))
                .Where(role => !string.IsNullOrWhiteSpace(role))
                .GroupBy(role => role!, StringComparer.Ordinal)
                .ToDictionary(group => group.Key, group => group.Count(), StringComparer.Ordinal);
            var roles = roleCounts.Keys.ToHashSet(StringComparer.Ordinal);
            foreach (var requiredRole in RequiredRoles(c.Kind))
            {
                if (!roles.Contains(requiredRole))
                    findings.Add(new("article-required-graphic-role-missing", c.CandidateId, requiredRole));
            }

            foreach (var (role, minimum) in MinimumRoleCounts(c.Kind))
            {
                if (!roleCounts.TryGetValue(role, out var actual) || actual < minimum)
                {
                    findings.Add(new("article-required-graphic-role-count", c.CandidateId,
                        $"Role '{role}' requires at least {minimum} visible primitives; found {actual}."));
                }
            }

            foreach (var invariant in ValidateHighStandardGeometry(c.Kind, document))
                findings.Add(new(invariant.Code, c.CandidateId, invariant.Evidence));
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
        var check = new ScientificExpectedVisualCheck($"expected-{label}", label, "The figure must be visually legible, scientifically grounded, and independently useful after explanatory prose is hidden.", string.Join("; ", Required(c.Kind)), null, ["SVG-first authoritative geometry", "visible apparatus/objects and causal relations", "clear visual focus on the article's key question"], ["unsupported scientific overclaim", "label-only artwork", "floating or disconnected apparatus labels"], ids, ScientificExpectedVisualAuthority.LocatedSourceEvidencePendingGateOne);
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
        ArticleScientificFigureCandidateKind.BernoulliFanEnergy => ["intake-flow", "fan-blade", "electrical-work", "outlet-flow", "fan-body", "outlet-channel"],
        ArticleScientificFigureCandidateKind.BernoulliFanZones => ["duct-wall", "suction-flow", "compression-flow", "fan-body", "fan-blade"],
        ArticleScientificFigureCandidateKind.BernoulliStreamlineBoundary => ["same-streamline", "free-jet", "comparison-boundary", "throat-section"],
        ArticleScientificFigureCandidateKind.PinholeGeometry => ["object", "barrier", "principal-ray", "image-plane", "inverted-image"],
        ArticleScientificFigureCandidateKind.PinholeFocusPlane => ["focus-plane", "ray", "camera-body", "camera-input-ray", "camera-focused-ray", "sensor"],
        ArticleScientificFigureCandidateKind.PinholeObservation => ["barrier", "near-aperture", "far-aperture", "near-field", "far-field", "near-object", "far-object", "near-camera", "far-camera"],
        ArticleScientificFigureCandidateKind.SuperconductingEnergy => ["circuit", "switch", "magnetic-field", "power-source", "coil", "electrical-work"],
        ArticleScientificFigureCandidateKind.SuperconductingPersistentCurrent => ["charging-loop", "charging-coil", "persistent-current", "power-source"],
        ArticleScientificFigureCandidateKind.SuperconductingExcitation => ["excitation-circuit", "persistent-switch-branch", "superconducting-switch", "heater-circuit", "heater-element", "thermal-coupling", "cryostat", "main-coil"],
        _ => [],
    };

    private static IReadOnlyDictionary<string, int> MinimumRoleCounts(ArticleScientificFigureCandidateKind kind) => kind switch
    {
        ArticleScientificFigureCandidateKind.BernoulliFanEnergy => new Dictionary<string, int>(StringComparer.Ordinal)
        {
            ["intake-flow"] = 3, ["fan-blade"] = 4, ["electrical-work"] = 1, ["outlet-flow"] = 3, ["fan-body"] = 1, ["outlet-channel"] = 1,
        },
        ArticleScientificFigureCandidateKind.BernoulliFanZones => new Dictionary<string, int>(StringComparer.Ordinal)
        {
            ["duct-wall"] = 2, ["suction-flow"] = 3, ["compression-flow"] = 3, ["fan-body"] = 1, ["fan-blade"] = 3,
        },
        ArticleScientificFigureCandidateKind.BernoulliStreamlineBoundary => new Dictionary<string, int>(StringComparer.Ordinal)
        {
            ["duct-wall"] = 4, ["same-streamline"] = 6, ["free-jet"] = 2, ["comparison-boundary"] = 1, ["throat-section"] = 1,
        },
        ArticleScientificFigureCandidateKind.PinholeGeometry => new Dictionary<string, int>(StringComparer.Ordinal)
        {
            ["object"] = 3, ["barrier"] = 3, ["principal-ray"] = 4, ["image-plane"] = 1, ["inverted-image"] = 3,
        },
        ArticleScientificFigureCandidateKind.PinholeFocusPlane => new Dictionary<string, int>(StringComparer.Ordinal)
        {
            ["focus-plane"] = 3, ["ray"] = 4, ["camera-body"] = 1, ["camera-input-ray"] = 2, ["camera-focused-ray"] = 2, ["sensor"] = 1,
        },
        ArticleScientificFigureCandidateKind.PinholeObservation => new Dictionary<string, int>(StringComparer.Ordinal)
        {
            ["barrier"] = 2, ["near-aperture"] = 1, ["far-aperture"] = 1, ["near-field"] = 3, ["far-field"] = 3, ["near-object"] = 1, ["far-object"] = 1, ["near-camera"] = 1, ["far-camera"] = 1,
        },
        ArticleScientificFigureCandidateKind.SuperconductingEnergy => new Dictionary<string, int>(StringComparer.Ordinal)
        {
            ["circuit"] = 7, ["switch"] = 2, ["magnetic-field"] = 4, ["power-source"] = 1, ["coil"] = 6, ["electrical-work"] = 1,
        },
        ArticleScientificFigureCandidateKind.SuperconductingPersistentCurrent => new Dictionary<string, int>(StringComparer.Ordinal)
        {
            ["charging-loop"] = 6, ["charging-coil"] = 3, ["persistent-current"] = 4, ["power-source"] = 1,
        },
        ArticleScientificFigureCandidateKind.SuperconductingExcitation => new Dictionary<string, int>(StringComparer.Ordinal)
        {
            ["excitation-circuit"] = 8, ["persistent-switch-branch"] = 2, ["superconducting-switch"] = 1, ["heater-circuit"] = 2, ["heater-element"] = 1, ["thermal-coupling"] = 1, ["cryostat"] = 1, ["main-coil"] = 6,
        },
        _ => new Dictionary<string, int>(StringComparer.Ordinal),
    };

    private static IReadOnlyList<(string Code, string Evidence)> ValidateHighStandardGeometry(
        ArticleScientificFigureCandidateKind kind,
        XDocument document)
    {
        var findings = new List<(string Code, string Evidence)>();
        if (!IsHighStandardProfile(kind)) return findings;

        var graphicCount = VisibleGraphicElements(document).Count();
        if (graphicCount < MinimumGraphicCount(kind))
            findings.Add(("article-graphic-density-insufficient", $"This profile requires at least {MinimumGraphicCount(kind)} visible primitives for a content-bearing illustration; found {graphicCount}."));

        var roleNames = document.Descendants()
            .Select(element => (string?)element.Attribute("data-article-role"))
            .Where(role => !string.IsNullOrWhiteSpace(role))
            .ToHashSet(StringComparer.Ordinal);
        if (roleNames.Count < 3)
            findings.Add(("article-visual-role-diversity-insufficient", $"A high-standard figure requires at least three distinct visual roles; found {roleNames.Count}."));

        if (kind == ArticleScientificFigureCandidateKind.PinholeGeometry &&
            (!HasSharedEndpoint(document, "principal-ray", 520, 360, 4, 4) ||
             !HasEndpointNear(document, "principal-ray", 1030, 260, 4, 1) ||
             !HasEndpointNear(document, "principal-ray", 1030, 610, 4, 1)))
            findings.Add(("article-pinhole-ray-topology-invalid", "Principal rays must visibly converge at the aperture and reach both image endpoints."));

        if (kind == ArticleScientificFigureCandidateKind.PinholeFocusPlane &&
            (!HasSharedEndpoint(document, "ray", 490, 365, 4, 4) ||
             !HasSharedEndpoint(document, "camera-input-ray", 765, 260, 4, 2) ||
             !HasSharedEndpoint(document, "camera-focused-ray", 1060, 365, 4, 2)))
            findings.Add(("article-focus-ray-topology-invalid", "Focus-plane rays must join at the aperture, image position, lens input, and sensor target."));

        if (kind == ArticleScientificFigureCandidateKind.BernoulliStreamlineBoundary &&
            (!HasSharedEndpoint(document, "same-streamline", 330, 330, 4, 2) ||
             !HasEndpointNear(document, "throat-section", 330, 275, 4, 1) ||
             !HasEndpointNear(document, "throat-section", 330, 385, 4, 1)))
            findings.Add(("article-bernoulli-throat-topology-invalid", "The converging duct, throat section, and same-streamline paths must visibly share the throat boundary."));

        if (kind == ArticleScientificFigureCandidateKind.PinholeObservation &&
            (!HasSharedEndpoint(document, "near-field", 280, 390, 4, 3) ||
             !HasSharedEndpoint(document, "far-field", 840, 390, 4, 3)))
            findings.Add(("article-observation-comparison-topology-invalid", "Near and far comparison paths must visibly join their respective apertures."));

        if (kind == ArticleScientificFigureCandidateKind.SuperconductingPersistentCurrent &&
            (!HasEndpointNear(document, "charging-loop", 225, 350, 4, 1) ||
             !HasEndpointNear(document, "charging-loop", 225, 420, 4, 1) ||
             !HasEndpointNear(document, "charging-loop", 495, 465, 4, 1) ||
             !HasEndpointNear(document, "charging-loop", 325, 465, 4, 1)))
            findings.Add(("article-persistent-current-topology-invalid", "The charging loop must visibly connect the excitation source to both sides of the coil."));

        if (kind == ArticleScientificFigureCandidateKind.SuperconductingEnergy &&
            (!HasEndpointNear(document, "circuit", 300, 350, 4, 1) ||
             !HasEndpointNear(document, "circuit", 300, 450, 4, 1) ||
             !HasEndpointNear(document, "circuit", 468, 400, 4, 1) ||
             !HasEndpointNear(document, "circuit", 832, 400, 4, 1) ||
             !HasEndpointNear(document, "switch", 385, 350, 4, 1) ||
             !HasEndpointNear(document, "switch", 435, 350, 4, 1)))
            findings.Add(("article-superconducting-energy-topology-invalid", "The excitation diagram must show a source-connected closed circuit through the switch and both sides of the coil."));

        if (kind == ArticleScientificFigureCandidateKind.SuperconductingExcitation &&
            (!HasEndpointNear(document, "excitation-circuit", 285, 315, 4, 1) ||
             !HasEndpointNear(document, "excitation-circuit", 285, 370, 4, 1) ||
             !HasEndpointNear(document, "heater-circuit", 685, 205, 4, 1) ||
             !HasEndpointNear(document, "heater-circuit", 685, 220, 4, 1) ||
             !HasEndpointNear(document, "thermal-coupling", 635, 225, 4, 1) ||
             !HasEndpointNear(document, "thermal-coupling", 635, 235, 4, 1)))
            findings.Add(("article-superconducting-heater-topology-invalid", "The heater circuit must terminate at the heater and the thermal-coupling marker must reach the switch branch without becoming a main-loop wire."));

        return findings;
    }

    private static bool IsHighStandardProfile(ArticleScientificFigureCandidateKind kind) => kind is
        ArticleScientificFigureCandidateKind.BernoulliFanEnergy or
        ArticleScientificFigureCandidateKind.BernoulliFanZones or
        ArticleScientificFigureCandidateKind.BernoulliStreamlineBoundary or
        ArticleScientificFigureCandidateKind.PinholeGeometry or
        ArticleScientificFigureCandidateKind.PinholeFocusPlane or
        ArticleScientificFigureCandidateKind.PinholeObservation or
        ArticleScientificFigureCandidateKind.SuperconductingEnergy or
        ArticleScientificFigureCandidateKind.SuperconductingPersistentCurrent or
        ArticleScientificFigureCandidateKind.SuperconductingExcitation;

    private static int MinimumGraphicCount(ArticleScientificFigureCandidateKind kind) => kind switch
    {
        ArticleScientificFigureCandidateKind.BernoulliStreamlineBoundary => 16,
        ArticleScientificFigureCandidateKind.PinholeGeometry => 15,
        ArticleScientificFigureCandidateKind.PinholeFocusPlane => 20,
        ArticleScientificFigureCandidateKind.PinholeObservation => 28,
        _ => 20,
    };

    private static bool HasSharedEndpoint(XDocument document, string role, double x, double y, double tolerance, int minimum)
    {
        return HasEndpointNear(document, role, x, y, tolerance, minimum);
    }

    private static bool HasEndpointNear(XDocument document, string role, double x, double y, double tolerance, int minimum)
    {
        var endpointCount = document.Descendants(Svg + "path")
            .Where(IsVisible)
            .Where(path => string.Equals((string?)path.Attribute("data-article-role"), role, StringComparison.Ordinal))
            .SelectMany(ReadLineEndpoints)
            .Count(point => Math.Abs(point.X - x) <= tolerance && Math.Abs(point.Y - y) <= tolerance);
        return endpointCount >= minimum;
    }

    private static IEnumerable<XElement> VisibleGraphicElements(XDocument document) => document.Descendants()
        .Where(element => element.Name.LocalName is "path" or "rect" or "circle" or "ellipse" or "line" or "polyline" or "polygon")
        .Where(IsVisible);

    private static bool IsVisible(XElement element)
    {
        if (element.Ancestors(Svg + "defs").Any()) return false;
        foreach (var node in element.AncestorsAndSelf())
        {
            if (string.Equals((string?)node.Attribute("display"), "none", StringComparison.OrdinalIgnoreCase) ||
                string.Equals((string?)node.Attribute("visibility"), "hidden", StringComparison.OrdinalIgnoreCase) ||
                string.Equals((string?)node.Attribute("visibility"), "collapse", StringComparison.OrdinalIgnoreCase) ||
                string.Equals((string?)node.Attribute("opacity"), "0", StringComparison.OrdinalIgnoreCase))
                return false;

            var style = (string?)node.Attribute("style") ?? string.Empty;
            foreach (var declaration in style.Split(';', StringSplitOptions.RemoveEmptyEntries))
            {
                var parts = declaration.Split(':', 2, StringSplitOptions.TrimEntries);
                if (parts.Length != 2) continue;
                if ((parts[0].Equals("display", StringComparison.OrdinalIgnoreCase) && parts[1].Equals("none", StringComparison.OrdinalIgnoreCase)) ||
                    (parts[0].Equals("visibility", StringComparison.OrdinalIgnoreCase) && (parts[1].Equals("hidden", StringComparison.OrdinalIgnoreCase) || parts[1].Equals("collapse", StringComparison.OrdinalIgnoreCase))) ||
                    (parts[0].Equals("opacity", StringComparison.OrdinalIgnoreCase) && parts[1].Equals("0", StringComparison.OrdinalIgnoreCase)))
                    return false;
            }
        }
        return true;
    }

    private static IEnumerable<(double X, double Y)> ReadLineEndpoints(XElement path)
    {
        var tokens = ((string?)path.Attribute("d") ?? string.Empty)
            .Split([' ', 'M', 'L'], StringSplitOptions.RemoveEmptyEntries);
        if (tokens.Length < 4 ||
            !double.TryParse(tokens[0], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var x1) ||
            !double.TryParse(tokens[1], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var y1) ||
            !double.TryParse(tokens[^2], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var x2) ||
            !double.TryParse(tokens[^1], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var y2))
            return [];
        return [(x1, y1), (x2, y2)];
    }
}
