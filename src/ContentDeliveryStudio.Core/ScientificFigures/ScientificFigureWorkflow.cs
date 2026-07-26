namespace ContentDeliveryStudio.Core.ScientificFigures;

public sealed record ScientificGate1Approval
{
    private ScientificGate1Approval(
        Guid specificationId,
        int approvedSpecVersion,
        Guid understandingId,
        int approvedUnderstandingVersion,
        string reviewer,
        string notes,
        DateTimeOffset reviewedAt)
    {
        SpecificationId = specificationId;
        ApprovedSpecVersion = approvedSpecVersion;
        UnderstandingId = understandingId;
        ApprovedUnderstandingVersion = approvedUnderstandingVersion;
        Reviewer = reviewer;
        Notes = notes;
        ReviewedAt = reviewedAt;
    }

    public Guid SpecificationId { get; }

    public int ApprovedSpecVersion { get; }

    public Guid UnderstandingId { get; }

    public int ApprovedUnderstandingVersion { get; }

    public string Reviewer { get; }

    public string Notes { get; }

    public DateTimeOffset ReviewedAt { get; }

    internal static ScientificGate1Approval Create(
        ScientificFigureSpec specification,
        string reviewer,
        string notes,
        DateTimeOffset reviewedAt)
    {
        return new ScientificGate1Approval(
            specification.SpecificationId,
            specification.Version,
            specification.UnderstandingId,
            specification.UnderstandingVersion,
            ScientificSourceGuard.RequireText(reviewer, nameof(reviewer)),
            ScientificSourceGuard.RequireText(notes, nameof(notes)),
            reviewedAt);
    }
}

public sealed record ScientificDownstreamApproval
{
    private ScientificDownstreamApproval(
        ScientificDownstreamApprovalStage stage,
        int approvedSpecVersion,
        string reviewer,
        DateTimeOffset reviewedAt)
    {
        Stage = stage;
        ApprovedSpecVersion = approvedSpecVersion;
        Reviewer = reviewer;
        ReviewedAt = reviewedAt;
    }

    public ScientificDownstreamApprovalStage Stage { get; }

    public int ApprovedSpecVersion { get; }

    public string Reviewer { get; }

    public DateTimeOffset ReviewedAt { get; }

    internal static ScientificDownstreamApproval Create(
        ScientificDownstreamApprovalStage stage,
        int approvedSpecVersion,
        string reviewer,
        DateTimeOffset reviewedAt)
    {
        ScientificSourceGuard.RequireDefined(stage, nameof(stage));
        return new ScientificDownstreamApproval(
            stage,
            approvedSpecVersion,
            ScientificSourceGuard.RequireText(reviewer, nameof(reviewer)),
            reviewedAt);
    }
}

public sealed record ScientificFigureWorkflow
{
    private ScientificFigureWorkflow(
        ScientificFigureSpec specification,
        ScientificFigureWorkflowState state,
        ScientificGate1Approval? gate1Approval,
        IReadOnlyList<ScientificDownstreamApproval> downstreamApprovals)
    {
        Specification = specification;
        State = state;
        Gate1Approval = gate1Approval;
        DownstreamApprovals = downstreamApprovals;
    }

    public ScientificFigureSpec Specification { get; }

    public ScientificFigureWorkflowState State { get; }

    public ScientificGate1Approval? Gate1Approval { get; }

    public IReadOnlyList<ScientificDownstreamApproval> DownstreamApprovals { get; }

    public static ScientificFigureWorkflow Create(ScientificFigureSpec specification)
    {
        ArgumentNullException.ThrowIfNull(specification);
        return new ScientificFigureWorkflow(
            specification,
            ScientificFigureWorkflowState.FigureSpecDraft,
            null,
            Array.AsReadOnly(Array.Empty<ScientificDownstreamApproval>()));
    }

