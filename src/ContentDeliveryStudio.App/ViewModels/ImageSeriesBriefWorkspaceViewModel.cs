using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ContentDeliveryStudio.App.ViewModels;

internal delegate Task<bool> ImageSeriesFakePlanningRunner(
    string goal,
    string audience,
    int itemCount,
    string styleBrief);

internal delegate Task<bool> ImageSeriesBriefRunner(
    SeriesSummaryViewModel selectedSeries,
    string goal,
    string audience,
    string styleBrief);

internal delegate Task<bool> ImageSeriesBlueprintPromotionRunner(DesignBlueprintRowViewModel selectedBlueprint);

internal delegate Task<bool> ImageSeriesPromptDirectionPromotionRunner(
    SeriesItemViewModel selectedItem,
    PromptDirectionRowViewModel selectedDirection);

public sealed partial class ImageSeriesBriefWorkspaceViewModel : ObservableObject
{
    private readonly ImageSeriesFakePlanningRunner _runFakePlanning;
    private readonly ImageSeriesBriefRunner _createBrief;
    private readonly ImageSeriesBriefRunner _generateDesignBlueprints;
    private readonly ImageSeriesBriefRunner _generatePromptDirections;
    private readonly ImageSeriesBlueprintPromotionRunner _promoteDesignBlueprint;
    private readonly ImageSeriesPromptDirectionPromotionRunner _promotePromptDirection;
    private readonly Func<bool> _canMutate;
    private readonly Func<bool> _hasSelectedProject;
    private readonly Func<SeriesSummaryViewModel?> _getSelectedSeries;
    private readonly Func<SeriesItemViewModel?> _getSelectedItem;

    [ObservableProperty]
    private IReadOnlyList<DesignBlueprintRowViewModel> _designBlueprintRows = [];

    [ObservableProperty]
    private DesignBlueprintRowViewModel? _selectedDesignBlueprint;

    [ObservableProperty]
    private IReadOnlyList<PromptDirectionRowViewModel> _promptDirectionRows = [];

    [ObservableProperty]
    private PromptDirectionRowViewModel? _selectedPromptDirection;

    [ObservableProperty]
    private string _newPlanningGoal = string.Empty;

    [ObservableProperty]
    private string _newPlanningAudience = string.Empty;

    [ObservableProperty]
    private string _newPlanningItemCount = "3";

    [ObservableProperty]
    private string _newPlanningStyleBrief = string.Empty;

    [ObservableProperty]
    private string _fakePlanningTitle = string.Empty;

    [ObservableProperty]
    private string _planningGoalLabel = string.Empty;

    [ObservableProperty]
    private string _planningAudienceLabel = string.Empty;

    [ObservableProperty]
    private string _planningItemCountLabel = string.Empty;

    [ObservableProperty]
    private string _planningStyleBriefLabel = string.Empty;

    [ObservableProperty]
    private string _runFakePlanningText = string.Empty;

    [ObservableProperty]
    private string _briefGoalLabel = string.Empty;

    [ObservableProperty]
    private string _briefAudienceLabel = string.Empty;

    [ObservableProperty]
    private string _briefStyleIntentLabel = string.Empty;

    [ObservableProperty]
    private string _createBriefText = string.Empty;

    [ObservableProperty]
    private string _generateDesignBlueprintsText = string.Empty;

    [ObservableProperty]
    private string _promoteDesignBlueprintText = string.Empty;

    [ObservableProperty]
    private string _blueprintRoutesHeader = string.Empty;

    [ObservableProperty]
    private string _noBlueprintRowsText = string.Empty;

    [ObservableProperty]
    private string _generatePromptDirectionsText = string.Empty;

    [ObservableProperty]
    private string _promotePromptDirectionText = string.Empty;

    [ObservableProperty]
    private string _promptDirectionsHeader = string.Empty;

    [ObservableProperty]
    private string _noPromptDirectionRowsText = string.Empty;

