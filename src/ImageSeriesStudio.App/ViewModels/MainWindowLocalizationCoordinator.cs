using ImageSeriesStudio.Application.Localization;
using ImageSeriesStudio.Core.Documents;
using ImageSeriesStudio.Core.Styles;

namespace ImageSeriesStudio.App.ViewModels;

public sealed class MainWindowLocalizationCoordinator
{
    private readonly LocalizationService _localizationService;

    public MainWindowLocalizationCoordinator(LocalizationService localizationService)
    {
        _localizationService = localizationService;
    }

    public MainWindowLocalizationPayload BuildPayload()
    {
        return new MainWindowLocalizationPayload
        {
            AppTitle = Text(LocalizationKey.AppTitle),
            ProviderMode = Text(LocalizationKey.ProviderModeFake),
            WorkspaceHeader = Text(LocalizationKey.Workspace),
            InspectorTitle = Text(LocalizationKey.Inspector),
            ActivityTitle = Text(LocalizationKey.Activity),
            InspectorSummary = Text(LocalizationKey.NoItemSelected),
            LanguageLabel = Text(LocalizationKey.LanguageLabel),
            ProjectNameLabel = Text(LocalizationKey.ProjectName),
            NewProjectNamePlaceholder = Text(LocalizationKey.NewProjectNamePlaceholder),
            CreateProjectText = Text(LocalizationKey.CreateProject),
            AvailableProjectsTitle = Text(LocalizationKey.AvailableProjects),
            CurrentProjectTitle = Text(LocalizationKey.CurrentProject),
            FakePlanningTitle = Text(LocalizationKey.FakePlanningTitle),
            PlanningGoalLabel = Text(LocalizationKey.PlanningGoal),
            PlanningAudienceLabel = Text(LocalizationKey.PlanningAudience),
            PlanningItemCountLabel = Text(LocalizationKey.PlanningItemCount),
            PlanningStyleBriefLabel = Text(LocalizationKey.PlanningStyleBrief),
            DocumentIllustrationTitle = Text(LocalizationKey.DocumentIllustrationTitle),
            DocumentSourceTextLabel = Text(LocalizationKey.DocumentSourceText),
            DocumentAudienceLabel = Text(LocalizationKey.DocumentAudience),
            DocumentStrictnessLabel = Text(LocalizationKey.DocumentStrictness),
            RunFakeDocumentPlanningText = Text(LocalizationKey.RunFakeDocumentPlanning),
            DocumentPlanningResultText = Text(LocalizationKey.DocumentPlanningResult),
            DefaultDocumentSourceText = Text(LocalizationKey.DefaultDocumentSourceText),
            DefaultDocumentAudience = Text(LocalizationKey.DefaultDocumentAudience),
            BriefGoalLabel = Text(LocalizationKey.BriefGoal),
            BriefAudienceLabel = Text(LocalizationKey.BriefAudience),
            BriefStyleIntentLabel = Text(LocalizationKey.BriefStyleIntent),
            CreateBriefText = Text(LocalizationKey.CreateBrief),
            GenerateDesignBlueprintsText = Text(LocalizationKey.GenerateDesignBlueprints),
            PromoteDesignBlueprintText = Text(LocalizationKey.PromoteDesignBlueprint),
            BlueprintRoutesHeader = Text(LocalizationKey.BlueprintRoutesHeader),
            NoBlueprintRowsText = Text(LocalizationKey.NoBlueprintRows),
            GeneratePromptDirectionsText = Text(LocalizationKey.GeneratePromptDirections),
            PromotePromptDirectionText = Text(LocalizationKey.PromotePromptDirection),
            PromptDirectionsHeader = Text(LocalizationKey.PromptDirectionsHeader),
            NoPromptDirectionRowsText = Text(LocalizationKey.NoPromptDirectionRows),
            RunFakePlanningText = Text(LocalizationKey.RunFakePlanning),
            RunFakeGenerationText = Text(LocalizationKey.RunFakeGeneration),
            QueueItemColumn = Text(LocalizationKey.QueueItemColumn),
            QueueStatusColumn = Text(LocalizationKey.QueueStatusColumn),
            QueueAttemptsColumn = Text(LocalizationKey.QueueAttemptsColumn),
            QueueOutputColumn = Text(LocalizationKey.QueueOutputColumn),
            QueueErrorColumn = Text(LocalizationKey.QueueErrorColumn),
            NoQueueRowsText = Text(LocalizationKey.NoQueueRows),
            GalleryItemColumn = Text(LocalizationKey.GalleryItemColumn),
            GalleryImageColumn = Text(LocalizationKey.GalleryImageColumn),
            GalleryMetadataColumn = Text(LocalizationKey.GalleryMetadataColumn),
            NoGalleryRowsText = Text(LocalizationKey.NoGalleryRows),
            RunFakeReviewText = Text(LocalizationKey.RunFakeReview),
            ReviewItemColumn = Text(LocalizationKey.ReviewItemColumn),
            ReviewDecisionColumn = Text(LocalizationKey.ReviewDecisionColumn),
            ReviewScoreColumn = Text(LocalizationKey.ReviewScoreColumn),
            ReviewCommentsColumn = Text(LocalizationKey.ReviewCommentsColumn),
            ReviewFixColumn = Text(LocalizationKey.ReviewFixColumn),
            ReviewRouteColumn = Text(LocalizationKey.ReviewRouteColumn),
            HumanApprovalColumn = Text(LocalizationKey.HumanApprovalColumn),
            NoReviewRowsText = Text(LocalizationKey.NoReviewRows),
            FinalApprovalReviewerLabel = Text(LocalizationKey.FinalApprovalReviewer),
            FinalApprovalNotesLabel = Text(LocalizationKey.FinalApprovalNotes),
            ApproveSelectedReviewText = Text(LocalizationKey.ApproveSelectedReview),
            RejectSelectedReviewText = Text(LocalizationKey.RejectSelectedReview),
            ExportDeliveryText = Text(LocalizationKey.ExportDelivery),
            DeliveryPackageColumn = Text(LocalizationKey.DeliveryPackageColumn),
            DeliveryManifestColumn = Text(LocalizationKey.DeliveryManifestColumn),
            DeliveryReportColumn = Text(LocalizationKey.DeliveryReportColumn),
            DeliveryFinalImagesColumn = Text(LocalizationKey.DeliveryFinalImagesColumn),
            NoDeliveryRowsText = Text(LocalizationKey.NoDeliveryRows),
            GraphNodeColumn = Text(LocalizationKey.GraphNodeColumn),
            GraphSummaryColumn = Text(LocalizationKey.GraphSummaryColumn),
            GraphLinksColumn = Text(LocalizationKey.GraphLinksColumn),
            NoGraphRowsText = Text(LocalizationKey.NoGraphRows),
            PlanEditorTitle = Text(LocalizationKey.PlanEditor),
            SeriesTitleLabel = Text(LocalizationKey.SeriesTitle),
            SeriesDescriptionLabel = Text(LocalizationKey.SeriesDescription),
            CreateSeriesText = Text(LocalizationKey.CreateSeries),
            AvailableSeriesTitle = Text(LocalizationKey.AvailableSeries),
            ItemTitleLabel = Text(LocalizationKey.ItemTitle),
            ItemBriefLabel = Text(LocalizationKey.ItemBrief),
            AddItemText = Text(LocalizationKey.AddItem),
            SeriesItemsTitle = Text(LocalizationKey.SeriesItems),
            NoSeriesSelectedText = Text(LocalizationKey.NoSeriesSelected),
            PlanSeriesColumn = Text(LocalizationKey.PlanSeriesColumn),
            PlanItemColumn = Text(LocalizationKey.PlanItemColumn),
            PlanBriefColumn = Text(LocalizationKey.PlanBriefColumn),
            PlanKindColumn = Text(LocalizationKey.PlanKindColumn),
            PlanStatusColumn = Text(LocalizationKey.PlanStatusColumn),
            NoPlanRowsText = Text(LocalizationKey.NoPlanRows),
            NoItemsInSeriesText = Text(LocalizationKey.NoItemsInSeries),
            PromptEditorTitle = Text(LocalizationKey.PromptEditor),
            SelectedItemTitle = Text(LocalizationKey.SelectedItem),
            PromptTextLabel = Text(LocalizationKey.PromptText),
            DefaultGenerationSettingsText = Text(LocalizationKey.DefaultGenerationSettings),
            CreatePromptVersionText = Text(LocalizationKey.CreatePromptVersion),
            PromptHistoryTitle = Text(LocalizationKey.PromptHistory),
            PromptVersionColumn = Text(LocalizationKey.PromptVersionColumn),
            PromptItemColumn = Text(LocalizationKey.PromptItemColumn),
            PromptTextColumn = Text(LocalizationKey.PromptTextColumn),
            PromptSettingsColumn = Text(LocalizationKey.PromptSettingsColumn),
            PromptCreatedColumn = Text(LocalizationKey.PromptCreatedColumn),
            NoPromptRowsText = Text(LocalizationKey.NoPromptRows),
            NoItemSelectedForPromptText = Text(LocalizationKey.NoItemSelectedForPrompt),
            StyleRecipeInspectorTitle = Text(LocalizationKey.StyleRecipeInspector),
            ImageTypePresetLabel = Text(LocalizationKey.ImageTypePreset),
            StyleGuideLabel = Text(LocalizationKey.StyleGuide),
            GenerationRecipeLabel = Text(LocalizationKey.GenerationRecipe),
            StyleRecipeSummaryTitle = Text(LocalizationKey.StyleRecipeSummary),
            ImageEditTitle = Text(LocalizationKey.ImageEditTitle),
            SelectedCandidateLabel = Text(LocalizationKey.SelectedCandidate),
            ImageEditPromptLabel = Text(LocalizationKey.ImageEditPrompt),
            ImageEditMaskPathLabel = Text(LocalizationKey.ImageEditMaskPath),
            RunFakeImageEditText = Text(LocalizationKey.RunFakeImageEdit),
            ImageEditResultText = Text(LocalizationKey.ImageEditResult),
            NavigationItems = BuildNavigationItems(),
            WorkbenchTabs = BuildWorkbenchTabs(),
            ActivityItems = BuildActivityItems(),
            LanguageOptions = BuildLanguageOptions(),
            DocumentStrictnessOptions = BuildDocumentStrictnessOptions(),
            ImageTypePresetOptions = BuildImageTypePresetOptions(),
            StyleGuideOptions = BuildStyleGuideOptions(),
            GenerationRecipeOptions = BuildGenerationRecipeOptions(),
        };
    }

