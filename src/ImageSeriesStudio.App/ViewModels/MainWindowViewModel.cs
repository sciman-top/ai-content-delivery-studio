using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ImageSeriesStudio.Application.Localization;
using ImageSeriesStudio.Application.Projects;
using ImageSeriesStudio.Core.Generation;
using ImageSeriesStudio.Core.Projects;
using ImageSeriesStudio.Core.Providers;

namespace ImageSeriesStudio.App.ViewModels;

public sealed partial class MainWindowViewModel : ObservableObject
{
    private readonly LocalizationService _localizationService;
    private readonly ProjectApplicationService _projectService;

    private string _appTitle = string.Empty;
    private string _providerMode = string.Empty;
    private string _workspaceHeader = string.Empty;
    private string _inspectorTitle = string.Empty;
    private string _activityTitle = string.Empty;
    private string _inspectorSummary = string.Empty;
    private string _languageLabel = string.Empty;
    private IReadOnlyList<string> _navigationItems = [];
    private IReadOnlyList<WorkbenchTabViewModel> _workbenchTabs = [];
    private IReadOnlyList<string> _activityItems = [];
    private IReadOnlyList<LanguageOptionViewModel> _languageOptions = [];
    private IReadOnlyList<ProjectSummaryViewModel> _projects = [];
    private LanguageOptionViewModel? _selectedLanguageOption;
    private ProjectSummaryViewModel? _selectedProject;
    private string _newProjectName = string.Empty;
    private string _newPlanningGoal = string.Empty;
    private string _newPlanningAudience = string.Empty;
    private string _newPlanningItemCount = "3";
    private string _newPlanningStyleBrief = string.Empty;
    private string _projectNameLabel = string.Empty;
    private string _newProjectNamePlaceholder = string.Empty;
    private string _createProjectText = string.Empty;
    private string _availableProjectsTitle = string.Empty;
    private string _currentProjectTitle = string.Empty;
    private string _currentProjectSummary = string.Empty;
    private string _fakePlanningTitle = string.Empty;
    private string _planningGoalLabel = string.Empty;
    private string _planningAudienceLabel = string.Empty;
    private string _planningItemCountLabel = string.Empty;
    private string _planningStyleBriefLabel = string.Empty;
    private string _runFakePlanningText = string.Empty;
    private string _runFakeGenerationText = string.Empty;
    private string _queueItemColumn = string.Empty;
    private string _queueStatusColumn = string.Empty;
    private string _queueAttemptsColumn = string.Empty;
    private string _queueOutputColumn = string.Empty;
    private string _queueErrorColumn = string.Empty;
    private string _noQueueRowsText = string.Empty;
    private string _galleryItemColumn = string.Empty;
    private string _galleryImageColumn = string.Empty;
    private string _galleryMetadataColumn = string.Empty;
    private string _noGalleryRowsText = string.Empty;
    private string _planEditorTitle = string.Empty;
    private string _seriesTitleLabel = string.Empty;
    private string _seriesDescriptionLabel = string.Empty;
    private string _createSeriesText = string.Empty;
    private string _availableSeriesTitle = string.Empty;
    private string _itemTitleLabel = string.Empty;
    private string _itemBriefLabel = string.Empty;
    private string _addItemText = string.Empty;
    private string _seriesItemsTitle = string.Empty;
    private string _noSeriesSelectedText = string.Empty;
    private string _planSeriesColumn = string.Empty;
    private string _planItemColumn = string.Empty;
    private string _planBriefColumn = string.Empty;
    private string _planStatusColumn = string.Empty;
    private string _noPlanRowsText = string.Empty;
    private string _noItemsInSeriesText = string.Empty;
    private string _promptEditorTitle = string.Empty;
    private string _selectedItemTitle = string.Empty;
    private string _promptTextLabel = string.Empty;
    private string _defaultGenerationSettingsText = string.Empty;
    private string _createPromptVersionText = string.Empty;
    private string _promptHistoryTitle = string.Empty;
    private string _promptVersionColumn = string.Empty;
    private string _promptItemColumn = string.Empty;
    private string _promptTextColumn = string.Empty;
    private string _promptSettingsColumn = string.Empty;
    private string _promptCreatedColumn = string.Empty;
    private string _noPromptRowsText = string.Empty;
    private string _noItemSelectedForPromptText = string.Empty;
    private string _selectedSeriesItemTitleText = string.Empty;
    private string _newSeriesTitle = string.Empty;
    private string _newSeriesDescription = string.Empty;
    private string _newItemTitle = string.Empty;
    private string _newItemBrief = string.Empty;
    private string _newPromptText = string.Empty;
    private IReadOnlyList<SeriesSummaryViewModel> _series = [];
    private IReadOnlyList<SeriesItemViewModel> _seriesItems = [];
    private IReadOnlyList<PlanRowViewModel> _planRows = [];
    private IReadOnlyList<PromptVersionViewModel> _promptVersions = [];
    private IReadOnlyList<PromptRowViewModel> _promptRows = [];
    private IReadOnlyList<QueueRowViewModel> _queueRows = [];
    private IReadOnlyList<GalleryRowViewModel> _galleryRows = [];
    private SeriesSummaryViewModel? _selectedSeries;
    private SeriesItemViewModel? _selectedSeriesItem;

