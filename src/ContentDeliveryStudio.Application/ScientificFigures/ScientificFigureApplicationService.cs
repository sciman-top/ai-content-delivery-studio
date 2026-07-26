using System.Text;
using ContentDeliveryStudio.Core.ScientificFigures;

namespace ContentDeliveryStudio.Application.ScientificFigures;

public sealed class ScientificFigureApplicationService
{
    private readonly IScientificUnderstandingProvider _understandingProvider;
    private readonly IScientificFigureWorkflowRepository _repository;
    private readonly ScientificUnderstandingChunkPolicy _chunkPolicy;

    public ScientificFigureApplicationService(
        IScientificUnderstandingProvider understandingProvider,
        IScientificFigureWorkflowRepository repository,
        ScientificUnderstandingChunkPolicy? chunkPolicy = null)
    {
        _understandingProvider = understandingProvider;
        _repository = repository;
        _chunkPolicy = (chunkPolicy ?? new ScientificUnderstandingChunkPolicy()).Validate();
    }

    public async Task<ScientificFigureWorkflowAggregate> CreateDraftAsync(
        Guid projectId,
        ScientificDocumentExtraction extraction,
        ScientificFigureDraftRequest request,
        DateTimeOffset timestamp,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(extraction);
        ArgumentNullException.ThrowIfNull(request);
        if (extraction.Status != ScientificExtractionStatus.Ready)
        {
            throw new InvalidOperationException(
                "A blocked scientific extraction cannot enter understanding.");
        }

        var chunks = BuildChunks(extraction.Blocks);
        var chunkResults = new List<ScientificUnderstandingChunkResult>(chunks.Count);
        for (var index = 0; index < chunks.Count; index++)
        {
            chunkResults.Add(await _understandingProvider.AnalyzeChunkAsync(
                new ScientificUnderstandingChunkRequest(
                    extraction,
                    request.Objective,
                    index,
                    chunks.Count,
                    chunks[index]),
                cancellationToken));
        }

        var understanding = BuildUnderstanding(
            extraction,
            request.Objective,
            chunkResults.SelectMany(result => result.Claims).ToArray());
        var specification = BuildSpecification(understanding, request);
        var aggregate = ScientificFigureWorkflowAggregate.Create(
            Guid.NewGuid(),
            projectId,
            extraction,
            understanding,
            ScientificFigureWorkflow.Create(specification),
            timestamp,
            timestamp);
        await _repository.SaveAsync(aggregate, cancellationToken);
        return aggregate;
    }

    public async Task<ScientificFigureWorkflowAggregate> DecideGateOneAsync(
        Guid workflowId,
        ScientificGateOneDecision decision,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(decision);
        if (!decision.Approved)
        {
            throw new InvalidOperationException(
                "Gate 1 requires an explicit affirmative human decision.");
        }

        var existing = await _repository.LoadAsync(workflowId, cancellationToken)
            ?? throw new InvalidOperationException(
                $"Scientific figure workflow not found: {workflowId}");
        var approvedWorkflow = existing.Workflow.ApproveGate1(
            decision.Reviewer,
            decision.Notes,
            decision.ReviewedAt);
        var approved = ScientificFigureWorkflowAggregate.Create(
            existing.Id,
            existing.ProjectId,
            existing.Extraction,
            existing.Understanding,
            approvedWorkflow,
            existing.CreatedAt,
            decision.ReviewedAt);
        await _repository.SaveAsync(approved, cancellationToken);
        return approved;
    }

    private IReadOnlyList<IReadOnlyList<ScientificSourceBlock>> BuildChunks(
        IReadOnlyList<ScientificSourceBlock> blocks)
    {
        var chunks = new List<IReadOnlyList<ScientificSourceBlock>>();
        var current = new List<ScientificSourceBlock>();
        var currentCharacters = 0;
        foreach (var block in blocks)
        {
            var blockCharacters = block.OriginalText?.Length ?? 0;
            var exceedsPolicy = current.Count > 0
                && (current.Count >= _chunkPolicy.MaxBlocksPerChunk
                    || currentCharacters + blockCharacters > _chunkPolicy.MaxCharactersPerChunk);
            if (exceedsPolicy)
            {
                chunks.Add(current.ToArray());
                current = [];
                currentCharacters = 0;
            }

            current.Add(block);
            currentCharacters += blockCharacters;
        }

        if (current.Count > 0)
        {
            chunks.Add(current.ToArray());
        }

        return chunks;
    }

