using CommunityToolkit.Mvvm.ComponentModel;

namespace ContentDeliveryStudio.App.ViewModels;

public sealed partial class ImageSeriesGenerationSettingsWorkspaceViewModel : ObservableObject
{
    private readonly Func<
        ImageTypePresetOptionViewModel?,
        StyleGuideOptionViewModel?,
        GenerationRecipeOptionViewModel?,
        string> _buildSummary;

    [ObservableProperty]
    private IReadOnlyList<ImageTypePresetOptionViewModel> _imageTypePresetOptions = [];

    [ObservableProperty]
    private ImageTypePresetOptionViewModel? _selectedImageTypePresetOption;

    [ObservableProperty]
    private IReadOnlyList<StyleGuideOptionViewModel> _styleGuideOptions = [];

    [ObservableProperty]
    private StyleGuideOptionViewModel? _selectedStyleGuideOption;

    [ObservableProperty]
    private IReadOnlyList<GenerationRecipeOptionViewModel> _generationRecipeOptions = [];

    [ObservableProperty]
    private GenerationRecipeOptionViewModel? _selectedGenerationRecipeOption;

    [ObservableProperty]
    private string _styleRecipeInspectorTitle = string.Empty;

    [ObservableProperty]
    private string _imageTypePresetLabel = string.Empty;

    [ObservableProperty]
    private string _styleGuideLabel = string.Empty;

    [ObservableProperty]
    private string _generationRecipeLabel = string.Empty;

    [ObservableProperty]
    private string _styleRecipeSummaryTitle = string.Empty;

    [ObservableProperty]
    private string _styleRecipeSummaryText = string.Empty;

    internal ImageSeriesGenerationSettingsWorkspaceViewModel(
        Func<
            ImageTypePresetOptionViewModel?,
            StyleGuideOptionViewModel?,
            GenerationRecipeOptionViewModel?,
            string> buildSummary)
    {
        _buildSummary = buildSummary ?? throw new ArgumentNullException(nameof(buildSummary));
    }

    internal void ApplyLocalization(MainWindowLocalizationPayload payload)
    {
        ArgumentNullException.ThrowIfNull(payload);

        StyleRecipeInspectorTitle = payload.StyleRecipeInspectorTitle;
        ImageTypePresetLabel = payload.ImageTypePresetLabel;
        StyleGuideLabel = payload.StyleGuideLabel;
        GenerationRecipeLabel = payload.GenerationRecipeLabel;
        StyleRecipeSummaryTitle = payload.StyleRecipeSummaryTitle;
    }

    internal void ApplyOptions(
        IReadOnlyList<ImageTypePresetOptionViewModel> imageTypePresets,
        ImageTypePresetOptionViewModel? selectedImageTypePreset,
        IReadOnlyList<StyleGuideOptionViewModel> styleGuides,
        StyleGuideOptionViewModel? selectedStyleGuide,
        IReadOnlyList<GenerationRecipeOptionViewModel> generationRecipes,
        GenerationRecipeOptionViewModel? selectedGenerationRecipe)
    {
        ArgumentNullException.ThrowIfNull(imageTypePresets);
        ArgumentNullException.ThrowIfNull(styleGuides);
        ArgumentNullException.ThrowIfNull(generationRecipes);

        ImageTypePresetOptions = imageTypePresets;
        SelectedImageTypePresetOption = selectedImageTypePreset;
        StyleGuideOptions = styleGuides;
        SelectedStyleGuideOption = selectedStyleGuide;
        GenerationRecipeOptions = generationRecipes;
        SelectedGenerationRecipeOption = selectedGenerationRecipe;
        RefreshSummary();
    }

    partial void OnSelectedImageTypePresetOptionChanged(ImageTypePresetOptionViewModel? value) => RefreshSummary();

    partial void OnSelectedStyleGuideOptionChanged(StyleGuideOptionViewModel? value) => RefreshSummary();

    partial void OnSelectedGenerationRecipeOptionChanged(GenerationRecipeOptionViewModel? value) => RefreshSummary();

    private void RefreshSummary()
    {
        StyleRecipeSummaryText = _buildSummary(
            SelectedImageTypePresetOption,
            SelectedStyleGuideOption,
            SelectedGenerationRecipeOption);
    }
}