    public MainWindowViewModel(
        LocalizationService localizationService,
        ProjectApplicationService projectService)
    {
        _localizationService = localizationService;
        _projectService = projectService;
        RefreshLocalizedText();
        SelectedLanguageOption = LanguageOptions.First(option => option.Preference == _localizationService.Preference);
        NewProjectName = NewProjectNamePlaceholder;
        NewPlanningAudience = Text(LocalizationKey.DefaultPlanningAudience);
        NewPlanningStyleBrief = Text(LocalizationKey.DefaultPlanningStyleBrief);
        _ = RefreshProjectsAsync();
    }

    public string AppTitle
    {
        get => _appTitle;
        private set => SetProperty(ref _appTitle, value);
    }

    public string ProviderMode
    {
        get => _providerMode;
        private set => SetProperty(ref _providerMode, value);
    }

    public string WorkspaceHeader
    {
        get => _workspaceHeader;
        private set => SetProperty(ref _workspaceHeader, value);
    }

    public string InspectorTitle
    {
        get => _inspectorTitle;
        private set => SetProperty(ref _inspectorTitle, value);
    }

    public string ActivityTitle
    {
        get => _activityTitle;
        private set => SetProperty(ref _activityTitle, value);
    }

    public string InspectorSummary
    {
        get => _inspectorSummary;
        private set => SetProperty(ref _inspectorSummary, value);
    }

    public string LanguageLabel
    {
        get => _languageLabel;
        private set => SetProperty(ref _languageLabel, value);
    }

    public IReadOnlyList<string> NavigationItems
    {
        get => _navigationItems;
        private set => SetProperty(ref _navigationItems, value);
    }

    public IReadOnlyList<WorkbenchTabViewModel> WorkbenchTabs
    {
        get => _workbenchTabs;
        private set => SetProperty(ref _workbenchTabs, value);
    }

    public IReadOnlyList<string> ActivityItems
    {
        get => _activityItems;
        private set => SetProperty(ref _activityItems, value);
    }

    public IReadOnlyList<LanguageOptionViewModel> LanguageOptions
    {
        get => _languageOptions;
        private set => SetProperty(ref _languageOptions, value);
    }

    public IReadOnlyList<ProjectSummaryViewModel> Projects
    {
        get => _projects;
        private set => SetProperty(ref _projects, value);
    }

    public ProjectSummaryViewModel? SelectedProject
    {
        get => _selectedProject;
        set
        {
            if (!SetProperty(ref _selectedProject, value))
            {
                return;
            }

            CurrentProjectSummary = value is null
                ? Text(LocalizationKey.NoProjectLoaded)
                : $"{value.Name} ({value.UpdatedAt.LocalDateTime:g})";
            if (value is not null && string.IsNullOrWhiteSpace(NewPlanningGoal))
            {
                NewPlanningGoal = value.Name;
            }

            QueueRows = [];
            GalleryRows = [];
            RunFakePlanningCommand.NotifyCanExecuteChanged();
            RunFakeGenerationCommand.NotifyCanExecuteChanged();
            _ = value is null ? ClearPlanAsync() : LoadPlanAsync(value.Id);
        }
    }

    public string NewProjectName
    {
        get => _newProjectName;
        set
        {
            if (SetProperty(ref _newProjectName, value))
            {
                CreateProjectCommand.NotifyCanExecuteChanged();
            }
        }
    }

    public string NewPlanningGoal
    {
        get => _newPlanningGoal;
        set
        {
            if (SetProperty(ref _newPlanningGoal, value))
            {
                RunFakePlanningCommand.NotifyCanExecuteChanged();
            }
        }
    }

    public string NewPlanningAudience
    {
        get => _newPlanningAudience;
        set
        {
            if (SetProperty(ref _newPlanningAudience, value))
            {
                RunFakePlanningCommand.NotifyCanExecuteChanged();
            }
        }
    }

    public string NewPlanningItemCount
    {
        get => _newPlanningItemCount;
        set
        {
            if (SetProperty(ref _newPlanningItemCount, value))
            {
                RunFakePlanningCommand.NotifyCanExecuteChanged();
            }
        }
    }

    public string NewPlanningStyleBrief
    {
        get => _newPlanningStyleBrief;
        set => SetProperty(ref _newPlanningStyleBrief, value);
    }

    public string ProjectNameLabel
    {
        get => _projectNameLabel;
        private set => SetProperty(ref _projectNameLabel, value);
    }

    public string NewProjectNamePlaceholder
    {
        get => _newProjectNamePlaceholder;
        private set => SetProperty(ref _newProjectNamePlaceholder, value);
    }

    public string CreateProjectText
    {
        get => _createProjectText;
        private set => SetProperty(ref _createProjectText, value);
    }

    public string AvailableProjectsTitle
    {
        get => _availableProjectsTitle;
        private set => SetProperty(ref _availableProjectsTitle, value);
    }

