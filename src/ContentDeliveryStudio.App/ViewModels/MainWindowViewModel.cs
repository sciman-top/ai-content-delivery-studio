using System.IO;
using System.ComponentModel;
using System.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ContentDeliveryStudio.Application.Localization;
using ContentDeliveryStudio.Application.Projects;
using ContentDeliveryStudio.Application.Sources;
using ContentDeliveryStudio.Core.Documents;
using ContentDeliveryStudio.Core.Projects;
using ContentDeliveryStudio.Core.Providers;
using ContentDeliveryStudio.Core.Sources;
using ContentDeliveryStudio.Core.Styles;
using ContentDeliveryStudio.App.Services;

namespace ContentDeliveryStudio.App.ViewModels;

public sealed partial class MainWindowViewModel : ObservableObject
{
    private readonly LocalizationService _localizationService;
    private readonly ProjectApplicationService _projectService;
    private readonly ProjectWorkspaceCoordinator _projectWorkspaceCoordinator;
    private readonly PlanningWorkflowCoordinator _planningWorkflowCoordinator;
    private readonly BriefWorkflowCoordinator _briefWorkflowCoordinator;
    private readonly GenerationWorkflowCoordinator _generationWorkflowCoordinator;
    private readonly ReviewWorkflowCoordinator _reviewWorkflowCoordinator;
    private readonly DeliveryWorkflowCoordinator _deliveryWorkflowCoordinator;
    private readonly WorkflowGraphCoordinator _workflowGraphCoordinator;
    private readonly ProjectWorkbenchProjectionCoordinator _projectWorkbenchProjectionCoordinator;
    private readonly ProjectWorkbenchStateCoordinator _projectWorkbenchStateCoordinator;
    private readonly MainWindowLocalizationCoordinator _mainWindowLocalizationCoordinator;
    private readonly GalleryThumbnailWarmupService _galleryThumbnailWarmupService;
    private readonly IDocumentSourceFilePickerService? _documentSourceFilePickerService;
    private readonly MainWindowOperationGate _operationGate;
    private bool _isMutatingOperationActive;
    private bool _suppressSelectedProjectLoad;

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
    private string _projectNameLabel = string.Empty;
    private string _newProjectNamePlaceholder = string.Empty;
    private string _createProjectText = string.Empty;
    private string _availableProjectsTitle = string.Empty;
    private string _currentProjectTitle = string.Empty;
    private string _currentProjectSummary = string.Empty;
    private string _documentIllustrationTitle = string.Empty;
    private string _documentSourceFilePathLabel = string.Empty;
    private string _documentSourceTextLabel = string.Empty;
    private string _documentAudienceLabel = string.Empty;
    private string _documentStrictnessLabel = string.Empty;
    private string _browseDocumentSourceFileText = string.Empty;
    private string _importDocumentSourceFileText = string.Empty;
    private string _runFakeDocumentPlanningText = string.Empty;
    private string _documentPlanningResultText = string.Empty;
    private string _documentPlanningResultSummary = string.Empty;
    private string _newDocumentSourceFilePath = string.Empty;
    private string _newDocumentSourceText = string.Empty;
    private string _newDocumentAudience = string.Empty;
    private string _importedDocumentSourcePath = string.Empty;
    private string _defaultDocumentSourceText = string.Empty;
    private string _defaultDocumentAudience = string.Empty;
    private IReadOnlyList<DocumentStrictnessOptionViewModel> _documentStrictnessOptions = [];
    private DocumentStrictnessOptionViewModel? _selectedDocumentStrictnessOption;
    private string _graphNodeColumn = string.Empty;
    private string _graphSummaryColumn = string.Empty;
    private string _graphLinksColumn = string.Empty;
    private string _noGraphRowsText = string.Empty;
    private string _imageEditResultText = string.Empty;
    private IReadOnlyList<WorkflowGraphRowViewModel> _workflowGraphRows = [];
    private Guid? _activeCreativeBriefId;

