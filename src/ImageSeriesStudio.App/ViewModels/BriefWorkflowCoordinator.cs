using ImageSeriesStudio.Application.Projects;
using ImageSeriesStudio.Core.Projects;
using ImageSeriesStudio.Core.Providers;
using ImageSeriesStudio.Core.Styles;

namespace ImageSeriesStudio.App.ViewModels;

public sealed class BriefWorkflowCoordinator
{
    private readonly ProjectApplicationService _projectService;

    public BriefWorkflowCoordinator(ProjectApplicationService projectService)
    {
        _projectService = projectService;
    }

    public async Task<Guid> CreateBriefAsync(
        Guid projectId,
        SeriesSummaryViewModel selectedSeries,
        string planningGoal,
        string planningAudience,
        string planningStyleBrief,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(selectedSeries);

        var brief = await _projectService.CreateCreativeBriefAsync(
            projectId,
            selectedSeries.Id,
            planningGoal.Trim(),
            planningAudience.Trim(),
            ImageTextPolicy.Hybrid,
            planningStyleBrief.Trim(),
            BuildBriefMustInclude(selectedSeries, planningGoal),
            ["unreadable small text"],
            DateTimeOffset.UtcNow,
            cancellationToken);

        return brief.Id;
    }

    public async Task<Guid> EnsureActiveCreativeBriefIdAsync(
        Guid projectId,
        SeriesSummaryViewModel? selectedSeries,
        Guid? activeCreativeBriefId,
        string planningGoal,
        string planningAudience,
        string planningStyleBrief,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(selectedSeries);

        var project = await _projectService.LoadProjectAsync(projectId, cancellationToken);
        var selectedSeriesModel = project?.Series.SingleOrDefault(series => series.Id == selectedSeries.Id);

        if (activeCreativeBriefId is { } existingBriefId
            && selectedSeriesModel?.CreativeBriefs.Any(brief => brief.Id == existingBriefId) == true)
        {
            return existingBriefId;
        }

        var latestBrief = selectedSeriesModel?.CreativeBriefs
            .OrderByDescending(brief => brief.UpdatedAt)
            .FirstOrDefault();

        if (latestBrief is not null)
        {
            return latestBrief.Id;
        }

        return await CreateBriefAsync(
            projectId,
            selectedSeries,
            planningGoal,
            planningAudience,
            planningStyleBrief,
            cancellationToken);
    }

    public async Task<CreativeBrief> GeneratePromptDirectionsAsync(
        Guid projectId,
        SeriesSummaryViewModel selectedSeries,
        Guid? activeCreativeBriefId,
        string planningGoal,
        string planningAudience,
        string planningStyleBrief,
        CancellationToken cancellationToken)
    {
        var briefId = await EnsureActiveCreativeBriefIdAsync(
            projectId,
            selectedSeries,
            activeCreativeBriefId,
            planningGoal,
            planningAudience,
            planningStyleBrief,
            cancellationToken);

        return await _projectService.CreatePromptDirectionsAsync(
            projectId,
            briefId,
            DateTimeOffset.UtcNow,
            cancellationToken);
    }

    public async Task<CreativeBrief> GenerateDesignBlueprintsAsync(
        Guid projectId,
        SeriesSummaryViewModel selectedSeries,
        Guid? activeCreativeBriefId,
        string planningGoal,
        string planningAudience,
        string planningStyleBrief,
        CancellationToken cancellationToken)
    {
        var briefId = await EnsureActiveCreativeBriefIdAsync(
            projectId,
            selectedSeries,
            activeCreativeBriefId,
            planningGoal,
            planningAudience,
            planningStyleBrief,
            cancellationToken);

        return await _projectService.CreateDesignBlueprintsAsync(
            projectId,
            briefId,
            DateTimeOffset.UtcNow,
            cancellationToken);
    }

    public static IReadOnlyList<string> BuildBriefMustInclude(
        SeriesSummaryViewModel? selectedSeries,
        string planningGoal)
    {
        var itemBriefs = selectedSeries?.Items
            .Select(item => string.IsNullOrWhiteSpace(item.Brief) ? item.Title : $"{item.Title}: {item.Brief}")
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .ToArray() ?? [];

        return itemBriefs.Length == 0
            ? [selectedSeries?.Title ?? planningGoal.Trim()]
            : itemBriefs;
    }

    public static IReadOnlyList<DesignBlueprintRowViewModel> BuildDesignBlueprintRows(
        ImageProject project,
        string promotedText,
        string candidateText)
    {
        return project.Series
            .SelectMany(series => series.CreativeBriefs.SelectMany(brief => brief.DesignBlueprints.Select(blueprint => new DesignBlueprintRowViewModel(
                brief.Id,
                blueprint.Id,
                blueprint.Key,
                blueprint.DisplayName,
                blueprint.Category,
                blueprint.Summary,
                blueprint.IntendedUse,
                $"{blueprint.MinimumRecommendedItemCount}-{blueprint.MaximumRecommendedItemCount}",
                blueprint.SupportsPanelSequence ? "panel sequence" : "standard items",
                blueprint.DefaultTextPolicy.ToString(),
                blueprint.DefaultReviewRubricTemplateId,
                FormatList(blueprint.ConsistencyRules),
                FormatList(blueprint.VariationRules),
                FormatList(blueprint.RiskNotes),
                blueprint.Id == brief.PromotedBlueprintId,
                blueprint.Id == brief.PromotedBlueprintId ? promotedText : candidateText))))
            .ToArray();
    }

    public static IReadOnlyList<PromptDirectionRowViewModel> BuildPromptDirectionRows(ImageProject project)
    {
        return project.Series
            .SelectMany(series => series.CreativeBriefs.SelectMany(brief => brief.PromptDirections.Select(direction => new PromptDirectionRowViewModel(
                brief.Id,
                direction.Key,
                direction.Name,
                direction.IntendedUse,
                direction.PromptText,
                direction.Strength,
                direction.Risk,
                FormatRecommendation(direction.Recommendation),
                direction.Recommendation?.RecommendationReason ?? string.Empty,
                FormatList(direction.Recommendation?.CapabilityWarnings),
                FormatList(direction.Recommendation?.NonExecutableSuggestions)))))
            .ToArray();
    }

    private static string FormatRecommendation(PromptDirectionRecommendation? recommendation)
    {
        if (recommendation is null)
        {
            return string.Empty;
        }

        return string.Join(
            " / ",
            [
                recommendation.ImageTypePresetId,
                recommendation.TextPolicy.ToString(),
                $"{recommendation.Width}x{recommendation.Height}",
                recommendation.QualityBand,
                recommendation.OutputFormat,
                recommendation.ReviewRubricTemplateId,
            ]);
    }

    private static string FormatList(IReadOnlyList<string>? values)
    {
        return values is null || values.Count == 0 ? string.Empty : string.Join("; ", values);
    }
}
