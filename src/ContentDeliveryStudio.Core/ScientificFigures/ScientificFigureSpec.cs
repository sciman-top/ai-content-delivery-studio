namespace ContentDeliveryStudio.Core.ScientificFigures;

public sealed record ScientificFigureProvenance
{
    private ScientificFigureProvenance(
        ScientificProvenanceKind kind,
        string? claimId,
        ClaimEvidenceLink? evidence,
        string? conventionId,
        string? conventionStatement)
    {
        Kind = kind;
        ClaimId = claimId;
        Evidence = evidence;
        ConventionId = conventionId;
        ConventionStatement = conventionStatement;
    }

    public ScientificProvenanceKind Kind { get; }

    public string? ClaimId { get; }

    public ClaimEvidenceLink? Evidence { get; }

    public string? ConventionId { get; }

    public string? ConventionStatement { get; }

    public static ScientificFigureProvenance FromEvidence(
        ScientificClaim claim,
        ClaimEvidenceLink evidence)
    {
        ArgumentNullException.ThrowIfNull(claim);
        ArgumentNullException.ThrowIfNull(evidence);
        if (claim.Status != ScientificClaimStatus.Accepted
            || !claim.EvidenceLinks.Contains(evidence)
            || evidence.ValidationState != EvidenceValidationState.Validated
            || evidence.Role == ClaimEvidenceRole.Contradiction)
        {
            throw new ArgumentException(
                "Figure provenance requires validated, non-contradictory evidence from an accepted claim.",
                nameof(evidence));
        }

        return new ScientificFigureProvenance(
            ScientificProvenanceKind.ClaimEvidence,
            claim.ClaimId,
            evidence,
            null,
            null);
    }

    public static ScientificFigureProvenance FromConvention(
        string conventionId,
        string conventionStatement)
    {
        var normalizedId =
            ScientificSourceGuard.RequireText(conventionId, nameof(conventionId));
        if (!normalizedId.StartsWith("scientific_convention:", StringComparison.Ordinal))
        {
            throw new ArgumentException(
                "Scientific convention ids must use the scientific_convention: prefix.",
                nameof(conventionId));
        }

        return new ScientificFigureProvenance(
            ScientificProvenanceKind.ScientificConvention,
            null,
            null,
            normalizedId,
            ScientificSourceGuard.RequireText(
                conventionStatement,
                nameof(conventionStatement)));
    }
}

public sealed record FigureElementSpec
{
    private FigureElementSpec(
        string elementId,
        string scientificMeaning,
        FigureElementKind kind,
        string? labelOrFormula,
        string renderStrategy,
        FigureContentRequirement requirement,
        bool isCritical,
        ScientificFigureProvenance? provenance)
    {
        ElementId = elementId;
        ScientificMeaning = scientificMeaning;
        Kind = kind;
        LabelOrFormula = labelOrFormula;
        RenderStrategy = renderStrategy;
        Requirement = requirement;
        IsCritical = isCritical;
        Provenance = provenance;
    }

    public string ElementId { get; }

    public string ScientificMeaning { get; }

    public FigureElementKind Kind { get; }

    public string? LabelOrFormula { get; }

    public string RenderStrategy { get; }

    public FigureContentRequirement Requirement { get; }

    public bool IsCritical { get; }

    public ScientificFigureProvenance? Provenance { get; }

