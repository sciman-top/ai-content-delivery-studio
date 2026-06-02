using System.Text.Json.Serialization;
using ImageSeriesStudio.Core.Styles;

namespace ImageSeriesStudio.Core.Documents;

public enum DocumentSourceKind
{
    Docx = 0,
    Pdf = 1,
    Markdown = 2,
    Text = 3,
    Paste = 4,
}

public enum DocumentFamily
{
    Editorial = 0,
    Educational = 1,
    ScholarlyDraft = 2,
}

public enum IllustrationStrictnessLevel
{
    Editorial = 0,
    Educational = 1,
    ScholarlyDraft = 2,
}

public enum IllustrationPurpose
{
    Cover = 0,
    InlineIllustration = 1,
    ConceptDiagram = 2,
    MechanismDiagram = 3,
    Timeline = 4,
    Comparison = 5,
    GraphicalAbstract = 6,
    BackgroundPlate = 7,
    ExperimentalEvidence = 8,
}

public enum IllustrationTargetApprovalState
{
    Draft = 0,
    Approved = 1,
    Rejected = 2,
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
        if (projectId == Guid.Empty)
        {
            throw new ArgumentException("Project id cannot be empty.", nameof(projectId));
        }

        return new DocumentBrief(
            Guid.NewGuid(),
            projectId,
            sourceKind,
            RequireText(sourceDisplayName, nameof(sourceDisplayName)),
            RequireText(title, nameof(title)),
            documentFamily,
            RequireText(audience, nameof(audience)),
            NormalizeList(sections),
            NormalizeList(keyClaims),
            NormalizeList(visualOpportunities),
            NormalizeList(knownConstraints),
            strictnessLevel,
            createdAt);
    }

    private static string RequireText(string value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Value cannot be empty.", parameterName);
        }

        return value.Trim();
    }

    private static IReadOnlyList<string> NormalizeList(IReadOnlyList<string> values)
    {
        return values
            .Select(value => value.Trim())
            .Where(value => value.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
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

    public IReadOnlyList<IllustrationTarget> ApprovedTargets => Targets
        .Where(target => target.ApprovalState is IllustrationTargetApprovalState.Approved)
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
        if (projectId == Guid.Empty)
        {
            throw new ArgumentException("Project id cannot be empty.", nameof(projectId));
        }

        if (documentBriefId == Guid.Empty)
        {
            throw new ArgumentException("Document brief id cannot be empty.", nameof(documentBriefId));
        }

        if (targets.Count == 0)
        {
            throw new ArgumentException("At least one target is required.", nameof(targets));
        }

        return new IllustrationPlan(
            Guid.NewGuid(),
            projectId,
            documentBriefId,
            RequireText(summary, nameof(summary)),
            targets,
            NormalizeList(coverageNotes),
            NormalizeList(riskNotes),
            createdAt,
            createdAt);
    }

    public IllustrationPlan ApproveTarget(Guid targetId, DateTimeOffset timestamp)
    {
        return UpdateTarget(targetId, target => target.Approve(timestamp), timestamp);
    }

    public IllustrationPlan RejectTarget(Guid targetId, DateTimeOffset timestamp)
    {
        return UpdateTarget(targetId, target => target.Reject(timestamp), timestamp);
    }

    private IllustrationPlan UpdateTarget(
        Guid targetId,
        Func<IllustrationTarget, IllustrationTarget> update,
        DateTimeOffset timestamp)
    {
        if (!Targets.Any(target => target.Id == targetId))
        {
            throw new InvalidOperationException($"Illustration target not found: {targetId}");
        }

        return new IllustrationPlan(
            Id,
            ProjectId,
            DocumentBriefId,
            Summary,
            Targets
                .Select(target => target.Id == targetId ? update(target) : target)
                .ToArray(),
            CoverageNotes,
            RiskNotes,
            CreatedAt,
            timestamp);
    }

    private static string RequireText(string value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Value cannot be empty.", parameterName);
        }

        return value.Trim();
    }

    private static IReadOnlyList<string> NormalizeList(IReadOnlyList<string> values)
    {
        return values
            .Select(value => value.Trim())
            .Where(value => value.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }
}

public sealed record IllustrationTarget
{
    [JsonConstructor]
    public IllustrationTarget(
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
        if (documentBriefId == Guid.Empty)
        {
            throw new ArgumentException("Document brief id cannot be empty.", nameof(documentBriefId));
        }

        if (purpose is IllustrationPurpose.ExperimentalEvidence)
        {
            throw new InvalidOperationException("Experimental evidence targets are blocked in document illustration planning.");
        }

        return new IllustrationTarget(
            Guid.NewGuid(),
            documentBriefId,
            RequireText(title, nameof(title)),
            RequireText(documentLocation, nameof(documentLocation)),
            purpose,
            NormalizeRequiredList(mustShow, nameof(mustShow)),
            NormalizeList(mustNotShow),
            NormalizeRequiredList(sourceEvidence, nameof(sourceEvidence)),
            RequireText(suggestedImageTypePresetId, nameof(suggestedImageTypePresetId)),
            RequireText(suggestedReviewRubricTemplateId, nameof(suggestedReviewRubricTemplateId)),
            textPolicy,
            NormalizeList(strictnessNotes),
            IllustrationTargetApprovalState.Draft,
            createdAt,
            createdAt);
    }

    public IllustrationTarget Approve(DateTimeOffset timestamp)
    {
        return WithApprovalState(IllustrationTargetApprovalState.Approved, timestamp);
    }

    public IllustrationTarget Reject(DateTimeOffset timestamp)
    {
        return WithApprovalState(IllustrationTargetApprovalState.Rejected, timestamp);
    }

    private IllustrationTarget WithApprovalState(
        IllustrationTargetApprovalState approvalState,
        DateTimeOffset timestamp)
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
            timestamp);
    }

    private static string RequireText(string value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Value cannot be empty.", parameterName);
        }

        return value.Trim();
    }

    private static IReadOnlyList<string> NormalizeRequiredList(IReadOnlyList<string> values, string parameterName)
    {
        var normalized = NormalizeList(values);
        if (normalized.Count == 0)
        {
            throw new ArgumentException("At least one value is required.", parameterName);
        }

        return normalized;
    }

    private static IReadOnlyList<string> NormalizeList(IReadOnlyList<string> values)
    {
        return values
            .Select(value => value.Trim())
            .Where(value => value.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }
}