    internal ImageSeriesBriefWorkspaceViewModel(
        ImageSeriesFakePlanningRunner runFakePlanning,
        ImageSeriesBriefRunner createBrief,
        ImageSeriesBriefRunner generateDesignBlueprints,
        ImageSeriesBriefRunner generatePromptDirections,
        ImageSeriesBlueprintPromotionRunner promoteDesignBlueprint,
        ImageSeriesPromptDirectionPromotionRunner promotePromptDirection,
        Func<bool> canMutate,
        Func<bool> hasSelectedProject,
        Func<SeriesSummaryViewModel?> getSelectedSeries,
        Func<SeriesItemViewModel?> getSelectedItem)
    {
        _runFakePlanning = runFakePlanning ?? throw new ArgumentNullException(nameof(runFakePlanning));
        _createBrief = createBrief ?? throw new ArgumentNullException(nameof(createBrief));
        _generateDesignBlueprints = generateDesignBlueprints ?? throw new ArgumentNullException(nameof(generateDesignBlueprints));
        _generatePromptDirections = generatePromptDirections ?? throw new ArgumentNullException(nameof(generatePromptDirections));
        _promoteDesignBlueprint = promoteDesignBlueprint ?? throw new ArgumentNullException(nameof(promoteDesignBlueprint));
        _promotePromptDirection = promotePromptDirection ?? throw new ArgumentNullException(nameof(promotePromptDirection));
        _canMutate = canMutate ?? throw new ArgumentNullException(nameof(canMutate));
        _hasSelectedProject = hasSelectedProject ?? throw new ArgumentNullException(nameof(hasSelectedProject));
        _getSelectedSeries = getSelectedSeries ?? throw new ArgumentNullException(nameof(getSelectedSeries));
        _getSelectedItem = getSelectedItem ?? throw new ArgumentNullException(nameof(getSelectedItem));
    }

    public bool HasDesignBlueprintRows => DesignBlueprintRows.Count > 0;

    public bool HasPromptDirectionRows => PromptDirectionRows.Count > 0;

    internal void ApplyProjection(
        IReadOnlyList<DesignBlueprintRowViewModel> blueprintRows,
        IReadOnlyList<PromptDirectionRowViewModel> promptDirectionRows,
        DesignBlueprintRowViewModel? selectedBlueprint,
        PromptDirectionRowViewModel? selectedDirection)
    {
        ArgumentNullException.ThrowIfNull(blueprintRows);
        ArgumentNullException.ThrowIfNull(promptDirectionRows);

        var blueprintId = selectedBlueprint?.BlueprintId ?? SelectedDesignBlueprint?.BlueprintId;
        var directionBriefId = selectedDirection?.CreativeBriefId ?? SelectedPromptDirection?.CreativeBriefId;
        var directionKey = selectedDirection?.DirectionKey ?? SelectedPromptDirection?.DirectionKey;
        DesignBlueprintRows = blueprintRows;
        SelectedDesignBlueprint = blueprintRows.FirstOrDefault(row => row.BlueprintId == blueprintId)
            ?? blueprintRows.FirstOrDefault();
        PromptDirectionRows = promptDirectionRows;
        SelectedPromptDirection = promptDirectionRows.FirstOrDefault(
            row => row.CreativeBriefId == directionBriefId && row.DirectionKey == directionKey)
            ?? promptDirectionRows.FirstOrDefault();
    }

    internal void ApplyLocalization(MainWindowLocalizationPayload payload)
    {
        ArgumentNullException.ThrowIfNull(payload);

        FakePlanningTitle = payload.FakePlanningTitle;
        PlanningGoalLabel = payload.PlanningGoalLabel;
        PlanningAudienceLabel = payload.PlanningAudienceLabel;
        PlanningItemCountLabel = payload.PlanningItemCountLabel;
        PlanningStyleBriefLabel = payload.PlanningStyleBriefLabel;
        RunFakePlanningText = payload.RunFakePlanningText;
        BriefGoalLabel = payload.BriefGoalLabel;
        BriefAudienceLabel = payload.BriefAudienceLabel;
        BriefStyleIntentLabel = payload.BriefStyleIntentLabel;
        CreateBriefText = payload.CreateBriefText;
        GenerateDesignBlueprintsText = payload.GenerateDesignBlueprintsText;
        PromoteDesignBlueprintText = payload.PromoteDesignBlueprintText;
        BlueprintRoutesHeader = payload.BlueprintRoutesHeader;
        NoBlueprintRowsText = payload.NoBlueprintRowsText;
        GeneratePromptDirectionsText = payload.GeneratePromptDirectionsText;
        PromotePromptDirectionText = payload.PromotePromptDirectionText;
        PromptDirectionsHeader = payload.PromptDirectionsHeader;
        NoPromptDirectionRowsText = payload.NoPromptDirectionRowsText;
    }

    internal void NotifyCommandStatesChanged()
    {
        RunFakePlanningCommand.NotifyCanExecuteChanged();
        CreateBriefCommand.NotifyCanExecuteChanged();
        GenerateDesignBlueprintsCommand.NotifyCanExecuteChanged();
        GeneratePromptDirectionsCommand.NotifyCanExecuteChanged();
        PromoteDesignBlueprintCommand.NotifyCanExecuteChanged();
        PromotePromptDirectionCommand.NotifyCanExecuteChanged();
    }