    public MainWindowViewModel(
        LocalizationService localizationService,
        ProjectApplicationService projectService,
        ProviderCenterViewModel providerCenter,
        GalleryThumbnailWarmupService galleryThumbnailWarmupService,
        IDocumentSourceFilePickerService? documentSourceFilePickerService = null,
        IFinalDeliveryRootPickerService? finalDeliveryRootPickerService = null,
        IScientificDeliveryPackageSaveService? scientificDeliveryPackageSaveService = null,
        DiagnosticsPanelViewModel? diagnostics = null,
        BackupRestorePanelViewModel? backupRestore = null,
        IImageEditProvider? imageEditProvider = null)
    {
        _localizationService = localizationService;
        _projectService = projectService;
        _galleryThumbnailWarmupService = galleryThumbnailWarmupService;
        _documentSourceFilePickerService = documentSourceFilePickerService;
        _projectWorkspaceCoordinator = new ProjectWorkspaceCoordinator(projectService);
        _planningWorkflowCoordinator = new PlanningWorkflowCoordinator(projectService, localizationService);
        _briefWorkflowCoordinator = new BriefWorkflowCoordinator(projectService);
        _generationWorkflowCoordinator = new GenerationWorkflowCoordinator(projectService);
        _reviewWorkflowCoordinator = new ReviewWorkflowCoordinator(projectService, localizationService);
        _deliveryWorkflowCoordinator = new DeliveryWorkflowCoordinator(projectService);
        _workflowGraphCoordinator = new WorkflowGraphCoordinator(localizationService);
        _projectWorkbenchProjectionCoordinator = new ProjectWorkbenchProjectionCoordinator(localizationService, projectService);
        _projectWorkbenchStateCoordinator = new ProjectWorkbenchStateCoordinator(
            localizationService,
            projectService,
            _projectWorkbenchProjectionCoordinator);
        _mainWindowLocalizationCoordinator = new MainWindowLocalizationCoordinator(
            localizationService,
            scientificDeliveryPackageSaveService is null
                ? null
                : bytes => scientificDeliveryPackageSaveService.SavePackage(bytes));
        var generationSettingsWorkspace = new ImageSeriesGenerationSettingsWorkspaceViewModel(
            BuildStyleRecipeSummary);
        var planningWorkspace = new ImageSeriesPlanningWorkspaceViewModel(
            RunImageSeriesCreateSeriesAsync,
            RunImageSeriesAddItemAsync,
            RunImageSeriesCreatePromptVersionAsync,
            CanRunMutation,
            () => SelectedProject is not null,
            item => item?.Title ?? Text(LocalizationKey.NoItemSelectedForPrompt),
            OnImageSeriesSelectedSeriesChanged,
            OnImageSeriesSelectedItemChanged,
            OnImageSeriesPromptProjectionChanged);
        var briefWorkspace = new ImageSeriesBriefWorkspaceViewModel(
            RunImageSeriesFakePlanningAsync,
            RunImageSeriesCreateBriefAsync,
            RunImageSeriesGenerateDesignBlueprintsAsync,
            RunImageSeriesGeneratePromptDirectionsAsync,
            RunImageSeriesPromoteDesignBlueprintAsync,
            RunImageSeriesPromotePromptDirectionAsync,
            CanRunMutation,
            () => SelectedProject is not null,
            () => planningWorkspace.SelectedSeries,
            () => planningWorkspace.SelectedSeriesItem);
        var galleryWorkspace = new ImageSeriesGalleryWorkspaceViewModel(
            RunImageSeriesGalleryEditAsync,
            CanRunMutation,
            () => SelectedProject is not null,
            QueueGalleryWarmup,
            ApplyImageSeriesGalleryEditResult,
            row => row is null
                ? Text(LocalizationKey.NoCandidateSelectedForEdit)
                : $"{row.ItemTitle} ({row.CandidateImageId:N})",
            OnImageSeriesGalleryProjectionChanged);
        galleryWorkspace.ApplyProviderCapability(imageEditProvider?.Capabilities);
        var reviewWorkspace = new ImageSeriesReviewWorkspaceViewModel(
            RunImageSeriesReviewAsync,
            RunImageSeriesFinalApprovalAsync,
            CanRunMutation,
            () => SelectedProject is not null,
            () => galleryWorkspace.GalleryRows,
            OnImageSeriesReviewMutated,
            OnImageSeriesReviewProjectionChanged);
        var deliveryWorkspace = new ImageSeriesDeliveryWorkspaceViewModel(
            RunImageSeriesDeliveryAsync,
            finalDeliveryRootPickerService,
            CanRunMutation,
            () => SelectedProject is not null,
            () => galleryWorkspace.GalleryRows,
            () => reviewWorkspace.ReviewRows,
            BuildFinalDeliveryCategoryOptions,
            OnImageSeriesDeliveryProjectionChanged);
        ImageSeriesWorkspace = new ImageSeriesWorkspaceViewModel(
            _generationWorkflowCoordinator,
            RunImageSeriesQueueMutationAndReloadAsync,
            RunImageSeriesFakeGenerationAsync,
            CanRunMutation,
            () => SelectedProject is not null,
            () => false,
            () => RunFakeGenerationCommand.NotifyCanExecuteChanged(),
            planningWorkspace,
            briefWorkspace,
            generationSettingsWorkspace,
            galleryWorkspace,
            reviewWorkspace,
            deliveryWorkspace);
        ImageSeriesWorkspace.PropertyChanged += OnImageSeriesWorkspacePropertyChanged;
        ImageSeriesPlanningWorkspace.PropertyChanged += OnImageSeriesWorkspacePropertyChanged;
        ImageSeriesBriefWorkspace.PropertyChanged += OnImageSeriesWorkspacePropertyChanged;
        ImageSeriesGenerationSettingsWorkspace.PropertyChanged += OnImageSeriesWorkspacePropertyChanged;
        ImageSeriesGalleryWorkspace.PropertyChanged += OnImageSeriesWorkspacePropertyChanged;
        ImageSeriesReviewWorkspace.PropertyChanged += OnImageSeriesWorkspacePropertyChanged;
        ImageSeriesDeliveryWorkspace.PropertyChanged += OnImageSeriesWorkspacePropertyChanged;
        _operationGate = new MainWindowOperationGate(SetExclusiveBusyState);
        ProviderCenter = providerCenter;
        Diagnostics = diagnostics;
        BackupRestore = backupRestore;
        RefreshLocalizedText();
        SelectedLanguageOption = LanguageOptions.First(option => option.Preference == _localizationService.Preference);
        NewProjectName = NewProjectNamePlaceholder;
        NewPlanningAudience = Text(LocalizationKey.DefaultPlanningAudience);
        NewPlanningStyleBrief = Text(LocalizationKey.DefaultPlanningStyleBrief);
        ScheduleStartupRefresh();
    }

    public ProviderCenterViewModel ProviderCenter { get; }

    public ImageSeriesWorkspaceViewModel ImageSeriesWorkspace { get; }

    public ImageSeriesGalleryWorkspaceViewModel ImageSeriesGalleryWorkspace => ImageSeriesWorkspace.Gallery;

    public ImageSeriesPlanningWorkspaceViewModel ImageSeriesPlanningWorkspace => ImageSeriesWorkspace.Planning;

    public ImageSeriesBriefWorkspaceViewModel ImageSeriesBriefWorkspace => ImageSeriesWorkspace.Brief;

    public ImageSeriesGenerationSettingsWorkspaceViewModel ImageSeriesGenerationSettingsWorkspace =>
        ImageSeriesWorkspace.GenerationSettings;

