using System.IO;
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
    private readonly PlanEditorWorkflowCoordinator _planEditorWorkflowCoordinator;
    private readonly WorkflowGraphCoordinator _workflowGraphCoordinator;
    private readonly ProjectWorkbenchProjectionCoordinator _projectWorkbenchProjectionCoordinator;
    private readonly ProjectWorkbenchStateCoordinator _projectWorkbenchStateCoordinator;
    private readonly WorkbenchInspectorCoordinator _workbenchInspectorCoordinator;
    private readonly MainWindowLocalizationCoordinator _mainWindowLocalizationCoordinator;
    private readonly MainWindowSelectionSummaryCoordinator _mainWindowSelectionSummaryCoordinator;
    private readonly IDocumentSourceFilePickerService? _documentSourceFilePickerService;

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
    private string _briefGoalLabel = string.Empty;
    private string _briefAudienceLabel = string.Empty;
    private string _briefStyleIntentLabel = string.Empty;
    private string _createBriefText = string.Empty;
    private string _generateDesignBlueprintsText = string.Empty;
    private string _promoteDesignBlueprintText = string.Empty;
    private string _blueprintRoutesHeader = string.Empty;
    private string _noBlueprintRowsText = string.Empty;
    private string _generatePromptDirectionsText = string.Empty;
    private string _promotePromptDirectionText = string.Empty;
    private string _promptDirectionsHeader = string.Empty;
    private string _noPromptDirectionRowsText = string.Empty;
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
    private string _runFakeReviewText = string.Empty;
    private string _reviewItemColumn = string.Empty;
    private string _reviewDecisionColumn = string.Empty;
    private string _reviewScoreColumn = string.Empty;
    private string _reviewCommentsColumn = string.Empty;
    private string _reviewFixColumn = string.Empty;
    private string _reviewRouteColumn = string.Empty;
    private string _noReviewRowsText = string.Empty;
    private string _humanApprovalColumn = string.Empty;
    private string _finalApprovalReviewerLabel = string.Empty;
    private string _finalApprovalNotesLabel = string.Empty;
    private string _approveSelectedReviewText = string.Empty;
    private string _rejectSelectedReviewText = string.Empty;
    private string _exportDeliveryText = string.Empty;
    private string _deliveryPackageColumn = string.Empty;
    private string _deliveryManifestColumn = string.Empty;
    private string _deliveryReportColumn = string.Empty;
    private string _deliveryFinalImagesColumn = string.Empty;
    private string _noDeliveryRowsText = string.Empty;
    private string _graphNodeColumn = string.Empty;
    private string _graphSummaryColumn = string.Empty;
    private string _graphLinksColumn = string.Empty;
    private string _noGraphRowsText = string.Empty;
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
    private string _planKindColumn = string.Empty;
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
    private string _styleRecipeInspectorTitle = string.Empty;
    private string _imageTypePresetLabel = string.Empty;
    private string _styleGuideLabel = string.Empty;
    private string _generationRecipeLabel = string.Empty;
    private string _styleRecipeSummaryTitle = string.Empty;
    private string _styleRecipeSummaryText = string.Empty;
    private string _selectedSeriesItemTitleText = string.Empty;
    private string _imageEditTitle = string.Empty;
    private string _selectedCandidateLabel = string.Empty;
    private string _imageEditPromptLabel = string.Empty;
    private string _imageEditMaskPathLabel = string.Empty;
    private string _runFakeImageEditText = string.Empty;
    private string _imageEditResultText = string.Empty;
    private string _newSeriesTitle = string.Empty;
    private string _newSeriesDescription = string.Empty;
    private string _newItemTitle = string.Empty;
    private string _newItemBrief = string.Empty;
    private string _newPromptText = string.Empty;
    private string _newImageEditPrompt = string.Empty;
    private string _newImageEditMaskPath = string.Empty;
    private string _finalApprovalReviewer = string.Empty;
    private string _finalApprovalNotes = string.Empty;
    private IReadOnlyList<SeriesSummaryViewModel> _series = [];
    private IReadOnlyList<SeriesItemViewModel> _seriesItems = [];
    private IReadOnlyList<PlanRowViewModel> _planRows = [];
    private IReadOnlyList<PromptVersionViewModel> _promptVersions = [];
    private IReadOnlyList<DesignBlueprintRowViewModel> _designBlueprintRows = [];
    private IReadOnlyList<PromptDirectionRowViewModel> _promptDirectionRows = [];
    private IReadOnlyList<PromptRowViewModel> _promptRows = [];
    private IReadOnlyList<QueueRowViewModel> _queueRows = [];
    private IReadOnlyList<GalleryRowViewModel> _galleryRows = [];
    private IReadOnlyList<ReviewRowViewModel> _reviewRows = [];
    private IReadOnlyList<DeliveryRowViewModel> _deliveryRows = [];
    private IReadOnlyList<WorkflowGraphRowViewModel> _workflowGraphRows = [];
    private IReadOnlyList<ImageTypePresetOptionViewModel> _imageTypePresetOptions = [];
    private IReadOnlyList<StyleGuideOptionViewModel> _styleGuideOptions = [];
    private IReadOnlyList<GenerationRecipeOptionViewModel> _generationRecipeOptions = [];
    private SeriesSummaryViewModel? _selectedSeries;
    private SeriesItemViewModel? _selectedSeriesItem;
    private ImageTypePresetOptionViewModel? _selectedImageTypePresetOption;
    private StyleGuideOptionViewModel? _selectedStyleGuideOption;
    private GenerationRecipeOptionViewModel? _selectedGenerationRecipeOption;
    private DesignBlueprintRowViewModel? _selectedDesignBlueprint;
    private PromptDirectionRowViewModel? _selectedPromptDirection;
    private GalleryRowViewModel? _selectedGalleryRow;
    private ReviewRowViewModel? _selectedReviewRow;
    private Guid? _activeCreativeBriefId;

    public MainWindowViewModel(
        LocalizationService localizationService,
        ProjectApplicationService projectService,
        ProviderCenterViewModel providerCenter,
        IDocumentSourceFilePickerService? documentSourceFilePickerService = null)
    {
        _localizationService = localizationService;
        _projectService = projectService;
        _documentSourceFilePickerService = documentSourceFilePickerService;
        _projectWorkspaceCoordinator = new ProjectWorkspaceCoordinator(projectService);
        _planningWorkflowCoordinator = new PlanningWorkflowCoordinator(projectService, localizationService);
        _briefWorkflowCoordinator = new BriefWorkflowCoordinator(projectService);
        _generationWorkflowCoordinator = new GenerationWorkflowCoordinator(projectService);
        _reviewWorkflowCoordinator = new ReviewWorkflowCoordinator(projectService, localizationService);
        _deliveryWorkflowCoordinator = new DeliveryWorkflowCoordinator(projectService);
        _planEditorWorkflowCoordinator = new PlanEditorWorkflowCoordinator(projectService);
        _workflowGraphCoordinator = new WorkflowGraphCoordinator(localizationService);
        _projectWorkbenchProjectionCoordinator = new ProjectWorkbenchProjectionCoordinator(localizationService, projectService);
        _projectWorkbenchStateCoordinator = new ProjectWorkbenchStateCoordinator(
            localizationService,
            projectService,
            _projectWorkbenchProjectionCoordinator);
        _workbenchInspectorCoordinator = new WorkbenchInspectorCoordinator(
            _projectWorkspaceCoordinator,
            _planningWorkflowCoordinator,
            _generationWorkflowCoordinator);
        _mainWindowLocalizationCoordinator = new MainWindowLocalizationCoordinator(localizationService);
        _mainWindowSelectionSummaryCoordinator = new MainWindowSelectionSummaryCoordinator();
        ProviderCenter = providerCenter;
        RefreshLocalizedText();
        SelectedLanguageOption = LanguageOptions.First(option => option.Preference == _localizationService.Preference);
        NewProjectName = NewProjectNamePlaceholder;
        NewPlanningAudience = Text(LocalizationKey.DefaultPlanningAudience);
        NewPlanningStyleBrief = Text(LocalizationKey.DefaultPlanningStyleBrief);
        _ = RefreshProjectsAsync();
    }

    public ProviderCenterViewModel ProviderCenter { get; }

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

            CurrentProjectSummary = _mainWindowSelectionSummaryCoordinator.BuildCurrentProjectSummary(
                value,
                Text(LocalizationKey.NoProjectLoaded));
            if (value is not null && string.IsNullOrWhiteSpace(NewPlanningGoal))
            {
                NewPlanningGoal = value.Name;
            }

            QueueRows = [];
            GalleryRows = [];
            ReviewRows = [];
            DeliveryRows = [];
            DesignBlueprintRows = [];
            PromptDirectionRows = [];
            DocumentPlanningResultSummary = string.Empty;
            SelectedDesignBlueprint = null;
            SelectedPromptDirection = null;
            _activeCreativeBriefId = null;
            RebuildWorkflowGraphRows();
            CreateBriefCommand.NotifyCanExecuteChanged();
            GenerateDesignBlueprintsCommand.NotifyCanExecuteChanged();
            PromoteDesignBlueprintCommand.NotifyCanExecuteChanged();
            GeneratePromptDirectionsCommand.NotifyCanExecuteChanged();
            PromotePromptDirectionCommand.NotifyCanExecuteChanged();
            RunFakePlanningCommand.NotifyCanExecuteChanged();
            RunFakeDocumentPlanningCommand.NotifyCanExecuteChanged();
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
                CreateBriefCommand.NotifyCanExecuteChanged();
                GenerateDesignBlueprintsCommand.NotifyCanExecuteChanged();
                GeneratePromptDirectionsCommand.NotifyCanExecuteChanged();
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
                CreateBriefCommand.NotifyCanExecuteChanged();
                GenerateDesignBlueprintsCommand.NotifyCanExecuteChanged();
                GeneratePromptDirectionsCommand.NotifyCanExecuteChanged();
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
        set
        {
            if (SetProperty(ref _newPlanningStyleBrief, value))
            {
                CreateBriefCommand.NotifyCanExecuteChanged();
                GenerateDesignBlueprintsCommand.NotifyCanExecuteChanged();
                GeneratePromptDirectionsCommand.NotifyCanExecuteChanged();
            }
        }
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

    public string RunFakeReviewText
    {
        get => _runFakeReviewText;
        private set => SetProperty(ref _runFakeReviewText, value);
    }

    public string ReviewItemColumn
    {
        get => _reviewItemColumn;
        private set => SetProperty(ref _reviewItemColumn, value);
    }

    public string ReviewDecisionColumn
    {
        get => _reviewDecisionColumn;
        private set => SetProperty(ref _reviewDecisionColumn, value);
    }

    public string ReviewScoreColumn
    {
        get => _reviewScoreColumn;
        private set => SetProperty(ref _reviewScoreColumn, value);
    }

    public string ReviewCommentsColumn
    {
        get => _reviewCommentsColumn;
        private set => SetProperty(ref _reviewCommentsColumn, value);
    }

    public string ReviewFixColumn
    {
        get => _reviewFixColumn;
        private set => SetProperty(ref _reviewFixColumn, value);
    }

    public string ReviewRouteColumn
    {
        get => _reviewRouteColumn;
        private set => SetProperty(ref _reviewRouteColumn, value);
    }

    public string HumanApprovalColumn
    {
        get => _humanApprovalColumn;
        private set => SetProperty(ref _humanApprovalColumn, value);
    }

    public string NoReviewRowsText
    {
        get => _noReviewRowsText;
        private set => SetProperty(ref _noReviewRowsText, value);
    }

    public string FinalApprovalReviewerLabel
    {
        get => _finalApprovalReviewerLabel;
        private set => SetProperty(ref _finalApprovalReviewerLabel, value);
    }

    public string FinalApprovalNotesLabel
    {
        get => _finalApprovalNotesLabel;
        private set => SetProperty(ref _finalApprovalNotesLabel, value);
    }

    public string ApproveSelectedReviewText
    {
        get => _approveSelectedReviewText;
        private set => SetProperty(ref _approveSelectedReviewText, value);
    }

    public string RejectSelectedReviewText
    {
        get => _rejectSelectedReviewText;
        private set => SetProperty(ref _rejectSelectedReviewText, value);
    }

    public string ExportDeliveryText
    {
        get => _exportDeliveryText;
        private set => SetProperty(ref _exportDeliveryText, value);
    }

    public string DeliveryPackageColumn
    {
        get => _deliveryPackageColumn;
        private set => SetProperty(ref _deliveryPackageColumn, value);
    }

    public string DeliveryManifestColumn
    {
        get => _deliveryManifestColumn;
        private set => SetProperty(ref _deliveryManifestColumn, value);
    }

    public string DeliveryReportColumn
    {
        get => _deliveryReportColumn;
        private set => SetProperty(ref _deliveryReportColumn, value);
    }

    public string DeliveryFinalImagesColumn
    {
        get => _deliveryFinalImagesColumn;
        private set => SetProperty(ref _deliveryFinalImagesColumn, value);
    }

    public string NoDeliveryRowsText
    {
        get => _noDeliveryRowsText;
        private set => SetProperty(ref _noDeliveryRowsText, value);
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

    public string PlanKindColumn
    {
        get => _planKindColumn;
        private set => SetProperty(ref _planKindColumn, value);
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

    public string BriefGoalLabel
    {
        get => _briefGoalLabel;
        private set => SetProperty(ref _briefGoalLabel, value);
    }

    public string BriefAudienceLabel
    {
        get => _briefAudienceLabel;
        private set => SetProperty(ref _briefAudienceLabel, value);
    }

    public string BriefStyleIntentLabel
    {
        get => _briefStyleIntentLabel;
        private set => SetProperty(ref _briefStyleIntentLabel, value);
    }

    public string CreateBriefText
    {
        get => _createBriefText;
        private set => SetProperty(ref _createBriefText, value);
    }

    public string GenerateDesignBlueprintsText
    {
        get => _generateDesignBlueprintsText;
        private set => SetProperty(ref _generateDesignBlueprintsText, value);
    }

    public string PromoteDesignBlueprintText
    {
        get => _promoteDesignBlueprintText;
        private set => SetProperty(ref _promoteDesignBlueprintText, value);
    }

    public string BlueprintRoutesHeader
    {
        get => _blueprintRoutesHeader;
        private set => SetProperty(ref _blueprintRoutesHeader, value);
    }

    public string NoBlueprintRowsText
    {
        get => _noBlueprintRowsText;
        private set => SetProperty(ref _noBlueprintRowsText, value);
    }

    public string GeneratePromptDirectionsText
    {
        get => _generatePromptDirectionsText;
        private set => SetProperty(ref _generatePromptDirectionsText, value);
    }

    public string PromotePromptDirectionText
    {
        get => _promotePromptDirectionText;
        private set => SetProperty(ref _promotePromptDirectionText, value);
    }

    public string PromptDirectionsHeader
    {
        get => _promptDirectionsHeader;
        private set => SetProperty(ref _promptDirectionsHeader, value);
    }

    public string NoPromptDirectionRowsText
    {
        get => _noPromptDirectionRowsText;
        private set => SetProperty(ref _noPromptDirectionRowsText, value);
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

    public string StyleRecipeInspectorTitle
    {
        get => _styleRecipeInspectorTitle;
        private set => SetProperty(ref _styleRecipeInspectorTitle, value);
    }

    public string ImageTypePresetLabel
    {
        get => _imageTypePresetLabel;
        private set => SetProperty(ref _imageTypePresetLabel, value);
    }

    public string StyleGuideLabel
    {
        get => _styleGuideLabel;
        private set => SetProperty(ref _styleGuideLabel, value);
    }

    public string GenerationRecipeLabel
    {
        get => _generationRecipeLabel;
        private set => SetProperty(ref _generationRecipeLabel, value);
    }

    public string StyleRecipeSummaryTitle
    {
        get => _styleRecipeSummaryTitle;
        private set => SetProperty(ref _styleRecipeSummaryTitle, value);
    }

    public string StyleRecipeSummaryText
    {
        get => _styleRecipeSummaryText;
        private set => SetProperty(ref _styleRecipeSummaryText, value);
    }

    public string SelectedSeriesItemTitleText
    {
        get => _selectedSeriesItemTitleText;
        private set => SetProperty(ref _selectedSeriesItemTitleText, value);
    }

    public string ImageEditTitle
    {
        get => _imageEditTitle;
        private set => SetProperty(ref _imageEditTitle, value);
    }

    public string SelectedCandidateLabel
    {
        get => _selectedCandidateLabel;
        private set => SetProperty(ref _selectedCandidateLabel, value);
    }

    public string SelectedCandidateSummary => _mainWindowSelectionSummaryCoordinator.BuildSelectedCandidateSummary(
        SelectedGalleryRow,
        Text(LocalizationKey.NoCandidateSelectedForEdit));

    public string ImageEditPromptLabel
    {
        get => _imageEditPromptLabel;
        private set => SetProperty(ref _imageEditPromptLabel, value);
    }

    public string ImageEditMaskPathLabel
    {
        get => _imageEditMaskPathLabel;
        private set => SetProperty(ref _imageEditMaskPathLabel, value);
    }

    public string RunFakeImageEditText
    {
        get => _runFakeImageEditText;
        private set => SetProperty(ref _runFakeImageEditText, value);
    }

    public string ImageEditResultText
    {
        get => _imageEditResultText;
        private set => SetProperty(ref _imageEditResultText, value);
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

    public string NewImageEditPrompt
    {
        get => _newImageEditPrompt;
        set
        {
            if (SetProperty(ref _newImageEditPrompt, value))
            {
                RunFakeImageEditCommand.NotifyCanExecuteChanged();
            }
        }
    }

    public string NewImageEditMaskPath
    {
        get => _newImageEditMaskPath;
        set => SetProperty(ref _newImageEditMaskPath, value);
    }

    public string FinalApprovalReviewer
    {
        get => _finalApprovalReviewer;
        set
        {
            if (SetProperty(ref _finalApprovalReviewer, value))
            {
                ApproveSelectedReviewCommand.NotifyCanExecuteChanged();
                RejectSelectedReviewCommand.NotifyCanExecuteChanged();
            }
        }
    }

    public string FinalApprovalNotes
    {
        get => _finalApprovalNotes;
        set
        {
            if (SetProperty(ref _finalApprovalNotes, value))
            {
                RejectSelectedReviewCommand.NotifyCanExecuteChanged();
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
            CreateBriefCommand.NotifyCanExecuteChanged();
            GenerateDesignBlueprintsCommand.NotifyCanExecuteChanged();
            PromoteDesignBlueprintCommand.NotifyCanExecuteChanged();
            GeneratePromptDirectionsCommand.NotifyCanExecuteChanged();
        }
    }

    public IReadOnlyList<SeriesItemViewModel> SeriesItems
    {
        get => _seriesItems;
        private set => SetProperty(ref _seriesItems, value);
    }

    public IReadOnlyList<ImageTypePresetOptionViewModel> ImageTypePresetOptions
    {
        get => _imageTypePresetOptions;
        private set => SetProperty(ref _imageTypePresetOptions, value);
    }

    public ImageTypePresetOptionViewModel? SelectedImageTypePresetOption
    {
        get => _selectedImageTypePresetOption;
        set
        {
            if (SetProperty(ref _selectedImageTypePresetOption, value))
            {
                RefreshStyleRecipeSummary();
            }
        }
    }

    public IReadOnlyList<StyleGuideOptionViewModel> StyleGuideOptions
    {
        get => _styleGuideOptions;
        private set => SetProperty(ref _styleGuideOptions, value);
    }

    public StyleGuideOptionViewModel? SelectedStyleGuideOption
    {
        get => _selectedStyleGuideOption;
        set
        {
            if (SetProperty(ref _selectedStyleGuideOption, value))
            {
                RefreshStyleRecipeSummary();
            }
        }
    }

    public IReadOnlyList<GenerationRecipeOptionViewModel> GenerationRecipeOptions
    {
        get => _generationRecipeOptions;
        private set => SetProperty(ref _generationRecipeOptions, value);
    }

    public GenerationRecipeOptionViewModel? SelectedGenerationRecipeOption
    {
        get => _selectedGenerationRecipeOption;
        set
        {
            if (SetProperty(ref _selectedGenerationRecipeOption, value))
            {
                RefreshStyleRecipeSummary();
            }
        }
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
            SelectedSeriesItemTitleText = _mainWindowSelectionSummaryCoordinator.BuildSelectedSeriesItemTitle(
                value,
                NoItemSelectedForPromptText);
            CreatePromptVersionCommand.NotifyCanExecuteChanged();
            PromotePromptDirectionCommand.NotifyCanExecuteChanged();
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

    public IReadOnlyList<DesignBlueprintRowViewModel> DesignBlueprintRows
    {
        get => _designBlueprintRows;
        private set
        {
            if (SetProperty(ref _designBlueprintRows, value))
            {
                OnPropertyChanged(nameof(HasDesignBlueprintRows));
                GenerateDesignBlueprintsCommand.NotifyCanExecuteChanged();
            }
        }
    }

    public bool HasDesignBlueprintRows => DesignBlueprintRows.Count > 0;

    public DesignBlueprintRowViewModel? SelectedDesignBlueprint
    {
        get => _selectedDesignBlueprint;
        set
        {
            if (SetProperty(ref _selectedDesignBlueprint, value))
            {
                PromoteDesignBlueprintCommand.NotifyCanExecuteChanged();
            }
        }
    }

    public IReadOnlyList<PromptDirectionRowViewModel> PromptDirectionRows
    {
        get => _promptDirectionRows;
        private set
        {
            if (SetProperty(ref _promptDirectionRows, value))
            {
                OnPropertyChanged(nameof(HasPromptDirectionRows));
                GeneratePromptDirectionsCommand.NotifyCanExecuteChanged();
            }
        }
    }

    public bool HasPromptDirectionRows => PromptDirectionRows.Count > 0;

    public PromptDirectionRowViewModel? SelectedPromptDirection
    {
        get => _selectedPromptDirection;
        set
        {
            if (SetProperty(ref _selectedPromptDirection, value))
            {
                PromotePromptDirectionCommand.NotifyCanExecuteChanged();
            }
        }
    }

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
                if (value.Count == 0)
                {
                    SelectedGalleryRow = null;
                }
                else if (SelectedGalleryRow is null
                    || !value.Any(row => row.CandidateImageId == SelectedGalleryRow.CandidateImageId))
                {
                    SelectedGalleryRow = value[0];
                }

                RunFakeReviewCommand.NotifyCanExecuteChanged();
                RunFakeImageEditCommand.NotifyCanExecuteChanged();
                RebuildWorkflowGraphRows();
            }
        }
    }

    public bool HasGalleryRows => GalleryRows.Count > 0;

    public GalleryRowViewModel? SelectedGalleryRow
    {
        get => _selectedGalleryRow;
        set
        {
            if (SetProperty(ref _selectedGalleryRow, value))
            {
                OnPropertyChanged(nameof(SelectedCandidateSummary));
                RunFakeImageEditCommand.NotifyCanExecuteChanged();
            }
        }
    }

    public IReadOnlyList<ReviewRowViewModel> ReviewRows
    {
        get => _reviewRows;
        private set
        {
            if (SetProperty(ref _reviewRows, value))
            {
                OnPropertyChanged(nameof(HasReviewRows));
                SelectedReviewRow = value.FirstOrDefault(row =>
                    SelectedReviewRow is null || row.CandidateImageId == SelectedReviewRow.CandidateImageId);
                ExportDeliveryCommand.NotifyCanExecuteChanged();
                RebuildWorkflowGraphRows();
            }
        }
    }

    public bool HasReviewRows => ReviewRows.Count > 0;

    public ReviewRowViewModel? SelectedReviewRow
    {
        get => _selectedReviewRow;
        set
        {
            if (SetProperty(ref _selectedReviewRow, value))
            {
                ApproveSelectedReviewCommand.NotifyCanExecuteChanged();
                RejectSelectedReviewCommand.NotifyCanExecuteChanged();
            }
        }
    }

    public IReadOnlyList<DeliveryRowViewModel> DeliveryRows
    {
        get => _deliveryRows;
        private set
        {
            if (SetProperty(ref _deliveryRows, value))
            {
                OnPropertyChanged(nameof(HasDeliveryRows));
                RebuildWorkflowGraphRows();
            }
        }
    }

    public bool HasDeliveryRows => DeliveryRows.Count > 0;

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
        FakePlanningTitle = payload.FakePlanningTitle;
        PlanningGoalLabel = payload.PlanningGoalLabel;
        PlanningAudienceLabel = payload.PlanningAudienceLabel;
        PlanningItemCountLabel = payload.PlanningItemCountLabel;
        PlanningStyleBriefLabel = payload.PlanningStyleBriefLabel;
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
        RunFakePlanningText = payload.RunFakePlanningText;
        RunFakeGenerationText = payload.RunFakeGenerationText;
        QueueItemColumn = payload.QueueItemColumn;
        QueueStatusColumn = payload.QueueStatusColumn;
        QueueAttemptsColumn = payload.QueueAttemptsColumn;
        QueueOutputColumn = payload.QueueOutputColumn;
        QueueErrorColumn = payload.QueueErrorColumn;
        NoQueueRowsText = payload.NoQueueRowsText;
        GalleryItemColumn = payload.GalleryItemColumn;
        GalleryImageColumn = payload.GalleryImageColumn;
        GalleryMetadataColumn = payload.GalleryMetadataColumn;
        NoGalleryRowsText = payload.NoGalleryRowsText;
        RunFakeReviewText = payload.RunFakeReviewText;
        ReviewItemColumn = payload.ReviewItemColumn;
        ReviewDecisionColumn = payload.ReviewDecisionColumn;
        ReviewScoreColumn = payload.ReviewScoreColumn;
        ReviewCommentsColumn = payload.ReviewCommentsColumn;
        ReviewFixColumn = payload.ReviewFixColumn;
        ReviewRouteColumn = payload.ReviewRouteColumn;
        HumanApprovalColumn = payload.HumanApprovalColumn;
        NoReviewRowsText = payload.NoReviewRowsText;
        FinalApprovalReviewerLabel = payload.FinalApprovalReviewerLabel;
        FinalApprovalNotesLabel = payload.FinalApprovalNotesLabel;
        ApproveSelectedReviewText = payload.ApproveSelectedReviewText;
        RejectSelectedReviewText = payload.RejectSelectedReviewText;
        ExportDeliveryText = payload.ExportDeliveryText;
        DeliveryPackageColumn = payload.DeliveryPackageColumn;
        DeliveryManifestColumn = payload.DeliveryManifestColumn;
        DeliveryReportColumn = payload.DeliveryReportColumn;
        DeliveryFinalImagesColumn = payload.DeliveryFinalImagesColumn;
        NoDeliveryRowsText = payload.NoDeliveryRowsText;
        GraphNodeColumn = payload.GraphNodeColumn;
        GraphSummaryColumn = payload.GraphSummaryColumn;
        GraphLinksColumn = payload.GraphLinksColumn;
        NoGraphRowsText = payload.NoGraphRowsText;
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
        StyleRecipeInspectorTitle = payload.StyleRecipeInspectorTitle;
        ImageTypePresetLabel = payload.ImageTypePresetLabel;
        StyleGuideLabel = payload.StyleGuideLabel;
        GenerationRecipeLabel = payload.GenerationRecipeLabel;
        StyleRecipeSummaryTitle = payload.StyleRecipeSummaryTitle;
        ImageEditTitle = payload.ImageEditTitle;
        SelectedCandidateLabel = payload.SelectedCandidateLabel;
        ImageEditPromptLabel = payload.ImageEditPromptLabel;
        ImageEditMaskPathLabel = payload.ImageEditMaskPathLabel;
        RunFakeImageEditText = payload.RunFakeImageEditText;
        ImageEditResultText = payload.ImageEditResultText;
        OnPropertyChanged(nameof(SelectedCandidateSummary));
        NewDocumentSourceText = restoredSelections.DocumentSourceText;
        NewDocumentAudience = restoredSelections.DocumentAudience;
        DocumentStrictnessOptions = restoredSelections.DocumentStrictnessOptions;
        SelectedDocumentStrictnessOption = restoredSelections.SelectedDocumentStrictnessOption;
        ImageTypePresetOptions = restoredSelections.ImageTypePresetOptions;
        SelectedImageTypePresetOption = restoredSelections.SelectedImageTypePresetOption;
        StyleGuideOptions = restoredSelections.StyleGuideOptions;
        SelectedStyleGuideOption = restoredSelections.SelectedStyleGuideOption;
        GenerationRecipeOptions = restoredSelections.GenerationRecipeOptions;
        SelectedGenerationRecipeOption = restoredSelections.SelectedGenerationRecipeOption;
        CurrentProjectSummary = _mainWindowSelectionSummaryCoordinator.BuildCurrentProjectSummary(
            SelectedProject,
            Text(LocalizationKey.NoProjectLoaded));
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
        SelectedSeriesItemTitleText = _mainWindowSelectionSummaryCoordinator.BuildSelectedSeriesItemTitle(
            SelectedSeriesItem,
            NoItemSelectedForPromptText);
        RebuildPlanRows();
        RebuildPromptRows();
        RebuildWorkflowGraphRows();
    }

    private void RefreshStyleRecipeSummary()
    {
        StyleRecipeSummaryText = _mainWindowSelectionSummaryCoordinator.BuildStyleRecipeSummary(
            SelectedImageTypePresetOption,
            SelectedStyleGuideOption,
            SelectedGenerationRecipeOption);
    }

    [RelayCommand(CanExecute = nameof(CanCreateProject))]
    private async Task CreateProjectAsync()
    {
        var result = await _workbenchInspectorCoordinator.CreateProjectAsync(
            NewProjectName,
            CancellationToken.None);

        Projects = result.Projects;
        SelectedProject = result.SelectedProject;
    }

    private bool CanCreateProject()
    {
        return !string.IsNullOrWhiteSpace(NewProjectName);
    }

    private async Task RefreshProjectsAsync(Guid? selectedProjectId = null)
    {
        var result = await _projectWorkspaceCoordinator.RefreshProjectsAsync(
            selectedProjectId,
            CancellationToken.None);
        Projects = result.Projects;
        SelectedProject = result.SelectedProject;
    }

    [RelayCommand]
    private async Task RefreshProviderCenterAsync()
    {
        await _workbenchInspectorCoordinator.RefreshProviderCenterAsync(
            ProviderCenter,
            CancellationToken.None);
    }

    [RelayCommand]
    private async Task CheckProviderHealthAsync()
    {
        await _workbenchInspectorCoordinator.CheckProviderHealthAsync(
            ProviderCenter,
            CancellationToken.None);
    }

    [RelayCommand(CanExecute = nameof(CanRunFakePlanning))]
    private async Task RunFakePlanningAsync()
    {
        if (SelectedProject is null || !TryGetPlanningItemCount(out var itemCount))
        {
            return;
        }

        var result = await _planningWorkflowCoordinator.RunFakePlanningAsync(
            SelectedProject.Id,
            NewPlanningGoal,
            NewPlanningAudience,
            itemCount,
            NewPlanningStyleBrief,
            CancellationToken.None);

        await LoadPlanAsync(SelectedProject.Id, result.SeriesId);
    }

    private bool CanRunFakePlanning()
    {
        return SelectedProject is not null
            && !string.IsNullOrWhiteSpace(NewPlanningGoal)
            && !string.IsNullOrWhiteSpace(NewPlanningAudience)
            && TryGetPlanningItemCount(out _);
    }

    [RelayCommand(CanExecute = nameof(CanRunFakeDocumentPlanning))]
    private async Task RunFakeDocumentPlanningAsync()
    {
        if (SelectedProject is null || string.IsNullOrWhiteSpace(NewDocumentSourceText))
        {
            return;
        }

        DocumentPlanningResultSummary = string.Empty;
        var projectId = SelectedProject.Id;
        var result = await _workbenchInspectorCoordinator.RunFakeDocumentPlanningAsync(
            SelectedProject,
            NewDocumentSourceText,
            NewDocumentAudience,
            SelectedDocumentStrictnessOption?.Value ?? IllustrationStrictnessLevel.Educational,
            _defaultDocumentAudience,
            ActivityItems,
            CancellationToken.None);

        Projects = result.Workspace.Projects;
        SelectedProject = result.Workspace.SelectedProject;
        await LoadPlanAsync(projectId, result.SeriesId);
        DocumentPlanningResultSummary = result.ResultSummary;
        ActivityItems = result.ActivityItems;
    }

    private bool CanRunFakeDocumentPlanning()
    {
        return SelectedProject is not null && !string.IsNullOrWhiteSpace(NewDocumentSourceText);
    }

    [RelayCommand(CanExecute = nameof(CanImportDocumentSourceFile))]
    private async Task ImportDocumentSourceFileAsync(string? filePath)
    {
        if (SelectedProject is null)
        {
            return;
        }

        var normalizedPath = NormalizeDocumentSourceFilePath(filePath);
        if (normalizedPath is null)
        {
            return;
        }

        var sourceKind = ResolveDocumentSourceKind(normalizedPath);
        if (sourceKind is null)
        {
            return;
        }

        var result = await _projectService.IngestSourceAsync(
            SelectedProject.Id,
            new SourceIngestionRequest(
                sourceKind.Value,
                Path.GetFileName(normalizedPath),
                string.Empty,
                OriginalPath: normalizedPath,
                MimeType: ResolveDocumentMimeType(sourceKind.Value),
                SizeBytes: new FileInfo(normalizedPath).Length,
                Sha256: null),
            DateTimeOffset.UtcNow,
            CancellationToken.None);

        var loadedProject = await _projectService.LoadProjectAsync(SelectedProject.Id, CancellationToken.None);
        var importedAsset = loadedProject?.SourceAssets.LastOrDefault(asset =>
            string.Equals(asset.OriginalPath, normalizedPath, StringComparison.OrdinalIgnoreCase));
        if (importedAsset is not null && importedAsset.ExtractedContents.Count > 0)
        {
            NewDocumentSourceText = string.Join(
                Environment.NewLine,
                importedAsset.ExtractedContents.Select(content => content.Text));
        }

        ImportedDocumentSourcePath = normalizedPath;
        NewDocumentSourceFilePath = normalizedPath;
        ActivityItems = new[] { $"Imported {Path.GetFileName(normalizedPath)}." }
            .Concat(ActivityItems)
            .ToArray();
        RunFakeDocumentPlanningCommand.NotifyCanExecuteChanged();
    }

    private bool CanImportDocumentSourceFile(string? filePath)
    {
        return SelectedProject is not null && SupportsDocumentSourceFile(filePath);
    }

    [RelayCommand]
    private async Task BrowseDocumentSourceFileAsync()
    {
        if (_documentSourceFilePickerService is null)
        {
            return;
        }

        var filePath = await _documentSourceFilePickerService.PickAsync(CancellationToken.None);
        if (string.IsNullOrWhiteSpace(filePath))
        {
            return;
        }

        if (ImportDocumentSourceFileCommand.CanExecute(filePath))
        {
            await ImportDocumentSourceFileCommand.ExecuteAsync(filePath);
        }
    }

    private bool TryGetPlanningItemCount(out int itemCount)
    {
        return int.TryParse(NewPlanningItemCount, out itemCount) && itemCount > 0;
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

    [RelayCommand(CanExecute = nameof(CanCreateBrief))]
    private async Task CreateBriefAsync()
    {
        if (SelectedProject is null || SelectedSeries is null)
        {
            return;
        }

        _activeCreativeBriefId = await _briefWorkflowCoordinator.CreateBriefAsync(
            SelectedProject.Id,
            SelectedSeries,
            NewPlanningGoal,
            NewPlanningAudience,
            NewPlanningStyleBrief,
            CancellationToken.None);
        await LoadPlanAsync(SelectedProject.Id, SelectedSeries.Id, SelectedSeriesItem?.Id);
    }

    private bool CanCreateBrief()
    {
        return SelectedProject is not null
            && SelectedSeries is not null
            && !string.IsNullOrWhiteSpace(NewPlanningGoal)
            && !string.IsNullOrWhiteSpace(NewPlanningAudience);
    }

    [RelayCommand(CanExecute = nameof(CanGeneratePromptDirections))]
    private async Task GeneratePromptDirectionsAsync()
    {
        if (SelectedProject is null || SelectedSeries is null)
        {
            return;
        }

        var brief = await _briefWorkflowCoordinator.GeneratePromptDirectionsAsync(
            SelectedProject.Id,
            SelectedSeries,
            _activeCreativeBriefId,
            NewPlanningGoal,
            NewPlanningAudience,
            NewPlanningStyleBrief,
            CancellationToken.None);

        _activeCreativeBriefId = brief.Id;
        await LoadPlanAsync(SelectedProject.Id, SelectedSeries.Id, SelectedSeriesItem?.Id);
        _activeCreativeBriefId = brief.Id;
        SelectedPromptDirection = PromptDirectionRows.FirstOrDefault(direction => direction.CreativeBriefId == brief.Id)
            ?? PromptDirectionRows.FirstOrDefault();
    }

    private bool CanGeneratePromptDirections()
    {
        return CanCreateBrief();
    }

    [RelayCommand(CanExecute = nameof(CanGenerateDesignBlueprints))]
    private async Task GenerateDesignBlueprintsAsync()
    {
        if (SelectedProject is null || SelectedSeries is null)
        {
            return;
        }

        var brief = await _briefWorkflowCoordinator.GenerateDesignBlueprintsAsync(
            SelectedProject.Id,
            SelectedSeries,
            _activeCreativeBriefId,
            NewPlanningGoal,
            NewPlanningAudience,
            NewPlanningStyleBrief,
            CancellationToken.None);

        _activeCreativeBriefId = brief.Id;
        await LoadPlanAsync(SelectedProject.Id, SelectedSeries.Id, SelectedSeriesItem?.Id);
        _activeCreativeBriefId = brief.Id;
        SelectedDesignBlueprint = DesignBlueprintRows.FirstOrDefault(blueprint => blueprint.CreativeBriefId == brief.Id)
            ?? DesignBlueprintRows.FirstOrDefault();
    }

    private bool CanGenerateDesignBlueprints()
    {
        return CanCreateBrief();
    }

    [RelayCommand(CanExecute = nameof(CanPromoteDesignBlueprint))]
    private async Task PromoteDesignBlueprintAsync()
    {
        if (SelectedProject is null || SelectedDesignBlueprint is null)
        {
            return;
        }

        var promoted = await _projectService.PromoteDesignBlueprintAsync(
            SelectedProject.Id,
            SelectedDesignBlueprint.CreativeBriefId,
            SelectedDesignBlueprint.BlueprintId,
            DateTimeOffset.UtcNow,
            CancellationToken.None);

        _activeCreativeBriefId = SelectedDesignBlueprint.CreativeBriefId;
        await LoadPlanAsync(SelectedProject.Id, SelectedSeries?.Id, SelectedSeriesItem?.Id);
        SelectedDesignBlueprint = DesignBlueprintRows.FirstOrDefault(blueprint => blueprint.BlueprintId == promoted.Id)
            ?? DesignBlueprintRows.FirstOrDefault();
    }

    private bool CanPromoteDesignBlueprint()
    {
        return SelectedProject is not null
            && SelectedDesignBlueprint is not null;
    }

    [RelayCommand(CanExecute = nameof(CanPromotePromptDirection))]
    private async Task PromotePromptDirectionAsync()
    {
        if (SelectedProject is null || SelectedSeriesItem is null || SelectedPromptDirection is null)
        {
            return;
        }

        await _projectService.PromotePromptDirectionAsync(
            SelectedProject.Id,
            SelectedSeriesItem.Id,
            SelectedPromptDirection.CreativeBriefId,
            SelectedPromptDirection.DirectionKey,
            DateTimeOffset.UtcNow,
            CancellationToken.None);

        await LoadPlanAsync(SelectedProject.Id, SelectedSeries?.Id, SelectedSeriesItem.Id);
    }

    private bool CanPromotePromptDirection()
    {
        return SelectedProject is not null
            && SelectedSeriesItem is not null
            && SelectedPromptDirection is not null;
    }

    private async Task<Guid> EnsureActiveCreativeBriefIdAsync()
    {
        if (SelectedProject is null || SelectedSeries is null)
        {
            throw new InvalidOperationException("A project and series must be selected before resolving a brief.");
        }

        var briefId = await _briefWorkflowCoordinator.EnsureActiveCreativeBriefIdAsync(
            SelectedProject.Id,
            SelectedSeries,
            _activeCreativeBriefId,
            NewPlanningGoal,
            NewPlanningAudience,
            NewPlanningStyleBrief,
            CancellationToken.None);
        _activeCreativeBriefId = briefId;
        return briefId;
    }

    private async Task<CreativeBrief> CreateBriefForSelectedSeriesAsync()
    {
        if (SelectedProject is null || SelectedSeries is null)
        {
            throw new InvalidOperationException("A project and series must be selected before creating a brief.");
        }

        var briefId = await _briefWorkflowCoordinator.CreateBriefAsync(
            SelectedProject.Id,
            SelectedSeries,
            NewPlanningGoal,
            NewPlanningAudience,
            NewPlanningStyleBrief,
            CancellationToken.None);
        var project = await _projectService.LoadProjectAsync(SelectedProject.Id, CancellationToken.None)
            ?? throw new InvalidOperationException($"Project not found: {SelectedProject.Id}");

        return project.Series
            .SelectMany(series => series.CreativeBriefs)
            .Single(brief => brief.Id == briefId);
    }

    private IReadOnlyList<string> BuildBriefMustInclude()
    {
        return BriefWorkflowCoordinator.BuildBriefMustInclude(SelectedSeries, NewPlanningGoal);
    }

    [RelayCommand(CanExecute = nameof(CanRunFakeGeneration))]
    private async Task RunFakeGenerationAsync()
    {
        if (SelectedProject is null)
        {
            return;
        }

        var result = await _generationWorkflowCoordinator.RunFakeGenerationAsync(
            SelectedProject.Id,
            Series,
            CancellationToken.None);

        QueueRows = result.QueueRows;
        GalleryRows = result.GalleryRows;
        ReviewRows = [];
        DeliveryRows = [];
        RunFakeReviewCommand.NotifyCanExecuteChanged();
    }

    private bool CanRunFakeGeneration()
    {
        return SelectedProject is not null && PromptRows.Count > 0;
    }

    [RelayCommand(CanExecute = nameof(CanRunFakeImageEdit))]
    private async Task RunFakeImageEditAsync()
    {
        if (SelectedProject is null || SelectedGalleryRow is null)
        {
            return;
        }

        var result = await _workbenchInspectorCoordinator.RunFakeImageEditAsync(
            SelectedProject.Id,
            SelectedGalleryRow,
            NewImageEditPrompt,
            NewImageEditMaskPath,
            ImageEditResultText,
            GalleryRows,
            ActivityItems,
            CancellationToken.None);

        GalleryRows = result.GalleryRows;
        SelectedGalleryRow = result.SelectedGalleryRow;
        ReviewRows = [];
        DeliveryRows = [];
        ActivityItems = result.ActivityItems;
    }

    private bool CanRunFakeImageEdit()
    {
        return SelectedProject is not null
            && SelectedGalleryRow is not null
            && !string.IsNullOrWhiteSpace(NewImageEditPrompt);
    }

    [RelayCommand(CanExecute = nameof(CanRunFakeReview))]
    private async Task RunFakeReviewAsync()
    {
        if (SelectedProject is null)
        {
            return;
        }

        ReviewRows = await _reviewWorkflowCoordinator.RunFakeReviewAsync(
            SelectedProject.Id,
            GalleryRows,
            CancellationToken.None);
        DeliveryRows = [];
        ExportDeliveryCommand.NotifyCanExecuteChanged();
    }

    private bool CanRunFakeReview()
    {
        return SelectedProject is not null && GalleryRows.Count > 0;
    }

    [RelayCommand(CanExecute = nameof(CanApproveSelectedReview))]
    private Task ApproveSelectedReviewAsync()
    {
        return ApplyFinalApprovalAsync(approve: true);
    }

    private bool CanApproveSelectedReview()
    {
        return SelectedReviewRow is { Review.Decision: ReviewDecision.Pass, Review.NeedsRepair: false }
            && !string.IsNullOrWhiteSpace(FinalApprovalReviewer);
    }

    [RelayCommand(CanExecute = nameof(CanRejectSelectedReview))]
    private Task RejectSelectedReviewAsync()
    {
        return ApplyFinalApprovalAsync(approve: false);
    }

    private bool CanRejectSelectedReview()
    {
        return SelectedReviewRow is not null
            && !string.IsNullOrWhiteSpace(FinalApprovalReviewer)
            && !string.IsNullOrWhiteSpace(FinalApprovalNotes);
    }

    private async Task ApplyFinalApprovalAsync(bool approve)
    {
        if (SelectedReviewRow is null || SelectedProject is null)
        {
            return;
        }

        var updated = await _reviewWorkflowCoordinator.ApplyFinalApprovalAsync(
            SelectedProject.Id,
            SelectedReviewRow,
            approve,
            FinalApprovalReviewer,
            FinalApprovalNotes,
            CancellationToken.None);

        ReviewRows = ReviewRows
            .Select(row => row.CandidateImageId == updated.CandidateImageId ? updated : row)
            .ToArray();
        SelectedReviewRow = updated;
        DeliveryRows = [];
        ExportDeliveryCommand.NotifyCanExecuteChanged();
    }

    [RelayCommand(CanExecute = nameof(CanExportDelivery))]
    private async Task ExportDeliveryAsync()
    {
        if (SelectedProject is null)
        {
            return;
        }

        var result = await _deliveryWorkflowCoordinator.ExportDeliveryAsync(
            SelectedProject.Id,
            SelectedProject.Name,
            GalleryRows,
            ReviewRows,
            DesignBlueprintRows,
            _activeCreativeBriefId,
            CancellationToken.None);

        DeliveryRows = result.DeliveryRows;
    }

    private bool CanExportDelivery()
    {
        return SelectedProject is not null
            && GalleryRows.Count > 0
            && ReviewRows.Any(row => row.HumanApproved && row.Decision == ReviewDecision.Pass.ToString());
    }

    [RelayCommand(CanExecute = nameof(CanCreateSeries))]
    private async Task CreateSeriesAsync()
    {
        if (SelectedProject is null)
        {
            return;
        }

        var seriesId = await _planEditorWorkflowCoordinator.CreateSeriesAsync(
            SelectedProject.Id,
            NewSeriesTitle,
            NewSeriesDescription,
            CancellationToken.None);

        NewSeriesTitle = string.Empty;
        NewSeriesDescription = string.Empty;
        await LoadPlanAsync(SelectedProject.Id, seriesId);
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

        var itemId = await _planEditorWorkflowCoordinator.AddItemAsync(
            SelectedProject.Id,
            SelectedSeries.Id,
            NewItemTitle,
            NewItemBrief,
            CancellationToken.None);

        NewItemTitle = string.Empty;
        NewItemBrief = string.Empty;
        await LoadPlanAsync(SelectedProject.Id, SelectedSeries.Id, itemId);
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

        await _planEditorWorkflowCoordinator.CreatePromptVersionAsync(
            SelectedProject.Id,
            SelectedSeriesItem.Id,
            NewPromptText,
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

    private async Task LoadPlanAsync(Guid projectId, Guid? selectedSeriesId = null, Guid? selectedItemId = null)
    {
        var state = await _projectWorkbenchStateCoordinator.LoadAsync(
            projectId,
            selectedSeriesId,
            selectedItemId,
            _activeCreativeBriefId,
            NoItemsInSeriesText,
            CancellationToken.None);
        if (state.Series.Count == 0)
        {
            await ClearPlanAsync();
            return;
        }

        ApplyWorkbenchState(state);
        CreateSeriesCommand.NotifyCanExecuteChanged();
        RebuildWorkflowGraphRows();
    }

    private Task ClearPlanAsync()
    {
        ApplyWorkbenchState(_projectWorkbenchStateCoordinator.CreateEmptyState());
        QueueRows = [];
        _activeCreativeBriefId = null;
        RebuildWorkflowGraphRows();
        CreateSeriesCommand.NotifyCanExecuteChanged();
        AddItemCommand.NotifyCanExecuteChanged();
        CreatePromptVersionCommand.NotifyCanExecuteChanged();
        CreateBriefCommand.NotifyCanExecuteChanged();
        GenerateDesignBlueprintsCommand.NotifyCanExecuteChanged();
        PromoteDesignBlueprintCommand.NotifyCanExecuteChanged();
        GeneratePromptDirectionsCommand.NotifyCanExecuteChanged();
        PromotePromptDirectionCommand.NotifyCanExecuteChanged();
        return Task.CompletedTask;
    }

    private void ApplyWorkbenchState(ProjectWorkbenchStateResult state)
    {
        Series = state.Series;
        DesignBlueprintRows = state.DesignBlueprintRows;
        PromptDirectionRows = state.PromptDirectionRows;
        PlanRows = state.PlanRows;
        PromptRows = state.PromptRows;
        GalleryRows = state.GalleryRows;
        ReviewRows = state.ReviewRows;
        DeliveryRows = state.DeliveryRows;
        SelectedSeries = state.SelectedSeries;
        SelectedSeriesItem = state.SelectedSeriesItem;
        SelectedPromptDirection = state.SelectedPromptDirection;
        SelectedDesignBlueprint = state.SelectedDesignBlueprint;
    }

    private void RebuildPlanRows()
    {
        PlanRows = _projectWorkbenchProjectionCoordinator.BuildPlanRows(Series, NoItemsInSeriesText);
    }

    private void RebuildPromptRows()
    {
        PromptRows = _projectWorkbenchProjectionCoordinator.BuildPromptRows(Series);
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

public sealed record WorkbenchTabViewModel(WorkbenchTabKind Kind, string Title, string EmptyState)
{
    public bool IsBrief => Kind is WorkbenchTabKind.Brief;

    public bool IsPlan => Kind is WorkbenchTabKind.Plan;

    public bool IsPrompts => Kind is WorkbenchTabKind.Prompts;

    public bool IsQueue => Kind is WorkbenchTabKind.Queue;

    public bool IsGallery => Kind is WorkbenchTabKind.Gallery;

    public bool IsReview => Kind is WorkbenchTabKind.Review;

    public bool IsDelivery => Kind is WorkbenchTabKind.Delivery;

    public bool IsGraph => Kind is WorkbenchTabKind.Graph;
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
    string ItemTitle,
    string Status,
    string Attempts,
    string OutputPath,
    string ErrorMessage);

public sealed record GalleryRowViewModel(
    Guid CandidateImageId,
    Guid SeriesItemId,
    string ItemTitle,
    string AssetPath,
    string MetadataPath,
    string PromptText);

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
