using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Xml;
using System.Xml.Linq;
using ContentDeliveryStudio.Core.ScientificFigures;

namespace ContentDeliveryStudio.Application.ScientificFigures;

public sealed record ScientificContractReviewRequest(
    ScientificFigureSpec Specification,
    SvgRenderPlan RenderPlan,
    ScientificSvgArtifact Svg,
    ScientificFigureExportBundle Exports,
    double AdvisoryScore = 1);

public sealed class ScientificContractReviewer
{
    private static readonly XNamespace SvgNamespace = "http://www.w3.org/2000/svg";

    public ScientificContractReviewReport Review(ScientificContractReviewRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.Specification);
        ArgumentNullException.ThrowIfNull(request.RenderPlan);
        ArgumentNullException.ThrowIfNull(request.Svg);
        ArgumentNullException.ThrowIfNull(request.Exports);

        var findings = new List<ScientificContractFinding>();
        CompareAuthorityIdentity(request, findings);
        CompareSpecificationAndPlan(request.Specification, request.RenderPlan, findings);
        var svgSnapshot = ReadSvg(request.Svg, request.RenderPlan, findings);
        if (svgSnapshot is not null)
        {
            ComparePlanAndSvg(request.RenderPlan, svgSnapshot, findings);
            CompareExports(request.Svg, request.Exports, svgSnapshot, findings);
        }
        else
        {
            Add(
                findings,
                "exports-unverifiable",
                ScientificContractInvariant.ExportEquivalence,
                request.Exports.ExporterId,
                "Export equivalence cannot be established because the SVG authority is invalid.",
                ScientificContractRepairLayer.Exporter);
        }

