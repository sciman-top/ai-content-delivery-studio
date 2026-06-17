using ContentDeliveryStudio.Core.Projects;
using ContentDeliveryStudio.Core.Providers;
using ContentDeliveryStudio.Core.Styles;

namespace ContentDeliveryStudio.Application.Projects;

public sealed class ReviewWorkflowApplicationService
{
    private readonly IProjectRepository _repository;
    private readonly IVisionReviewProvider? _visionReviewProvider;

    public ReviewWorkflowApplicationService(
        IProjectRepository repository,
        IVisionReviewProvider? visionReviewProvider)
    {
        _repository = repository;
        _visionReviewProvider = visionReviewProvider;
    }

    public async Task<IReadOnlyList<VisionReviewResult>> RunVisionReviewAsync(
        Guid projectId,
        IReadOnlyList<ReviewCandidateInput> candidates,
        CancellationToken cancellationToken)
    {
        var reviews = await RunStructuredVisionReviewAsync(projectId, candidates, cancellationToken);

        return reviews
            .Select(review => new VisionReviewResult(
                review.CandidateImageId,
                review.Decision,
                review.Scores.ToDictionary(score => score.Name, score => score.Score),
                review.HardFailures,
                review.Comments,
                review.SuggestedFix))
            .ToArray();
    }

    public async Task<IReadOnlyList<StructuredReviewOutput>> RunStructuredVisionReviewAsync(
        Guid projectId,
        IReadOnlyList<ReviewCandidateInput> candidates,
        CancellationToken cancellationToken)
    {
        if (_visionReviewProvider is null)
        {
            throw new InvalidOperationException("Vision review provider is not registered.");
        }

        ValidateReviewBatch(_visionReviewProvider.Capabilities.ProviderId, candidates);

        if (!_visionReviewProvider.Capabilities.ProviderId.StartsWith("fake", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Real vision review requires explicit approval.");
        }

        _ = await RequireProjectAsync(projectId, cancellationToken);

        var rubric = ReviewRubricTemplateCatalog
            .GetById(ReviewRubricTemplateCatalog.GeneralImage)
            .CreateRubric(projectId, DateTimeOffset.UtcNow);

        var results = new List<StructuredReviewOutput>();
        foreach (var candidate in candidates)
        {
            var result = await _visionReviewProvider.ReviewAsync(
                new VisionReviewRequest(
                    candidate.CandidateImageId,
                    candidate.AssetPath,
                    rubric,
                    candidate.PromptText,
                    candidate.ReviewPrep),
                cancellationToken);
            results.Add(StructuredReviewOutput.FromProviderResult(result, rubric));
        }

        return results;
    }

    public async Task<FinalApprovalDecision> RecordFinalApprovalAsync(
        Guid projectId,
        FinalApprovalRequest request,
        DateTimeOffset decidedAt,
        CancellationToken cancellationToken)
    {
        _ = await RequireProjectAsync(projectId, cancellationToken);

        var decision = FinalApprovalWorkflow.Decide(request, decidedAt);
        await _repository.SaveReviewResultAsync(projectId, decision.ReviewResult, cancellationToken);
        return decision;
    }

    private async Task<ImageProject> RequireProjectAsync(Guid projectId, CancellationToken cancellationToken)
    {
        return await _repository.LoadAsync(projectId, cancellationToken)
            ?? throw new InvalidOperationException($"Project not found: {projectId}");
    }

    private static void ValidateReviewBatch(string providerId, IReadOnlyList<ReviewCandidateInput> candidates)
    {
        var descriptor = VisionReviewExecutionPolicy.CreateOperatorDescriptor(providerId);

        if (candidates.Count > descriptor.BatchItemLimit)
        {
            throw new InvalidOperationException(
                $"Remote vision review batch contains {candidates.Count} items, which exceeds the default low-502 limit of {descriptor.BatchItemLimit}. Split the review batch before dispatch.");
        }

        if (!descriptor.RequiresCompactLocalArtifacts)
        {
            return;
        }

        var missingCompactArtifact = candidates.FirstOrDefault(candidate =>
            string.IsNullOrWhiteSpace(candidate.ReviewPrep?.Summary));

        if (missingCompactArtifact is not null)
        {
            throw new InvalidOperationException(
                "Remote vision review requires compact local review artifacts before dispatch. Add a bounded local summary, manifest, or evidence selection for each candidate.");
        }

        var oversizedCompactArtifact = candidates.FirstOrDefault(candidate =>
            candidate.ReviewPrep is not null &&
            candidate.ReviewPrep.Summary.Length > descriptor.CompactSummaryCharacterLimit);

        if (oversizedCompactArtifact is not null)
        {
            throw new InvalidOperationException(
                $"Remote vision review compact summary exceeds the bounded local-direct default of {descriptor.CompactSummaryCharacterLimit} characters. Trim the review-prep summary locally before dispatch.");
        }

        var missingEvidenceSelection = candidates.FirstOrDefault(candidate =>
            !HasMinimumLocalEvidence(candidate.ReviewPrep));

        if (missingEvidenceSelection is not null)
        {
            throw new InvalidOperationException(
                "Remote vision review requires at least one typed local evidence selection before dispatch. Add a bounded local candidate-image, metadata, or prompt-summary evidence entry for each candidate.");
        }
    }

    private static bool HasMinimumLocalEvidence(ReviewPrepArtifactContract? reviewPrep)
    {
        if (reviewPrep is null || reviewPrep.EvidenceSelections.Count == 0)
        {
            return false;
        }

        return reviewPrep.EvidenceSelections.Any(selection =>
            !string.IsNullOrWhiteSpace(selection.Role) &&
            !string.IsNullOrWhiteSpace(selection.SourceKind) &&
            (!string.IsNullOrWhiteSpace(selection.LocalPath) || !string.IsNullOrWhiteSpace(selection.Summary)));
    }
}
