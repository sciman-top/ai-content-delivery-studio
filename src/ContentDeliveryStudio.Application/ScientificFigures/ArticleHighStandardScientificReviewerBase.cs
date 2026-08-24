using System.Xml;
using System.Xml.Linq;

namespace ContentDeliveryStudio.Application.ScientificFigures;

/// <summary>
/// Deep deterministic reviewer for high-standard article figures. It owns the
/// common fail-closed SVG and semantic checks; narrow domain reviewers only
/// admit one profile and add geometry checks that actually vary by physics.
/// </summary>
public abstract class ArticleHighStandardScientificReviewerBase : IArticleScientificFigureReviewer
{
    protected static readonly XNamespace Svg = "http://www.w3.org/2000/svg";
    protected abstract IReadOnlySet<string> SupportedPackageIds { get; }

    public ArticleScientificReviewReport Review(ArticleScientificFigureCandidate candidate, ScientificSvgArtifact? artifact, ArticleSourceFigureAudit audit, ArticleSourceEvidenceBoard? board)
    {
        ArgumentNullException.ThrowIfNull(candidate);
        ArgumentNullException.ThrowIfNull(audit);
        var findings = new List<ArticleScientificFinding>();
        if (!ArticleHighStandardFigureProfileCatalog.TryResolve(candidate, out var profile))
        {
            findings.Add(new("article-profile-missing", candidate.CandidateId, "No high-standard profile matches this candidate."));
            return BuildReport(candidate, "article-high-standard-invalid", findings);
        }
        if (!SupportedPackageIds.Contains(profile.PackageId)) findings.Add(new("article-profile-mismatch", candidate.CandidateId, $"Reviewer does not admit {profile.PackageId}."));
        if (candidate.Evidence.Count == 0) findings.Add(new("article-evidence-missing", candidate.CandidateId, "Located source evidence is required."));
        if (!candidate.RequiresGateOneApproval || candidate.GateOneStatus != ArticleScientificFigureGateStatus.PendingHumanApproval) findings.Add(new("article-gate-one-boundary-invalid", candidate.CandidateId, "Human Gate 1 remains pending."));

        if (candidate.Kind == ArticleScientificFigureCandidateKind.SourceEvidenceBoard)
        {
            if (board is null || board.PngBytes.Length == 0) findings.Add(new("source-board-invalid", candidate.CandidateId, "Source board pixels are missing."));
            return BuildReport(candidate, profile.PackageId, findings);
        }
        if (artifact is null)
        {
            findings.Add(new("article-svg-missing", candidate.CandidateId, "Deterministic SVG is required."));
            return BuildReport(candidate, profile.PackageId, findings);
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
            findings.Add(new("article-svg-invalid", candidate.CandidateId, exception.Message));
            return BuildReport(candidate, profile.PackageId, findings);
        }
        if (!profile.CandidateContracts.TryGetValue(candidate.Kind, out var contract))
        {
            findings.Add(new("article-profile-contract-missing", candidate.CandidateId, candidate.Kind.ToString()));
            return BuildReport(candidate, profile.PackageId, findings);
        }

        ValidateCommonContract(candidate, document, contract, findings);
        foreach (var invariant in ValidateProfileGeometry(candidate.Kind, document)) findings.Add(new(invariant.Code, candidate.CandidateId, invariant.Evidence));
        return BuildReport(candidate, profile.PackageId, findings);
    }

    public IReadOnlyList<ArticleScientificVisualRegion> BuildRegions(ArticleScientificFigureCandidate candidate)
    {
        var packageId = ArticleHighStandardFigureProfileCatalog.TryResolve(candidate, out var profile) ? profile.PackageId : "article-high-standard-invalid";
        var ids = candidate.Evidence.Select(evidence => evidence.SourceBlockId).Distinct().ToArray();
        var contract = ArticleHighStandardFigureProfileCatalog.TryGetEffectiveContract(candidate);
        var expected = new ScientificExpectedVisualCheck($"expected-{candidate.Kind}", candidate.Kind.ToString(), "The figure must retain its physics, causal topology, concrete apparatus, and central comparison after explanatory prose is hidden.", contract is null ? packageId : string.Join("; ", contract.RequiredLabels), null, ["SVG-first authoritative geometry", "visible concrete apparatus/objects and causal relations", "clear visual focus on the article's key question"], ["unsupported scientific overclaim", "label-only artwork", "floating or disconnected apparatus labels", "plausible but physically misleading topology"], ids, ScientificExpectedVisualAuthority.LocatedSourceEvidencePendingGateOne);
        return [new ArticleScientificVisualRegion(ScientificVisualRegionKind.Relation, new ScientificPixelRegion(40, 120, 1120, 580), expected)];
    }

    protected abstract IReadOnlyList<(string Code, string Evidence)> ValidateProfileGeometry(ArticleScientificFigureCandidateKind kind, XDocument document);

    protected static bool HasEndpointNear(XDocument document, string role, double x, double y, double tolerance, int minimum) =>
        document.Descendants(Svg + "path").Where(IsVisible).Where(path => string.Equals((string?)path.Attribute("data-article-role"), role, StringComparison.Ordinal)).SelectMany(ReadLineEndpoints).Count(point => Math.Abs(point.X - x) <= tolerance && Math.Abs(point.Y - y) <= tolerance) >= minimum;