    public ImageSeriesReviewWorkspaceViewModel ImageSeriesReviewWorkspace => ImageSeriesWorkspace.Review;

    public ImageSeriesDeliveryWorkspaceViewModel ImageSeriesDeliveryWorkspace => ImageSeriesWorkspace.Delivery;

    public DiagnosticsPanelViewModel? Diagnostics { get; }

    public BackupRestorePanelViewModel? BackupRestore { get; }

    private void OnImageSeriesWorkspacePropertyChanged(object? sender, PropertyChangedEventArgs eventArgs)
    {
        if (!string.IsNullOrWhiteSpace(eventArgs.PropertyName))
        {
            OnPropertyChanged(eventArgs.PropertyName);
        }
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

            CurrentProjectSummary = BuildCurrentProjectSummary(value);
            if (value is not null && string.IsNullOrWhiteSpace(NewPlanningGoal))
            {
                NewPlanningGoal = value.Name;
            }

            ImageSeriesWorkspace.ApplyProjection([]);
            ImageSeriesGalleryWorkspace.ApplyProjection([]);
            ImageSeriesReviewWorkspace.ApplyProjection([]);
            ImageSeriesDeliveryWorkspace.ApplyProjection([]);
            ImageSeriesBriefWorkspace.ApplyProjection([], [], null, null);
            DocumentPlanningResultSummary = string.Empty;
            _activeCreativeBriefId = null;
            RebuildWorkflowGraphRows();
            ImageSeriesBriefWorkspace.NotifyCommandStatesChanged();
            RunFakeDocumentPlanningCommand.NotifyCanExecuteChanged();
            RunFakeGenerationCommand.NotifyCanExecuteChanged();
            QueueSelectedProjectLoad(value);
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
        get => ImageSeriesBriefWorkspace.NewPlanningGoal;
        set => ImageSeriesBriefWorkspace.NewPlanningGoal = value;
    }

    public string NewPlanningAudience
    {
        get => ImageSeriesBriefWorkspace.NewPlanningAudience;
        set => ImageSeriesBriefWorkspace.NewPlanningAudience = value;
    }

    public string NewPlanningItemCount
    {
        get => ImageSeriesBriefWorkspace.NewPlanningItemCount;
        set => ImageSeriesBriefWorkspace.NewPlanningItemCount = value;
    }

