namespace ContentDeliveryStudio.Core.ScientificFigures;

public sealed record ScientificFigureWorkflowAggregate
{
    private ScientificFigureWorkflowAggregate(
        Guid id,
        Guid projectId,
        ScientificDocumentExtraction extraction,
        ScientificDocumentUnderstanding understanding,
        ScientificFigureWorkflow workflow,
        DateTimeOffset createdAt,
        DateTimeOffset updatedAt)
    {
        Id = id;
        ProjectId = projectId;
        Extraction = extraction;
        Understanding = understanding;
        Workflow = workflow;
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
    }

    public Guid Id { get; }

    public Guid ProjectId { get; }

    public ScientificDocumentExtraction Extraction { get; }

    public ScientificDocumentUnderstanding Understanding { get; }

    public ScientificFigureWorkflow Workflow { get; }

    public DateTimeOffset CreatedAt { get; }

    public DateTimeOffset UpdatedAt { get; }

    public static ScientificFigureWorkflowAggregate Create(
        Guid id,
        Guid projectId,
        ScientificDocumentExtraction extraction,
        ScientificDocumentUnderstanding understanding,
        ScientificFigureWorkflow workflow,
        DateTimeOffset createdAt,
        DateTimeOffset updatedAt)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Scientific workflow id cannot be empty.", nameof(id));
        }

        if (projectId == Guid.Empty)
        {
            throw new ArgumentException("Project id cannot be empty.", nameof(projectId));
        }

        ArgumentNullException.ThrowIfNull(extraction);
        ArgumentNullException.ThrowIfNull(understanding);
        ArgumentNullException.ThrowIfNull(workflow);
        if (updatedAt < createdAt)
        {
            throw new ArgumentOutOfRangeException(
                nameof(updatedAt),
                updatedAt,
                "Updated timestamp cannot precede creation.");
        }

        if (understanding.SourceAssetId != extraction.SourceAssetId
            || !string.Equals(
                understanding.SourceSha256,
                extraction.SourceSha256,
                StringComparison.Ordinal))
        {
            throw new ArgumentException(
                "Scientific understanding must reference the persisted extraction.",
                nameof(understanding));
        }

        var specification = workflow.Specification;
        if (specification.UnderstandingId != understanding.UnderstandingId
            || specification.UnderstandingVersion != understanding.Version
            || specification.SourceAssetId != extraction.SourceAssetId
            || !string.Equals(
                specification.SourceSha256,
                extraction.SourceSha256,
                StringComparison.Ordinal))
        {
            throw new ArgumentException(
                "Scientific workflow specification must reference the persisted understanding and extraction.",
                nameof(workflow));
        }

        return new ScientificFigureWorkflowAggregate(
            id,
            projectId,
            extraction,
            understanding,
            workflow,
            createdAt,
            updatedAt);
    }
}
