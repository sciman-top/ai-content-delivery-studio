using ImageSeriesStudio.Core.Projects;
using ImageSeriesStudio.Core.Providers;
using ImageSeriesStudio.Core.Styles;

namespace ImageSeriesStudio.Application.Projects;

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
                    candidate.PromptText),
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
}