    private IReadOnlyList<string> BuildNavigationItems()
    {
        return
        [
            Text(LocalizationKey.Workspaces),
            Text(LocalizationKey.Projects),
            Text(LocalizationKey.Settings),
        ];
    }

    private IReadOnlyList<WorkbenchTabViewModel> BuildWorkbenchTabs()
    {
        return
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
    }

    private IReadOnlyList<string> BuildActivityItems()
    {
        return
        [
            Text(LocalizationKey.GenericHostStarted),
            Text(LocalizationKey.FakeProvidersRegistered),
            Text(LocalizationKey.NoRealApiCalls),
        ];
    }

    private IReadOnlyList<LanguageOptionViewModel> BuildLanguageOptions()
    {
        return
        [
            new(LanguagePreference.System, Text(LocalizationKey.LanguageSystem)),
            new(LanguagePreference.Chinese, Text(LocalizationKey.LanguageChinese)),
            new(LanguagePreference.English, Text(LocalizationKey.LanguageEnglish)),
        ];
    }

    private IReadOnlyList<DocumentStrictnessOptionViewModel> BuildDocumentStrictnessOptions()
    {
        return
        [
            new(IllustrationStrictnessLevel.Editorial, Text(LocalizationKey.DocumentStrictnessEditorial)),
            new(IllustrationStrictnessLevel.Educational, Text(LocalizationKey.DocumentStrictnessEducational)),
            new(IllustrationStrictnessLevel.ScholarlyDraft, Text(LocalizationKey.DocumentStrictnessScholarlyDraft)),
        ];
    }

