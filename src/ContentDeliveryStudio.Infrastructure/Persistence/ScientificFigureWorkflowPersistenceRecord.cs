using ContentDeliveryStudio.Core.ScientificFigures;

namespace ContentDeliveryStudio.Infrastructure.Persistence;

internal sealed class ScientificFigureWorkflowPersistenceRecord
{
    private ScientificFigureWorkflowPersistenceRecord()
    {
        SourceSha256 = string.Empty;
        PayloadSchemaVersion = string.Empty;
        PayloadJson = string.Empty;
    }

    public Guid Id { get; private set; }

    public Guid ProjectId { get; private set; }

    public Guid SourceAssetId { get; private set; }

    public string SourceSha256 { get; private set; }

    public Guid UnderstandingId { get; private set; }

    public int UnderstandingVersion { get; private set; }

    public Guid SpecificationId { get; private set; }

    public int SpecificationVersion { get; private set; }

    public ScientificFigureWorkflowState WorkflowState { get; private set; }

    public int? Gate1ApprovedSpecVersion { get; private set; }

    public string PayloadSchemaVersion { get; private set; }

    public string PayloadJson { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public static ScientificFigureWorkflowPersistenceRecord FromAggregate(
        ScientificFigureWorkflowAggregate aggregate)
    {
        ArgumentNullException.ThrowIfNull(aggregate);
        return new ScientificFigureWorkflowPersistenceRecord
        {
            Id = aggregate.Id,
            ProjectId = aggregate.ProjectId,
            SourceAssetId = aggregate.Extraction.SourceAssetId,
            SourceSha256 = aggregate.Extraction.SourceSha256,
            UnderstandingId = aggregate.Understanding.UnderstandingId,
            UnderstandingVersion = aggregate.Understanding.Version,
            SpecificationId = aggregate.Workflow.Specification.SpecificationId,
            SpecificationVersion = aggregate.Workflow.Specification.Version,
            WorkflowState = aggregate.Workflow.State,
            Gate1ApprovedSpecVersion = aggregate.Workflow.Gate1Approval?.ApprovedSpecVersion,
            PayloadSchemaVersion = ScientificFigureWorkflowJsonCodec.CurrentSchemaVersion,
            PayloadJson = ScientificFigureWorkflowJsonCodec.Serialize(aggregate),
            CreatedAt = aggregate.CreatedAt,
            UpdatedAt = aggregate.UpdatedAt,
        };
    }

    public ScientificFigureWorkflowAggregate ToAggregate()
    {
        if (!string.Equals(
                PayloadSchemaVersion,
                ScientificFigureWorkflowJsonCodec.CurrentSchemaVersion,
                StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"Unsupported scientific figure workflow schema: {PayloadSchemaVersion}");
        }

        var aggregate = ScientificFigureWorkflowJsonCodec.Deserialize(PayloadJson);
        if (aggregate.Id != Id
            || aggregate.ProjectId != ProjectId
            || aggregate.Extraction.SourceAssetId != SourceAssetId
            || !string.Equals(aggregate.Extraction.SourceSha256, SourceSha256, StringComparison.Ordinal)
            || aggregate.Understanding.UnderstandingId != UnderstandingId
            || aggregate.Understanding.Version != UnderstandingVersion
            || aggregate.Workflow.Specification.SpecificationId != SpecificationId
            || aggregate.Workflow.Specification.Version != SpecificationVersion
            || aggregate.Workflow.State != WorkflowState
            || aggregate.Workflow.Gate1Approval?.ApprovedSpecVersion != Gate1ApprovedSpecVersion)
        {
            throw new InvalidOperationException(
                $"Scientific figure workflow index fields do not match payload: {Id}");
        }

        return aggregate;
    }
}
