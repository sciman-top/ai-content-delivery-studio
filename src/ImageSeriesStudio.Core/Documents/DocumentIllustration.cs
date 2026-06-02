using ImageSeriesStudio.Core.Styles;

namespace ImageSeriesStudio.Core.Documents;

public enum DocumentSourceKind
{
    Docx,
    Pdf,
    Markdown,
    Text,
    Paste,
}

public enum DocumentFamily
{
    Editorial,
    Educational,
    ScholarlyDraft,
}

public enum IllustrationStrictnessLevel
{
    Editorial,
    Educational,
    ScholarlyDraft,
}

public enum IllustrationPurpose
{
    Cover,
    InlineIllustration,
    ConceptDiagram,
    MechanismDiagram,
    Timeline,
    Comparison,
    GraphicalAbstract,
    BackgroundPlate,
    ExperimentalEvidence,
}

public enum IllustrationTargetApprovalState
{
    Draft,
    Approved,
    Rejected,
}

public sealed record DocumentBrief
{
    private DocumentBrief(
        Guid id,
        Guid projectId,
        DocumentSourceKind sourceKind,
        string sourceDisplayName,
        string title,
        DocumentFamily documentFamily,
        string audience,
        IReadOnlyList<string> sections,
        IReadOnlyList<string> keyClaims,
        IReadOnlyList<string> visualOpportunities,
        IReadOnlyList<string> knownConstraints,
        IllustrationStrictnessLevel strictnessLevel,
        DateTimeOffset createdAt)
    {
        Id = id;
        ProjectId = projectId;
        SourceKind = sourceKind;
        SourceDisplayName = sourceDisplayName;
        Title = title;
        DocumentFamily = documentFamily;
        Audience = audience;
        Sections = sections;
        KeyClaims = keyClaims;
        VisualOpportunities = visualOpportunities;
        KnownConstraints = knownConstraints;
        StrictnessLevel = strictnessLevel;
        CreatedAt = createdAt;
    }

    public Guid Id { get; }

    public Guid ProjectId { get; }

    public DocumentSourceKind SourceKind { get; }

    public string SourceDisplayName { get; }

    public string Title { get; }

    public DocumentFamily DocumentFamily { get; }

    public string Audience { get; }

    public IReadOnlyList<string> Sections { get; }

    public IReadOnlyList<string> KeyClaims { get; }

    public IReadOnlyList<string> VisualOpportunities { get; }

    public IReadOnlyList<string> KnownConstraints { get; }

    public IllustrationStrictnessLevel StrictnessLevel { get; }

    public DateTimeOffset CreatedAt { get; }

    public static DocumentBrief Create(
        Guid projectId,
        DocumentSourceKind sourceKind,
        string sourceDisplayName,
        string title,
        DocumentFamily documentFamily,
        string audience,
        IReadOnlyList<string> sections,
        IReadOnlyList<string> keyClaims,
        IReadOnlyList<string> visualOpportunities,
        IReadOnlyList<string> knownConstraints,
        IllustrationStrictnessLevel strictnessLevel,
        DateTimeOffset createdAt)
    {
        return new DocumentBrief(
            Guid.NewGuid(),
            RequireNonEmptyId(projectId, nameof(projectId)),
            sourceKind,
            RequireText(sourceDisplayName, nameof(sourceDisplayName)),
            RequireText(title, nameof(title)),
            documentFamily,
            RequireText(audience, nameof(audience)),
            DocumentIllustrationNormalization.NormalizeOptionalList(sections),
            DocumentIllustrationNormalization.NormalizeOptionalList(keyClaims),
            DocumentIllustrationNormalization.NormalizeOptionalList(visualOpportunities),
            DocumentIllustrationNormalization.NormalizeOptionalList(knownConstraints),
            strictnessLevel,
            createdAt);
    }

    private static Guid RequireNonEmptyId(Guid value, string parameterName)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException("Value cannot be empty.", parameterName);
        }

        return value;
    }

    private static string RequireText(string value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Value cannot be empty.", parameterName);
        }

        return value.Trim();
    }
}

public sealed record IllustrationPlan
{
    private IllustrationPlan(
        Guid id,
        Guid projectId,
        Guid documentBriefId,
        string summary,
        IReadOnlyList<IllustrationTarget> targets,
        IReadOnlyList<string> coverageNotes,
        IReadOnlyList<string> riskNotes,
        DateTimeOffset createdAt,
        DateTimeOffset updatedAt)
    {
        Id = id;
        ProjectId = projectId;
        DocumentBriefId = documentBriefId;
        Summary = summary;
        Targets = targets;
        CoverageNotes = coverageNotes;
        RiskNotes = riskNotes;
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
    }

