namespace ContentDeliveryStudio.Core.ScientificFigures;

public sealed record ScientificTermDefinition
{
    private ScientificTermDefinition(
        string termId,
        string canonicalTerm,
        string definition,
        IReadOnlyList<string> aliases)
    {
        TermId = termId;
        CanonicalTerm = canonicalTerm;
        Definition = definition;
        Aliases = aliases;
    }

    public string TermId { get; }

    public string CanonicalTerm { get; }

    public string Definition { get; }

    public IReadOnlyList<string> Aliases { get; }

    public static ScientificTermDefinition Create(
        string termId,
        string canonicalTerm,
        string definition,
        IReadOnlyList<string> aliases)
    {
        ArgumentNullException.ThrowIfNull(aliases);
        var normalizedCanonicalTerm =
            ScientificSourceGuard.RequireText(canonicalTerm, nameof(canonicalTerm));
        var aliasSnapshot = aliases
            .Select(alias => ScientificSourceGuard.RequireText(alias, nameof(aliases)))
            .Where(alias => !string.Equals(alias, normalizedCanonicalTerm, StringComparison.OrdinalIgnoreCase))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return new ScientificTermDefinition(
            ScientificSourceGuard.RequireText(termId, nameof(termId)),
            normalizedCanonicalTerm,
            ScientificSourceGuard.RequireText(definition, nameof(definition)),
            Array.AsReadOnly(aliasSnapshot));
    }
}

public sealed record ClaimEvidenceLink
{
    private ClaimEvidenceLink(
        Guid sourceAssetId,
        string sourceSha256,
        string sourceBlockId,
        ScientificSourceLocation location,
        string quotedText,
        ClaimEvidenceRole role,
        double confidence,
        EvidenceValidationState validationState)
    {
        SourceAssetId = sourceAssetId;
        SourceSha256 = sourceSha256;
        SourceBlockId = sourceBlockId;
        Location = location;
        QuotedText = quotedText;
        Role = role;
        Confidence = confidence;
        ValidationState = validationState;
    }

    public Guid SourceAssetId { get; }

    public string SourceSha256 { get; }

    public string SourceBlockId { get; }

    public ScientificSourceLocation Location { get; }

    public string QuotedText { get; }

    public ClaimEvidenceRole Role { get; }

    public double Confidence { get; }

    public EvidenceValidationState ValidationState { get; }

    public static ClaimEvidenceLink Create(
        ScientificDocumentExtraction extraction,
        ScientificSourceBlock sourceBlock,
        string quotedText,
        ClaimEvidenceRole role,
        double confidence,
        EvidenceValidationState validationState)
    {
        ArgumentNullException.ThrowIfNull(extraction);
        ArgumentNullException.ThrowIfNull(sourceBlock);
        ScientificSourceGuard.RequireDefined(role, nameof(role));
        ScientificSourceGuard.RequireDefined(validationState, nameof(validationState));
        ScientificUnderstandingGuard.RequireConfidence(confidence, nameof(confidence));

        var authoritativeBlock = extraction.Blocks.FirstOrDefault(block =>
            string.Equals(block.BlockId, sourceBlock.BlockId, StringComparison.Ordinal));
        if (authoritativeBlock is null || authoritativeBlock != sourceBlock)
        {
            throw new ArgumentException(
                "Evidence source block must belong to the supplied scientific extraction.",
                nameof(sourceBlock));
        }

        var normalizedQuote = ScientificSourceGuard.RequireText(quotedText, nameof(quotedText));
        if (sourceBlock.OriginalText is null
            || !sourceBlock.OriginalText.Contains(normalizedQuote, StringComparison.Ordinal))
        {
            throw new ArgumentException(
                "Evidence quotation must occur verbatim in the scientific source block.",
                nameof(quotedText));
        }

        return new ClaimEvidenceLink(
            extraction.SourceAssetId,
            extraction.SourceSha256,
            sourceBlock.BlockId,
            sourceBlock.Location,
            normalizedQuote,
            role,
            confidence,
            validationState);
    }
}

