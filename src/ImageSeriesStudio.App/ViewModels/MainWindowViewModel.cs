using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ImageSeriesStudio.Application.Delivery;
using ImageSeriesStudio.Application.Localization;
using ImageSeriesStudio.Application.Projects;
using ImageSeriesStudio.Core.Generation;
using ImageSeriesStudio.Core.Documents;
using ImageSeriesStudio.Core.Projects;
using ImageSeriesStudio.Core.Providers;
using ImageSeriesStudio.Core.Styles;

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
    private string _documentIllustrationTitle = string.Empty;
    private string _documentSourceTextLabel = string.Empty;
    private string _documentAudienceLabel = string.Empty;
    private string _documentStrictnessLabel = string.Empty;
    private string _runFakeDocumentPlanningText = string.Empty;
    private string _documentPlanningResultText = string.Empty;
    private string _documentPlanningResultSummary = string.Empty;
    private string _newDocumentSourceText = string.Empty;
    private string _newDocumentAudience = string.Empty;
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

    public string SelectedCandidateSummary => SelectedGalleryRow is null
        ? Text(LocalizationKey.NoCandidateSelectedForEdit)
        : $"{SelectedGalleryRow.ItemTitle} ({SelectedGalleryRow.CandidateImageId:N})";

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
            SelectedSeriesItemTitleText = value?.Title ?? NoItemSelectedForPromptText;
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
        DocumentIllustrationTitle = Text(LocalizationKey.DocumentIllustrationTitle);
        DocumentSourceTextLabel = Text(LocalizationKey.DocumentSourceText);
        DocumentAudienceLabel = Text(LocalizationKey.DocumentAudience);
        DocumentStrictnessLabel = Text(LocalizationKey.DocumentStrictness);
        RunFakeDocumentPlanningText = Text(LocalizationKey.RunFakeDocumentPlanning);
        DocumentPlanningResultText = Text(LocalizationKey.DocumentPlanningResult);
        _defaultDocumentSourceText = Text(LocalizationKey.DefaultDocumentSourceText);
        _defaultDocumentAudience = Text(LocalizationKey.DefaultDocumentAudience);
        BriefGoalLabel = Text(LocalizationKey.BriefGoal);
        BriefAudienceLabel = Text(LocalizationKey.BriefAudience);
        BriefStyleIntentLabel = Text(LocalizationKey.BriefStyleIntent);
        CreateBriefText = Text(LocalizationKey.CreateBrief);
        GenerateDesignBlueprintsText = Text(LocalizationKey.GenerateDesignBlueprints);
        PromoteDesignBlueprintText = Text(LocalizationKey.PromoteDesignBlueprint);
        BlueprintRoutesHeader = Text(LocalizationKey.BlueprintRoutesHeader);
        NoBlueprintRowsText = Text(LocalizationKey.NoBlueprintRows);
        GeneratePromptDirectionsText = Text(LocalizationKey.GeneratePromptDirections);
        PromotePromptDirectionText = Text(LocalizationKey.PromotePromptDirection);
        PromptDirectionsHeader = Text(LocalizationKey.PromptDirectionsHeader);
        NoPromptDirectionRowsText = Text(LocalizationKey.NoPromptDirectionRows);
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
        RunFakeReviewText = Text(LocalizationKey.RunFakeReview);
        ReviewItemColumn = Text(LocalizationKey.ReviewItemColumn);
        ReviewDecisionColumn = Text(LocalizationKey.ReviewDecisionColumn);
        ReviewScoreColumn = Text(LocalizationKey.ReviewScoreColumn);
        ReviewCommentsColumn = Text(LocalizationKey.ReviewCommentsColumn);
        ReviewFixColumn = Text(LocalizationKey.ReviewFixColumn);
        HumanApprovalColumn = Text(LocalizationKey.HumanApprovalColumn);
        NoReviewRowsText = Text(LocalizationKey.NoReviewRows);
        FinalApprovalReviewerLabel = Text(LocalizationKey.FinalApprovalReviewer);
        FinalApprovalNotesLabel = Text(LocalizationKey.FinalApprovalNotes);
        ApproveSelectedReviewText = Text(LocalizationKey.ApproveSelectedReview);
        RejectSelectedReviewText = Text(LocalizationKey.RejectSelectedReview);
        ExportDeliveryText = Text(LocalizationKey.ExportDelivery);
        DeliveryPackageColumn = Text(LocalizationKey.DeliveryPackageColumn);
        DeliveryManifestColumn = Text(LocalizationKey.DeliveryManifestColumn);
        DeliveryReportColumn = Text(LocalizationKey.DeliveryReportColumn);
        DeliveryFinalImagesColumn = Text(LocalizationKey.DeliveryFinalImagesColumn);
        NoDeliveryRowsText = Text(LocalizationKey.NoDeliveryRows);
        GraphNodeColumn = Text(LocalizationKey.GraphNodeColumn);
        GraphSummaryColumn = Text(LocalizationKey.GraphSummaryColumn);
        GraphLinksColumn = Text(LocalizationKey.GraphLinksColumn);
        NoGraphRowsText = Text(LocalizationKey.NoGraphRows);
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
        StyleRecipeInspectorTitle = Text(LocalizationKey.StyleRecipeInspector);
        ImageTypePresetLabel = Text(LocalizationKey.ImageTypePreset);
        StyleGuideLabel = Text(LocalizationKey.StyleGuide);
        GenerationRecipeLabel = Text(LocalizationKey.GenerationRecipe);
        StyleRecipeSummaryTitle = Text(LocalizationKey.StyleRecipeSummary);
        ImageEditTitle = Text(LocalizationKey.ImageEditTitle);
        SelectedCandidateLabel = Text(LocalizationKey.SelectedCandidate);
        ImageEditPromptLabel = Text(LocalizationKey.ImageEditPrompt);
        ImageEditMaskPathLabel = Text(LocalizationKey.ImageEditMaskPath);
        RunFakeImageEditText = Text(LocalizationKey.RunFakeImageEdit);
        ImageEditResultText = Text(LocalizationKey.ImageEditResult);
        OnPropertyChanged(nameof(SelectedCandidateSummary));
        RefreshDocumentDefaults(previousDefaultDocumentSourceText, previousDefaultDocumentAudience);
        RefreshDocumentStrictnessOptions(previousDocumentStrictness);
        RefreshStyleRecipeOptions();
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
            new(WorkbenchTabKind.Graph, Text(LocalizationKey.Graph), Text(LocalizationKey.GraphEmptyState)),
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
        RebuildWorkflowGraphRows();
    }

    private void RefreshDocumentDefaults(string previousSourceTextDefault, string previousAudienceDefault)
    {
        if (string.IsNullOrWhiteSpace(NewDocumentSourceText)
            || NewDocumentSourceText.Equals(previousSourceTextDefault, StringComparison.Ordinal))
        {
            NewDocumentSourceText = _defaultDocumentSourceText;
        }

        if (string.IsNullOrWhiteSpace(NewDocumentAudience)
            || NewDocumentAudience.Equals(previousAudienceDefault, StringComparison.Ordinal))
        {
            NewDocumentAudience = _defaultDocumentAudience;
        }
    }

    private void RefreshDocumentStrictnessOptions(IllustrationStrictnessLevel selectedStrictness)
    {
        DocumentStrictnessOptions =
        [
            new(IllustrationStrictnessLevel.Editorial, Text(LocalizationKey.DocumentStrictnessEditorial)),
            new(IllustrationStrictnessLevel.Educational, Text(LocalizationKey.DocumentStrictnessEducational)),
            new(IllustrationStrictnessLevel.ScholarlyDraft, Text(LocalizationKey.DocumentStrictnessScholarlyDraft)),
        ];
        SelectedDocumentStrictnessOption =
            DocumentStrictnessOptions.FirstOrDefault(option => option.Value == selectedStrictness)
            ?? DocumentStrictnessOptions.First(option => option.Value is IllustrationStrictnessLevel.Educational);
    }

    private void RefreshStyleRecipeOptions()
    {
        var previousPresetId = SelectedImageTypePresetOption?.Id;
        var previousStyleGuideId = SelectedStyleGuideOption?.Id;
        var previousRecipeId = SelectedGenerationRecipeOption?.Id;

        ImageTypePresetOptions = ImageTypePresetCatalog.Defaults
            .Select(preset => new ImageTypePresetOptionViewModel(
                preset.Id,
                preset.DisplayName,
                $"{preset.DefaultAspectRatio}, {preset.DefaultOutputFormat}, {preset.TextPolicy}"))
            .ToArray();
        StyleGuideOptions =
        [
            new(
                "default-editorial",
                Text(LocalizationKey.DefaultStyleGuideName),
                Text(LocalizationKey.DefaultStyleGuideSummary)),
        ];
        GenerationRecipeOptions =
        [
            new(
                "fake-standard-png",
                Text(LocalizationKey.DefaultGenerationRecipeName),
                "fake-image-v1, 1024x1024, standard, png, auto"),
        ];

        SelectedImageTypePresetOption = SelectById(ImageTypePresetOptions, previousPresetId) ?? ImageTypePresetOptions.FirstOrDefault();
        SelectedStyleGuideOption = SelectById(StyleGuideOptions, previousStyleGuideId) ?? StyleGuideOptions.FirstOrDefault();
        SelectedGenerationRecipeOption = SelectById(GenerationRecipeOptions, previousRecipeId) ?? GenerationRecipeOptions.FirstOrDefault();
        RefreshStyleRecipeSummary();
    }

    private void RefreshStyleRecipeSummary()
    {
        var preset = SelectedImageTypePresetOption?.DisplayName ?? "-";
        var guide = SelectedStyleGuideOption?.Name ?? "-";
        var recipe = SelectedGenerationRecipeOption?.DisplayName ?? "-";
        StyleRecipeSummaryText = $"{preset} / {guide} / {recipe}";
    }

    private static T? SelectById<T>(IReadOnlyList<T> values, string? id)
        where T : IIdentifiedOption
    {
        return string.IsNullOrWhiteSpace(id)
            ? default
            : values.FirstOrDefault(value => value.Id.Equals(id, StringComparison.OrdinalIgnoreCase));
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

    [RelayCommand(CanExecute = nameof(CanRunFakeDocumentPlanning))]
    private async Task RunFakeDocumentPlanningAsync()
    {
        if (SelectedProject is null || string.IsNullOrWhiteSpace(NewDocumentSourceText))
        {
            return;
        }

        var projectId = SelectedProject.Id;
        var projectName = SelectedProject.Name;
        var sourceText = NewDocumentSourceText.Trim();
        var audience = string.IsNullOrWhiteSpace(NewDocumentAudience)
            ? _defaultDocumentAudience
            : NewDocumentAudience.Trim();
        var strictness = SelectedDocumentStrictnessOption?.Value ?? IllustrationStrictnessLevel.Educational;
        DocumentPlanningResultSummary = string.Empty;
        var result = await _projectService.CreateDocumentIllustrationPlanWithProviderAsync(
            projectId,
            new DocumentIllustrationPlanningRequest(
                projectName,
                sourceText,
                audience,
                MapDocumentFamily(strictness),
                strictness,
                [Text(LocalizationKey.DocumentPastedSourceSection)],
                [sourceText],
                [Text(LocalizationKey.DocumentReadableTextConstraint)]),
            approveAllTargets: true,
            DateTimeOffset.UtcNow,
            CancellationToken.None);

        await RefreshProjectsAsync(projectId);
        await LoadPlanAsync(projectId, result.SeriesId);

        var resultMessage = string.Format(
            Text(LocalizationKey.DocumentPlanningResultTemplate),
            result.ApprovedTargetCount);
        DocumentPlanningResultSummary = resultMessage;
        ActivityItems = ActivityItems.Concat([resultMessage]).ToArray();
    }

    private bool CanRunFakeDocumentPlanning()
    {
        return SelectedProject is not null && !string.IsNullOrWhiteSpace(NewDocumentSourceText);
    }

    private static DocumentFamily MapDocumentFamily(IllustrationStrictnessLevel strictness)
    {
        return strictness switch
        {
            IllustrationStrictnessLevel.Editorial => DocumentFamily.Editorial,
            IllustrationStrictnessLevel.ScholarlyDraft => DocumentFamily.ScholarlyDraft,
            _ => DocumentFamily.Educational,
        };
    }

    private bool TryGetPlanningItemCount(out int itemCount)
    {
        return int.TryParse(NewPlanningItemCount, out itemCount) && itemCount > 0;
    }

    [RelayCommand(CanExecute = nameof(CanCreateBrief))]
    private async Task CreateBriefAsync()
    {
        if (SelectedProject is null || SelectedSeries is null)
        {
            return;
        }

        var brief = await CreateBriefForSelectedSeriesAsync();
        _activeCreativeBriefId = brief.Id;
        await LoadPlanAsync(SelectedProject.Id, SelectedSeries.Id, SelectedSeriesItem?.Id);
        _activeCreativeBriefId = brief.Id;
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

        var briefId = await EnsureActiveCreativeBriefIdAsync();
        var brief = await _projectService.CreatePromptDirectionsAsync(
            SelectedProject.Id,
            briefId,
            DateTimeOffset.UtcNow,
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

        var briefId = await EnsureActiveCreativeBriefIdAsync();
        var brief = await _projectService.CreateDesignBlueprintsAsync(
            SelectedProject.Id,
            briefId,
            DateTimeOffset.UtcNow,
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
        var project = SelectedProject is null
            ? null
            : await _projectService.LoadProjectAsync(SelectedProject.Id, CancellationToken.None);
        var selectedSeries = project?.Series.SingleOrDefault(series => series.Id == SelectedSeries?.Id);

        if (_activeCreativeBriefId is { } existingBriefId
            && selectedSeries?.CreativeBriefs.Any(brief => brief.Id == existingBriefId) == true)
        {
            return existingBriefId;
        }

        var latestBrief = selectedSeries?.CreativeBriefs
            .OrderByDescending(brief => brief.UpdatedAt)
            .FirstOrDefault();

        if (latestBrief is not null)
        {
            _activeCreativeBriefId = latestBrief.Id;
            return latestBrief.Id;
        }

        var created = await CreateBriefForSelectedSeriesAsync();
        _activeCreativeBriefId = created.Id;
        return created.Id;
    }

    private async Task<CreativeBrief> CreateBriefForSelectedSeriesAsync()
    {
        if (SelectedProject is null || SelectedSeries is null)
        {
            throw new InvalidOperationException("A project and series must be selected before creating a brief.");
        }

        return await _projectService.CreateCreativeBriefAsync(
            SelectedProject.Id,
            SelectedSeries.Id,
            NewPlanningGoal.Trim(),
            NewPlanningAudience.Trim(),
            ImageTextPolicy.Hybrid,
            NewPlanningStyleBrief.Trim(),
            BuildBriefMustInclude(),
            ["unreadable small text"],
            DateTimeOffset.UtcNow,
            CancellationToken.None);
    }

    private IReadOnlyList<string> BuildBriefMustInclude()
    {
        var itemBriefs = SelectedSeries?.Items
            .Select(item => string.IsNullOrWhiteSpace(item.Brief) ? item.Title : $"{item.Title}: {item.Brief}")
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .ToArray() ?? [];

        return itemBriefs.Length == 0
            ? [SelectedSeries?.Title ?? NewPlanningGoal.Trim()]
            : itemBriefs;
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
        ReviewRows = [];
        DeliveryRows = [];
        RunFakeReviewCommand.NotifyCanExecuteChanged();
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
            var promptText = task is null ? string.Empty : FindPromptText(task.PromptVersionId);

            return new GalleryRowViewModel(
                image.CandidateImageId,
                task?.SeriesItemId ?? Guid.Empty,
                itemTitle,
                image.AssetPath,
                image.MetadataPath,
                promptText);
        }).ToArray();
    }

    [RelayCommand(CanExecute = nameof(CanRunFakeImageEdit))]
    private async Task RunFakeImageEditAsync()
    {
        if (SelectedProject is null || SelectedGalleryRow is null)
        {
            return;
        }

        var selectedRow = SelectedGalleryRow;
        var editPrompt = NewImageEditPrompt.Trim();
        var maskPath = string.IsNullOrWhiteSpace(NewImageEditMaskPath)
            ? null
            : NewImageEditMaskPath.Trim();
        var outputDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "ImageSeriesStudio",
            "edited",
            SelectedProject.Id.ToString("N"));

        var result = await _projectService.RunImageEditAsync(
            new ImageEditWorkflowRequest(
                SelectedProject.Id,
                selectedRow.SeriesItemId,
                selectedRow.CandidateImageId,
                selectedRow.AssetPath,
                maskPath,
                editPrompt,
                CreateDefaultGenerationSettings(),
                outputDirectory,
                CreateImageEditOutputFileName(selectedRow)),
            CancellationToken.None);

        var editedRow = new GalleryRowViewModel(
            result.CandidateImageId,
            selectedRow.SeriesItemId,
            $"{selectedRow.ItemTitle} (edited)",
            result.AssetPath,
            result.MetadataPath,
            BuildEditedPromptText(selectedRow.PromptText, editPrompt));

        GalleryRows = [.. GalleryRows, editedRow];
        SelectedGalleryRow = editedRow;
        ReviewRows = [];
        DeliveryRows = [];
        ActivityItems = [ImageEditResultText, .. ActivityItems];
    }

    private bool CanRunFakeImageEdit()
    {
        return SelectedProject is not null
            && SelectedGalleryRow is not null
            && !string.IsNullOrWhiteSpace(NewImageEditPrompt);
    }

    private static string CreateImageEditOutputFileName(GalleryRowViewModel row)
    {
        return $"{DateTimeOffset.UtcNow:yyyyMMddHHmmss}-{row.CandidateImageId:N}-{SanitizeFileName(row.ItemTitle)}-edited.png";
    }

    private static string BuildEditedPromptText(string sourcePrompt, string editPrompt)
    {
        return string.IsNullOrWhiteSpace(sourcePrompt)
            ? editPrompt
            : string.Join(
                Environment.NewLine,
                [
                    $"Source prompt: {sourcePrompt}",
                    $"Edit instruction: {editPrompt}",
                ]);
    }

    [RelayCommand(CanExecute = nameof(CanRunFakeReview))]
    private async Task RunFakeReviewAsync()
    {
        if (SelectedProject is null)
        {
            return;
        }

        var reviews = await _projectService.RunStructuredVisionReviewAsync(
            SelectedProject.Id,
            GalleryRows
                .Select(row => new ReviewCandidateInput(
                    row.CandidateImageId,
                    row.ItemTitle,
                    row.AssetPath,
                    row.PromptText))
                .ToArray(),
            CancellationToken.None);

        ReviewRows = reviews.Zip(GalleryRows).Select(pair =>
        {
            var scoreText = string.Join(", ", pair.First.Scores.Select(score => $"{score.Name}:{score.Score}"));
            return new ReviewRowViewModel(
                pair.First.CandidateImageId,
                pair.Second.ItemTitle,
                pair.First.Decision.ToString(),
                scoreText,
                pair.First.Comments,
                pair.First.SuggestedFix ?? string.Empty,
                HumanApproved: false,
                Text(LocalizationKey.HumanApprovalPending),
                string.Empty,
                string.Empty,
                pair.First);
        }).ToArray();
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
        ApplyFinalApproval(approve: true);
        return Task.CompletedTask;
    }

    private bool CanApproveSelectedReview()
    {
        return SelectedReviewRow is { Review.Decision: ReviewDecision.Pass, Review.NeedsRepair: false }
            && !string.IsNullOrWhiteSpace(FinalApprovalReviewer);
    }

    [RelayCommand(CanExecute = nameof(CanRejectSelectedReview))]
    private Task RejectSelectedReviewAsync()
    {
        ApplyFinalApproval(approve: false);
        return Task.CompletedTask;
    }

    private bool CanRejectSelectedReview()
    {
        return SelectedReviewRow is not null
            && !string.IsNullOrWhiteSpace(FinalApprovalReviewer)
            && !string.IsNullOrWhiteSpace(FinalApprovalNotes);
    }

    private void ApplyFinalApproval(bool approve)
    {
        if (SelectedReviewRow is null)
        {
            return;
        }

        var decision = FinalApprovalWorkflow.Decide(
            new FinalApprovalRequest(
                SelectedReviewRow.Review,
                approve,
                FinalApprovalReviewer,
                FinalApprovalNotes),
            DateTimeOffset.UtcNow);

        var updated = SelectedReviewRow with
        {
            HumanApproved = decision.HumanApproved,
            HumanApprovalStatus = decision.HumanApproved
                ? Text(LocalizationKey.HumanApprovalApproved)
                : Text(LocalizationKey.HumanApprovalRejected),
            FinalReviewer = decision.Reviewer,
            FinalApprovalNotes = decision.Notes,
        };

        ReviewRows = ReviewRows
            .Select(row => row.CandidateImageId == updated.CandidateImageId ? updated : row)
            .ToArray();
        SelectedReviewRow = updated;
        DeliveryRows = [];
        ExportDeliveryCommand.NotifyCanExecuteChanged();
    }

    private string FindPromptText(Guid promptVersionId)
    {
        return Series
            .SelectMany(series => series.Items)
            .SelectMany(item => item.PromptVersions)
            .FirstOrDefault(prompt => prompt.Id == promptVersionId)
            ?.PromptText ?? string.Empty;
    }

    [RelayCommand(CanExecute = nameof(CanExportDelivery))]
    private async Task ExportDeliveryAsync()
    {
        if (SelectedProject is null)
        {
            return;
        }

        var reviewByCandidate = ReviewRows.ToDictionary(row => row.CandidateImageId);
        var outputDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "ImageSeriesStudio",
            "deliveries",
            SelectedProject.Id.ToString("N"),
            DateTimeOffset.UtcNow.ToString("yyyyMMdd-HHmmss"));
        var items = GalleryRows
            .Where(row => reviewByCandidate.TryGetValue(row.CandidateImageId, out var review)
                && review.HumanApproved
                && Enum.TryParse<ReviewDecision>(review.Decision, out var decision)
                && decision is ReviewDecision.Pass)
            .Select((row, index) => new DeliveryExportItem(
                $"{index + 1:000}-{row.ItemTitle}",
                row.ItemTitle,
                row.AssetPath,
                row.MetadataPath,
                row.PromptText,
                ReviewDecision.Pass,
                HumanApproved: true))
            .ToArray();

        var result = await _projectService.ExportDeliveryPackageAsync(
            new DeliveryExportRequest(
                SelectedProject.Name,
                outputDirectory,
                items),
            CancellationToken.None);

        DeliveryRows =
        [
            new DeliveryRowViewModel(
                result.PackageDirectory,
                result.ManifestJsonPath,
                result.ManifestCsvPath,
                result.ReviewReportPath,
                result.FinalImagePaths.Count.ToString()),
        ];
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
        DesignBlueprintRows = BuildDesignBlueprintRows(project, Text(LocalizationKey.BlueprintPromoted), Text(LocalizationKey.BlueprintCandidate));
        PromptDirectionRows = BuildPromptDirectionRows(project);
        RebuildPlanRows();
        RebuildPromptRows();

        SelectedSeries = selectedSeriesId is null
            ? Series.FirstOrDefault()
            : Series.FirstOrDefault(series => series.Id == selectedSeriesId);
        if (selectedItemId is not null)
        {
            SelectedSeriesItem = SelectedSeries?.Items.FirstOrDefault(item => item.Id == selectedItemId) ?? SelectedSeriesItem;
        }

        SelectedPromptDirection = PromptDirectionRows.FirstOrDefault(direction =>
            _activeCreativeBriefId is null || direction.CreativeBriefId == _activeCreativeBriefId);
        SelectedDesignBlueprint = DesignBlueprintRows.FirstOrDefault(blueprint =>
            _activeCreativeBriefId is null || blueprint.CreativeBriefId == _activeCreativeBriefId);
        CreateSeriesCommand.NotifyCanExecuteChanged();
        RebuildWorkflowGraphRows();
    }

    private Task ClearPlanAsync()
    {
        Series = [];
        SelectedSeries = null;
        SeriesItems = [];
        SelectedSeriesItem = null;
        QueueRows = [];
        GalleryRows = [];
        ReviewRows = [];
        DeliveryRows = [];
        DesignBlueprintRows = [];
        SelectedDesignBlueprint = null;
        PromptDirectionRows = [];
        SelectedPromptDirection = null;
        _activeCreativeBriefId = null;
        RebuildPlanRows();
        RebuildPromptRows();
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

    private void RebuildWorkflowGraphRows()
    {
        var rows = new List<WorkflowGraphRowViewModel>();

        if (SelectedProject is not null)
        {
            rows.Add(new WorkflowGraphRowViewModel(
                Text(LocalizationKey.GraphProjectNode),
                SelectedProject.Name,
                $"series={Series.Count}",
                string.Empty));
        }

        foreach (var series in Series)
        {
            rows.Add(new WorkflowGraphRowViewModel(
                Text(LocalizationKey.GraphSeriesNode),
                series.Title,
                $"items={series.Items.Count}",
                SelectedProject?.Name ?? string.Empty));

            foreach (var item in series.Items)
            {
                var candidateCount = GalleryRows.Count(row =>
                    row.SeriesItemId == item.Id
                    || row.ItemTitle.Equals(item.Title, StringComparison.OrdinalIgnoreCase));
                var reviewCount = ReviewRows.Count(row =>
                    row.ItemTitle.Equals(item.Title, StringComparison.OrdinalIgnoreCase));

                rows.Add(new WorkflowGraphRowViewModel(
                    Text(LocalizationKey.GraphItemNode),
                    item.Title,
                    $"{_localizationService.GetSeriesItemStatusText(item.Status)}; prompts={item.PromptVersions.Count}; candidates={candidateCount}; reviews={reviewCount}",
                    series.Title));

                foreach (var prompt in item.PromptVersions.OrderBy(prompt => prompt.VersionNumber))
                {
                    rows.Add(new WorkflowGraphRowViewModel(
                        Text(LocalizationKey.GraphPromptNode),
                        $"v{prompt.VersionNumber}",
                        $"{prompt.SettingsSummary}; {prompt.CreatedAt.LocalDateTime:g}",
                        item.Title));
                }
            }
        }

        foreach (var candidate in GalleryRows)
        {
            rows.Add(new WorkflowGraphRowViewModel(
                Text(LocalizationKey.GraphCandidateNode),
                ShortId(candidate.CandidateImageId),
                candidate.AssetPath,
                candidate.ItemTitle));
        }

        foreach (var review in ReviewRows)
        {
            rows.Add(new WorkflowGraphRowViewModel(
                Text(LocalizationKey.GraphReviewNode),
                review.Decision,
                review.ScoreText,
                review.ItemTitle));
        }

        foreach (var delivery in DeliveryRows)
        {
            rows.Add(new WorkflowGraphRowViewModel(
                Text(LocalizationKey.GraphDeliveryNode),
                Path.GetFileName(delivery.PackageDirectory),
                $"{delivery.FinalImageCount} {Text(LocalizationKey.GraphDeliveryImages)}",
                SelectedProject?.Name ?? string.Empty));
        }

        WorkflowGraphRows = rows;
    }

    private static IReadOnlyList<DesignBlueprintRowViewModel> BuildDesignBlueprintRows(
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

    private static IReadOnlyList<PromptDirectionRowViewModel> BuildPromptDirectionRows(ImageProject project)
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

    private static string FormatGenerationSettings(GenerationSettings settings)
    {
        return $"{settings.Width}x{settings.Height} {settings.Quality} {settings.OutputFormat}";
    }

    private static string SanitizeFileName(string value)
    {
        var invalidChars = Path.GetInvalidFileNameChars();
        var sanitized = new string(value.Select(character => invalidChars.Contains(character) ? '-' : character).ToArray());
        return string.IsNullOrWhiteSpace(sanitized) ? "image" : sanitized.Trim();
    }

    private static string ShortId(Guid id)
    {
        return id.ToString("N")[..8];
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
    SeriesItemStatus Status,
    IReadOnlyList<PromptVersionViewModel> PromptVersions);

public sealed record PlanRowViewModel(string SeriesTitle, string ItemTitle, string Brief, string StatusText);

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
    bool HumanApproved,
    string HumanApprovalStatus,
    string FinalReviewer,
    string FinalApprovalNotes,
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
