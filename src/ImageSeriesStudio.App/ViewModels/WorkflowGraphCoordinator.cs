using System.IO;
using ImageSeriesStudio.Application.Localization;

namespace ImageSeriesStudio.App.ViewModels;

public sealed class WorkflowGraphCoordinator
{
    private readonly LocalizationService _localizationService;

    public WorkflowGraphCoordinator(LocalizationService localizationService)
    {
        _localizationService = localizationService;
    }

    public IReadOnlyList<WorkflowGraphRowViewModel> BuildRows(
        ProjectSummaryViewModel? selectedProject,
        IReadOnlyList<SeriesSummaryViewModel> series,
        IReadOnlyList<GalleryRowViewModel> galleryRows,
        IReadOnlyList<ReviewRowViewModel> reviewRows,
        IReadOnlyList<DeliveryRowViewModel> deliveryRows)
    {
        ArgumentNullException.ThrowIfNull(series);
        ArgumentNullException.ThrowIfNull(galleryRows);
        ArgumentNullException.ThrowIfNull(reviewRows);
        ArgumentNullException.ThrowIfNull(deliveryRows);

        var rows = new List<WorkflowGraphRowViewModel>();

        if (selectedProject is not null)
        {
            rows.Add(new WorkflowGraphRowViewModel(
                Text(LocalizationKey.GraphProjectNode),
                selectedProject.Name,
                $"series={series.Count}",
                string.Empty));
        }

        foreach (var seriesSummary in series)
        {
            rows.Add(new WorkflowGraphRowViewModel(
                Text(LocalizationKey.GraphSeriesNode),
                seriesSummary.Title,
                $"items={seriesSummary.Items.Count}",
                selectedProject?.Name ?? string.Empty));

            foreach (var item in seriesSummary.Items)
            {
                var candidateCount = galleryRows.Count(row =>
                    row.SeriesItemId == item.Id
                    || row.ItemTitle.Equals(item.Title, StringComparison.OrdinalIgnoreCase));
                var reviewCount = reviewRows.Count(row =>
                    row.ItemTitle.Equals(item.Title, StringComparison.OrdinalIgnoreCase));

                rows.Add(new WorkflowGraphRowViewModel(
                    Text(LocalizationKey.GraphItemNode),
                    item.Title,
                    $"{_localizationService.GetSeriesItemStatusText(item.Status)}; prompts={item.PromptVersions.Count}; candidates={candidateCount}; reviews={reviewCount}",
                    seriesSummary.Title));

                foreach (var prompt in item.PromptVersions.OrderBy(prompt => prompt.VersionNumber))
                {
                    rows.Add(new WorkflowGraphRowViewModel(
                        Text(LocalizationKey.GraphPromptNode),
                        $"v{prompt.VersionNumber}",
                        $"{prompt.SettingsSummary}; {prompt.CreatedAt.LocalDateTime:g}",
                        item.Title));
                }
            }
        }

        foreach (var candidate in galleryRows)
        {
            rows.Add(new WorkflowGraphRowViewModel(
                Text(LocalizationKey.GraphCandidateNode),
                ShortId(candidate.CandidateImageId),
                candidate.AssetPath,
                candidate.ItemTitle));
        }

        foreach (var review in reviewRows)
        {
            rows.Add(new WorkflowGraphRowViewModel(
                Text(LocalizationKey.GraphReviewNode),
                review.Decision,
                review.ScoreText,
                review.ItemTitle));
        }

        foreach (var delivery in deliveryRows)
        {
            rows.Add(new WorkflowGraphRowViewModel(
                Text(LocalizationKey.GraphDeliveryNode),
                Path.GetFileName(delivery.PackageDirectory),
                $"{delivery.FinalImageCount} {Text(LocalizationKey.GraphDeliveryImages)}",
                selectedProject?.Name ?? string.Empty));
        }

        return rows;
    }

    private static string ShortId(Guid id)
    {
        return id.ToString("N")[..8];
    }

    private string Text(LocalizationKey key)
    {
        return _localizationService.GetText(key);
    }
}