    public static FigureElementSpec Create(
        string elementId,
        string scientificMeaning,
        FigureElementKind kind,
        string? labelOrFormula,
        string renderStrategy,
        FigureContentRequirement requirement,
        bool isCritical,
        ScientificFigureProvenance? provenance)
    {
        ScientificSourceGuard.RequireDefined(kind, nameof(kind));
        ScientificSourceGuard.RequireDefined(requirement, nameof(requirement));
        var isDecorative = kind == FigureElementKind.DecorativeAsset;
        if (!isDecorative && provenance is null)
        {
            throw new ArgumentException(
                "Scientific figure elements require evidence or scientific-convention provenance.",
                nameof(provenance));
        }

        if (isDecorative && isCritical)
        {
            throw new ArgumentException(
                "A decorative element cannot be critical scientific content.",
                nameof(isCritical));
        }

        var normalizedLabelOrFormula =
            ScientificSourceGuard.NormalizeOptionalText(labelOrFormula);
        var requiresExactContent = kind is
            FigureElementKind.Label
            or FigureElementKind.Symbol
            or FigureElementKind.Formula
            or FigureElementKind.Value
            or FigureElementKind.Unit
            or FigureElementKind.Legend
            or FigureElementKind.Annotation;
        if (requiresExactContent && normalizedLabelOrFormula is null)
        {
            throw new ArgumentException(
                "This scientific element kind requires exact label or formula content.",
                nameof(labelOrFormula));
        }

        return new FigureElementSpec(
            ScientificSourceGuard.RequireText(elementId, nameof(elementId)),
            ScientificSourceGuard.RequireText(scientificMeaning, nameof(scientificMeaning)),
            kind,
            normalizedLabelOrFormula,
            ScientificSourceGuard.RequireText(renderStrategy, nameof(renderStrategy)),
            requirement,
            isCritical,
            provenance);
    }
}

public sealed record FigureRelationSpec
{
    private FigureRelationSpec(
        string relationId,
        string sourceElementId,
        string targetElementId,
        FigureRelationKind kind,
        FigureRelationDirection direction,
        string? label,
        string scientificMeaning,
        string representationConstraint,
        FigureContentRequirement requirement,
        bool isCritical,
        ScientificFigureProvenance provenance)
    {
        RelationId = relationId;
        SourceElementId = sourceElementId;
        TargetElementId = targetElementId;
        Kind = kind;
        Direction = direction;
        Label = label;
        ScientificMeaning = scientificMeaning;
        RepresentationConstraint = representationConstraint;
        Requirement = requirement;
        IsCritical = isCritical;
        Provenance = provenance;
    }

    public string RelationId { get; }

    public string SourceElementId { get; }

    public string TargetElementId { get; }

    public FigureRelationKind Kind { get; }

    public FigureRelationDirection Direction { get; }

    public string? Label { get; }

    public string ScientificMeaning { get; }

    public string RepresentationConstraint { get; }

    public FigureContentRequirement Requirement { get; }

    public bool IsCritical { get; }

    public ScientificFigureProvenance Provenance { get; }

    public static FigureRelationSpec Create(
        string relationId,
        string sourceElementId,
        string targetElementId,
        FigureRelationKind kind,
        FigureRelationDirection direction,
        string? label,
        string scientificMeaning,
        string representationConstraint,
        FigureContentRequirement requirement,
        bool isCritical,
        ScientificFigureProvenance? provenance)
    {
        ScientificSourceGuard.RequireDefined(kind, nameof(kind));
        ScientificSourceGuard.RequireDefined(direction, nameof(direction));
        ScientificSourceGuard.RequireDefined(requirement, nameof(requirement));
        if (provenance is null)
        {
            throw new ArgumentException(
                "Scientific figure relations require evidence or scientific-convention provenance.",
                nameof(provenance));
        }

        var normalizedSourceId =
            ScientificSourceGuard.RequireText(sourceElementId, nameof(sourceElementId));
        var normalizedTargetId =
            ScientificSourceGuard.RequireText(targetElementId, nameof(targetElementId));
        if (string.Equals(normalizedSourceId, normalizedTargetId, StringComparison.Ordinal))
        {
            throw new ArgumentException(
                "A figure relation requires different source and target elements.",
                nameof(targetElementId));
        }

        return new FigureRelationSpec(
            ScientificSourceGuard.RequireText(relationId, nameof(relationId)),
            normalizedSourceId,
            normalizedTargetId,
            kind,
            direction,
            ScientificSourceGuard.NormalizeOptionalText(label),
            ScientificSourceGuard.RequireText(scientificMeaning, nameof(scientificMeaning)),
            ScientificSourceGuard.RequireText(
                representationConstraint,
                nameof(representationConstraint)),
            requirement,
            isCritical,
            provenance);
    }
}

public sealed record ScientificFigureIssue
{
    private ScientificFigureIssue(
        string issueId,
        ScientificFigureIssueKind kind,
        string description,
        ScientificFigureIssueStatus status,
        string? resolution)
    {
        IssueId = issueId;
        Kind = kind;
        Description = description;
        Status = status;
        Resolution = resolution;
    }