    public string CurrentProjectTitle
    {
        get => _currentProjectTitle;
        private set => SetProperty(ref _currentProjectTitle, value);
    }

    public string CurrentProjectSummary
    {
        get => _currentProjectSummary;
        private set => SetProperty(ref _currentProjectSummary, value);
    }

    public string FakePlanningTitle
    {
        get => _fakePlanningTitle;
        private set => SetProperty(ref _fakePlanningTitle, value);
    }

    public string PlanningGoalLabel
    {
        get => _planningGoalLabel;
        private set => SetProperty(ref _planningGoalLabel, value);
    }

    public string PlanningAudienceLabel
    {
        get => _planningAudienceLabel;
        private set => SetProperty(ref _planningAudienceLabel, value);
    }

    public string PlanningItemCountLabel
    {
        get => _planningItemCountLabel;
        private set => SetProperty(ref _planningItemCountLabel, value);
    }

    public string PlanningStyleBriefLabel
    {
        get => _planningStyleBriefLabel;
        private set => SetProperty(ref _planningStyleBriefLabel, value);
    }

    public string RunFakePlanningText
    {
        get => _runFakePlanningText;
        private set => SetProperty(ref _runFakePlanningText, value);
    }

    public string RunFakeGenerationText
    {
        get => _runFakeGenerationText;
        private set => SetProperty(ref _runFakeGenerationText, value);
    }

    public string QueueItemColumn
    {
        get => _queueItemColumn;
        private set => SetProperty(ref _queueItemColumn, value);
    }

    public string QueueStatusColumn
    {
        get => _queueStatusColumn;
        private set => SetProperty(ref _queueStatusColumn, value);
    }

    public string QueueAttemptsColumn
    {
        get => _queueAttemptsColumn;
        private set => SetProperty(ref _queueAttemptsColumn, value);
    }

    public string QueueOutputColumn
    {
        get => _queueOutputColumn;
        private set => SetProperty(ref _queueOutputColumn, value);
    }

    public string QueueErrorColumn
    {
        get => _queueErrorColumn;
        private set => SetProperty(ref _queueErrorColumn, value);
    }

    public string NoQueueRowsText
    {
        get => _noQueueRowsText;
        private set => SetProperty(ref _noQueueRowsText, value);
    }

    public string GalleryItemColumn
    {
        get => _galleryItemColumn;
        private set => SetProperty(ref _galleryItemColumn, value);
    }

    public string GalleryImageColumn
    {
        get => _galleryImageColumn;
        private set => SetProperty(ref _galleryImageColumn, value);
    }

    public string GalleryMetadataColumn
    {
        get => _galleryMetadataColumn;
        private set => SetProperty(ref _galleryMetadataColumn, value);
    }

    public string NoGalleryRowsText
    {
        get => _noGalleryRowsText;
        private set => SetProperty(ref _noGalleryRowsText, value);
    }

    public string PlanEditorTitle
    {
        get => _planEditorTitle;
        private set => SetProperty(ref _planEditorTitle, value);
    }

    public string SeriesTitleLabel
    {
        get => _seriesTitleLabel;
        private set => SetProperty(ref _seriesTitleLabel, value);
    }

    public string SeriesDescriptionLabel
    {
        get => _seriesDescriptionLabel;
        private set => SetProperty(ref _seriesDescriptionLabel, value);
    }

    public string CreateSeriesText
    {
        get => _createSeriesText;
        private set => SetProperty(ref _createSeriesText, value);
    }

    public string AvailableSeriesTitle
    {
        get => _availableSeriesTitle;
        private set => SetProperty(ref _availableSeriesTitle, value);
    }

    public string ItemTitleLabel
    {
        get => _itemTitleLabel;
        private set => SetProperty(ref _itemTitleLabel, value);
    }

    public string ItemBriefLabel
    {
        get => _itemBriefLabel;
        private set => SetProperty(ref _itemBriefLabel, value);
    }

    public string AddItemText
    {
        get => _addItemText;
        private set => SetProperty(ref _addItemText, value);
    }

    public string SeriesItemsTitle
    {
        get => _seriesItemsTitle;
        private set => SetProperty(ref _seriesItemsTitle, value);
    }

    public string NoSeriesSelectedText
    {
        get => _noSeriesSelectedText;
        private set => SetProperty(ref _noSeriesSelectedText, value);
    }

    public string PlanSeriesColumn
    {
        get => _planSeriesColumn;
        private set => SetProperty(ref _planSeriesColumn, value);
    }

    public string PlanItemColumn
    {
        get => _planItemColumn;
        private set => SetProperty(ref _planItemColumn, value);
    }

    public string PlanBriefColumn
    {
        get => _planBriefColumn;
        private set => SetProperty(ref _planBriefColumn, value);
    }

    public string PlanStatusColumn
    {
        get => _planStatusColumn;
        private set => SetProperty(ref _planStatusColumn, value);
    }

    public string NoPlanRowsText
    {
        get => _noPlanRowsText;
        private set => SetProperty(ref _noPlanRowsText, value);
    }