public sealed record ScientificClaim
{
    private ScientificClaim(
        string claimId,
        ScientificClaimCategory category,
        string normalizedStatement,
        string sourceWording,
        double confidence,
        ScientificClaimStatus status,
        IReadOnlyList<ClaimEvidenceLink> evidenceLinks,
        IReadOnlyList<ClaimEvidenceLink> supportingEvidence)
    {
        ClaimId = claimId;
        Category = category;
        NormalizedStatement = normalizedStatement;
        SourceWording = sourceWording;
        Confidence = confidence;
        Status = status;
        EvidenceLinks = evidenceLinks;
        SupportingEvidence = supportingEvidence;
    }

    public string ClaimId { get; }

    public ScientificClaimCategory Category { get; }

    public string NormalizedStatement { get; }

    public string SourceWording { get; }

    public double Confidence { get; }

    public ScientificClaimStatus Status { get; }

    public IReadOnlyList<ClaimEvidenceLink> EvidenceLinks { get; }

    public IReadOnlyList<ClaimEvidenceLink> SupportingEvidence { get; }

    public bool HasValidatedQualification =>
        EvidenceLinks.Any(link =>
            link.Role == ClaimEvidenceRole.Qualification
            && link.ValidationState == EvidenceValidationState.Validated);

    public bool HasValidatedContradiction =>
        EvidenceLinks.Any(link =>
            link.Role == ClaimEvidenceRole.Contradiction
            && link.ValidationState == EvidenceValidationState.Validated);

    public static ScientificClaim Create(
        string claimId,
        ScientificClaimCategory category,
        string normalizedStatement,
        string sourceWording,
        double confidence,
        ScientificClaimStatus status,
        IReadOnlyList<ClaimEvidenceLink> evidenceLinks)
    {
        ArgumentNullException.ThrowIfNull(evidenceLinks);
        ScientificSourceGuard.RequireDefined(category, nameof(category));
        ScientificSourceGuard.RequireDefined(status, nameof(status));
        ScientificUnderstandingGuard.RequireConfidence(confidence, nameof(confidence));
        ScientificUnderstandingGuard.RequireNoNullElements(evidenceLinks, nameof(evidenceLinks));

        var evidenceSnapshot = Array.AsReadOnly(evidenceLinks.ToArray());
        var supportingEvidence = Array.AsReadOnly(
            evidenceSnapshot
                .Where(link =>
                    link.ValidationState == EvidenceValidationState.Validated
                    && link.Role is ClaimEvidenceRole.Support or ClaimEvidenceRole.Definition)
                .ToArray());
        if (status == ScientificClaimStatus.Accepted && supportingEvidence.Count == 0)
        {
            throw new ArgumentException(
                "An accepted scientific claim requires validated supporting or definition evidence.",
                nameof(evidenceLinks));
        }

        return new ScientificClaim(
            ScientificSourceGuard.RequireText(claimId, nameof(claimId)),
            category,
            ScientificSourceGuard.RequireText(normalizedStatement, nameof(normalizedStatement)),
            ScientificSourceGuard.RequireText(sourceWording, nameof(sourceWording)),
            confidence,
            status,
            evidenceSnapshot,
            supportingEvidence);
    }
}

public sealed record ScientificClaimConflict
{
    private ScientificClaimConflict(
        string conflictId,
        string firstClaimId,
        string secondClaimId,
        string description,
        ScientificConflictStatus status,
        string? resolution)
    {
        ConflictId = conflictId;
        FirstClaimId = firstClaimId;
        SecondClaimId = secondClaimId;
        Description = description;
        Status = status;
        Resolution = resolution;
    }

    public string ConflictId { get; }

    public string FirstClaimId { get; }

    public string SecondClaimId { get; }

    public string Description { get; }

    public ScientificConflictStatus Status { get; }

    public string? Resolution { get; }