    public string IssueId { get; }

    public ScientificFigureIssueKind Kind { get; }

    public string Description { get; }

    public ScientificFigureIssueStatus Status { get; }

    public string? Resolution { get; }

    public static ScientificFigureIssue Create(
        string issueId,
        ScientificFigureIssueKind kind,
        string description,
        ScientificFigureIssueStatus status,
        string? resolution = null)
    {
        ScientificSourceGuard.RequireDefined(kind, nameof(kind));
        ScientificSourceGuard.RequireDefined(status, nameof(status));
        var normalizedResolution = ScientificSourceGuard.NormalizeOptionalText(resolution);
        if (status == ScientificFigureIssueStatus.Resolved && normalizedResolution is null)
        {
            throw new ArgumentException(
                "A resolved scientific figure issue requires a resolution.",
                nameof(resolution));
        }

        if (status == ScientificFigureIssueStatus.Unresolved && normalizedResolution is not null)
        {
            throw new ArgumentException(
                "An unresolved scientific figure issue cannot carry a resolution.",
                nameof(resolution));
        }

        return new ScientificFigureIssue(
            ScientificSourceGuard.RequireText(issueId, nameof(issueId)),
            kind,
            ScientificSourceGuard.RequireText(description, nameof(description)),
            status,
            normalizedResolution);
    }
}

public sealed record ScientificFigureSpec
{
    private ScientificFigureSpec(
        Guid specificationId,
        Guid understandingId,
        int understandingVersion,
        Guid sourceAssetId,
        string sourceSha256,
        int version,
        string purpose,
        string centralMessage,
        string audience,
        bool isSchematic,
        ScientificFigureRiskLevel riskLevel,
        IReadOnlyList<FigureElementSpec> elements,
        IReadOnlyList<FigureRelationSpec> relations,
        IReadOnlyList<ScientificFigureIssue> issues,
        IReadOnlyList<string> blockingCodes)
    {
        SpecificationId = specificationId;
        UnderstandingId = understandingId;
        UnderstandingVersion = understandingVersion;
        SourceAssetId = sourceAssetId;
        SourceSha256 = sourceSha256;
        Version = version;
        Purpose = purpose;
        CentralMessage = centralMessage;
        Audience = audience;
        IsSchematic = isSchematic;
        RiskLevel = riskLevel;
        Elements = elements;
        Relations = relations;
        Issues = issues;
        BlockingCodes = blockingCodes;
        Status = blockingCodes.Count == 0
            ? ScientificFigureSpecStatus.ReadyForGate1
            : ScientificFigureSpecStatus.Blocked;
    }

    public Guid SpecificationId { get; }

    public Guid UnderstandingId { get; }

    public int UnderstandingVersion { get; }

    public Guid SourceAssetId { get; }

    public string SourceSha256 { get; }

    public int Version { get; }

    public string Purpose { get; }

    public string CentralMessage { get; }

    public string Audience { get; }

    public bool IsSchematic { get; }

    public ScientificFigureRiskLevel RiskLevel { get; }

    public IReadOnlyList<FigureElementSpec> Elements { get; }

    public IReadOnlyList<FigureRelationSpec> Relations { get; }

    public IReadOnlyList<ScientificFigureIssue> Issues { get; }

    public ScientificFigureSpecStatus Status { get; }

    public IReadOnlyList<string> BlockingCodes { get; }

    public static ScientificFigureSpec Create(
        Guid specificationId,
        ScientificDocumentUnderstanding understanding,
        string purpose,
        string centralMessage,
        string audience,
        bool isSchematic,
        ScientificFigureRiskLevel riskLevel,
        IReadOnlyList<FigureElementSpec> elements,
        IReadOnlyList<FigureRelationSpec> relations,
        IReadOnlyList<ScientificFigureIssue> issues)
    {
        return CreateVersion(
            specificationId,
            understanding,
            version: 1,
            purpose,
            centralMessage,
            audience,
            isSchematic,
            riskLevel,
            elements,
            relations,
            issues);
    }