    public string NewPlanningStyleBrief
    {
        get => ImageSeriesBriefWorkspace.NewPlanningStyleBrief;
        set => ImageSeriesBriefWorkspace.NewPlanningStyleBrief = value;
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

    public string DocumentIllustrationTitle
    {
        get => _documentIllustrationTitle;
        private set => SetProperty(ref _documentIllustrationTitle, value);
    }

    public string DocumentSourceFilePathLabel
    {
        get => _documentSourceFilePathLabel;
        private set => SetProperty(ref _documentSourceFilePathLabel, value);
    }

    public string DocumentSourceTextLabel
    {
        get => _documentSourceTextLabel;
        private set => SetProperty(ref _documentSourceTextLabel, value);
    }

    public string DocumentAudienceLabel
    {
        get => _documentAudienceLabel;
        private set => SetProperty(ref _documentAudienceLabel, value);
    }

    public string DocumentStrictnessLabel
    {
        get => _documentStrictnessLabel;
        private set => SetProperty(ref _documentStrictnessLabel, value);
    }

    public string ImportDocumentSourceFileText
    {
        get => _importDocumentSourceFileText;
        private set => SetProperty(ref _importDocumentSourceFileText, value);
    }

    public string BrowseDocumentSourceFileText
    {
        get => _browseDocumentSourceFileText;
        private set => SetProperty(ref _browseDocumentSourceFileText, value);
    }

    public string RunFakeDocumentPlanningText
    {
        get => _runFakeDocumentPlanningText;
        private set => SetProperty(ref _runFakeDocumentPlanningText, value);
    }

    public string NewDocumentSourceText
    {
        get => _newDocumentSourceText;
        set
        {
            if (SetProperty(ref _newDocumentSourceText, value))
            {
                RunFakeDocumentPlanningCommand.NotifyCanExecuteChanged();
            }
        }
    }

    public string NewDocumentSourceFilePath
    {
        get => _newDocumentSourceFilePath;
        set
        {
            if (SetProperty(ref _newDocumentSourceFilePath, value))
            {
                ImportDocumentSourceFileCommand.NotifyCanExecuteChanged();
            }
        }
    }

    public string ImportedDocumentSourcePath
    {
        get => _importedDocumentSourcePath;
        private set => SetProperty(ref _importedDocumentSourcePath, value);
    }

    public string NewDocumentAudience
    {
        get => _newDocumentAudience;
        set => SetProperty(ref _newDocumentAudience, value);
    }

    public string DocumentPlanningResultText
    {
        get => _documentPlanningResultText;
        private set => SetProperty(ref _documentPlanningResultText, value);
    }

    public string DocumentPlanningResultSummary
    {
        get => _documentPlanningResultSummary;
        private set => SetProperty(ref _documentPlanningResultSummary, value);
    }

    public IReadOnlyList<DocumentStrictnessOptionViewModel> DocumentStrictnessOptions
    {
        get => _documentStrictnessOptions;
        private set => SetProperty(ref _documentStrictnessOptions, value);
    }

    public DocumentStrictnessOptionViewModel? SelectedDocumentStrictnessOption
    {
        get => _selectedDocumentStrictnessOption;
        set => SetProperty(ref _selectedDocumentStrictnessOption, value);
    }

    public string GraphNodeColumn
    {
        get => _graphNodeColumn;
        private set => SetProperty(ref _graphNodeColumn, value);
    }

    public string GraphSummaryColumn
    {
        get => _graphSummaryColumn;
        private set => SetProperty(ref _graphSummaryColumn, value);
    }

    public string GraphLinksColumn
    {
        get => _graphLinksColumn;
        private set => SetProperty(ref _graphLinksColumn, value);
    }

    public string NoGraphRowsText
    {
        get => _noGraphRowsText;
        private set => SetProperty(ref _noGraphRowsText, value);
    }

    public string ImageEditResultText
    {
        get => _imageEditResultText;
        private set => SetProperty(ref _imageEditResultText, value);
    }

    public string NewSeriesTitle
    {
        get => ImageSeriesPlanningWorkspace.NewSeriesTitle;
        set => ImageSeriesPlanningWorkspace.NewSeriesTitle = value;
    }

    public string NewSeriesDescription
    {
        get => ImageSeriesPlanningWorkspace.NewSeriesDescription;
        set => ImageSeriesPlanningWorkspace.NewSeriesDescription = value;
    }

    public string NewItemTitle
    {
        get => ImageSeriesPlanningWorkspace.NewItemTitle;
        set => ImageSeriesPlanningWorkspace.NewItemTitle = value;
    }

    public string NewItemBrief
    {
        get => ImageSeriesPlanningWorkspace.NewItemBrief;
        set => ImageSeriesPlanningWorkspace.NewItemBrief = value;
    }

    public string NewPromptText
    {
        get => ImageSeriesPlanningWorkspace.NewPromptText;
        set => ImageSeriesPlanningWorkspace.NewPromptText = value;
    }

    public string NewImageEditPrompt
    {
        get => ImageSeriesGalleryWorkspace.NewImageEditPrompt;
        set => ImageSeriesGalleryWorkspace.NewImageEditPrompt = value;
    }

    public string NewImageEditMaskPath
    {
        get => ImageSeriesGalleryWorkspace.NewImageEditMaskPath;
        set => ImageSeriesGalleryWorkspace.NewImageEditMaskPath = value;
    }

    public string FinalApprovalReviewer
    {
        get => ImageSeriesReviewWorkspace.FinalApprovalReviewer;
        set => ImageSeriesReviewWorkspace.FinalApprovalReviewer = value;
    }

    public string FinalApprovalNotes
    {
        get => ImageSeriesReviewWorkspace.FinalApprovalNotes;
        set => ImageSeriesReviewWorkspace.FinalApprovalNotes = value;
    }

    public IReadOnlyList<FinalImageDeliveryCategoryOptionViewModel> FinalDeliveryCategoryOptions =>
        ImageSeriesDeliveryWorkspace.FinalDeliveryCategoryOptions;

    public FinalImageDeliveryCategoryOptionViewModel? SelectedFinalDeliveryCategoryOption
    {
        get => ImageSeriesDeliveryWorkspace.SelectedFinalDeliveryCategoryOption;
        set => ImageSeriesDeliveryWorkspace.SelectedFinalDeliveryCategoryOption = value;
    }

    public string FinalDeliveryRootPath
    {
        get => ImageSeriesDeliveryWorkspace.FinalDeliveryRootPath;
        set => ImageSeriesDeliveryWorkspace.FinalDeliveryRootPath = value;
    }

    public string FinalDeliveryDestinationPreview => ImageSeriesDeliveryWorkspace.FinalDeliveryDestinationPreview;

    public IReadOnlyList<SeriesSummaryViewModel> Series => ImageSeriesPlanningWorkspace.Series;

    public SeriesSummaryViewModel? SelectedSeries
    {
        get => ImageSeriesPlanningWorkspace.SelectedSeries;
        set => ImageSeriesPlanningWorkspace.SelectedSeries = value;
    }

    public IReadOnlyList<SeriesItemViewModel> SeriesItems => ImageSeriesPlanningWorkspace.SeriesItems;

    public IReadOnlyList<ImageTypePresetOptionViewModel> ImageTypePresetOptions =>
        ImageSeriesGenerationSettingsWorkspace.ImageTypePresetOptions;

    public ImageTypePresetOptionViewModel? SelectedImageTypePresetOption
    {
        get => ImageSeriesGenerationSettingsWorkspace.SelectedImageTypePresetOption;
        set => ImageSeriesGenerationSettingsWorkspace.SelectedImageTypePresetOption = value;
    }

    public IReadOnlyList<StyleGuideOptionViewModel> StyleGuideOptions =>
        ImageSeriesGenerationSettingsWorkspace.StyleGuideOptions;

    public StyleGuideOptionViewModel? SelectedStyleGuideOption
    {
        get => ImageSeriesGenerationSettingsWorkspace.SelectedStyleGuideOption;
        set => ImageSeriesGenerationSettingsWorkspace.SelectedStyleGuideOption = value;
    }

    public IReadOnlyList<GenerationRecipeOptionViewModel> GenerationRecipeOptions =>
        ImageSeriesGenerationSettingsWorkspace.GenerationRecipeOptions;

    public GenerationRecipeOptionViewModel? SelectedGenerationRecipeOption
    {
        get => ImageSeriesGenerationSettingsWorkspace.SelectedGenerationRecipeOption;
        set => ImageSeriesGenerationSettingsWorkspace.SelectedGenerationRecipeOption = value;
    }

    public SeriesItemViewModel? SelectedSeriesItem
    {
        get => ImageSeriesPlanningWorkspace.SelectedSeriesItem;
        set => ImageSeriesPlanningWorkspace.SelectedSeriesItem = value;
    }

    public IReadOnlyList<PlanRowViewModel> PlanRows => ImageSeriesPlanningWorkspace.PlanRows;

    public bool HasPlanRows => ImageSeriesPlanningWorkspace.HasPlanRows;

    public IReadOnlyList<DesignBlueprintRowViewModel> DesignBlueprintRows =>
        ImageSeriesBriefWorkspace.DesignBlueprintRows;

    public bool HasDesignBlueprintRows => ImageSeriesBriefWorkspace.HasDesignBlueprintRows;

    public DesignBlueprintRowViewModel? SelectedDesignBlueprint
    {
        get => ImageSeriesBriefWorkspace.SelectedDesignBlueprint;
        set => ImageSeriesBriefWorkspace.SelectedDesignBlueprint = value;
    }

    public IReadOnlyList<PromptDirectionRowViewModel> PromptDirectionRows =>
        ImageSeriesBriefWorkspace.PromptDirectionRows;

    public bool HasPromptDirectionRows => ImageSeriesBriefWorkspace.HasPromptDirectionRows;

    public PromptDirectionRowViewModel? SelectedPromptDirection
    {
        get => ImageSeriesBriefWorkspace.SelectedPromptDirection;
        set => ImageSeriesBriefWorkspace.SelectedPromptDirection = value;
    }

    public IAsyncRelayCommand RunFakePlanningCommand => ImageSeriesBriefWorkspace.RunFakePlanningCommand;

    public IAsyncRelayCommand CreateBriefCommand => ImageSeriesBriefWorkspace.CreateBriefCommand;

    public IAsyncRelayCommand GenerateDesignBlueprintsCommand =>
        ImageSeriesBriefWorkspace.GenerateDesignBlueprintsCommand;

    public IAsyncRelayCommand PromoteDesignBlueprintCommand =>
        ImageSeriesBriefWorkspace.PromoteDesignBlueprintCommand;

    public IAsyncRelayCommand GeneratePromptDirectionsCommand =>
        ImageSeriesBriefWorkspace.GeneratePromptDirectionsCommand;

    public IAsyncRelayCommand PromotePromptDirectionCommand =>
        ImageSeriesBriefWorkspace.PromotePromptDirectionCommand;

    public IReadOnlyList<PromptVersionViewModel> PromptVersions => ImageSeriesPlanningWorkspace.PromptVersions;

    public IReadOnlyList<PromptRowViewModel> PromptRows => ImageSeriesPlanningWorkspace.PromptRows;

    public bool HasPromptRows => ImageSeriesPlanningWorkspace.HasPromptRows;

    public IAsyncRelayCommand CreateSeriesCommand => ImageSeriesPlanningWorkspace.CreateSeriesCommand;

    public IAsyncRelayCommand AddItemCommand => ImageSeriesPlanningWorkspace.AddItemCommand;

    public IAsyncRelayCommand CreatePromptVersionCommand =>
        ImageSeriesPlanningWorkspace.CreatePromptVersionCommand;

    public IReadOnlyList<QueueRowViewModel> QueueRows => ImageSeriesWorkspace.QueueRows;

    public bool HasQueueRows => ImageSeriesWorkspace.HasQueueRows;

    public QueueRowViewModel? SelectedQueueRow
    {
        get => ImageSeriesWorkspace.SelectedQueueRow;
        set => ImageSeriesWorkspace.SelectedQueueRow = value;
    }

    public IAsyncRelayCommand PrepareGenerationQueueCommand =>
        ImageSeriesWorkspace.PrepareGenerationQueueCommand;

    public IAsyncRelayCommand RunFakeGenerationCommand => ImageSeriesWorkspace.RunFakeGenerationCommand;

    public IAsyncRelayCommand ExecutePreparedGenerationQueueCommand =>
        ImageSeriesWorkspace.ExecutePreparedGenerationQueueCommand;

    public IAsyncRelayCommand PauseSelectedGenerationTaskCommand =>
        ImageSeriesWorkspace.PauseSelectedGenerationTaskCommand;

    public IAsyncRelayCommand ResumeSelectedGenerationTaskCommand =>
        ImageSeriesWorkspace.ResumeSelectedGenerationTaskCommand;

    public IAsyncRelayCommand RetrySelectedGenerationTaskCommand =>
        ImageSeriesWorkspace.RetrySelectedGenerationTaskCommand;

    public IAsyncRelayCommand MoveSelectedGenerationTaskUpCommand =>
        ImageSeriesWorkspace.MoveSelectedGenerationTaskUpCommand;

    public IAsyncRelayCommand MoveSelectedGenerationTaskDownCommand =>
        ImageSeriesWorkspace.MoveSelectedGenerationTaskDownCommand;

    public IReadOnlyList<GalleryRowViewModel> GalleryRows => ImageSeriesGalleryWorkspace.GalleryRows;

    public bool HasGalleryRows => ImageSeriesGalleryWorkspace.HasGalleryRows;

    public GalleryRowViewModel? SelectedGalleryRow
    {
        get => ImageSeriesGalleryWorkspace.SelectedGalleryRow;
        set => ImageSeriesGalleryWorkspace.SelectedGalleryRow = value;
    }

    public IAsyncRelayCommand RunFakeImageEditCommand =>
        ImageSeriesGalleryWorkspace.RunFakeImageEditCommand;

    public IReadOnlyList<ReviewRowViewModel> ReviewRows => ImageSeriesReviewWorkspace.ReviewRows;

    public bool HasReviewRows => ImageSeriesReviewWorkspace.HasReviewRows;

    public ReviewRowViewModel? SelectedReviewRow
    {
        get => ImageSeriesReviewWorkspace.SelectedReviewRow;
        set => ImageSeriesReviewWorkspace.SelectedReviewRow = value;
    }

    public IAsyncRelayCommand RunFakeReviewCommand => ImageSeriesReviewWorkspace.RunFakeReviewCommand;

    public IAsyncRelayCommand ApproveSelectedReviewCommand =>
        ImageSeriesReviewWorkspace.ApproveSelectedReviewCommand;

    public IAsyncRelayCommand RejectSelectedReviewCommand =>
        ImageSeriesReviewWorkspace.RejectSelectedReviewCommand;

    public IReadOnlyList<DeliveryRowViewModel> DeliveryRows => ImageSeriesDeliveryWorkspace.DeliveryRows;

    public bool HasDeliveryRows => ImageSeriesDeliveryWorkspace.HasDeliveryRows;

    public IAsyncRelayCommand BrowseFinalDeliveryRootCommand =>
        ImageSeriesDeliveryWorkspace.BrowseFinalDeliveryRootCommand;

    public IAsyncRelayCommand ExportDeliveryCommand => ImageSeriesDeliveryWorkspace.ExportDeliveryCommand;

    public IReadOnlyList<WorkflowGraphRowViewModel> WorkflowGraphRows
    {
        get => _workflowGraphRows;
        private set
        {
            if (SetProperty(ref _workflowGraphRows, value))
            {
                OnPropertyChanged(nameof(HasWorkflowGraphRows));
            }
        }
    }

    public bool HasWorkflowGraphRows => WorkflowGraphRows.Count > 0;

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
        Diagnostics?.RefreshLocalizedText();
        BackupRestore?.RefreshLocalizedText();
        var previousPreference = SelectedLanguageOption?.Preference ?? _localizationService.Preference;
        var previousDefaultDocumentSourceText = _defaultDocumentSourceText;
        var previousDefaultDocumentAudience = _defaultDocumentAudience;
        var previousDocumentStrictness = SelectedDocumentStrictnessOption?.Value ?? IllustrationStrictnessLevel.Educational;
        var previousPresetId = SelectedImageTypePresetOption?.Id;
        var previousStyleGuideId = SelectedStyleGuideOption?.Id;
        var previousRecipeId = SelectedGenerationRecipeOption?.Id;
        var payload = _mainWindowLocalizationCoordinator.BuildPayload();
        var restoredSelections = _mainWindowLocalizationCoordinator.RestoreSelectionState(
            payload,
            NewDocumentSourceText,
            NewDocumentAudience,
            previousDefaultDocumentSourceText,
            previousDefaultDocumentAudience,
            previousDocumentStrictness,
            previousPresetId,
            previousStyleGuideId,
            previousRecipeId,
            previousPreference);

        AppTitle = payload.AppTitle;
        ProviderMode = payload.ProviderMode;
        WorkspaceHeader = payload.WorkspaceHeader;
        InspectorTitle = payload.InspectorTitle;
        ActivityTitle = payload.ActivityTitle;
        InspectorSummary = payload.InspectorSummary;
        LanguageLabel = payload.LanguageLabel;
        ProjectNameLabel = payload.ProjectNameLabel;
        NewProjectNamePlaceholder = payload.NewProjectNamePlaceholder;
        CreateProjectText = payload.CreateProjectText;
        AvailableProjectsTitle = payload.AvailableProjectsTitle;
        CurrentProjectTitle = payload.CurrentProjectTitle;
        DocumentIllustrationTitle = payload.DocumentIllustrationTitle;
        DocumentSourceFilePathLabel = payload.DocumentSourceFilePathLabel;
        DocumentSourceTextLabel = payload.DocumentSourceTextLabel;
        DocumentAudienceLabel = payload.DocumentAudienceLabel;
        DocumentStrictnessLabel = payload.DocumentStrictnessLabel;
        BrowseDocumentSourceFileText = payload.BrowseDocumentSourceFileText;
        ImportDocumentSourceFileText = payload.ImportDocumentSourceFileText;
        RunFakeDocumentPlanningText = payload.RunFakeDocumentPlanningText;
        DocumentPlanningResultText = payload.DocumentPlanningResultText;
        _defaultDocumentSourceText = payload.DefaultDocumentSourceText;
        _defaultDocumentAudience = payload.DefaultDocumentAudience;
        ImageSeriesWorkspace.ApplyLocalization(payload);
        ImageSeriesPlanningWorkspace.ApplyLocalization(payload);
        ImageSeriesBriefWorkspace.ApplyLocalization(payload);
        ImageSeriesGalleryWorkspace.ApplyLocalization(payload);
        ImageSeriesReviewWorkspace.ApplyLocalization(payload);
        ImageSeriesDeliveryWorkspace.ApplyLocalization(payload);
        ImageSeriesGenerationSettingsWorkspace.ApplyLocalization(payload);
        GraphNodeColumn = payload.GraphNodeColumn;
        GraphSummaryColumn = payload.GraphSummaryColumn;
        GraphLinksColumn = payload.GraphLinksColumn;
        NoGraphRowsText = payload.NoGraphRowsText;
        ImageEditResultText = payload.ImageEditResultText;
        NewDocumentSourceText = restoredSelections.DocumentSourceText;
        NewDocumentAudience = restoredSelections.DocumentAudience;
        DocumentStrictnessOptions = restoredSelections.DocumentStrictnessOptions;
        SelectedDocumentStrictnessOption = restoredSelections.SelectedDocumentStrictnessOption;
        ImageSeriesGenerationSettingsWorkspace.ApplyOptions(
            restoredSelections.ImageTypePresetOptions,
            restoredSelections.SelectedImageTypePresetOption,
            restoredSelections.StyleGuideOptions,
            restoredSelections.SelectedStyleGuideOption,
            restoredSelections.GenerationRecipeOptions,
            restoredSelections.SelectedGenerationRecipeOption);
        CurrentProjectSummary = BuildCurrentProjectSummary(SelectedProject);
        if (string.IsNullOrWhiteSpace(NewPlanningAudience))
        {
            NewPlanningAudience = Text(LocalizationKey.DefaultPlanningAudience);
        }

        if (string.IsNullOrWhiteSpace(NewPlanningStyleBrief))
        {
            NewPlanningStyleBrief = Text(LocalizationKey.DefaultPlanningStyleBrief);
        }

        NavigationItems = payload.NavigationItems;
        WorkbenchTabs = payload.WorkbenchTabs;
        ActivityItems = payload.ActivityItems;
        LanguageOptions = restoredSelections.LanguageOptions;

        _selectedLanguageOption = restoredSelections.SelectedLanguageOption;
        OnPropertyChanged(nameof(SelectedLanguageOption));
        RebuildPlanRows();
        RebuildPromptRows();
        RebuildWorkflowGraphRows();
    }

