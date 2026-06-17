using ContentDeliveryStudio.Application.Localization;
using ContentDeliveryStudio.Application.Projects;
using ContentDeliveryStudio.Core.Projects;

namespace ContentDeliveryStudio.App.ViewModels;

public sealed class ProjectWorkbenchStateCoordinator
{
    private readonly LocalizationService _localizationService;
    private readonly ProjectApplicationService _projectService;
    private readonly ProjectWorkbenchProjectionCoordinator _projectWorkbenchProjectionCoordinator;

    public ProjectWorkbenchStateCoordinator(
        LocalizationService localizationService,
        ProjectApplicationService projectService,
        ProjectWorkbenchProjectionCoordinator projectWorkbenchProjectionCoordinator)
    {
        _localizationService = localizationService;
        _projectService = projectService;
        _projectWorkbenchProjectionCoordinator = projectWorkbenchProjectionCoordinator;
    }

    public async Task<ProjectWorkbenchStateResult> LoadAsync(
        Guid projectId,
        Guid? selectedSeriesId,
        Guid? selectedSeriesItemId,
        Guid? activeCreativeBriefId,
        string noItemsInSeriesText,
        CancellationToken cancellationToken)
    {
        var project = await _projectService.LoadProjectAsync(projectId, cancellationToken);
        return project is null
            ? CreateEmptyState()
            : BuildState(
                project,
                selectedSeriesId,
                selectedSeriesItemId,
                activeCreativeBriefId,
                noItemsInSeriesText);
    }

    public ProjectWorkbenchStateResult CreateEmptyState()
    {
        return new ProjectWorkbenchStateResult(
            [],
            null,
            null,
            [],
            null,
            [],
            null,
            [],
            [],
            [],
            [],
            []);
    }

    private ProjectWorkbenchStateResult BuildState(
        ImageProject project,
        Guid? selectedSeriesId,
        Guid? selectedSeriesItemId,
        Guid? activeCreativeBriefId,
        string noItemsInSeriesText)
    {
        var series = _projectWorkbenchProjectionCoordinator.BuildSeries(project);
        var designBlueprintRows = BriefWorkflowCoordinator.BuildDesignBlueprintRows(
            project,
            Text(LocalizationKey.BlueprintPromoted),
            Text(LocalizationKey.BlueprintCandidate));
        var promptDirectionRows = BriefWorkflowCoordinator.BuildPromptDirectionRows(project);
        var selectedSeries = selectedSeriesId is null
            ? series.FirstOrDefault()
            : series.FirstOrDefault(candidate => candidate.Id == selectedSeriesId);
        var selectedSeriesItem = selectedSeriesItemId is null
            ? selectedSeries?.Items.FirstOrDefault()
            : selectedSeries?.Items.FirstOrDefault(candidate => candidate.Id == selectedSeriesItemId)
                ?? selectedSeries?.Items.FirstOrDefault();

        return new ProjectWorkbenchStateResult(
            series,
            selectedSeries,
            selectedSeriesItem,
            designBlueprintRows,
            designBlueprintRows.FirstOrDefault(row =>
                activeCreativeBriefId is null || row.CreativeBriefId == activeCreativeBriefId),
            promptDirectionRows,
            promptDirectionRows.FirstOrDefault(row =>
                activeCreativeBriefId is null || row.CreativeBriefId == activeCreativeBriefId),
            _projectWorkbenchProjectionCoordinator.BuildPlanRows(series, noItemsInSeriesText),
            _projectWorkbenchProjectionCoordinator.BuildPromptRows(series),
            _projectWorkbenchProjectionCoordinator.BuildGalleryRows(project),
            _projectWorkbenchProjectionCoordinator.BuildReviewRows(project),
            []);
    }

    private string Text(LocalizationKey key)
    {
        return _localizationService.GetText(key);
    }
}

public sealed record ProjectWorkbenchStateResult(
    IReadOnlyList<SeriesSummaryViewModel> Series,
    SeriesSummaryViewModel? SelectedSeries,
    SeriesItemViewModel? SelectedSeriesItem,
    IReadOnlyList<DesignBlueprintRowViewModel> DesignBlueprintRows,
    DesignBlueprintRowViewModel? SelectedDesignBlueprint,
    IReadOnlyList<PromptDirectionRowViewModel> PromptDirectionRows,
    PromptDirectionRowViewModel? SelectedPromptDirection,
    IReadOnlyList<PlanRowViewModel> PlanRows,
    IReadOnlyList<PromptRowViewModel> PromptRows,
    IReadOnlyList<GalleryRowViewModel> GalleryRows,
    IReadOnlyList<ReviewRowViewModel> ReviewRows,
    IReadOnlyList<DeliveryRowViewModel> DeliveryRows);