    private static ScientificDocumentUnderstanding BuildUnderstanding(
        ScientificDocumentExtraction extraction,
        string objective,
        IReadOnlyList<ScientificUnderstandingClaimDraft> drafts)
    {
        var blockById = extraction.Blocks.ToDictionary(
            block => block.BlockId,
            StringComparer.Ordinal);
        var materialized = new List<MaterializedClaimDraft>(drafts.Count);
        foreach (var group in drafts.GroupBy(draft => draft.MergeKey, StringComparer.Ordinal))
        {
            var entries = group.ToArray();
            for (var index = 0; index < entries.Length; index++)
            {
                var draft = entries[index];
                if (!blockById.TryGetValue(draft.SourceBlockId, out var block))
                {
                    throw new InvalidOperationException(
                        $"Understanding provider referenced an unknown source block: {draft.SourceBlockId}");
                }

                var evidence = ClaimEvidenceLink.Create(
                    extraction,
                    block,
                    draft.QuotedText,
                    draft.EvidenceRole,
                    draft.Confidence,
                    draft.EvidenceValidationState);
                var suffix = entries.Length == 1 ? string.Empty : $"-{index + 1}";
                var claimId = $"claim-{Slug(group.Key)}{suffix}";
                materialized.Add(new MaterializedClaimDraft(
                    group.Key,
                    ScientificClaim.Create(
                        claimId,
                        draft.Category,
                        draft.NormalizedStatement,
                        draft.SourceWording,
                        draft.Confidence,
                        draft.Status,
                        [evidence])));
            }
        }

        var conflicts = materialized
            .GroupBy(item => item.MergeKey, StringComparer.Ordinal)
            .Where(group => group
                .Select(item => item.Claim.NormalizedStatement)
                .Distinct(StringComparer.Ordinal)
                .Skip(1)
                .Any())
            .Select(group =>
            {
                var pair = group.Take(2).ToArray();
                return ScientificClaimConflict.Create(
                    $"conflict-{Slug(group.Key)}",
                    pair[0].Claim.ClaimId,
                    pair[1].Claim.ClaimId,
                    $"Merged chunks disagree for '{group.Key}'.",
                    ScientificConflictStatus.Unresolved,
                    resolution: null);
            })
            .ToArray();
        var claims = materialized.Select(item => item.Claim).ToArray();
        var coverage = ScientificCoverageRequirement.Create(
            "coverage-figure-objective",
            "All claims selected for the bounded figure objective are represented.",
            isRequired: true,
            claims.Length == 0
                ? ScientificCoverageStatus.Incomplete
                : ScientificCoverageStatus.Complete,
            claims.Select(claim => claim.ClaimId).ToArray());
        return ScientificDocumentUnderstanding.Create(
            Guid.NewGuid(),
            extraction,
            objective,
            version: 1,
            terminology: [],
            claims,
            conflicts,
            [coverage]);
    }

    private static ScientificFigureSpec BuildSpecification(
        ScientificDocumentUnderstanding understanding,
        ScientificFigureDraftRequest request)
    {
        var approvedClaims = understanding.Claims
            .Where(claim =>
                claim.Status == ScientificClaimStatus.Accepted
                && claim.SupportingEvidence.Count > 0)
            .ToArray();
        if (approvedClaims.Length == 0)
        {
            throw new InvalidOperationException(
                "A scientific figure draft requires at least one evidence-bound claim.");
        }

        var elements = approvedClaims.Select(claim =>
        {
            var evidence = claim.SupportingEvidence.First();
            return FigureElementSpec.Create(
                $"element-{Slug(claim.ClaimId)}",
                claim.NormalizedStatement,
                FigureElementKind.Entity,
                claim.NormalizedStatement,
                "deterministic-node",
                FigureContentRequirement.Required,
                isCritical: true,
                ScientificFigureProvenance.FromEvidence(claim, evidence));
        }).ToArray();
        return ScientificFigureSpec.Create(
            Guid.NewGuid(),
            understanding,
            request.Objective,
            request.CentralMessage,
            request.Audience,
            request.IsSchematic,
            request.RiskLevel,
            elements,
            relations: [],
            issues: []);
    }

    private static string Slug(string value)
    {
        var builder = new StringBuilder(value.Length);
        var pendingSeparator = false;
        foreach (var character in value.ToLowerInvariant())
        {
            if (char.IsAsciiLetterOrDigit(character))
            {
                if (pendingSeparator && builder.Length > 0)
                {
                    builder.Append('-');
                }

                builder.Append(character);
                pendingSeparator = false;
            }
            else
            {
                pendingSeparator = true;
            }
        }

        return builder.Length == 0 ? "item" : builder.ToString();
    }

    private sealed record MaterializedClaimDraft(
        string MergeKey,
        ScientificClaim Claim);
}