    public ScientificFigureSpec ReviseScientificContent(
        ScientificDocumentUnderstanding understanding,
        string centralMessage,
        IReadOnlyList<FigureElementSpec> elements,
        IReadOnlyList<FigureRelationSpec> relations,
        IReadOnlyList<ScientificFigureIssue> issues)
    {
        return CreateVersion(
            SpecificationId,
            understanding,
            checked(Version + 1),
            Purpose,
            centralMessage,
            Audience,
            IsSchematic,
            RiskLevel,
            elements,
            relations,
            issues);
    }

    private static ScientificFigureSpec CreateVersion(
        Guid specificationId,
        ScientificDocumentUnderstanding understanding,
        int version,
        string purpose,
        string centralMessage,
        string audience,
        bool isSchematic,
        ScientificFigureRiskLevel riskLevel,
        IReadOnlyList<FigureElementSpec> elements,
        IReadOnlyList<FigureRelationSpec> relations,
        IReadOnlyList<ScientificFigureIssue> issues)
    {
        if (specificationId == Guid.Empty)
        {
            throw new ArgumentException("Specification id cannot be empty.", nameof(specificationId));
        }

        ArgumentNullException.ThrowIfNull(understanding);
        ArgumentNullException.ThrowIfNull(elements);
        ArgumentNullException.ThrowIfNull(relations);
        ArgumentNullException.ThrowIfNull(issues);
        ScientificSourceGuard.RequireDefined(riskLevel, nameof(riskLevel));
        ScientificUnderstandingGuard.RequireNoNullElements(elements, nameof(elements));
        ScientificUnderstandingGuard.RequireNoNullElements(relations, nameof(relations));
        ScientificUnderstandingGuard.RequireNoNullElements(issues, nameof(issues));
        if (elements.Count == 0)
        {
            throw new ArgumentException(
                "A scientific figure specification requires at least one element.",
                nameof(elements));
        }

        var elementSnapshot = Array.AsReadOnly(elements.ToArray());
        var relationSnapshot = Array.AsReadOnly(relations.ToArray());
        var issueSnapshot = Array.AsReadOnly(issues.ToArray());
        ScientificUnderstandingGuard.RequireUnique(
            elementSnapshot.Select(element => element.ElementId),
            nameof(elements));
        ScientificUnderstandingGuard.RequireUnique(
            relationSnapshot.Select(relation => relation.RelationId),
            nameof(relations));
        ScientificUnderstandingGuard.RequireUnique(
            issueSnapshot.Select(issue => issue.IssueId),
            nameof(issues));
        ValidateRelationEndpoints(elementSnapshot, relationSnapshot);

        var blockingCodes = BuildBlockingCodes(
            understanding,
            elementSnapshot,
            relationSnapshot,
            issueSnapshot);
        return new ScientificFigureSpec(
            specificationId,
            understanding.UnderstandingId,
            understanding.Version,
            understanding.SourceAssetId,
            understanding.SourceSha256,
            version,
            ScientificSourceGuard.RequireText(purpose, nameof(purpose)),
            ScientificSourceGuard.RequireText(centralMessage, nameof(centralMessage)),
            ScientificSourceGuard.RequireText(audience, nameof(audience)),
            isSchematic,
            riskLevel,
            elementSnapshot,
            relationSnapshot,
            issueSnapshot,
            blockingCodes);
    }

    private static void ValidateRelationEndpoints(
        IReadOnlyList<FigureElementSpec> elements,
        IReadOnlyList<FigureRelationSpec> relations)
    {
        var elementsById = elements.ToDictionary(
            element => element.ElementId,
            StringComparer.Ordinal);
        foreach (var relation in relations)
        {
            if (!elementsById.TryGetValue(relation.SourceElementId, out var source)
                || !elementsById.TryGetValue(relation.TargetElementId, out var target))
            {
                throw new ArgumentException(
                    $"Figure relation '{relation.RelationId}' references an unknown element.",
                    nameof(relations));
            }

            if (relation.Requirement != FigureContentRequirement.Forbidden
                && (source.Requirement == FigureContentRequirement.Forbidden
                    || target.Requirement == FigureContentRequirement.Forbidden))
            {
                throw new ArgumentException(
                    $"Included relation '{relation.RelationId}' cannot reference a forbidden element.",
                    nameof(relations));
            }
        }
    }