    private IReadOnlyList<FinalImageDeliveryCategoryOptionViewModel> BuildFinalDeliveryCategoryOptions()
    {
        return
        [
            new(FinalImageDeliveryCategory.ImageSeries, Text(LocalizationKey.FinalDeliveryImageSeries)),
            new(FinalImageDeliveryCategory.ImageEdits, Text(LocalizationKey.FinalDeliveryImageEdits)),
            new(FinalImageDeliveryCategory.ArticleFigureSets, Text(LocalizationKey.FinalDeliveryArticleFigureSets)),
            new(FinalImageDeliveryCategory.ScientificFigures, Text(LocalizationKey.FinalDeliveryScientificFigures)),
            new(FinalImageDeliveryCategory.DocumentIllustrations, Text(LocalizationKey.FinalDeliveryDocumentIllustrations)),
            new(FinalImageDeliveryCategory.CoursewareVisuals, Text(LocalizationKey.FinalDeliveryCoursewareVisuals)),
            new(FinalImageDeliveryCategory.PosterReportVisuals, Text(LocalizationKey.FinalDeliveryPosterReportVisuals)),
        ];
    }

    private static bool SupportsDocumentSourceFile(string? filePath)
    {
        return NormalizeDocumentSourceFilePath(filePath) is not null
            && ResolveDocumentSourceKind(filePath!) is not null;
    }