    public static ScientificClaimConflict Create(
        string conflictId,
        string firstClaimId,
        string secondClaimId,
        string description,
        ScientificConflictStatus status,
        string? resolution)
    {
        ScientificSourceGuard.RequireDefined(status, nameof(status));
        var normalizedFirstClaimId =
            ScientificSourceGuard.RequireText(firstClaimId, nameof(firstClaimId));
        var normalizedSecondClaimId =
            ScientificSourceGuard.RequireText(secondClaimId, nameof(secondClaimId));
        if (string.Equals(normalizedFirstClaimId, normalizedSecondClaimId, StringComparison.Ordinal))
        {
            throw new ArgumentException(
                "A scientific conflict requires two different claims.",
                nameof(secondClaimId));
        }

        var normalizedResolution = ScientificSourceGuard.NormalizeOptionalText(resolution);
        if (status == ScientificConflictStatus.Resolved && normalizedResolution is null)
        {
            throw new ArgumentException(
                "A resolved scientific conflict requires a resolution.",
                nameof(resolution));
        }

        if (status == ScientificConflictStatus.Unresolved && normalizedResolution is not null)
        {
            throw new ArgumentException(
                "An unresolved scientific conflict cannot carry a resolution.",
                nameof(resolution));
        }

        return new ScientificClaimConflict(
            ScientificSourceGuard.RequireText(conflictId, nameof(conflictId)),
            normalizedFirstClaimId,
            normalizedSecondClaimId,
            ScientificSourceGuard.RequireText(description, nameof(description)),
            status,
            normalizedResolution);
    }
}

public sealed record ScientificCoverageRequirement
{
    private ScientificCoverageRequirement(
        string requirementId,
        string description,
        bool isRequired,
        ScientificCoverageStatus status,
        IReadOnlyList<string> claimIds)
    {
        RequirementId = requirementId;
        Description = description;
        IsRequired = isRequired;
        Status = status;
        ClaimIds = claimIds;
    }

    public string RequirementId { get; }

    public string Description { get; }

    public bool IsRequired { get; }

    public ScientificCoverageStatus Status { get; }

    public IReadOnlyList<string> ClaimIds { get; }

    public static ScientificCoverageRequirement Create(
        string requirementId,
        string description,
        bool isRequired,
        ScientificCoverageStatus status,
        IReadOnlyList<string> claimIds)
    {
        ArgumentNullException.ThrowIfNull(claimIds);
        ScientificSourceGuard.RequireDefined(status, nameof(status));
        var claimIdSnapshot = claimIds
            .Select(claimId => ScientificSourceGuard.RequireText(claimId, nameof(claimIds)))
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        return new ScientificCoverageRequirement(
            ScientificSourceGuard.RequireText(requirementId, nameof(requirementId)),
            ScientificSourceGuard.RequireText(description, nameof(description)),
            isRequired,
            status,
            Array.AsReadOnly(claimIdSnapshot));
    }
}

public sealed record ScientificDocumentUnderstanding
{
    private ScientificDocumentUnderstanding(
        Guid understandingId,
        Guid sourceAssetId,
        string sourceSha256,
        string objective,
        int version,
        IReadOnlyList<ScientificTermDefinition> terminology,
        IReadOnlyList<ScientificClaim> claims,
        IReadOnlyList<ScientificClaimConflict> conflicts,
        IReadOnlyList<ScientificCoverageRequirement> coverage,
        IReadOnlyList<string> blockingCodes)
    {
        UnderstandingId = understandingId;
        SourceAssetId = sourceAssetId;
        SourceSha256 = sourceSha256;
        Objective = objective;
        Version = version;
        Terminology = terminology;
        Claims = claims;
        Conflicts = conflicts;
        Coverage = coverage;
        BlockingCodes = blockingCodes;
        Status = blockingCodes.Count == 0
            ? ScientificUnderstandingStatus.ReadyForApproval
            : ScientificUnderstandingStatus.Blocked;
    }

    public Guid UnderstandingId { get; }

    public Guid SourceAssetId { get; }

    public string SourceSha256 { get; }

    public string Objective { get; }

    public int Version { get; }

    public IReadOnlyList<ScientificTermDefinition> Terminology { get; }

    public IReadOnlyList<ScientificClaim> Claims { get; }

    public IReadOnlyList<ScientificClaimConflict> Conflicts { get; }

    public IReadOnlyList<ScientificCoverageRequirement> Coverage { get; }

    public ScientificUnderstandingStatus Status { get; }

    public IReadOnlyList<string> BlockingCodes { get; }

    public static ScientificDocumentUnderstanding Create(
        Guid understandingId,
        ScientificDocumentExtraction extraction,
        string objective,
        int version,
        IReadOnlyList<ScientificTermDefinition> terminology,
        IReadOnlyList<ScientificClaim> claims,
        IReadOnlyList<ScientificClaimConflict> conflicts,
        IReadOnlyList<ScientificCoverageRequirement> coverage)
    {
        if (understandingId == Guid.Empty)
        {
            throw new ArgumentException("Understanding id cannot be empty.", nameof(understandingId));
        }

        ArgumentNullException.ThrowIfNull(extraction);
        ArgumentNullException.ThrowIfNull(terminology);
        ArgumentNullException.ThrowIfNull(claims);
        ArgumentNullException.ThrowIfNull(conflicts);
        ArgumentNullException.ThrowIfNull(coverage);
        if (version < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(version), version, "Version must be positive.");
        }

