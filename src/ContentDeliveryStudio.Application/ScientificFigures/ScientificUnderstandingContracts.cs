using ContentDeliveryStudio.Core.ScientificFigures;

namespace ContentDeliveryStudio.Application.ScientificFigures;

public interface IScientificUnderstandingProvider
{
    Task<ScientificUnderstandingChunkResult> AnalyzeChunkAsync(
        ScientificUnderstandingChunkRequest request,
        CancellationToken cancellationToken);
}

public sealed record ScientificUnderstandingChunkRequest(
    ScientificDocumentExtraction Extraction,
    string Objective,
    int ChunkIndex,
    int ChunkCount,
    IReadOnlyList<ScientificSourceBlock> Blocks);

public sealed record ScientificUnderstandingClaimDraft(
    string MergeKey,
    ScientificClaimCategory Category,
    string NormalizedStatement,
    string SourceWording,
    double Confidence,
    ScientificClaimStatus Status,
    string SourceBlockId,
    string QuotedText,
    ClaimEvidenceRole EvidenceRole,
    EvidenceValidationState EvidenceValidationState);

public sealed record ScientificUnderstandingChunkResult(
    IReadOnlyList<ScientificUnderstandingClaimDraft> Claims,
    string ProviderTraceId)
{
    public IReadOnlyList<ScientificUnderstandingTermDraft> Terms { get; init; } = [];

    public IReadOnlyList<ScientificUnderstandingConflictDraft> Conflicts { get; init; } = [];

    public ScientificFigureProposalDraft? FigureProposal { get; init; }

    public IReadOnlyList<string> BlockingCodes { get; init; } = [];

    public bool IsBlocked => BlockingCodes.Count > 0;
}

public sealed record ScientificUnderstandingTermDraft(
    string TermId,
    string CanonicalTerm,
    string Definition,
    IReadOnlyList<string> Aliases,
    string SourceBlockId);

public sealed record ScientificUnderstandingConflictDraft(
    string ConflictId,
    string FirstMergeKey,
    string SecondMergeKey,
    string Description);

public sealed record ScientificFigureElementProposalDraft(
    string ProposalId,
    string Meaning,
    IReadOnlyList<string> SourceBlockIds);

public sealed record ScientificFigureRelationProposalDraft(
    string ProposalId,
    string SourceProposalId,
    string TargetProposalId,
    string RelationClass,
    IReadOnlyList<string> SourceBlockIds);

public sealed record ScientificFigureProposalDraft(
    string CentralMessage,
    IReadOnlyList<ScientificFigureElementProposalDraft> Elements,
    IReadOnlyList<ScientificFigureRelationProposalDraft> Relations);

public sealed record ScientificUnderstandingChunkPolicy(
    int MaxBlocksPerChunk = 8,
    int MaxCharactersPerChunk = 12_000)
{
    public ScientificUnderstandingChunkPolicy Validate()
    {
        if (MaxBlocksPerChunk < 1)
        {
            throw new ArgumentOutOfRangeException(
                nameof(MaxBlocksPerChunk),
                MaxBlocksPerChunk,
                "Maximum blocks per chunk must be positive.");
        }

        if (MaxCharactersPerChunk < 1)
        {
            throw new ArgumentOutOfRangeException(
                nameof(MaxCharactersPerChunk),
                MaxCharactersPerChunk,
                "Maximum characters per chunk must be positive.");
        }

        return this;
    }
}

public sealed record ScientificFigureDraftRequest(
    string Objective,
    string CentralMessage,
    string Audience,
    bool IsSchematic,
    ScientificFigureRiskLevel RiskLevel);

public sealed record ScientificGateOneDecision(
    bool Approved,
    string Reviewer,
    string Notes,
    DateTimeOffset ReviewedAt);

public interface IScientificFigureWorkflowRepository
{
    Task SaveAsync(
        ScientificFigureWorkflowAggregate aggregate,
        CancellationToken cancellationToken);

    Task<ScientificFigureWorkflowAggregate?> LoadAsync(
        Guid id,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<ScientificFigureWorkflowAggregate>> ListByProjectAsync(
        Guid projectId,
        CancellationToken cancellationToken);
}

public sealed class InMemoryScientificFigureWorkflowRepository
    : IScientificFigureWorkflowRepository
{
    private readonly Dictionary<Guid, ScientificFigureWorkflowAggregate> _records = [];

    public Task SaveAsync(
        ScientificFigureWorkflowAggregate aggregate,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(aggregate);
        _records[aggregate.Id] = aggregate;
        return Task.CompletedTask;
    }

    public Task<ScientificFigureWorkflowAggregate?> LoadAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _records.TryGetValue(id, out var aggregate);
        return Task.FromResult(aggregate);
    }

    public Task<IReadOnlyList<ScientificFigureWorkflowAggregate>> ListByProjectAsync(
        Guid projectId,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        IReadOnlyList<ScientificFigureWorkflowAggregate> result = _records.Values
            .Where(item => item.ProjectId == projectId)
            .OrderBy(item => item.Id)
            .ToArray();
        return Task.FromResult(result);
    }
}