    private static SourceAssetKind? ResolveDocumentSourceKind(string filePath)
    {
        return Path.GetExtension(filePath).ToLowerInvariant() switch
        {
            ".pdf" => SourceAssetKind.Pdf,
            ".docx" => SourceAssetKind.Docx,
            _ => null,
        };
    }

    private static string? NormalizeDocumentSourceFilePath(string? filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            return null;
        }

        var normalized = Path.GetFullPath(filePath.Trim());
        return File.Exists(normalized) ? normalized : null;
    }

    private static string? ResolveDocumentMimeType(SourceAssetKind sourceKind)
    {
        return sourceKind switch
        {
            SourceAssetKind.Pdf => "application/pdf",
            SourceAssetKind.Docx => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            _ => null,
        };
    }

    private void ApplyEmptyPlanState()
    {
        ApplyWorkbenchState(_projectWorkbenchStateCoordinator.CreateEmptyState());
        ImageSeriesWorkspace.ApplyProjection([]);
        _activeCreativeBriefId = null;
        RebuildWorkflowGraphRows();
        ImageSeriesPlanningWorkspace.NotifyCommandStatesChanged();
        ImageSeriesBriefWorkspace.NotifyCommandStatesChanged();
    }

    private void ApplyWorkbenchState(ProjectWorkbenchStateResult state)
    {
        ImageSeriesPlanningWorkspace.ApplyProjection(
            state.Series,
            state.PlanRows,
            state.PromptRows,
            state.SelectedSeries,
            state.SelectedSeriesItem);
        ImageSeriesBriefWorkspace.ApplyProjection(
            state.DesignBlueprintRows,
            state.PromptDirectionRows,
            state.SelectedDesignBlueprint,
            state.SelectedPromptDirection);
        ImageSeriesWorkspace.ApplyProjection(state.QueueRows);
        ImageSeriesGalleryWorkspace.ApplyProjection(state.GalleryRows);
        ImageSeriesReviewWorkspace.ApplyProjection(state.ReviewRows);
        ImageSeriesDeliveryWorkspace.ApplyProjection(state.DeliveryRows);
    }

    private void RebuildPlanRows()
    {
        ImageSeriesPlanningWorkspace.ApplyPlanRows(
            _projectWorkbenchProjectionCoordinator.BuildPlanRows(
                ImageSeriesWorkspace.Planning.Series,
                ImageSeriesWorkspace.Planning.NoItemsInSeriesText));
    }

    private void RebuildPromptRows()
    {
        ImageSeriesPlanningWorkspace.ApplyPromptRows(
            _projectWorkbenchProjectionCoordinator.BuildPromptRows(Series));
    }

    private void RebuildWorkflowGraphRows()
    {
        WorkflowGraphRows = _workflowGraphCoordinator.BuildRows(
            SelectedProject,
            Series,
            GalleryRows,
            ReviewRows,
            DeliveryRows);
    }

    private static string FormatList(IReadOnlyList<string>? values)
    {
        return values is null || values.Count == 0 ? string.Empty : string.Join("; ", values);
    }

    private string BuildCurrentProjectSummary(ProjectSummaryViewModel? project)
    {
        return project is null
            ? Text(LocalizationKey.NoProjectLoaded)
            : $"{project.Name} ({project.UpdatedAt.LocalDateTime:g})";
    }

    private static string BuildStyleRecipeSummary(
        ImageTypePresetOptionViewModel? preset,
        StyleGuideOptionViewModel? guide,
        GenerationRecipeOptionViewModel? recipe)
    {
        return $"{preset?.DisplayName ?? "-"} / {guide?.Name ?? "-"} / {recipe?.DisplayName ?? "-"}";
    }

    private static string SanitizeFileName(string value)
    {
        var invalidChars = Path.GetInvalidFileNameChars();
        var sanitized = new string(value.Select(character => invalidChars.Contains(character) ? '-' : character).ToArray());
        return string.IsNullOrWhiteSpace(sanitized) ? "image" : sanitized.Trim();
    }

    private string Text(LocalizationKey key)
    {
        return _localizationService.GetText(key);
    }
}