    public string NoItemsInSeriesText
    {
        get => _noItemsInSeriesText;
        private set => SetProperty(ref _noItemsInSeriesText, value);
    }

    public string PromptEditorTitle
    {
        get => _promptEditorTitle;
        private set => SetProperty(ref _promptEditorTitle, value);
    }

    public string SelectedItemTitle
    {
        get => _selectedItemTitle;
        private set => SetProperty(ref _selectedItemTitle, value);
    }

    public string PromptTextLabel
    {
        get => _promptTextLabel;
        private set => SetProperty(ref _promptTextLabel, value);
    }

    public string DefaultGenerationSettingsText
    {
        get => _defaultGenerationSettingsText;
        private set => SetProperty(ref _defaultGenerationSettingsText, value);
    }

    public string CreatePromptVersionText
    {
        get => _createPromptVersionText;
        private set => SetProperty(ref _createPromptVersionText, value);
    }

    public string PromptHistoryTitle
    {
        get => _promptHistoryTitle;
        private set => SetProperty(ref _promptHistoryTitle, value);
    }

    public string PromptVersionColumn
    {
        get => _promptVersionColumn;
        private set => SetProperty(ref _promptVersionColumn, value);
    }

    public string PromptItemColumn
    {
        get => _promptItemColumn;
        private set => SetProperty(ref _promptItemColumn, value);
    }

    public string PromptTextColumn
    {
        get => _promptTextColumn;
        private set => SetProperty(ref _promptTextColumn, value);
    }

    public string PromptSettingsColumn
    {
        get => _promptSettingsColumn;
        private set => SetProperty(ref _promptSettingsColumn, value);
    }

    public string PromptCreatedColumn
    {
        get => _promptCreatedColumn;
        private set => SetProperty(ref _promptCreatedColumn, value);
    }

    public string NoPromptRowsText
    {
        get => _noPromptRowsText;
        private set => SetProperty(ref _noPromptRowsText, value);
    }

    public string NoItemSelectedForPromptText
    {
        get => _noItemSelectedForPromptText;
        private set => SetProperty(ref _noItemSelectedForPromptText, value);
    }

    public string SelectedSeriesItemTitleText
    {
        get => _selectedSeriesItemTitleText;
        private set => SetProperty(ref _selectedSeriesItemTitleText, value);
    }

    public string NewSeriesTitle
    {
        get => _newSeriesTitle;
        set
        {
            if (SetProperty(ref _newSeriesTitle, value))
            {
                CreateSeriesCommand.NotifyCanExecuteChanged();
            }
        }
    }

    public string NewSeriesDescription
    {
        get => _newSeriesDescription;
        set => SetProperty(ref _newSeriesDescription, value);
    }

    public string NewItemTitle
    {
        get => _newItemTitle;
        set
        {
            if (SetProperty(ref _newItemTitle, value))
            {
                AddItemCommand.NotifyCanExecuteChanged();
            }
        }
    }

    public string NewItemBrief
    {
        get => _newItemBrief;
        set => SetProperty(ref _newItemBrief, value);
    }

    public string NewPromptText
    {
        get => _newPromptText;
        set
        {
            if (SetProperty(ref _newPromptText, value))
            {
                CreatePromptVersionCommand.NotifyCanExecuteChanged();
            }
        }
    }

    public IReadOnlyList<SeriesSummaryViewModel> Series
    {
        get => _series;
        private set => SetProperty(ref _series, value);
    }

    public SeriesSummaryViewModel? SelectedSeries
    {
        get => _selectedSeries;
        set
        {
            if (!SetProperty(ref _selectedSeries, value))
            {
                return;
            }

            SeriesItems = value?.Items ?? [];
            SelectedSeriesItem = SeriesItems.FirstOrDefault();
            AddItemCommand.NotifyCanExecuteChanged();
        }
    }

    public IReadOnlyList<SeriesItemViewModel> SeriesItems
    {
        get => _seriesItems;
        private set => SetProperty(ref _seriesItems, value);
    }

    public SeriesItemViewModel? SelectedSeriesItem
    {
        get => _selectedSeriesItem;
        set
        {
            if (!SetProperty(ref _selectedSeriesItem, value))
            {
                return;
            }

            PromptVersions = value?.PromptVersions ?? [];
            SelectedSeriesItemTitleText = value?.Title ?? NoItemSelectedForPromptText;
            CreatePromptVersionCommand.NotifyCanExecuteChanged();
        }
    }

    public IReadOnlyList<PlanRowViewModel> PlanRows
    {
        get => _planRows;
        private set
        {
            if (SetProperty(ref _planRows, value))
            {
                OnPropertyChanged(nameof(HasPlanRows));
            }
        }
    }

    public bool HasPlanRows => PlanRows.Count > 0;

    public IReadOnlyList<PromptVersionViewModel> PromptVersions
    {
        get => _promptVersions;
        private set => SetProperty(ref _promptVersions, value);
    }

