using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ContentDeliveryStudio.App.ViewModels;

internal delegate Task<bool> ImageSeriesCreateSeriesRunner(string title, string description);

internal delegate Task<bool> ImageSeriesAddItemRunner(
    SeriesSummaryViewModel selectedSeries,
    string title,
    string brief);

internal delegate Task<bool> ImageSeriesCreatePromptVersionRunner(
    SeriesSummaryViewModel selectedSeries,
    SeriesItemViewModel selectedItem,
    string promptText);

public sealed partial class ImageSeriesPlanningWorkspaceViewModel : ObservableObject
{
    private readonly ImageSeriesCreateSeriesRunner _createSeries;
    private readonly ImageSeriesAddItemRunner _addItem;
    private readonly ImageSeriesCreatePromptVersionRunner _createPromptVersion;
    private readonly Func<bool> _canMutate;
    private readonly Func<bool> _hasSelectedProject;
    private readonly Func<SeriesItemViewModel?, string> _buildSelectedItemTitle;
    private readonly Action<SeriesSummaryViewModel?> _seriesSelectionChanged;
    private readonly Action<SeriesItemViewModel?> _itemSelectionChanged;
    private readonly Action _promptProjectionChanged;

    [ObservableProperty]
    private IReadOnlyList<SeriesSummaryViewModel> _series = [];

    [ObservableProperty]
    private SeriesSummaryViewModel? _selectedSeries;

    [ObservableProperty]
    private IReadOnlyList<SeriesItemViewModel> _seriesItems = [];

    [ObservableProperty]
    private SeriesItemViewModel? _selectedSeriesItem;

    [ObservableProperty]
    private IReadOnlyList<PlanRowViewModel> _planRows = [];

    [ObservableProperty]
    private IReadOnlyList<PromptVersionViewModel> _promptVersions = [];

    [ObservableProperty]
    private IReadOnlyList<PromptRowViewModel> _promptRows = [];

    [ObservableProperty]
    private string _newSeriesTitle = string.Empty;

    [ObservableProperty]
    private string _newSeriesDescription = string.Empty;

    [ObservableProperty]
    private string _newItemTitle = string.Empty;

    [ObservableProperty]
    private string _newItemBrief = string.Empty;

    [ObservableProperty]
    private string _newPromptText = string.Empty;

    [ObservableProperty]
    private string _planEditorTitle = string.Empty;

    [ObservableProperty]
    private string _seriesTitleLabel = string.Empty;

    [ObservableProperty]
    private string _seriesDescriptionLabel = string.Empty;

    [ObservableProperty]
    private string _createSeriesText = string.Empty;

    [ObservableProperty]
    private string _availableSeriesTitle = string.Empty;

    [ObservableProperty]
    private string _itemTitleLabel = string.Empty;

    [ObservableProperty]
    private string _itemBriefLabel = string.Empty;

    [ObservableProperty]
    private string _addItemText = string.Empty;

    [ObservableProperty]
    private string _seriesItemsTitle = string.Empty;

    [ObservableProperty]
    private string _noSeriesSelectedText = string.Empty;

    [ObservableProperty]
    private string _planSeriesColumn = string.Empty;

    [ObservableProperty]
    private string _planItemColumn = string.Empty;

    [ObservableProperty]
    private string _planBriefColumn = string.Empty;

    [ObservableProperty]
    private string _planKindColumn = string.Empty;

    [ObservableProperty]
    private string _planStatusColumn = string.Empty;

    [ObservableProperty]
    private string _noPlanRowsText = string.Empty;

    [ObservableProperty]
    private string _noItemsInSeriesText = string.Empty;

    [ObservableProperty]
    private string _promptEditorTitle = string.Empty;

    [ObservableProperty]
    private string _selectedItemTitle = string.Empty;

    [ObservableProperty]
    private string _promptTextLabel = string.Empty;

    [ObservableProperty]
    private string _defaultGenerationSettingsText = string.Empty;

    [ObservableProperty]
    private string _createPromptVersionText = string.Empty;

    [ObservableProperty]
    private string _promptHistoryTitle = string.Empty;

    [ObservableProperty]
    private string _promptVersionColumn = string.Empty;

    [ObservableProperty]
    private string _promptItemColumn = string.Empty;

    [ObservableProperty]
    private string _promptTextColumn = string.Empty;

    [ObservableProperty]
    private string _promptSettingsColumn = string.Empty;

    [ObservableProperty]
    private string _promptCreatedColumn = string.Empty;

