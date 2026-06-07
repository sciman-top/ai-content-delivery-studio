using ImageSeriesStudio.Application.Localization;
using ImageSeriesStudio.Application.Projects;
using ImageSeriesStudio.Core.Projects;

namespace ImageSeriesStudio.App.ViewModels;

public sealed class ReviewWorkflowCoordinator
{
    private readonly ProjectApplicationService _projectService;
    private readonly LocalizationService _localizationService;

    public ReviewWorkflowCoordinator(
        ProjectApplicationService projectService,
        LocalizationService localizationService)
    {
        _projectService = projectService;
        _localizationService = localizationService;
    }

    public async Task<IReadOnlyList<ReviewRowViewModel>> RunFakeReviewAsync(
        Guid projectId,
        IReadOnlyList<GalleryRowViewModel> galleryRows,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(galleryRows);

        var reviews = await _projectService.RunStructuredVisionReviewAsync(
            projectId,
            galleryRows
                .Select(row => new ReviewCandidateInput(
                    row.CandidateImageId,
                    row.ItemTitle,
                    row.AssetPath,
                    row.PromptText))
                .ToArray(),
            cancellationToken);

        var routesByCandidate = _projectService.RouteReviewOutcomes(reviews)
            .ToDictionary(plan => plan.CandidateImageId);

        return reviews.Zip(galleryRows).Select(pair =>
        {
            var scoreText = string.Join(", ", pair.First.Scores.Select(score => $"{score.Name}:{score.Score}"));
            var routeSummary = routesByCandidate.TryGetValue(pair.First.CandidateImageId, out var plan)
                ? FormatReviewRoute(plan)
                : string.Empty;

            return new ReviewRowViewModel(
                pair.First.CandidateImageId,
                pair.Second.ItemTitle,
                pair.First.Decision.ToString(),
                scoreText,
                pair.First.Comments,
                pair.First.SuggestedFix ?? string.Empty,
                routeSummary,
                HumanApproved: false,
                _localizationService.GetText(LocalizationKey.HumanApprovalPending),
                string.Empty,
                string.Empty,
                null,
                pair.First);
        }).ToArray();
    }

    public async Task<ReviewRowViewModel> ApplyFinalApprovalAsync(
        Guid projectId,
        ReviewRowViewModel selectedReviewRow,
        bool approve,
        string reviewer,
        string notes,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(selectedReviewRow);

        var decision = await _projectService.RecordFinalApprovalAsync(
            projectId,
            new FinalApprovalRequest(
                selectedReviewRow.Review,
                approve,
                reviewer,
                notes),
            DateTimeOffset.UtcNow,
            cancellationToken);

        return selectedReviewRow with
        {
            HumanApproved = decision.HumanApproved,
            HumanApprovalStatus = decision.HumanApproved
                ? _localizationService.GetText(LocalizationKey.HumanApprovalApproved)
                : _localizationService.GetText(LocalizationKey.HumanApprovalRejected),
            FinalReviewer = decision.Reviewer,
            FinalApprovalNotes = decision.Notes,
            FinalApprovalDecidedAt = decision.DecidedAt,
        };
    }

    private static string FormatReviewRoute(ReviewOutcomeRoutingPlan plan)
    {
        var route = plan.PrimaryRoute;
        if (route.TargetLayer is ReviewOutcomeTargetLayer.None)
        {
            return ReviewOutcomeTargetLayer.None.ToString();
        }

        var firstAction = route.Actions.FirstOrDefault() ?? string.Empty;
        return string.IsNullOrWhiteSpace(firstAction)
            ? $"{route.TargetLayer} / {route.Severity}"
            : $"{route.TargetLayer} / {route.Severity}: {firstAction}";
    }
}