    public IReadOnlyList<PromptRowViewModel> PromptRows
    {
        get => _promptRows;
        private set
        {
            if (SetProperty(ref _promptRows, value))
            {
                OnPropertyChanged(nameof(HasPromptRows));
                RunFakeGenerationCommand.NotifyCanExecuteChanged();
            }
        }
    }

    public bool HasPromptRows => PromptRows.Count > 0;

    public IReadOnlyList<QueueRowViewModel> QueueRows
    {
        get => _queueRows;
        private set
        {
            if (SetProperty(ref _queueRows, value))
            {
                OnPropertyChanged(nameof(HasQueueRows));
            }
        }
    }

    public bool HasQueueRows => QueueRows.Count > 0;

    public IReadOnlyList<GalleryRowViewModel> GalleryRows
    {
        get => _galleryRows;
        private set
        {
            if (SetProperty(ref _galleryRows, value))
            {
                OnPropertyChanged(nameof(HasGalleryRows));
            }
        }
    }

    public bool HasGalleryRows => GalleryRows.Count > 0;

    public LanguageOptionViewModel? SelectedLanguageOption
    {
        get => _selectedLanguageOption;
        set
        {
            if (value is null || !SetProperty(ref _selectedLanguageOption, value))
            {
                return;
            }

            _localizationService.SetLanguage(value.Preference);
            RefreshLocalizedText();
        }
    }

    private void RefreshLocalizedText()
    {
        var previousPreference = SelectedLanguageOption?.Preference ?? _localizationService.Preference;

        AppTitle = Text(LocalizationKey.AppTitle);
        ProviderMode = Text(LocalizationKey.ProviderModeFake);
        WorkspaceHeader = Text(LocalizationKey.Workspace);
        InspectorTitle = Text(LocalizationKey.Inspector);
        ActivityTitle = Text(LocalizationKey.Activity);
        InspectorSummary = Text(LocalizationKey.NoItemSelected);
        LanguageLabel = Text(LocalizationKey.LanguageLabel);
        ProjectNameLabel = Text(LocalizationKey.ProjectName);
        NewProjectNamePlaceholder = Text(LocalizationKey.NewProjectNamePlaceholder);
        CreateProjectText = Text(LocalizationKey.CreateProject);
        AvailableProjectsTitle = Text(LocalizationKey.AvailableProjects);
        CurrentProjectTitle = Text(LocalizationKey.CurrentProject);
        FakePlanningTitle = Text(LocalizationKey.FakePlanningTitle);
        PlanningGoalLabel = Text(LocalizationKey.PlanningGoal);
        PlanningAudienceLabel = Text(LocalizationKey.PlanningAudience);
        PlanningItemCountLabel = Text(LocalizationKey.PlanningItemCount);
        PlanningStyleBriefLabel = Text(LocalizationKey.PlanningStyleBrief);
        RunFakePlanningText = Text(LocalizationKey.RunFakePlanning);
        RunFakeGenerationText = Text(LocalizationKey.RunFakeGeneration);
        QueueItemColumn = Text(LocalizationKey.QueueItemColumn);
        QueueStatusColumn = Text(LocalizationKey.QueueStatusColumn);
        QueueAttemptsColumn = Text(LocalizationKey.QueueAttemptsColumn);
        QueueOutputColumn = Text(LocalizationKey.QueueOutputColumn);
        QueueErrorColumn = Text(LocalizationKey.QueueErrorColumn);
        NoQueueRowsText = Text(LocalizationKey.NoQueueRows);
        GalleryItemColumn = Text(LocalizationKey.GalleryItemColumn);
        GalleryImageColumn = Text(LocalizationKey.GalleryImageColumn);
        GalleryMetadataColumn = Text(LocalizationKey.GalleryMetadataColumn);
        NoGalleryRowsText = Text(LocalizationKey.NoGalleryRows);
        PlanEditorTitle = Text(LocalizationKey.PlanEditor);
        SeriesTitleLabel = Text(LocalizationKey.SeriesTitle);
        SeriesDescriptionLabel = Text(LocalizationKey.SeriesDescription);
        CreateSeriesText = Text(LocalizationKey.CreateSeries);
        AvailableSeriesTitle = Text(LocalizationKey.AvailableSeries);
        ItemTitleLabel = Text(LocalizationKey.ItemTitle);
        ItemBriefLabel = Text(LocalizationKey.ItemBrief);
        AddItemText = Text(LocalizationKey.AddItem);
        SeriesItemsTitle = Text(LocalizationKey.SeriesItems);
        NoSeriesSelectedText = Text(LocalizationKey.NoSeriesSelected);
        PlanSeriesColumn = Text(LocalizationKey.PlanSeriesColumn);
        PlanItemColumn = Text(LocalizationKey.PlanItemColumn);
        PlanBriefColumn = Text(LocalizationKey.PlanBriefColumn);
        PlanStatusColumn = Text(LocalizationKey.PlanStatusColumn);
        NoPlanRowsText = Text(LocalizationKey.NoPlanRows);
        NoItemsInSeriesText = Text(LocalizationKey.NoItemsInSeries);
        PromptEditorTitle = Text(LocalizationKey.PromptEditor);
        SelectedItemTitle = Text(LocalizationKey.SelectedItem);
        PromptTextLabel = Text(LocalizationKey.PromptText);
        DefaultGenerationSettingsText = Text(LocalizationKey.DefaultGenerationSettings);
        CreatePromptVersionText = Text(LocalizationKey.CreatePromptVersion);
        PromptHistoryTitle = Text(LocalizationKey.PromptHistory);
        PromptVersionColumn = Text(LocalizationKey.PromptVersionColumn);
        PromptItemColumn = Text(LocalizationKey.PromptItemColumn);
        PromptTextColumn = Text(LocalizationKey.PromptTextColumn);
        PromptSettingsColumn = Text(LocalizationKey.PromptSettingsColumn);
        PromptCreatedColumn = Text(LocalizationKey.PromptCreatedColumn);
        NoPromptRowsText = Text(LocalizationKey.NoPromptRows);
        NoItemSelectedForPromptText = Text(LocalizationKey.NoItemSelectedForPrompt);
        CurrentProjectSummary = SelectedProject is null
            ? Text(LocalizationKey.NoProjectLoaded)
            : $"{SelectedProject.Name} ({SelectedProject.UpdatedAt.LocalDateTime:g})";
        if (string.IsNullOrWhiteSpace(NewPlanningAudience))
        {
            NewPlanningAudience = Text(LocalizationKey.DefaultPlanningAudience);
        }

        if (string.IsNullOrWhiteSpace(NewPlanningStyleBrief))
        {
            NewPlanningStyleBrief = Text(LocalizationKey.DefaultPlanningStyleBrief);
        }

        NavigationItems =
        [
            Text(LocalizationKey.Workspaces),
            Text(LocalizationKey.Projects),
            Text(LocalizationKey.Settings),
        ];
        WorkbenchTabs =
        [
            new(WorkbenchTabKind.Brief, Text(LocalizationKey.Brief), Text(LocalizationKey.BriefEmptyState)),
            new(WorkbenchTabKind.Plan, Text(LocalizationKey.Plan), Text(LocalizationKey.PlanEmptyState)),
            new(WorkbenchTabKind.Prompts, Text(LocalizationKey.Prompts), Text(LocalizationKey.PromptsEmptyState)),
            new(WorkbenchTabKind.Queue, Text(LocalizationKey.Queue), Text(LocalizationKey.QueueEmptyState)),
            new(WorkbenchTabKind.Gallery, Text(LocalizationKey.Gallery), Text(LocalizationKey.GalleryEmptyState)),
            new(WorkbenchTabKind.Review, Text(LocalizationKey.Review), Text(LocalizationKey.ReviewEmptyState)),
            new(WorkbenchTabKind.Delivery, Text(LocalizationKey.Delivery), Text(LocalizationKey.DeliveryEmptyState)),
        ];
        ActivityItems =
        [
            Text(LocalizationKey.GenericHostStarted),
            Text(LocalizationKey.FakeProvidersRegistered),
            Text(LocalizationKey.NoRealApiCalls),
        ];
        LanguageOptions =
        [
            new(LanguagePreference.System, Text(LocalizationKey.LanguageSystem)),
            new(LanguagePreference.Chinese, Text(LocalizationKey.LanguageChinese)),
            new(LanguagePreference.English, Text(LocalizationKey.LanguageEnglish)),
        ];

        _selectedLanguageOption = LanguageOptions.First(option => option.Preference == previousPreference);
        OnPropertyChanged(nameof(SelectedLanguageOption));
        SelectedSeriesItemTitleText = SelectedSeriesItem?.Title ?? NoItemSelectedForPromptText;
        RebuildPlanRows();
        RebuildPromptRows();
    }