public sealed record WorkbenchTabViewModel(
    WorkbenchTabKind Kind,
    string Title,
    string EmptyState,
    ScientificFigureWorkspaceProjection? ScientificWorkspace = null,
    ScientificSourceUnderstandingViewModel? ScientificSourceUnderstanding = null,
    ScientificFigureSpecViewModel? ScientificFigureSpec = null,
    ScientificRenderReviewViewModel? ScientificRenderReview = null,
    ScientificDeliveryViewModel? ScientificDelivery = null,
    ScientificChartWorkspaceViewModel? ScientificChartWorkspace = null)
{
    public bool IsBrief => Kind is WorkbenchTabKind.Brief;

    public bool IsPlan => Kind is WorkbenchTabKind.Plan;

    public bool IsPrompts => Kind is WorkbenchTabKind.Prompts;

    public bool IsQueue => Kind is WorkbenchTabKind.Queue;

    public bool IsGallery => Kind is WorkbenchTabKind.Gallery;

    public bool IsReview => Kind is WorkbenchTabKind.Review;

    public bool IsDelivery => Kind is WorkbenchTabKind.Delivery;

    public bool IsGraph => Kind is WorkbenchTabKind.Graph;

    public bool IsScientificFigure => Kind is WorkbenchTabKind.ScientificFigure;
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
    Graph = 7,
    ScientificFigure = 8,
}