    [ObservableProperty]
    private string _noPromptRowsText = string.Empty;

    [ObservableProperty]
    private string _noItemSelectedForPromptText = string.Empty;

    [ObservableProperty]
    private string _selectedSeriesItemTitleText = string.Empty;

    internal ImageSeriesPlanningWorkspaceViewModel(
        ImageSeriesCreateSeriesRunner createSeries,
        ImageSeriesAddItemRunner addItem,
        ImageSeriesCreatePromptVersionRunner createPromptVersion,
        Func<bool> canMutate,
        Func<bool> hasSelectedProject,
        Func<SeriesItemViewModel?, string> buildSelectedItemTitle,
        Action<SeriesSummaryViewModel?> seriesSelectionChanged,
        Action<SeriesItemViewModel?> itemSelectionChanged,
        Action promptProjectionChanged)
    {
        _createSeries = createSeries ?? throw new ArgumentNullException(nameof(createSeries));
        _addItem = addItem ?? throw new ArgumentNullException(nameof(addItem));
        _createPromptVersion = createPromptVersion ?? throw new ArgumentNullException(nameof(createPromptVersion));
        _canMutate = canMutate ?? throw new ArgumentNullException(nameof(canMutate));
        _hasSelectedProject = hasSelectedProject ?? throw new ArgumentNullException(nameof(hasSelectedProject));
        _buildSelectedItemTitle = buildSelectedItemTitle ?? throw new ArgumentNullException(nameof(buildSelectedItemTitle));
        _seriesSelectionChanged = seriesSelectionChanged ?? throw new ArgumentNullException(nameof(seriesSelectionChanged));
        _itemSelectionChanged = itemSelectionChanged ?? throw new ArgumentNullException(nameof(itemSelectionChanged));
        _promptProjectionChanged = promptProjectionChanged ?? throw new ArgumentNullException(nameof(promptProjectionChanged));
    }

    public bool HasPlanRows => PlanRows.Count > 0;

    public bool HasPromptRows => PromptRows.Count > 0;

    internal void ApplyProjection(
        IReadOnlyList<SeriesSummaryViewModel> series,
        IReadOnlyList<PlanRowViewModel> planRows,
        IReadOnlyList<PromptRowViewModel> promptRows,
        SeriesSummaryViewModel? selectedSeries,
        SeriesItemViewModel? selectedItem)
    {
        ArgumentNullException.ThrowIfNull(series);
        ArgumentNullException.ThrowIfNull(planRows);
        ArgumentNullException.ThrowIfNull(promptRows);

        Series = series;
        SelectedSeries = series.FirstOrDefault(value => value.Id == selectedSeries?.Id)
            ?? series.FirstOrDefault();
        SelectedSeriesItem = SeriesItems.FirstOrDefault(value => value.Id == selectedItem?.Id)
            ?? SeriesItems.FirstOrDefault();
        PlanRows = planRows;
        PromptRows = promptRows;
    }

    internal void ApplyPlanRows(IReadOnlyList<PlanRowViewModel> rows)
    {
        ArgumentNullException.ThrowIfNull(rows);
        PlanRows = rows;
    }

    internal void ApplyPromptRows(IReadOnlyList<PromptRowViewModel> rows)
    {
        ArgumentNullException.ThrowIfNull(rows);
        PromptRows = rows;
    }

    internal void ApplyLocalization(MainWindowLocalizationPayload payload)
    {
        ArgumentNullException.ThrowIfNull(payload);

        PlanEditorTitle = payload.PlanEditorTitle;
        SeriesTitleLabel = payload.SeriesTitleLabel;
        SeriesDescriptionLabel = payload.SeriesDescriptionLabel;
        CreateSeriesText = payload.CreateSeriesText;
        AvailableSeriesTitle = payload.AvailableSeriesTitle;
        ItemTitleLabel = payload.ItemTitleLabel;
        ItemBriefLabel = payload.ItemBriefLabel;
        AddItemText = payload.AddItemText;
        SeriesItemsTitle = payload.SeriesItemsTitle;
        NoSeriesSelectedText = payload.NoSeriesSelectedText;
        PlanSeriesColumn = payload.PlanSeriesColumn;
        PlanItemColumn = payload.PlanItemColumn;
        PlanBriefColumn = payload.PlanBriefColumn;
        PlanKindColumn = payload.PlanKindColumn;
        PlanStatusColumn = payload.PlanStatusColumn;
        NoPlanRowsText = payload.NoPlanRowsText;
        NoItemsInSeriesText = payload.NoItemsInSeriesText;
        PromptEditorTitle = payload.PromptEditorTitle;
        SelectedItemTitle = payload.SelectedItemTitle;
        PromptTextLabel = payload.PromptTextLabel;
        DefaultGenerationSettingsText = payload.DefaultGenerationSettingsText;
        CreatePromptVersionText = payload.CreatePromptVersionText;
        PromptHistoryTitle = payload.PromptHistoryTitle;
        PromptVersionColumn = payload.PromptVersionColumn;
        PromptItemColumn = payload.PromptItemColumn;
        PromptTextColumn = payload.PromptTextColumn;
        PromptSettingsColumn = payload.PromptSettingsColumn;
        PromptCreatedColumn = payload.PromptCreatedColumn;
        NoPromptRowsText = payload.NoPromptRowsText;
        NoItemSelectedForPromptText = payload.NoItemSelectedForPromptText;
        SelectedSeriesItemTitleText = _buildSelectedItemTitle(SelectedSeriesItem);
    }