        ScientificUnderstandingGuard.RequireNoNullElements(terminology, nameof(terminology));
        ScientificUnderstandingGuard.RequireNoNullElements(claims, nameof(claims));
        ScientificUnderstandingGuard.RequireNoNullElements(conflicts, nameof(conflicts));
        ScientificUnderstandingGuard.RequireNoNullElements(coverage, nameof(coverage));
        var terminologySnapshot = Array.AsReadOnly(terminology.ToArray());
        var claimSnapshot = Array.AsReadOnly(claims.ToArray());
        var conflictSnapshot = Array.AsReadOnly(conflicts.ToArray());
        var coverageSnapshot = Array.AsReadOnly(coverage.ToArray());

        ScientificUnderstandingGuard.RequireUnique(
            terminologySnapshot.Select(term => term.TermId),
            nameof(terminology));
        ScientificUnderstandingGuard.RequireUnique(
            claimSnapshot.Select(claim => claim.ClaimId),
            nameof(claims));
        ScientificUnderstandingGuard.RequireUnique(
            conflictSnapshot.Select(conflict => conflict.ConflictId),
            nameof(conflicts));
        ScientificUnderstandingGuard.RequireUnique(
            coverageSnapshot.Select(item => item.RequirementId),
            nameof(coverage));
        ValidateReferences(claimSnapshot, conflictSnapshot, coverageSnapshot);

        var blockingCodes = BuildBlockingCodes(
            extraction,
            claimSnapshot,
            conflictSnapshot,
            coverageSnapshot);
        return new ScientificDocumentUnderstanding(
            understandingId,
            extraction.SourceAssetId,
            extraction.SourceSha256,
            ScientificSourceGuard.RequireText(objective, nameof(objective)),
            version,
            terminologySnapshot,
            claimSnapshot,
            conflictSnapshot,
            coverageSnapshot,
            blockingCodes);
    }

    private static void ValidateReferences(
        IReadOnlyList<ScientificClaim> claims,
        IReadOnlyList<ScientificClaimConflict> conflicts,
        IReadOnlyList<ScientificCoverageRequirement> coverage)
    {
        var claimIds = claims.Select(claim => claim.ClaimId).ToHashSet(StringComparer.Ordinal);
        foreach (var conflict in conflicts)
        {
            if (!claimIds.Contains(conflict.FirstClaimId)
                || !claimIds.Contains(conflict.SecondClaimId))
            {
                throw new ArgumentException(
                    $"Scientific conflict '{conflict.ConflictId}' references an unknown claim.",
                    nameof(conflicts));
            }
        }

        foreach (var requirement in coverage)
        {
            if (requirement.ClaimIds.Any(claimId => !claimIds.Contains(claimId)))
            {
                throw new ArgumentException(
                    $"Coverage requirement '{requirement.RequirementId}' references an unknown claim.",
                    nameof(coverage));
            }
        }
    }

    private static IReadOnlyList<string> BuildBlockingCodes(
        ScientificDocumentExtraction extraction,
        IReadOnlyList<ScientificClaim> claims,
        IReadOnlyList<ScientificClaimConflict> conflicts,
        IReadOnlyList<ScientificCoverageRequirement> coverage)
    {
        var codes = new List<string>();
        if (extraction.Status == ScientificExtractionStatus.Blocked)
        {
            codes.Add("source-extraction-blocked");
        }

        if (claims.Count == 0)
        {
            codes.Add("no-scientific-claims");
        }

        var sourceBlocks = extraction.Blocks.ToDictionary(block => block.BlockId, StringComparer.Ordinal);
        foreach (var claim in claims.Where(claim => claim.Status != ScientificClaimStatus.Rejected))
        {
            var validSupportingEvidence = claim.SupportingEvidence.Any(link =>
                EvidenceMatchesExtraction(link, extraction, sourceBlocks));
            if (!validSupportingEvidence)
            {
                codes.Add($"claim-missing-evidence:{claim.ClaimId}");
            }

            if (claim.Status != ScientificClaimStatus.Accepted)
            {
                codes.Add($"claim-not-accepted:{claim.ClaimId}");
            }

            if (claim.HasValidatedContradiction)
            {
                codes.Add($"claim-contradicted:{claim.ClaimId}");
            }
        }

        codes.AddRange(
            conflicts
                .Where(conflict => conflict.Status == ScientificConflictStatus.Unresolved)
                .Select(conflict => $"unresolved-conflict:{conflict.ConflictId}"));

        var acceptedClaimIds = claims
            .Where(claim => claim.Status == ScientificClaimStatus.Accepted)
            .Select(claim => claim.ClaimId)
            .ToHashSet(StringComparer.Ordinal);
        if (acceptedClaimIds.Count == 0)
        {
            codes.Add("no-accepted-scientific-claims");
        }

        if (!coverage.Any(requirement => requirement.IsRequired))
        {
            codes.Add("no-required-coverage");
        }

        foreach (var requirement in coverage.Where(requirement => requirement.IsRequired))
        {
            if (requirement.Status != ScientificCoverageStatus.Complete)
            {
                codes.Add($"required-coverage-incomplete:{requirement.RequirementId}");
                continue;
            }

            if (requirement.ClaimIds.Count == 0
                || requirement.ClaimIds.Any(claimId => !acceptedClaimIds.Contains(claimId)))
            {
                codes.Add($"required-coverage-unaccepted:{requirement.RequirementId}");
            }
        }

        return Array.AsReadOnly(codes.Distinct(StringComparer.Ordinal).ToArray());
    }

    private static bool EvidenceMatchesExtraction(
        ClaimEvidenceLink link,
        ScientificDocumentExtraction extraction,
        IReadOnlyDictionary<string, ScientificSourceBlock> sourceBlocks)
    {
        return sourceBlocks.TryGetValue(link.SourceBlockId, out var sourceBlock)
            && link.SourceAssetId == extraction.SourceAssetId
            && string.Equals(link.SourceSha256, extraction.SourceSha256, StringComparison.Ordinal)
            && sourceBlock.Location == link.Location
            && sourceBlock.OriginalText?.Contains(link.QuotedText, StringComparison.Ordinal) == true;
    }
}