    [RelayCommand(CanExecute = nameof(CanCreateProject))]
    private async Task CreateProjectAsync()
    {
        var project = await _projectService.CreateProjectAsync(
            NewProjectName.Trim(),
            DateTimeOffset.UtcNow,
            CancellationToken.None);

        await RefreshProjectsAsync(project.Id);
    }

    private bool CanCreateProject()
    {
        return !string.IsNullOrWhiteSpace(NewProjectName);
    }

    private async Task RefreshProjectsAsync(Guid? selectedProjectId = null)
    {
        var projectSummaries = await _projectService.ListProjectsAsync(CancellationToken.None);
        Projects = projectSummaries
            .Select(project => new ProjectSummaryViewModel(project.Id, project.Name, project.UpdatedAt))
            .ToArray();

        SelectedProject = selectedProjectId is null
            ? Projects.FirstOrDefault()
            : Projects.FirstOrDefault(project => project.Id == selectedProjectId);
    }

    [RelayCommand(CanExecute = nameof(CanRunFakePlanning))]
    private async Task RunFakePlanningAsync()
    {
        if (SelectedProject is null || !TryGetPlanningItemCount(out var itemCount))
        {
            return;
        }

        var series = await _projectService.CreatePlanWithProviderAsync(
            SelectedProject.Id,
            new PlanningRequest(
                NewPlanningGoal.Trim(),
                NewPlanningAudience.Trim(),
                itemCount,
                NewPlanningStyleBrief.Trim()),
            DateTimeOffset.UtcNow,
            CancellationToken.None);

        await LoadPlanAsync(SelectedProject.Id, series.Id);
    }

