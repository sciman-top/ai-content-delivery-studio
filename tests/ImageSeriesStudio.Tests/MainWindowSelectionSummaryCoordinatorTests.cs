using ImageSeriesStudio.App.ViewModels;
using ImageSeriesStudio.Core.Projects;

namespace ImageSeriesStudio.Tests;

public sealed class MainWindowSelectionSummaryCoordinatorTests
{
    [Fact]
    public void BuildStyleRecipeSummary_UsesSelectedDisplayNamesAndFallbacks()
    {
        var coordinator = new MainWindowSelectionSummaryCoordinator();

        var summary = coordinator.BuildStyleRecipeSummary(
            new ImageTypePresetOptionViewModel("preset", "Editorial", "summary"),
            new StyleGuideOptionViewModel("guide", "Default guide", "summary"),
            new GenerationRecipeOptionViewModel("recipe", "Fake PNG", "summary"));

        Assert.Equal("Editorial / Default guide / Fake PNG", summary);
        Assert.Equal("- / - / -", coordinator.BuildStyleRecipeSummary(null, null, null));
    }

    [Fact]
    public void BuildSelectedSeriesItemTitle_ReturnsSelectedTitleOrEmptyState()
    {
        var coordinator = new MainWindowSelectionSummaryCoordinator();
        var item = new SeriesItemViewModel(
            Guid.NewGuid(),
            "Opening frame",
            "Opening visual",
            SeriesItemKind.Standard,
            SeriesItemStatus.Draft,
            []);

        Assert.Equal("Opening frame", coordinator.BuildSelectedSeriesItemTitle(item, "No item selected"));
        Assert.Equal("No item selected", coordinator.BuildSelectedSeriesItemTitle(null, "No item selected"));
    }

    [Fact]
    public void BuildSelectedCandidateSummary_ReturnsCandidateTextOrEmptyState()
    {
        var coordinator = new MainWindowSelectionSummaryCoordinator();
        var candidateId = Guid.Parse("11111111-2222-3333-4444-555555555555");
        var row = new GalleryRowViewModel(
            candidateId,
            Guid.NewGuid(),
            "Opening frame",
            @"C:\temp\candidate.png",
            @"C:\temp\candidate.json",
            "Prompt");

        Assert.Equal(
            $"Opening frame ({candidateId:N})",
            coordinator.BuildSelectedCandidateSummary(row, "No candidate selected"));
        Assert.Equal("No candidate selected", coordinator.BuildSelectedCandidateSummary(null, "No candidate selected"));
    }
}