    private static IReadOnlyList<ImageTypePresetOptionViewModel> BuildImageTypePresetOptions()
    {
        return ImageTypePresetCatalog.Defaults
            .Select(preset => new ImageTypePresetOptionViewModel(
                preset.Id,
                preset.DisplayName,
                $"{preset.DefaultAspectRatio}, {preset.DefaultOutputFormat}, {preset.TextPolicy}"))
            .ToArray();
    }

    private IReadOnlyList<StyleGuideOptionViewModel> BuildStyleGuideOptions()
    {
        return
        [
            new(
                "default-editorial",
                Text(LocalizationKey.DefaultStyleGuideName),
                Text(LocalizationKey.DefaultStyleGuideSummary)),
        ];
    }

    private IReadOnlyList<GenerationRecipeOptionViewModel> BuildGenerationRecipeOptions()
    {
        return
        [
            new(
                "fake-standard-png",
                Text(LocalizationKey.DefaultGenerationRecipeName),
                "fake-image-v1, 1024x1024, standard, png, auto"),
        ];
    }

    private string Text(LocalizationKey key)
    {
        return _localizationService.GetText(key);
    }
}

public sealed class MainWindowLocalizationPayload
{
    public string AppTitle { get; init; } = string.Empty;
    public string ProviderMode { get; init; } = string.Empty;
    public string WorkspaceHeader { get; init; } = string.Empty;
    public string InspectorTitle { get; init; } = string.Empty;
    public string ActivityTitle { get; init; } = string.Empty;
    public string InspectorSummary { get; init; } = string.Empty;
    public string LanguageLabel { get; init; } = string.Empty;
    public string ProjectNameLabel { get; init; } = string.Empty;
    public string NewProjectNamePlaceholder { get; init; } = string.Empty;
    public string CreateProjectText { get; init; } = string.Empty;
    public string AvailableProjectsTitle { get; init; } = string.Empty;
    public string CurrentProjectTitle { get; init; } = string.Empty;
    public string FakePlanningTitle { get; init; } = string.Empty;
    public string PlanningGoalLabel { get; init; } = string.Empty;
    public string PlanningAudienceLabel { get; init; } = string.Empty;
    public string PlanningItemCountLabel { get; init; } = string.Empty;
    public string PlanningStyleBriefLabel { get; init; } = string.Empty;
    public string DocumentIllustrationTitle { get; init; } = string.Empty;
    public string DocumentSourceTextLabel { get; init; } = string.Empty;
    public string DocumentAudienceLabel { get; init; } = string.Empty;
    public string DocumentStrictnessLabel { get; init; } = string.Empty;
    public string RunFakeDocumentPlanningText { get; init; } = string.Empty;
    public string DocumentPlanningResultText { get; init; } = string.Empty;
    public string DefaultDocumentSourceText { get; init; } = string.Empty;
    public string DefaultDocumentAudience { get; init; } = string.Empty;
    public string BriefGoalLabel { get; init; } = string.Empty;
    public string BriefAudienceLabel { get; init; } = string.Empty;
    public string BriefStyleIntentLabel { get; init; } = string.Empty;
    public string CreateBriefText { get; init; } = string.Empty;
    public string GenerateDesignBlueprintsText { get; init; } = string.Empty;
    public string PromoteDesignBlueprintText { get; init; } = string.Empty;
    public string BlueprintRoutesHeader { get; init; } = string.Empty;
    public string NoBlueprintRowsText { get; init; } = string.Empty;
    public string GeneratePromptDirectionsText { get; init; } = string.Empty;
    public string PromotePromptDirectionText { get; init; } = string.Empty;
    public string PromptDirectionsHeader { get; init; } = string.Empty;
    public string NoPromptDirectionRowsText { get; init; } = string.Empty;
    public string RunFakePlanningText { get; init; } = string.Empty;
    public string RunFakeGenerationText { get; init; } = string.Empty;
    public string QueueItemColumn { get; init; } = string.Empty;
    public string QueueStatusColumn { get; init; } = string.Empty;
    public string QueueAttemptsColumn { get; init; } = string.Empty;
    public string QueueOutputColumn { get; init; } = string.Empty;
    public string QueueErrorColumn { get; init; } = string.Empty;
    public string NoQueueRowsText { get; init; } = string.Empty;
    public string GalleryItemColumn { get; init; } = string.Empty;
    public string GalleryImageColumn { get; init; } = string.Empty;
    public string GalleryMetadataColumn { get; init; } = string.Empty;
    public string NoGalleryRowsText { get; init; } = string.Empty;
    public string RunFakeReviewText { get; init; } = string.Empty;
    public string ReviewItemColumn { get; init; } = string.Empty;
    public string ReviewDecisionColumn { get; init; } = string.Empty;
    public string ReviewScoreColumn { get; init; } = string.Empty;
    public string ReviewCommentsColumn { get; init; } = string.Empty;
    public string ReviewFixColumn { get; init; } = string.Empty;
    public string ReviewRouteColumn { get; init; } = string.Empty;
    public string HumanApprovalColumn { get; init; } = string.Empty;
    public string NoReviewRowsText { get; init; } = string.Empty;
    public string FinalApprovalReviewerLabel { get; init; } = string.Empty;
    public string FinalApprovalNotesLabel { get; init; } = string.Empty;
    public string ApproveSelectedReviewText { get; init; } = string.Empty;
    public string RejectSelectedReviewText { get; init; } = string.Empty;
    public string ExportDeliveryText { get; init; } = string.Empty;
    public string DeliveryPackageColumn { get; init; } = string.Empty;
    public string DeliveryManifestColumn { get; init; } = string.Empty;
    public string DeliveryReportColumn { get; init; } = string.Empty;
    public string DeliveryFinalImagesColumn { get; init; } = string.Empty;
    public string NoDeliveryRowsText { get; init; } = string.Empty;
    public string GraphNodeColumn { get; init; } = string.Empty;
    public string GraphSummaryColumn { get; init; } = string.Empty;
    public string GraphLinksColumn { get; init; } = string.Empty;
    public string NoGraphRowsText { get; init; } = string.Empty;
    public string PlanEditorTitle { get; init; } = string.Empty;
    public string SeriesTitleLabel { get; init; } = string.Empty;
    public string SeriesDescriptionLabel { get; init; } = string.Empty;
    public string CreateSeriesText { get; init; } = string.Empty;
    public string AvailableSeriesTitle { get; init; } = string.Empty;
    public string ItemTitleLabel { get; init; } = string.Empty;
    public string ItemBriefLabel { get; init; } = string.Empty;
    public string AddItemText { get; init; } = string.Empty;
    public string SeriesItemsTitle { get; init; } = string.Empty;
    public string NoSeriesSelectedText { get; init; } = string.Empty;
    public string PlanSeriesColumn { get; init; } = string.Empty;
    public string PlanItemColumn { get; init; } = string.Empty;
    public string PlanBriefColumn { get; init; } = string.Empty;
    public string PlanKindColumn { get; init; } = string.Empty;
    public string PlanStatusColumn { get; init; } = string.Empty;
    public string NoPlanRowsText { get; init; } = string.Empty;
    public string NoItemsInSeriesText { get; init; } = string.Empty;
    public string PromptEditorTitle { get; init; } = string.Empty;
    public string SelectedItemTitle { get; init; } = string.Empty;
    public string PromptTextLabel { get; init; } = string.Empty;
    public string DefaultGenerationSettingsText { get; init; } = string.Empty;
    public string CreatePromptVersionText { get; init; } = string.Empty;
    public string PromptHistoryTitle { get; init; } = string.Empty;
    public string PromptVersionColumn { get; init; } = string.Empty;
    public string PromptItemColumn { get; init; } = string.Empty;
    public string PromptTextColumn { get; init; } = string.Empty;
    public string PromptSettingsColumn { get; init; } = string.Empty;
    public string PromptCreatedColumn { get; init; } = string.Empty;
    public string NoPromptRowsText { get; init; } = string.Empty;
    public string NoItemSelectedForPromptText { get; init; } = string.Empty;
    public string StyleRecipeInspectorTitle { get; init; } = string.Empty;
    public string ImageTypePresetLabel { get; init; } = string.Empty;
    public string StyleGuideLabel { get; init; } = string.Empty;
    public string GenerationRecipeLabel { get; init; } = string.Empty;
    public string StyleRecipeSummaryTitle { get; init; } = string.Empty;
    public string ImageEditTitle { get; init; } = string.Empty;
    public string SelectedCandidateLabel { get; init; } = string.Empty;
    public string ImageEditPromptLabel { get; init; } = string.Empty;
    public string ImageEditMaskPathLabel { get; init; } = string.Empty;
    public string RunFakeImageEditText { get; init; } = string.Empty;
    public string ImageEditResultText { get; init; } = string.Empty;
    public IReadOnlyList<string> NavigationItems { get; init; } = [];
    public IReadOnlyList<WorkbenchTabViewModel> WorkbenchTabs { get; init; } = [];
    public IReadOnlyList<string> ActivityItems { get; init; } = [];
    public IReadOnlyList<LanguageOptionViewModel> LanguageOptions { get; init; } = [];
    public IReadOnlyList<DocumentStrictnessOptionViewModel> DocumentStrictnessOptions { get; init; } = [];
    public IReadOnlyList<ImageTypePresetOptionViewModel> ImageTypePresetOptions { get; init; } = [];
    public IReadOnlyList<StyleGuideOptionViewModel> StyleGuideOptions { get; init; } = [];
    public IReadOnlyList<GenerationRecipeOptionViewModel> GenerationRecipeOptions { get; init; } = [];
}