    public Guid Id { get; }

    public Guid ProjectId { get; }

    public Guid DocumentBriefId { get; }

    public string Summary { get; }

    public IReadOnlyList<IllustrationTarget> Targets { get; }

    public IReadOnlyList<IllustrationTarget> ApprovedTargets =>
        Targets
            .Where(target => target.ApprovalState == IllustrationTargetApprovalState.Approved)
            .ToArray();

    public IReadOnlyList<string> CoverageNotes { get; }

    public IReadOnlyList<string> RiskNotes { get; }

    public DateTimeOffset CreatedAt { get; }

    public DateTimeOffset UpdatedAt { get; }

    public static IllustrationPlan Create(
        Guid projectId,
        Guid documentBriefId,
        string summary,
        IReadOnlyList<IllustrationTarget> targets,
        IReadOnlyList<string> coverageNotes,
        IReadOnlyList<string> riskNotes,
        DateTimeOffset createdAt)
    {
        var normalizedTargets = NormalizeTargets(targets);

        return new IllustrationPlan(
            Guid.NewGuid(),
            DocumentIllustrationNormalization.RequireNonEmptyId(projectId, nameof(projectId)),
            DocumentIllustrationNormalization.RequireNonEmptyId(documentBriefId, nameof(documentBriefId)),
            DocumentIllustrationNormalization.RequireText(summary, nameof(summary)),
            normalizedTargets,
            DocumentIllustrationNormalization.NormalizeOptionalList(coverageNotes),
            DocumentIllustrationNormalization.NormalizeOptionalList(riskNotes),
            createdAt,
            createdAt);
    }

    public IllustrationPlan ApproveTarget(Guid targetId, DateTimeOffset updatedAt)
    {
        return ChangeTargetState(targetId, updatedAt, (target, timestamp) => target.Approve(timestamp));
    }

    public IllustrationPlan RejectTarget(Guid targetId, DateTimeOffset updatedAt)
    {
        return ChangeTargetState(targetId, updatedAt, (target, timestamp) => target.Reject(timestamp));
    }

    private IllustrationPlan ChangeTargetState(
        Guid targetId,
        DateTimeOffset updatedAt,
        Func<IllustrationTarget, DateTimeOffset, IllustrationTarget> changeTarget)
    {
        var found = false;
        var updatedTargets = Targets
            .Select(target =>
            {
                if (target.Id != targetId)
                {
                    return target;
                }

                found = true;
                return changeTarget(target, updatedAt);
            })
            .ToArray();

        if (!found)
        {
            throw new InvalidOperationException($"Illustration target not found: {targetId}");
        }

        return new IllustrationPlan(
            Id,
            ProjectId,
            DocumentBriefId,
            Summary,
            updatedTargets,
            CoverageNotes,
            RiskNotes,
            CreatedAt,
            updatedAt);
    }

    private static IReadOnlyList<IllustrationTarget> NormalizeTargets(IReadOnlyList<IllustrationTarget> targets)
    {
        ArgumentNullException.ThrowIfNull(targets);

        if (targets.Count == 0)
        {
            throw new ArgumentException("At least one target is required.", nameof(targets));
        }

        return targets.ToArray();
    }
}

public sealed record IllustrationTarget
{
    private IllustrationTarget(
        Guid id,
        Guid documentBriefId,
        string title,
        string documentLocation,
        IllustrationPurpose purpose,
        IReadOnlyList<string> mustShow,
        IReadOnlyList<string> mustNotShow,
        IReadOnlyList<string> sourceEvidence,
        string suggestedImageTypePresetId,
        string suggestedReviewRubricTemplateId,
        ImageTextPolicy textPolicy,
        IReadOnlyList<string> strictnessNotes,
        IllustrationTargetApprovalState approvalState,
        DateTimeOffset createdAt,
        DateTimeOffset updatedAt)
    {
        Id = id;
        DocumentBriefId = documentBriefId;
        Title = title;
        DocumentLocation = documentLocation;
        Purpose = purpose;
        MustShow = mustShow;
        MustNotShow = mustNotShow;
        SourceEvidence = sourceEvidence;
        SuggestedImageTypePresetId = suggestedImageTypePresetId;
        SuggestedReviewRubricTemplateId = suggestedReviewRubricTemplateId;
        TextPolicy = textPolicy;
        StrictnessNotes = strictnessNotes;
        ApprovalState = approvalState;
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
    }