public enum ScientificClaimCategory
{
    Definition = 0,
    Mechanism = 1,
    CausalRelation = 2,
    ProcessStep = 3,
    Comparison = 4,
    QuantitativeResult = 5,
    Constraint = 6,
    Limitation = 7,
    Uncertainty = 8,
}

public enum ScientificClaimStatus
{
    Draft = 0,
    Accepted = 1,
    Rejected = 2,
}

public enum ClaimEvidenceRole
{
    Support = 0,
    Qualification = 1,
    Contradiction = 2,
    Definition = 3,
}

public enum EvidenceValidationState
{
    Draft = 0,
    Validated = 1,
    Rejected = 2,
}

public enum ScientificConflictStatus
{
    Unresolved = 0,
    Resolved = 1,
}

public enum ScientificCoverageStatus
{
    Complete = 0,
    Incomplete = 1,
    Uncertain = 2,
}

public enum ScientificUnderstandingStatus
{
    ReadyForApproval = 0,
    Blocked = 1,
}

internal static class ScientificUnderstandingGuard
{
    public static void RequireConfidence(double value, string parameterName)
    {
        if (!double.IsFinite(value) || value is < 0 or > 1)
        {
            throw new ArgumentOutOfRangeException(
                parameterName,
                value,
                "Confidence must be finite and between zero and one.");
        }
    }

    public static void RequireUnique(IEnumerable<string> values, string parameterName)
    {
        var duplicate = values
            .GroupBy(value => value, StringComparer.Ordinal)
            .FirstOrDefault(group => group.Count() > 1)
            ?.Key;
        if (duplicate is not null)
        {
            throw new ArgumentException($"Duplicate identifier: {duplicate}", parameterName);
        }
    }

    public static void RequireNoNullElements<T>(
        IEnumerable<T> values,
        string parameterName)
        where T : class
    {
        if (values.Any(value => value is null))
        {
            throw new ArgumentException("Collection cannot contain null elements.", parameterName);
        }
    }
}