    public ScientificFigureWorkflow ApproveGate1(
        string reviewer,
        string notes,
        DateTimeOffset reviewedAt)
    {
        if (Specification.Status != ScientificFigureSpecStatus.ReadyForGate1)
        {
            throw new InvalidOperationException(
                "Gate 1 cannot approve a blocked scientific figure specification.");
        }

        if (Gate1Approval is not null)
        {
            throw new InvalidOperationException(
                "Gate 1 is already approved for the current specification version.");
        }

        return new ScientificFigureWorkflow(
            Specification,
            ScientificFigureWorkflowState.Gate1Approved,
            ScientificGate1Approval.Create(Specification, reviewer, notes, reviewedAt),
            Array.AsReadOnly(Array.Empty<ScientificDownstreamApproval>()));
    }

    public ScientificFigureWorkflow RecordDownstreamApproval(
        ScientificDownstreamApprovalStage stage,
        string reviewer,
        DateTimeOffset reviewedAt)
    {
        ScientificSourceGuard.RequireDefined(stage, nameof(stage));
        if (Gate1Approval is null
            || Gate1Approval.ApprovedSpecVersion != Specification.Version)
        {
            throw new InvalidOperationException(
                "A current Gate 1 approval is required before downstream approval.");
        }

        if (DownstreamApprovals.Any(approval => approval.Stage == stage))
        {
            throw new InvalidOperationException(
                $"Downstream stage '{stage}' is already approved.");
        }

        RequirePriorStage(stage);
        var approvals = Array.AsReadOnly(
            DownstreamApprovals
                .Append(ScientificDownstreamApproval.Create(
                    stage,
                    Specification.Version,
                    reviewer,
                    reviewedAt))
                .ToArray());
        return new ScientificFigureWorkflow(
            Specification,
            StateFor(stage),
            Gate1Approval,
            approvals);
    }

    public ScientificFigureWorkflow ReviseScientificContent(
        ScientificDocumentUnderstanding understanding,
        string centralMessage,
        IReadOnlyList<FigureElementSpec> elements,
        IReadOnlyList<FigureRelationSpec> relations,
        IReadOnlyList<ScientificFigureIssue> issues)
    {
        var revisedSpecification = Specification.ReviseScientificContent(
            understanding,
            centralMessage,
            elements,
            relations,
            issues);
        return new ScientificFigureWorkflow(
            revisedSpecification,
            ScientificFigureWorkflowState.FigureSpecDraft,
            null,
            Array.AsReadOnly(Array.Empty<ScientificDownstreamApproval>()));
    }

    private void RequirePriorStage(ScientificDownstreamApprovalStage stage)
    {
        ScientificDownstreamApprovalStage? requiredStage = stage switch
        {
            ScientificDownstreamApprovalStage.RenderPlan => null,
            ScientificDownstreamApprovalStage.ScientificReview =>
                ScientificDownstreamApprovalStage.RenderPlan,
            _ => throw new ArgumentOutOfRangeException(nameof(stage), stage, "Unsupported stage."),
        };

        if (requiredStage is not null
            && !DownstreamApprovals.Any(approval => approval.Stage == requiredStage))
        {
            throw new InvalidOperationException(
                $"Downstream stage '{stage}' requires '{requiredStage}' approval.");
        }
    }

    private static ScientificFigureWorkflowState StateFor(
        ScientificDownstreamApprovalStage stage)
    {
        return stage switch
        {
            ScientificDownstreamApprovalStage.RenderPlan =>
                ScientificFigureWorkflowState.Rendering,
            ScientificDownstreamApprovalStage.ScientificReview =>
                ScientificFigureWorkflowState.ReviewPassed,
            _ => throw new ArgumentOutOfRangeException(nameof(stage), stage, "Unsupported stage."),
        };
    }
}

public enum ScientificDownstreamApprovalStage
{
    RenderPlan = 0,
    ScientificReview = 1,
}

public enum ScientificFigureWorkflowState
{
    FigureSpecDraft = 0,
    Gate1Approved = 1,
    Rendering = 2,
    ReviewPassed = 3,
}