        return ScientificContractReviewReport.Create(request.AdvisoryScore, findings);
    }

    private static void CompareAuthorityIdentity(
        ScientificContractReviewRequest request,
        ICollection<ScientificContractFinding> findings)
    {
        var spec = request.Specification;
        var plan = request.RenderPlan;
        var svg = request.Svg;
        if (plan.SpecificationId != spec.SpecificationId
            || plan.SpecificationVersion != spec.Version)
        {
            Add(
                findings,
                "plan-specification-identity-drift",
                ScientificContractInvariant.AuthorityIdentity,
                plan.PlanId,
                $"Expected specification {spec.SpecificationId:D}/v{spec.Version}; plan carries {plan.SpecificationId:D}/v{plan.SpecificationVersion}.",
                ScientificContractRepairLayer.RenderPlanCompiler);
        }

        if (!string.Equals(svg.PlanId, plan.PlanId, StringComparison.Ordinal)
            || svg.SpecificationId != plan.SpecificationId
            || svg.SpecificationVersion != plan.SpecificationVersion)
        {
            Add(
                findings,
                "svg-authority-identity-drift",
                ScientificContractInvariant.AuthorityIdentity,
                svg.PlanId,
                "SVG artifact identity does not match its render plan.",
                ScientificContractRepairLayer.SvgRenderer);
        }
    }

    private static void CompareSpecificationAndPlan(
        ScientificFigureSpec specification,
        SvgRenderPlan plan,
        ICollection<ScientificContractFinding> findings)
    {
        var allowedElements = specification.Elements
            .Where(item => item.Requirement != FigureContentRequirement.Forbidden)
            .ToDictionary(item => item.ElementId, StringComparer.Ordinal);
        var planElements = plan.Elements.ToLookup(
            item => item.SourceSpecificationItemId,
            StringComparer.Ordinal);
        foreach (var required in specification.Elements.Where(item =>
                     item.Requirement == FigureContentRequirement.Required))
        {
            if (!planElements[required.ElementId].Any())
            {
                Add(
                    findings,
                    "required-element-missing-from-plan",
                    ScientificContractInvariant.RequiredElementCoverage,
                    required.ElementId,
                    "The required specification element has no render-plan item.",
                    ScientificContractRepairLayer.RenderPlanCompiler);
            }
        }

        foreach (var planElement in plan.Elements)
        {
            if (!allowedElements.TryGetValue(planElement.SourceSpecificationItemId, out var expected))
            {
                if (planElement.Kind != FigureElementKind.DecorativeAsset)
                {
                    Add(
                        findings,
                        "extra-scientific-element-in-plan",
                        ScientificContractInvariant.NoExtraScientificContent,
                        planElement.RenderElementId,
                        $"No allowed specification element authorizes '{planElement.SourceSpecificationItemId}'.",
                        ScientificContractRepairLayer.RenderPlanCompiler);
                }

                continue;
            }

            if (planElements[expected.ElementId].Count() > 1)
            {
                Add(
                    findings,
                    "duplicate-scientific-element-in-plan",
                    ScientificContractInvariant.NoExtraScientificContent,
                    expected.ElementId,
                    "The render plan contains more than one item for the same specification element.",
                    ScientificContractRepairLayer.RenderPlanCompiler);
            }

            if (planElement.Kind != expected.Kind
                || !Same(planElement.ScientificMeaning, expected.ScientificMeaning)
                || !Same(planElement.ExactContent, expected.LabelOrFormula)
                || planElement.IsCritical != expected.IsCritical
                || planElement.ProvenanceKind != expected.Provenance?.Kind)
            {
                Add(
                    findings,
                    "scientific-element-content-drift",
                    ScientificContractInvariant.ExactScientificContent,
                    expected.ElementId,
                    $"Plan kind/content/criticality/provenance differs from the specification; expected exact content '{expected.LabelOrFormula ?? "<none>"}', actual '{planElement.ExactContent ?? "<none>"}'.",
                    ScientificContractRepairLayer.RenderPlanCompiler);
            }
        }

        var allowedRelations = specification.Relations
            .Where(item => item.Requirement != FigureContentRequirement.Forbidden)
            .ToDictionary(item => item.RelationId, StringComparer.Ordinal);
        var planRelations = plan.Connections.ToLookup(
            item => item.SourceSpecificationItemId,
            StringComparer.Ordinal);
        foreach (var required in specification.Relations.Where(item =>
                     item.Requirement == FigureContentRequirement.Required))
        {
            if (!planRelations[required.RelationId].Any())
            {
                Add(
                    findings,
                    "required-relation-missing-from-plan",
                    ScientificContractInvariant.RequiredElementCoverage,
                    required.RelationId,
                    "The required specification relation has no render-plan connection.",
                    ScientificContractRepairLayer.RenderPlanCompiler);
            }
        }

        var renderElementsById = plan.Elements.ToLookup(
            item => item.RenderElementId,
            StringComparer.Ordinal);
        foreach (var connection in plan.Connections)
        {
            if (!allowedRelations.TryGetValue(connection.SourceSpecificationItemId, out var expected))
            {
                Add(
                    findings,
                    "extra-scientific-relation-in-plan",
                    ScientificContractInvariant.NoExtraScientificContent,
                    connection.RenderConnectionId,
                    $"No allowed specification relation authorizes '{connection.SourceSpecificationItemId}'.",
                    ScientificContractRepairLayer.RenderPlanCompiler);
                continue;
            }

            var sourceSpecId = SingleSourceId(renderElementsById[connection.SourceRenderElementId]);
            var targetSpecId = SingleSourceId(renderElementsById[connection.TargetRenderElementId]);
            if (Same(sourceSpecId, expected.TargetElementId)
                && Same(targetSpecId, expected.SourceElementId))
            {
                Add(
                    findings,
                    "relation-direction-reversed-in-plan",
                    ScientificContractInvariant.RelationDirection,
                    expected.RelationId,
                    $"Expected {expected.SourceElementId} -> {expected.TargetElementId}; plan endpoints are reversed.",
                    ScientificContractRepairLayer.RenderPlanCompiler);
            }
            else if (!Same(sourceSpecId, expected.SourceElementId)
                     || !Same(targetSpecId, expected.TargetElementId))
            {
                Add(
                    findings,
                    "relation-endpoint-drift-in-plan",
                    ScientificContractInvariant.RelationSemantics,
                    expected.RelationId,
                    $"Expected endpoints {expected.SourceElementId} -> {expected.TargetElementId}; actual {sourceSpecId ?? "<missing>"} -> {targetSpecId ?? "<missing>"}.",
                    ScientificContractRepairLayer.RenderPlanCompiler);
            }

            if (connection.Direction != expected.Direction)
            {
                Add(
                    findings,
                    "relation-direction-drift-in-plan",
                    ScientificContractInvariant.RelationDirection,
                    expected.RelationId,
                    $"Expected direction {expected.Direction}; actual {connection.Direction}.",
                    ScientificContractRepairLayer.RenderPlanCompiler);
            }

            if (connection.Kind != expected.Kind
                || !Same(connection.Label, expected.Label)
                || !Same(connection.RepresentationConstraint, expected.RepresentationConstraint)
                || connection.IsCritical != expected.IsCritical
                || connection.ProvenanceKind != expected.Provenance.Kind)
            {
                Add(
                    findings,
                    "relation-semantic-drift-in-plan",
                    ScientificContractInvariant.RelationSemantics,
                    expected.RelationId,
                    "Relation kind, label, representation, criticality, or provenance differs from the specification.",
                    ScientificContractRepairLayer.RenderPlanCompiler);
            }
        }
    }

    private static SvgSnapshot? ReadSvg(
        ScientificSvgArtifact artifact,
        SvgRenderPlan plan,
        ICollection<ScientificContractFinding> findings)
    {
        var actualHash = Hash(Encoding.UTF8.GetBytes(artifact.Svg));
        if (!HashEquals(actualHash, artifact.Sha256))
        {
            Add(
                findings,
                "svg-content-hash-drift",
                ScientificContractInvariant.SvgAuthority,
                artifact.PlanId,
                $"Recorded SVG hash {artifact.Sha256}; actual {actualHash}.",
                ScientificContractRepairLayer.SvgRenderer);
        }

        try
        {
            var settings = new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null };
            using var textReader = new StringReader(artifact.Svg);
            using var xmlReader = XmlReader.Create(textReader, settings);
            var document = XDocument.Load(xmlReader, LoadOptions.None);
            var root = document.Root;
            if (root?.Name != SvgNamespace + "svg")
            {
                throw new InvalidOperationException("Root element is not SVG.");
            }

            if (!Same((string?)root.Attribute("data-plan-id"), plan.PlanId)
                || !Same((string?)root.Attribute("data-specification-id"), plan.SpecificationId.ToString("D"))
                || !Same((string?)root.Attribute("data-specification-version"), plan.SpecificationVersion.ToString()))
            {
                Add(
                    findings,
                    "svg-root-authority-drift",
                    ScientificContractInvariant.SvgAuthority,
                    artifact.PlanId,
                    "SVG root provenance metadata differs from the render plan.",
                    ScientificContractRepairLayer.SvgRenderer);
            }

            return SvgSnapshot.Create(root);
        }
        catch (Exception exception) when (exception is XmlException or InvalidOperationException)
        {
            Add(
                findings,
                "svg-structure-invalid",
                ScientificContractInvariant.SvgAuthority,
                artifact.PlanId,
                exception.Message,
                ScientificContractRepairLayer.SvgRenderer);
            return null;
        }
    }

    private static void ComparePlanAndSvg(
        SvgRenderPlan plan,
        SvgSnapshot svg,
        ICollection<ScientificContractFinding> findings)
    {
        var svgElements = svg.Elements.ToLookup(item => item.SpecificationItemId, StringComparer.Ordinal);
        foreach (var expected in plan.Elements)
        {
            var matches = svgElements[expected.SourceSpecificationItemId].ToArray();
            if (matches.Length == 0)
            {
                Add(findings, "plan-element-missing-from-svg", ScientificContractInvariant.RequiredElementCoverage,
                    expected.SourceSpecificationItemId, "Render-plan element is missing from SVG.", ScientificContractRepairLayer.SvgRenderer);
                continue;
            }

            var actual = matches[0];
            if (matches.Length > 1 || !Same(actual.ElementId, expected.RenderElementId))
            {
                Add(findings, "svg-element-authority-duplicate", ScientificContractInvariant.NoExtraScientificContent,
                    expected.SourceSpecificationItemId, "SVG element authority is duplicated or uses an unexpected render id.", ScientificContractRepairLayer.SvgRenderer);
            }

            if (!Same(actual.Kind, expected.Kind.ToString())
                || !Same(actual.ExactContent, expected.ExactContent)
                || !Same(actual.ProvenanceKind, expected.ProvenanceKind?.ToString())
                || actual.IsAuthoritative != (expected.Kind != FigureElementKind.DecorativeAsset))
            {
                Add(findings, "scientific-element-content-drift-in-svg", ScientificContractInvariant.ExactScientificContent,
                    expected.SourceSpecificationItemId, $"SVG kind/content/provenance/authority differs; expected '{expected.ExactContent ?? "<none>"}', actual '{actual.ExactContent ?? "<none>"}'.", ScientificContractRepairLayer.SvgRenderer);
            }
        }

        foreach (var extra in svg.Elements.Where(item =>
                     !plan.Elements.Any(expected => Same(expected.SourceSpecificationItemId, item.SpecificationItemId))
                     && item.IsAuthoritative))
        {
            Add(findings, "extra-scientific-element-in-svg", ScientificContractInvariant.NoExtraScientificContent,
                extra.ElementId, $"SVG item '{extra.SpecificationItemId}' has no render-plan authority.", ScientificContractRepairLayer.SvgRenderer);
        }

        var svgRelations = svg.Relations.ToLookup(item => item.SpecificationItemId, StringComparer.Ordinal);
        foreach (var expected in plan.Connections)
        {
            var matches = svgRelations[expected.SourceSpecificationItemId].ToArray();
            if (matches.Length == 0)
            {
                Add(findings, "plan-relation-missing-from-svg", ScientificContractInvariant.RequiredElementCoverage,
                    expected.SourceSpecificationItemId, "Render-plan relation is missing from SVG.", ScientificContractRepairLayer.SvgRenderer);
                continue;
            }

            var actual = matches[0];
            if (matches.Length > 1
                || !Same(actual.RelationId, expected.RenderConnectionId)
                || !Same(actual.Kind, expected.Kind.ToString())
                || !Same(actual.Label, expected.Label)
                || !Same(actual.ProvenanceKind, expected.ProvenanceKind.ToString()))
            {
                Add(findings, "relation-semantic-drift-in-svg", ScientificContractInvariant.RelationSemantics,
                    expected.SourceSpecificationItemId, "SVG relation id, kind, label, or provenance differs from the render plan.", ScientificContractRepairLayer.SvgRenderer);
            }

            if (!Same(actual.Direction, expected.Direction.ToString())
                || !MarkerDirectionMatches(expected.Direction, actual.HasMarkerStart, actual.HasMarkerEnd))
            {
                Add(findings, "relation-direction-drift-in-svg", ScientificContractInvariant.RelationDirection,
                    expected.SourceSpecificationItemId, $"Expected {expected.Direction}; SVG direction/markers are {actual.Direction} (start={actual.HasMarkerStart}, end={actual.HasMarkerEnd}).", ScientificContractRepairLayer.SvgRenderer);
            }
        }

        foreach (var extra in svg.Relations.Where(item =>
                     !plan.Connections.Any(expected => Same(expected.SourceSpecificationItemId, item.SpecificationItemId))))
        {
            Add(findings, "extra-scientific-relation-in-svg", ScientificContractInvariant.NoExtraScientificContent,
                extra.RelationId, $"SVG relation '{extra.SpecificationItemId}' has no render-plan authority.", ScientificContractRepairLayer.SvgRenderer);
        }
    }

    private static void CompareExports(
        ScientificSvgArtifact svgArtifact,
        ScientificFigureExportBundle exports,
        SvgSnapshot svg,
        ICollection<ScientificContractFinding> findings)
    {
        var expectedSemantics = svg.ToExportSemantics();
        var expectedSemanticHash = Hash(JsonSerializer.SerializeToUtf8Bytes(expectedSemantics));
        if (!HashEquals(exports.SourceSvgSha256, svgArtifact.Sha256)
            || !HashEquals(exports.SemanticSha256, expectedSemanticHash))
        {
            Add(findings, "export-semantic-drift", ScientificContractInvariant.ExportEquivalence,
                exports.ExporterId, "Export source hash, semantic fixtures, or semantic hash differs from the approved SVG.", ScientificContractRepairLayer.Exporter);
        }

        var artifactsByFormat = exports.Artifacts.ToLookup(item => item.Format, StringComparer.OrdinalIgnoreCase);
        foreach (var format in new[] { "png", "pdf" })
        {
            if (artifactsByFormat[format].Count() != 1)
            {
                Add(findings, "required-export-missing-or-duplicated", ScientificContractInvariant.ExportEquivalence,
                    format, $"Expected exactly one {format} artifact.", ScientificContractRepairLayer.Exporter);
            }
        }

        foreach (var artifact in exports.Artifacts)
        {
            var actualHash = Hash(artifact.Bytes);
            if (!HashEquals(artifact.Sha256, actualHash)
                || !HashEquals(artifact.SourceSvgSha256, svgArtifact.Sha256)
                || !HashEquals(artifact.SemanticSha256, expectedSemanticHash))
            {
                Add(findings, "export-artifact-hash-drift", ScientificContractInvariant.ExportEquivalence,
                    artifact.Format, $"Artifact content/source/semantic hash binding is invalid; actual content hash is {actualHash}.", ScientificContractRepairLayer.Exporter);
            }
        }
    }

    private static string? SingleSourceId(IEnumerable<SvgRenderElement> elements)
    {
        var items = elements.Take(2).ToArray();
        return items.Length == 1 ? items[0].SourceSpecificationItemId : null;
    }

    private static bool MarkerDirectionMatches(
        FigureRelationDirection direction,
        bool markerStart,
        bool markerEnd)
    {
        return direction switch
        {
            FigureRelationDirection.Undirected => !markerStart && !markerEnd,
            FigureRelationDirection.Directed => !markerStart && markerEnd,
            FigureRelationDirection.Bidirectional => markerStart && markerEnd,
            _ => false,
        };
    }

    private static bool Same(string? first, string? second) =>
        string.Equals(first, second, StringComparison.Ordinal);

    private static string Hash(byte[] bytes) =>
        $"sha256:{Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant()}";

    private static bool HashEquals(string? first, string? second) =>
        !string.IsNullOrWhiteSpace(first)
        && !string.IsNullOrWhiteSpace(second)
        && string.Equals(first.Trim(), second.Trim(), StringComparison.OrdinalIgnoreCase);

    private static void Add(
        ICollection<ScientificContractFinding> findings,
        string code,
        ScientificContractInvariant invariant,
        string responsibleItemId,
        string evidence,
        ScientificContractRepairLayer repairLayer)
    {
        findings.Add(new ScientificContractFinding(
            code,
            invariant,
            responsibleItemId,
            evidence,
            repairLayer));
    }

    private sealed record SvgElementSnapshot(
        string ElementId,
        string SpecificationItemId,
        string Kind,
        bool IsAuthoritative,
        string? ProvenanceKind,
        string? ExactContent);

    private sealed record SvgRelationSnapshot(
        string RelationId,
        string SpecificationItemId,
        string Kind,
        string Direction,
        string? Label,
        string ProvenanceKind,
        bool HasMarkerStart,
        bool HasMarkerEnd);

    private sealed record SvgSnapshot(
        string AccessibilityTitle,
        string AccessibilityDescription,
        IReadOnlyList<SvgElementSnapshot> Elements,
        IReadOnlyList<SvgRelationSnapshot> Relations)
    {
        public static SvgSnapshot Create(XElement root)
        {
            var elements = root.Descendants(SvgNamespace + "g")
                .Where(item => item.Attribute("data-element-kind") is not null)
                .Select(item => new SvgElementSnapshot(
                    Required(item, "id"),
                    Required(item, "data-spec-id"),
                    Required(item, "data-element-kind"),
                    ParseBoolean(item, "data-authoritative"),
                    (string?)item.Attribute("data-provenance-kind"),
                    item.Elements(SvgNamespace + "text")
                        .FirstOrDefault(text => text.Attribute("data-content-kind") is not null)?.Value))
                .ToArray();
            var relations = root.Descendants(SvgNamespace + "path")
                .Where(item => item.Attribute("data-relation-kind") is not null)
                .Select(item => new SvgRelationSnapshot(
                    Required(item, "id"),
                    Required(item, "data-spec-id"),
                    Required(item, "data-relation-kind"),
                    Required(item, "data-direction"),
                    (string?)item.Attribute("data-exact-label")
                        ?? ReadRelationLabel(item.Parent),
                    Required(item, "data-provenance-kind"),
                    item.Attribute("marker-start") is not null,
                    item.Attribute("marker-end") is not null))
                .ToArray();
            return new SvgSnapshot(
                root.Element(SvgNamespace + "title")?.Value
                    ?? throw new InvalidOperationException("SVG accessibility title is missing."),
                root.Element(SvgNamespace + "desc")?.Value
                    ?? throw new InvalidOperationException("SVG accessibility description is missing."),
                elements,
                relations);
        }

        public ScientificExportSemantics ToExportSemantics()
        {
            return new ScientificExportSemantics(
                AccessibilityTitle,
                AccessibilityDescription,
                Elements.Select(item => new ScientificExportElementFixture(
                    item.ElementId,
                    item.SpecificationItemId,
                    item.Kind,
                    item.IsAuthoritative,
                    item.ProvenanceKind)).ToArray(),
                Elements.Where(item => item.ExactContent is not null).Select(item =>
                    new ScientificExportTextFixture(
                        item.ElementId,
                        item.SpecificationItemId,
                        item.Kind,
                        item.ExactContent!)).ToArray(),
                Relations.Select(item => new ScientificExportRelationFixture(
                    item.RelationId,
                    item.SpecificationItemId,
                    item.Direction,
                    item.Label,
                    item.ProvenanceKind)).ToArray());
        }

        private static string Required(XElement element, string attribute) =>
            (string?)element.Attribute(attribute)
            ?? throw new InvalidOperationException($"SVG attribute is missing: {attribute}.");

        private static string? ReadRelationLabel(XElement? group)
        {
            var lines = group?.Elements(SvgNamespace + "text")
                .Where(text => text.Attribute("data-relation-label") is not null)
                .OrderBy(text => (int?)text.Attribute("data-label-line") ?? int.MaxValue)
                .Select(text => text.Value)
                .ToArray() ?? [];
            return lines.Length == 0 ? null : string.Concat(lines);
        }

        private static bool ParseBoolean(XElement element, string attribute)
        {
            return Required(element, attribute) switch
            {
                "true" => true,
                "false" => false,
                _ => throw new InvalidOperationException($"SVG boolean attribute is invalid: {attribute}."),
            };
        }
    }
}