    internal void NotifyCommandStatesChanged()
    {
        CreateSeriesCommand.NotifyCanExecuteChanged();
        AddItemCommand.NotifyCanExecuteChanged();
        CreatePromptVersionCommand.NotifyCanExecuteChanged();
    }

    partial void OnSelectedSeriesChanged(SeriesSummaryViewModel? value)
    {
        SeriesItems = value?.Items ?? [];
        SelectedSeriesItem = SeriesItems.FirstOrDefault();
        AddItemCommand.NotifyCanExecuteChanged();
        _seriesSelectionChanged(value);
    }

    partial void OnSelectedSeriesItemChanged(SeriesItemViewModel? value)
    {
        PromptVersions = value?.PromptVersions ?? [];
        SelectedSeriesItemTitleText = _buildSelectedItemTitle(value);
        CreatePromptVersionCommand.NotifyCanExecuteChanged();
        _itemSelectionChanged(value);
    }

    partial void OnPlanRowsChanged(IReadOnlyList<PlanRowViewModel> value)
    {
        OnPropertyChanged(nameof(HasPlanRows));
    }

    partial void OnPromptRowsChanged(IReadOnlyList<PromptRowViewModel> value)
    {
        OnPropertyChanged(nameof(HasPromptRows));
        _promptProjectionChanged();
    }

    partial void OnNewSeriesTitleChanged(string value) => CreateSeriesCommand.NotifyCanExecuteChanged();

    partial void OnNewItemTitleChanged(string value) => AddItemCommand.NotifyCanExecuteChanged();

    partial void OnNewPromptTextChanged(string value) => CreatePromptVersionCommand.NotifyCanExecuteChanged();

    [RelayCommand(CanExecute = nameof(CanCreateSeries))]
    private async Task CreateSeriesAsync()
    {
        if (await _createSeries(NewSeriesTitle, NewSeriesDescription))
        {
            NewSeriesTitle = string.Empty;
            NewSeriesDescription = string.Empty;
        }
    }

    private bool CanCreateSeries()
    {
        return CanRunProjectMutation() && !string.IsNullOrWhiteSpace(NewSeriesTitle);
    }

    [RelayCommand(CanExecute = nameof(CanAddItem))]
    private async Task AddItemAsync()
    {
        if (SelectedSeries is null)
        {
            return;
        }

        if (await _addItem(SelectedSeries, NewItemTitle, NewItemBrief))
        {
            NewItemTitle = string.Empty;
            NewItemBrief = string.Empty;
        }
    }

    private bool CanAddItem()
    {
        return CanRunProjectMutation()
            && SelectedSeries is not null
            && !string.IsNullOrWhiteSpace(NewItemTitle);
    }

    [RelayCommand(CanExecute = nameof(CanCreatePromptVersion))]
    private async Task CreatePromptVersionAsync()
    {
        if (SelectedSeries is null || SelectedSeriesItem is null)
        {
            return;
        }

        if (await _createPromptVersion(SelectedSeries, SelectedSeriesItem, NewPromptText))
        {
            NewPromptText = string.Empty;
        }
    }

    private bool CanCreatePromptVersion()
    {
        return CanRunProjectMutation()
            && SelectedSeriesItem is not null
            && !string.IsNullOrWhiteSpace(NewPromptText);
    }

    private bool CanRunProjectMutation()
    {
        return _canMutate() && _hasSelectedProject();
    }
}