    partial void OnDesignBlueprintRowsChanged(IReadOnlyList<DesignBlueprintRowViewModel> value)
    {
        OnPropertyChanged(nameof(HasDesignBlueprintRows));
        NotifyCommandStatesChanged();
    }

    partial void OnSelectedDesignBlueprintChanged(DesignBlueprintRowViewModel? value) =>
        PromoteDesignBlueprintCommand.NotifyCanExecuteChanged();

    partial void OnPromptDirectionRowsChanged(IReadOnlyList<PromptDirectionRowViewModel> value)
    {
        OnPropertyChanged(nameof(HasPromptDirectionRows));
        NotifyCommandStatesChanged();
    }

    partial void OnSelectedPromptDirectionChanged(PromptDirectionRowViewModel? value) =>
        PromotePromptDirectionCommand.NotifyCanExecuteChanged();

    partial void OnNewPlanningGoalChanged(string value) => NotifyCommandStatesChanged();

    partial void OnNewPlanningAudienceChanged(string value) => NotifyCommandStatesChanged();

    partial void OnNewPlanningItemCountChanged(string value) => RunFakePlanningCommand.NotifyCanExecuteChanged();

    partial void OnNewPlanningStyleBriefChanged(string value) => NotifyCommandStatesChanged();

    [RelayCommand(CanExecute = nameof(CanRunFakePlanning))]
    private Task RunFakePlanningAsync()
    {
        return TryGetPlanningItemCount(out var itemCount)
            ? _runFakePlanning(NewPlanningGoal, NewPlanningAudience, itemCount, NewPlanningStyleBrief)
            : Task.CompletedTask;
    }

    private bool CanRunFakePlanning()
    {
        return CanRunProjectMutation()
            && !string.IsNullOrWhiteSpace(NewPlanningGoal)
            && !string.IsNullOrWhiteSpace(NewPlanningAudience)
            && TryGetPlanningItemCount(out _);
    }

    [RelayCommand(CanExecute = nameof(CanCreateBrief))]
    private Task CreateBriefAsync() => RunBriefMutationAsync(_createBrief);

    [RelayCommand(CanExecute = nameof(CanGenerateDesignBlueprints))]
    private Task GenerateDesignBlueprintsAsync() => RunBriefMutationAsync(_generateDesignBlueprints);

    [RelayCommand(CanExecute = nameof(CanGeneratePromptDirections))]
    private Task GeneratePromptDirectionsAsync() => RunBriefMutationAsync(_generatePromptDirections);

    private bool CanCreateBrief()
    {
        return CanRunProjectMutation()
            && _getSelectedSeries() is not null
            && !string.IsNullOrWhiteSpace(NewPlanningGoal)
            && !string.IsNullOrWhiteSpace(NewPlanningAudience);
    }

    private bool CanGenerateDesignBlueprints() => CanCreateBrief();

    private bool CanGeneratePromptDirections() => CanCreateBrief();

    [RelayCommand(CanExecute = nameof(CanPromoteDesignBlueprint))]
    private Task PromoteDesignBlueprintAsync()
    {
        return SelectedDesignBlueprint is null
            ? Task.CompletedTask
            : _promoteDesignBlueprint(SelectedDesignBlueprint);
    }

    private bool CanPromoteDesignBlueprint()
    {
        return CanRunProjectMutation() && SelectedDesignBlueprint is not null;
    }

    [RelayCommand(CanExecute = nameof(CanPromotePromptDirection))]
    private Task PromotePromptDirectionAsync()
    {
        var selectedItem = _getSelectedItem();
        return selectedItem is null || SelectedPromptDirection is null
            ? Task.CompletedTask
            : _promotePromptDirection(selectedItem, SelectedPromptDirection);
    }

    private bool CanPromotePromptDirection()
    {
        return CanRunProjectMutation()
            && _getSelectedItem() is not null
            && SelectedPromptDirection is not null;
    }

    private Task<bool> RunBriefMutationAsync(ImageSeriesBriefRunner mutation)
    {
        var selectedSeries = _getSelectedSeries();
        return selectedSeries is null
            ? Task.FromResult(false)
            : mutation(selectedSeries, NewPlanningGoal, NewPlanningAudience, NewPlanningStyleBrief);
    }

    private bool CanRunProjectMutation()
    {
        return _canMutate() && _hasSelectedProject();
    }

    private bool TryGetPlanningItemCount(out int itemCount)
    {
        return int.TryParse(NewPlanningItemCount, out itemCount) && itemCount > 0;
    }
}