public sealed record LanguageOptionViewModel(LanguagePreference Preference, string DisplayName);

public sealed record ProjectSummaryViewModel(Guid Id, string Name, DateTimeOffset UpdatedAt);

public sealed record SeriesSummaryViewModel(Guid Id, string Title, IReadOnlyList<SeriesItemViewModel> Items);

public interface IIdentifiedOption
{
    string Id { get; }
}

public sealed record ImageTypePresetOptionViewModel(string Id, string DisplayName, string Summary) : IIdentifiedOption;

public sealed record StyleGuideOptionViewModel(string Id, string Name, string Summary) : IIdentifiedOption;

public sealed record GenerationRecipeOptionViewModel(string Id, string DisplayName, string Summary) : IIdentifiedOption;

public sealed record DocumentStrictnessOptionViewModel(IllustrationStrictnessLevel Value, string DisplayName);

public sealed record FinalImageDeliveryCategoryOptionViewModel(
    FinalImageDeliveryCategory Category,
    string DisplayName);

public sealed record SeriesItemViewModel(
    Guid Id,
    string Title,
    string Brief,
    SeriesItemKind Kind,
    SeriesItemStatus Status,
    IReadOnlyList<PromptVersionViewModel> PromptVersions);

public sealed record PlanRowViewModel(string SeriesTitle, string ItemTitle, string Brief, string KindText, string StatusText);

public sealed record DesignBlueprintRowViewModel(
    Guid CreativeBriefId,
    Guid BlueprintId,
    string Key,
    string DisplayName,
    string Category,
    string Summary,
    string IntendedUse,
    string ItemCountRange,
    string SequenceMode,
    string TextPolicy,
    string ReviewRubricTemplateId,
    string ConsistencySummary,
    string VariationSummary,
    string RiskSummary,
    bool IsPromoted,
    string PromotionStatus);

public sealed record PromptDirectionRowViewModel(
    Guid CreativeBriefId,
    string DirectionKey,
    string Name,
    string IntendedUse,
    string PromptText,
    string Strength,
    string Risk,
    string RecommendationSummary,
    string RecommendationReason,
    string CapabilityWarningSummary,
    string NonExecutableSuggestionSummary);

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
    Guid TaskId,
    string ItemTitle,
    GenerationTaskStatus TaskStatus,
    int? QueuePosition,
    string Attempts,
    string OutputPath,
    string ErrorMessage,
    Guid? RetryOfTaskId,
    string RequestSummary = "",
    string ApprovalSummary = "")
{
    public string Status => TaskStatus.ToString();

    public string Position => QueuePosition?.ToString() ?? string.Empty;

    public bool CanPause => TaskStatus is GenerationTaskStatus.Queued;

    public bool CanResume => TaskStatus is GenerationTaskStatus.Paused;

    public bool CanRetry => TaskStatus is GenerationTaskStatus.Failed or GenerationTaskStatus.Cancelled;

    public bool CanReorder => TaskStatus is GenerationTaskStatus.Queued or GenerationTaskStatus.Paused;
}

public sealed record GalleryRowViewModel(
    Guid CandidateImageId,
    Guid SeriesItemId,
    string ItemTitle,
    string AssetPath,
    string MetadataPath,
    string PromptText,
    CandidateImageEditProvenance? EditProvenance = null);

public sealed record ReviewRowViewModel(
    Guid CandidateImageId,
    string ItemTitle,
    string Decision,
    string ScoreText,
    string Comments,
    string SuggestedFix,
    string RouteSummary,
    bool HumanApproved,
    string HumanApprovalStatus,
    string FinalReviewer,
    string FinalApprovalNotes,
    DateTimeOffset? FinalApprovalDecidedAt,
    StructuredReviewOutput Review);

public sealed record DeliveryRowViewModel(
    string PackageDirectory,
    string ManifestJsonPath,
    string ManifestCsvPath,
    string ReviewReportPath,
    string FinalImageCount);

public sealed record WorkflowGraphRowViewModel(
    string NodeType,
    string Title,
    string Summary,
    string LinksTo);