    private bool CanRunFakePlanning()
    {
        return SelectedProject is not null
            && !string.IsNullOrWhiteSpace(NewPlanningGoal)
            && !string.IsNullOrWhiteSpace(NewPlanningAudience)
            && TryGetPlanningItemCount(out _);
    }

    private bool TryGetPlanningItemCount(out int itemCount)
    {
        return int.TryParse(NewPlanningItemCount, out itemCount) && itemCount > 0;
    }

    [RelayCommand(CanExecute = nameof(CanRunFakeGeneration))]
    private async Task RunFakeGenerationAsync()
    {
        if (SelectedProject is null)
        {
            return;
        }

        var outputDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "ImageSeriesStudio",
            "generated",
            SelectedProject.Id.ToString("N"));
        var run = await _projectService.RunGenerationQueueAsync(
            SelectedProject.Id,
            outputDirectory,
            CancellationToken.None);

        QueueRows = BuildQueueRows(run);
        GalleryRows = BuildGalleryRows(run);
    }

    private bool CanRunFakeGeneration()
    {
        return SelectedProject is not null && PromptRows.Count > 0;
    }

    private IReadOnlyList<QueueRowViewModel> BuildQueueRows(GenerationQueueRun run)
    {
        var itemTitles = Series
            .SelectMany(series => series.Items)
            .ToDictionary(item => item.Id, item => item.Title);
        var imageIndex = 0;
        var images = run.Images.ToArray();

        return run.Tasks.Select(task =>
        {
            var outputPath = string.Empty;
            if (task.Status is GenerationTaskStatus.Succeeded && imageIndex < images.Length)
            {
                outputPath = images[imageIndex].AssetPath;
                imageIndex++;
            }

            return new QueueRowViewModel(
                itemTitles.GetValueOrDefault(task.SeriesItemId, task.SeriesItemId.ToString("N")),
                task.Status.ToString(),
                task.AttemptCount.ToString(),
                outputPath,
                task.ErrorMessage ?? string.Empty);
        }).ToArray();
    }

    private IReadOnlyList<GalleryRowViewModel> BuildGalleryRows(GenerationQueueRun run)
    {
        var itemTitles = Series
            .SelectMany(series => series.Items)
            .ToDictionary(item => item.Id, item => item.Title);
        var succeededTasks = run.Tasks
            .Where(task => task.Status is GenerationTaskStatus.Succeeded)
            .ToArray();

        return run.Images.Select((image, index) =>
        {
            var task = index < succeededTasks.Length ? succeededTasks[index] : null;
            var itemTitle = task is null
                ? image.CandidateImageId.ToString("N")
                : itemTitles.GetValueOrDefault(task.SeriesItemId, task.SeriesItemId.ToString("N"));

            return new GalleryRowViewModel(
                itemTitle,
                image.AssetPath,
                image.MetadataPath);
        }).ToArray();
    }

    [RelayCommand(CanExecute = nameof(CanCreateSeries))]
    private async Task CreateSeriesAsync()
    {
        if (SelectedProject is null)
        {
            return;
        }

        await _projectService.AddSeriesAsync(
            SelectedProject.Id,
            NewSeriesTitle.Trim(),
            NewSeriesDescription.Trim(),
            DateTimeOffset.UtcNow,
            CancellationToken.None);

        NewSeriesTitle = string.Empty;
        NewSeriesDescription = string.Empty;
        await LoadPlanAsync(SelectedProject.Id);
    }

    private bool CanCreateSeries()
    {
        return SelectedProject is not null && !string.IsNullOrWhiteSpace(NewSeriesTitle);
    }

    [RelayCommand(CanExecute = nameof(CanAddItem))]
    private async Task AddItemAsync()
    {
        if (SelectedProject is null || SelectedSeries is null)
        {
            return;
        }

        var item = await _projectService.AddItemAsync(
            SelectedProject.Id,
            SelectedSeries.Id,
            NewItemTitle.Trim(),
            NewItemBrief.Trim(),
            DateTimeOffset.UtcNow,
            CancellationToken.None);

        NewItemTitle = string.Empty;
        NewItemBrief = string.Empty;
        await LoadPlanAsync(SelectedProject.Id, SelectedSeries.Id, item.Id);
    }

    private bool CanAddItem()
    {
        return SelectedProject is not null
            && SelectedSeries is not null
            && !string.IsNullOrWhiteSpace(NewItemTitle);
    }

    [RelayCommand(CanExecute = nameof(CanCreatePromptVersion))]
    private async Task CreatePromptVersionAsync()
    {
        if (SelectedProject is null || SelectedSeries is null || SelectedSeriesItem is null)
        {
            return;
        }

        await _projectService.AddPromptVersionAsync(
            SelectedProject.Id,
            SelectedSeriesItem.Id,
            NewPromptText.Trim(),
            CreateDefaultGenerationSettings(),
            providerProfileId: null,
            DateTimeOffset.UtcNow,
            CancellationToken.None);

        NewPromptText = string.Empty;
        await LoadPlanAsync(SelectedProject.Id, SelectedSeries.Id, SelectedSeriesItem.Id);
    }

    private bool CanCreatePromptVersion()
    {
        return SelectedProject is not null
            && SelectedSeriesItem is not null
            && !string.IsNullOrWhiteSpace(NewPromptText);
    }

    private static GenerationSettings CreateDefaultGenerationSettings()
    {
        return new GenerationSettings(1024, 1024, "standard", "png");
    }

    private async Task LoadPlanAsync(Guid projectId, Guid? selectedSeriesId = null, Guid? selectedItemId = null)
    {
        var project = await _projectService.LoadProjectAsync(projectId, CancellationToken.None);
        if (project is null)
        {
            await ClearPlanAsync();
            return;
        }

        Series = project.Series
            .Select(series => new SeriesSummaryViewModel(
                series.Id,
                series.Title,
                series.Items
                    .Select(item => new SeriesItemViewModel(
                        item.Id,
                        item.Title,
                        item.Brief,
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
        RebuildPlanRows();
        RebuildPromptRows();

        SelectedSeries = selectedSeriesId is null
            ? Series.FirstOrDefault()
            : Series.FirstOrDefault(series => series.Id == selectedSeriesId);
        if (selectedItemId is not null)
        {
            SelectedSeriesItem = SelectedSeries?.Items.FirstOrDefault(item => item.Id == selectedItemId) ?? SelectedSeriesItem;
        }

        CreateSeriesCommand.NotifyCanExecuteChanged();
    }

    private Task ClearPlanAsync()
    {
        Series = [];
        SelectedSeries = null;
        SeriesItems = [];
        SelectedSeriesItem = null;
        QueueRows = [];
        GalleryRows = [];
        RebuildPlanRows();
        RebuildPromptRows();
        CreateSeriesCommand.NotifyCanExecuteChanged();
        AddItemCommand.NotifyCanExecuteChanged();
        CreatePromptVersionCommand.NotifyCanExecuteChanged();
        return Task.CompletedTask;
    }

    private void RebuildPlanRows()
    {
        PlanRows = Series
            .SelectMany(series => series.Items.Count == 0
                ? new[] { new PlanRowViewModel(series.Title, NoItemsInSeriesText, string.Empty, string.Empty) }
                : series.Items.Select(item => new PlanRowViewModel(
                    series.Title,
                    item.Title,
                    item.Brief,
                    _localizationService.GetSeriesItemStatusText(item.Status))))
            .ToArray();
    }

    private void RebuildPromptRows()
    {
        PromptRows = Series
            .SelectMany(series => series.Items.SelectMany(item => item.PromptVersions.Select(prompt => new PromptRowViewModel(
                item.Title,
                $"v{prompt.VersionNumber}",
                prompt.PromptText,
                prompt.SettingsSummary,
                prompt.CreatedAt.LocalDateTime.ToString("g")))))
            .ToArray();
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

public sealed record WorkbenchTabViewModel(WorkbenchTabKind Kind, string Title, string EmptyState)
{
    public bool IsPlan => Kind is WorkbenchTabKind.Plan;

    public bool IsPrompts => Kind is WorkbenchTabKind.Prompts;

    public bool IsQueue => Kind is WorkbenchTabKind.Queue;

    public bool IsGallery => Kind is WorkbenchTabKind.Gallery;
}

public enum WorkbenchTabKind
{
    Brief = 0,
    Plan = 1,
    Prompts = 2,
    Queue = 3,
    Gallery = 4,
    Review = 5,
    Delivery = 6,
}

public sealed record LanguageOptionViewModel(LanguagePreference Preference, string DisplayName);

public sealed record ProjectSummaryViewModel(Guid Id, string Name, DateTimeOffset UpdatedAt);

public sealed record SeriesSummaryViewModel(Guid Id, string Title, IReadOnlyList<SeriesItemViewModel> Items);

public sealed record SeriesItemViewModel(
    Guid Id,
    string Title,
    string Brief,
    SeriesItemStatus Status,
    IReadOnlyList<PromptVersionViewModel> PromptVersions);

public sealed record PlanRowViewModel(string SeriesTitle, string ItemTitle, string Brief, string StatusText);

public sealed record PromptVersionViewModel(
    Guid Id,
    int VersionNumber,
    string PromptText,
    string SettingsSummary,
    DateTimeOffset CreatedAt);

public sealed record PromptRowViewModel(
    string ItemTitle,
    string Version,
    string PromptText,
    string SettingsSummary,
    string CreatedAt);

public sealed record QueueRowViewModel(
    string ItemTitle,
    string Status,
    string Attempts,
    string OutputPath,
    string ErrorMessage);

public sealed record GalleryRowViewModel(
    string ItemTitle,
    string AssetPath,
    string MetadataPath);
