namespace ContentDeliveryStudio.App.ViewModels;

public sealed class MainWindowSelectionSummaryCoordinator
{
    public string BuildCurrentProjectSummary(
        ProjectSummaryViewModel? selectedProject,
        string noProjectLoadedText)
    {
        return selectedProject is null
            ? noProjectLoadedText
            : $"{selectedProject.Name} ({selectedProject.UpdatedAt.LocalDateTime:g})";
    }

    public string BuildStyleRecipeSummary(
        ImageTypePresetOptionViewModel? preset,
        StyleGuideOptionViewModel? guide,
        GenerationRecipeOptionViewModel? recipe)
    {
        var presetName = preset?.DisplayName ?? "-";
        var guideName = guide?.Name ?? "-";
        var recipeName = recipe?.DisplayName ?? "-";
        return $"{presetName} / {guideName} / {recipeName}";
    }

    public string BuildSelectedSeriesItemTitle(
        SeriesItemViewModel? selectedSeriesItem,
        string noItemSelectedText)
    {
        return selectedSeriesItem?.Title ?? noItemSelectedText;
    }

    public string BuildSelectedCandidateSummary(
        GalleryRowViewModel? selectedGalleryRow,
        string noCandidateSelectedText)
    {
        return selectedGalleryRow is null
            ? noCandidateSelectedText
            : $"{selectedGalleryRow.ItemTitle} ({selectedGalleryRow.CandidateImageId:N})";
    }
}