    public Guid Id { get; }

    public Guid DocumentBriefId { get; }

    public string Title { get; }

    public string DocumentLocation { get; }

    public IllustrationPurpose Purpose { get; }

    public IReadOnlyList<string> MustShow { get; }

    public IReadOnlyList<string> MustNotShow { get; }

    public IReadOnlyList<string> SourceEvidence { get; }

    public string SuggestedImageTypePresetId { get; }

    public string SuggestedReviewRubricTemplateId { get; }

    public ImageTextPolicy TextPolicy { get; }

    public IReadOnlyList<string> StrictnessNotes { get; }

    public IllustrationTargetApprovalState ApprovalState { get; }

    public DateTimeOffset CreatedAt { get; }

    public DateTimeOffset UpdatedAt { get; }

    public static IllustrationTarget Create(
        Guid documentBriefId,
        string title,
        string documentLocation,
        IllustrationPurpose purpose,
        IReadOnlyList<string> mustShow,
        IReadOnlyList<string> mustNotShow,
        IReadOnlyList<string> sourceEvidence,
        string suggestedImageTypePresetId,
        string suggestedReviewRubricTemplateId,
        ImageTextPolicy textPolicy,
        IReadOnlyList<string> strictnessNotes,
        DateTimeOffset createdAt)
    {
        if (purpose == IllustrationPurpose.ExperimentalEvidence)
        {
            throw new InvalidOperationException("Experimental evidence targets are blocked in document illustration planning.");
        }

        return new IllustrationTarget(
            Guid.NewGuid(),
            DocumentIllustrationNormalization.RequireNonEmptyId(documentBriefId, nameof(documentBriefId)),
            DocumentIllustrationNormalization.RequireText(title, nameof(title)),
            DocumentIllustrationNormalization.RequireText(documentLocation, nameof(documentLocation)),
            purpose,
            DocumentIllustrationNormalization.NormalizeRequiredList(mustShow, nameof(mustShow)),
            DocumentIllustrationNormalization.NormalizeOptionalList(mustNotShow),
            DocumentIllustrationNormalization.NormalizeRequiredList(sourceEvidence, nameof(sourceEvidence)),
            DocumentIllustrationNormalization.RequireText(suggestedImageTypePresetId, nameof(suggestedImageTypePresetId)),
            DocumentIllustrationNormalization.RequireText(suggestedReviewRubricTemplateId, nameof(suggestedReviewRubricTemplateId)),
            textPolicy,
            DocumentIllustrationNormalization.NormalizeOptionalList(strictnessNotes),
            IllustrationTargetApprovalState.Draft,
            createdAt,
            createdAt);
    }

    public IllustrationTarget Approve(DateTimeOffset updatedAt)
    {
        return WithApprovalState(IllustrationTargetApprovalState.Approved, updatedAt);
    }

    public IllustrationTarget Reject(DateTimeOffset updatedAt)
    {
        return WithApprovalState(IllustrationTargetApprovalState.Rejected, updatedAt);
    }

    private IllustrationTarget WithApprovalState(
        IllustrationTargetApprovalState approvalState,
        DateTimeOffset updatedAt)
    {
        return new IllustrationTarget(
            Id,
            DocumentBriefId,
            Title,
            DocumentLocation,
            Purpose,
            MustShow,
            MustNotShow,
            SourceEvidence,
            SuggestedImageTypePresetId,
            SuggestedReviewRubricTemplateId,
            TextPolicy,
            StrictnessNotes,
            approvalState,
            CreatedAt,
            updatedAt);
    }
}

internal static class DocumentIllustrationNormalization
{
    public static Guid RequireNonEmptyId(Guid value, string parameterName)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException("Value cannot be empty.", parameterName);
        }

        return value;
    }

    public static string RequireText(string value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Value cannot be empty.", parameterName);
        }

        return value.Trim();
    }

    public static IReadOnlyList<string> NormalizeRequiredList(IReadOnlyList<string> values, string parameterName)
    {
        var normalized = NormalizeOptionalList(values);
        if (normalized.Count == 0)
        {
            throw new ArgumentException("At least one value is required.", parameterName);
        }

        return normalized;
    }

    public static IReadOnlyList<string> NormalizeOptionalList(IReadOnlyList<string> values)
    {
        ArgumentNullException.ThrowIfNull(values);

        return values
            .Select(value => value?.Trim() ?? string.Empty)
            .Where(value => value.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }
}