    protected static bool HasDashedPaths(XDocument document, string role, int minimum) =>
        document.Descendants(Svg + "path").Where(IsVisible).Where(path => string.Equals((string?)path.Attribute("data-article-role"), role, StringComparison.Ordinal)).Count(path => !string.IsNullOrWhiteSpace((string?)path.Attribute("stroke-dasharray"))) >= minimum;

    protected static IEnumerable<XElement> VisibleGraphicElements(XDocument document) => document.Descendants().Where(element => element.Name.LocalName is "path" or "rect" or "circle" or "ellipse" or "line" or "polyline" or "polygon").Where(IsVisible);

    protected static bool IsVisible(XElement element)
    {
        if (element.Ancestors(Svg + "defs").Any()) return false;
        foreach (var node in element.AncestorsAndSelf())
        {
            if (string.Equals((string?)node.Attribute("display"), "none", StringComparison.OrdinalIgnoreCase) || string.Equals((string?)node.Attribute("visibility"), "hidden", StringComparison.OrdinalIgnoreCase) || string.Equals((string?)node.Attribute("visibility"), "collapse", StringComparison.OrdinalIgnoreCase) || string.Equals((string?)node.Attribute("opacity"), "0", StringComparison.OrdinalIgnoreCase)) return false;
            foreach (var declaration in ((string?)node.Attribute("style") ?? string.Empty).Split(';', StringSplitOptions.RemoveEmptyEntries))
            {
                var parts = declaration.Split(':', 2, StringSplitOptions.TrimEntries);
                if (parts.Length == 2 && ((parts[0].Equals("display", StringComparison.OrdinalIgnoreCase) && parts[1].Equals("none", StringComparison.OrdinalIgnoreCase)) || (parts[0].Equals("visibility", StringComparison.OrdinalIgnoreCase) && (parts[1].Equals("hidden", StringComparison.OrdinalIgnoreCase) || parts[1].Equals("collapse", StringComparison.OrdinalIgnoreCase))) || (parts[0].Equals("opacity", StringComparison.OrdinalIgnoreCase) && parts[1].Equals("0", StringComparison.OrdinalIgnoreCase)))) return false;
            }
        }
        return true;
    }

    private static void ValidateCommonContract(ArticleScientificFigureCandidate candidate, XDocument document, ArticleHighStandardFigureCandidateContract contract, ICollection<ArticleScientificFinding> findings)
    {
        var joinedText = string.Join("\n", document.Descendants(Svg + "text").Select(text => text.Value));
        foreach (var label in contract.RequiredLabels.Where(label => !joinedText.Contains(label, StringComparison.Ordinal))) findings.Add(new("required-label-missing", candidate.CandidateId, label));
        var graphics = VisibleGraphicElements(document).ToArray();
        if (graphics.Length < contract.MinimumGraphicCount) findings.Add(new("article-graphic-density-insufficient", candidate.CandidateId, $"This profile requires at least {contract.MinimumGraphicCount} visible primitives for a content-bearing illustration; found {graphics.Length}."));
        var roleCounts = graphics.Select(element => (string?)element.Attribute("data-article-role")).Where(role => !string.IsNullOrWhiteSpace(role)).GroupBy(role => role!, StringComparer.Ordinal).ToDictionary(group => group.Key, group => group.Count(), StringComparer.Ordinal);
        foreach (var role in contract.RequiredRoles.Where(role => !roleCounts.ContainsKey(role))) findings.Add(new("article-required-graphic-role-missing", candidate.CandidateId, role));
        foreach (var (role, minimum) in contract.MinimumRoleCounts) if (!roleCounts.TryGetValue(role, out var actual) || actual < minimum) findings.Add(new("article-required-graphic-role-count", candidate.CandidateId, $"Role '{role}' requires at least {minimum} visible primitives; found {actual}."));
        foreach (var role in contract.RequiredConcreteObjectRoles.Where(role => !roleCounts.ContainsKey(role))) findings.Add(new("article-required-concrete-object-missing", candidate.CandidateId, role));
        foreach (var connection in contract.RequiredConnectionIds) if (!graphics.Any(element => string.Equals((string?)element.Attribute("data-article-connection"), connection, StringComparison.Ordinal))) findings.Add(new("article-required-causal-connection-missing", candidate.CandidateId, connection));
        if (roleCounts.Keys.Distinct(StringComparer.Ordinal).Count() < 3) findings.Add(new("article-visual-role-diversity-insufficient", candidate.CandidateId, "A high-standard figure requires at least three distinct visual roles."));
    }

    private ArticleScientificReviewReport BuildReport(ArticleScientificFigureCandidate candidate, string packageId, IReadOnlyList<ArticleScientificFinding> findings) => new(packageId, "located source evidence, deterministic high-standard contract, visible concrete apparatus/topology, and profile invariants; human Gate 1 remains pending", findings, BuildRegions(candidate).Select(region => region.ExpectedCheck).ToArray());

    private static IEnumerable<(double X, double Y)> ReadLineEndpoints(XElement path)
    {
        var tokens = ((string?)path.Attribute("d") ?? string.Empty).Split([' ', 'M', 'L'], StringSplitOptions.RemoveEmptyEntries);
        if (tokens.Length < 4 || !double.TryParse(tokens[0], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var x1) || !double.TryParse(tokens[1], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var y1) || !double.TryParse(tokens[^2], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var x2) || !double.TryParse(tokens[^1], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var y2)) return [];
        return [(x1, y1), (x2, y2)];
    }
}