    private static IReadOnlyList<string> BuildBlockingCodes(
        ScientificDocumentUnderstanding understanding,
        IReadOnlyList<FigureElementSpec> elements,
        IReadOnlyList<FigureRelationSpec> relations,
        IReadOnlyList<ScientificFigureIssue> issues)
    {
        var codes = new List<string>();
        if (understanding.Status != ScientificUnderstandingStatus.ReadyForApproval)
        {
            codes.Add("understanding-blocked");
        }

        if (!elements.Any(element => element.Requirement == FigureContentRequirement.Required))
        {
            codes.Add("no-required-elements");
        }

        codes.AddRange(
            elements
                .Where(element =>
                    element.Provenance is not null
                    && !AuthorityBelongsToUnderstanding(element.Provenance, understanding))
                .Select(element => $"unsupported-element:{element.ElementId}"));
        codes.AddRange(
            relations
                .Where(relation =>
                    !AuthorityBelongsToUnderstanding(relation.Provenance, understanding))
                .Select(relation => $"unsupported-relation:{relation.RelationId}"));
        codes.AddRange(
            issues
                .Where(issue => issue.Status == ScientificFigureIssueStatus.Unresolved)
                .Select(issue =>
                    $"unresolved-{IssueCode(issue.Kind)}:{issue.IssueId}"));

        return Array.AsReadOnly(codes.Distinct(StringComparer.Ordinal).ToArray());
    }

    private static bool AuthorityBelongsToUnderstanding(
        ScientificFigureProvenance provenance,
        ScientificDocumentUnderstanding understanding)
    {
        if (provenance.Kind == ScientificProvenanceKind.ScientificConvention)
        {
            return provenance.ConventionId is not null
                && provenance.ConventionStatement is not null;
        }

        var claim = understanding.Claims.FirstOrDefault(candidate =>
            string.Equals(candidate.ClaimId, provenance.ClaimId, StringComparison.Ordinal));
        return claim is not null
            && claim.Status == ScientificClaimStatus.Accepted
            && provenance.Evidence is not null
            && claim.EvidenceLinks.Contains(provenance.Evidence)
            && provenance.Evidence.SourceAssetId == understanding.SourceAssetId
            && string.Equals(
                provenance.Evidence.SourceSha256,
                understanding.SourceSha256,
                StringComparison.Ordinal);
    }

    private static string IssueCode(ScientificFigureIssueKind kind)
    {
        return kind switch
        {
            ScientificFigureIssueKind.Conflict => "conflict",
            ScientificFigureIssueKind.Uncertainty => "uncertainty",
            ScientificFigureIssueKind.MissingCoverage => "missing-coverage",
            ScientificFigureIssueKind.UnsupportedContent => "unsupported-content",
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unsupported issue kind."),
        };
    }
}

public enum ScientificProvenanceKind
{
    ClaimEvidence = 0,
    ScientificConvention = 1,
}

public enum FigureElementKind
{
    Entity = 0,
    Label = 1,
    Symbol = 2,
    Formula = 3,
    Value = 4,
    Unit = 5,
    Legend = 6,
    Annotation = 7,
    DecorativeAsset = 8,
}

public enum FigureRelationKind
{
    Causes = 0,
    TransformsInto = 1,
    TransfersTo = 2,
    ComparesWith = 3,
    Constrains = 4,
    AssociatesWith = 5,
}

public enum FigureRelationDirection
{
    Directed = 0,
    Bidirectional = 1,
    Undirected = 2,
}

public enum FigureContentRequirement
{
    Required = 0,
    Optional = 1,
    Forbidden = 2,
}

public enum ScientificFigureRiskLevel
{
    Low = 0,
    Medium = 1,
    High = 2,
}

public enum ScientificFigureIssueKind
{
    Conflict = 0,
    Uncertainty = 1,
    MissingCoverage = 2,
    UnsupportedContent = 3,
}

public enum ScientificFigureIssueStatus
{
    Unresolved = 0,
    Resolved = 1,
}

public enum ScientificFigureSpecStatus
{
    ReadyForGate1 = 0,
    Blocked = 1,
}
