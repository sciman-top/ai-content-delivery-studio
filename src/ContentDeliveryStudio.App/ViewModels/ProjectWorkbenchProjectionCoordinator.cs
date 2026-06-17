using ContentDeliveryStudio.Application.Localization;
using ContentDeliveryStudio.Application.Projects;
using ContentDeliveryStudio.Core.Projects;

namespace ContentDeliveryStudio.App.ViewModels;

public sealed class ProjectWorkbenchProjectionCoordinator
{
    private readonly LocalizationService _localizationService;
    private readonly ProjectApplicationService _projectService;

    public ProjectWorkbenchProjectionCoordinator(
        LocalizationService localizationService,
        ProjectApplicationService projectService)
    {
        _localizationService = localizationService;
        _projectService = projectService;
    }

    public IReadOnlyList<SeriesSummaryViewModel> BuildSeries(ImageProject project)
    {
        ArgumentNullException.ThrowIfNull(project);

        return project.Series
            .Select(series => new SeriesSummaryViewModel(
                series.Id,
                series.Title,
                series.Items
                    .Select(item => new SeriesItemViewModel(
                        item.Id,
                        item.Title,
                        item.Brief,
                        item.Kind,
                        item.Status,
                        item.PromptVersions
                            .OrderByDescending(prompt => prompt.VersionNumber)
                            .Select(prompt => new PromptVersionViewModel(
                                prompt.Id,
                                prompt.VersionNumber,
                                prompt.PromptText,
                                FormatGenerationSettings(prompt.Settings),
                                prompt.CreatedAt))
                            .ToArray()))
                    .ToArray()))
            .ToArray();
    }

    public IReadOnlyList<GalleryRowViewModel> BuildGalleryRows(ImageProject project)
    {
        ArgumentNullException.ThrowIfNull(project);

        return project.Series
            .SelectMany(series => series.Items)
            .SelectMany(item => item.CandidateImages
                .OrderBy(candidate => candidate.CreatedAt)
                .Select(candidate => new GalleryRowViewModel(
                    candidate.Id,
                    item.Id,
                    item.Title,
                    candidate.AssetPath,
                    candidate.MetadataPath,
                    item.PromptVersions.FirstOrDefault(prompt => prompt.Id == candidate.PromptVersionId)?.PromptText ?? string.Empty)))
            .ToArray();
    }

    public IReadOnlyList<ReviewRowViewModel> BuildReviewRows(ImageProject project)
    {
        ArgumentNullException.ThrowIfNull(project);

        var restoredReviews = project.Series
            .SelectMany(series => series.Items)
            .SelectMany(item => item.CandidateImages
                .SelectMany(candidate => candidate.ReviewResults.Select(review => new
                {
                    ItemTitle = item.Title,
                    Review = review,
                    Output = new StructuredReviewOutput(
                        review.CandidateImageId,
                        review.Decision,
                        review.Scores
                            .OrderBy(score => score.Key, StringComparer.OrdinalIgnoreCase)
                            .Select(score => new StructuredReviewScore(score.Key, score.Key, 0, score.Value))
                            .ToArray(),
                        review.HardFailures.ToArray(),
                        review.Comments,
                        review.SuggestedFix),
                })))
            .OrderBy(entry => entry.Review.CreatedAt)
            .ToArray();

        var routesByCandidate = _projectService.RouteReviewOutcomes(restoredReviews.Select(entry => entry.Output).ToArray())
            .ToDictionary(plan => plan.CandidateImageId);

        return restoredReviews.Select(entry =>
        {
            var scoreText = string.Join(", ", entry.Review.Scores.Select(score => $"{score.Key}:{score.Value}"));
            var routeSummary = routesByCandidate.TryGetValue(entry.Review.CandidateImageId, out var plan)
                ? FormatReviewRoute(plan)
                : string.Empty;
            var approvalStatus = entry.Review.FinalApprovalDecidedAt is null
                ? Text(LocalizationKey.HumanApprovalPending)
                : entry.Review.HumanApproved
                    ? Text(LocalizationKey.HumanApprovalApproved)
                    : Text(LocalizationKey.HumanApprovalRejected);

            return new ReviewRowViewModel(
                entry.Review.CandidateImageId,
                entry.ItemTitle,
                entry.Review.Decision.ToString(),
                scoreText,
                entry.Review.Comments,
                entry.Review.SuggestedFix ?? string.Empty,
                routeSummary,
                entry.Review.HumanApproved,
                approvalStatus,
                entry.Review.FinalReviewer ?? string.Empty,
                entry.Review.FinalApprovalNotes ?? string.Empty,
                entry.Review.FinalApprovalDecidedAt,
                entry.Output);
        }).ToArray();
    }

    public IReadOnlyList<PlanRowViewModel> BuildPlanRows(
        IReadOnlyList<SeriesSummaryViewModel> series,
        string noItemsInSeriesText)
    {
        ArgumentNullException.ThrowIfNull(series);

        return series
            .SelectMany(seriesItem => seriesItem.Items.Count == 0
                ? new[] { new PlanRowViewModel(seriesItem.Title, noItemsInSeriesText, string.Empty, string.Empty, string.Empty) }
                : seriesItem.Items.Select(item => new PlanRowViewModel(
                    seriesItem.Title,
                    item.Title,
                    item.Brief,
                    _localizationService.GetSeriesItemKindText(item.Kind),
                    _localizationService.GetSeriesItemStatusText(item.Status))))
            .ToArray();
    }

    public IReadOnlyList<PromptRowViewModel> BuildPromptRows(IReadOnlyList<SeriesSummaryViewModel> series)
    {
        ArgumentNullException.ThrowIfNull(series);

        return series
            .SelectMany(seriesItem => seriesItem.Items.SelectMany(item => item.PromptVersions.Select(prompt => new PromptRowViewModel(
                item.Title,
                $"v{prompt.VersionNumber}",
                prompt.PromptText,
                prompt.SettingsSummary,
                prompt.CreatedAt.LocalDateTime.ToString("g")))))
            .ToArray();
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

    private static string FormatGenerationSettings(GenerationSettings settings)
    {
        return $"{settings.Width}x{settings.Height} {settings.Quality} {settings.OutputFormat}";
    }

    private string Text(LocalizationKey key)
    {
        return _localizationService.GetText(key);
    }
}
