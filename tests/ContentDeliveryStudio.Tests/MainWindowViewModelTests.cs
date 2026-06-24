using System.Globalization;
using System.Text.Json;
using ContentDeliveryStudio.Application.Localization;
using ContentDeliveryStudio.Application.Projects;
using ContentDeliveryStudio.Application.Sources;
using ContentDeliveryStudio.App.Services;
using ContentDeliveryStudio.App.ViewModels;
using ContentDeliveryStudio.Core.Documents;
using ContentDeliveryStudio.Core.Projects;
using ContentDeliveryStudio.Core.Providers;
using ContentDeliveryStudio.Core.Sources;
using ContentDeliveryStudio.Infrastructure.Delivery;
using ContentDeliveryStudio.Infrastructure.Fakes;
using ContentDeliveryStudio.Infrastructure.Sources;

namespace ContentDeliveryStudio.Tests;

public sealed class MainWindowViewModelTests
{
    [Fact]
    public async Task PlanRows_ShowLocalizedSeriesItemKind()
    {
        var viewModel = CreateViewModel();
        await viewModel.BackgroundTask;

        viewModel.NewProjectName = "Kind UI demo";
        await viewModel.CreateProjectCommand.ExecuteAsync(null);

        viewModel.NewSeriesTitle = "Storyboard";
        await viewModel.CreateSeriesCommand.ExecuteAsync(null);

        viewModel.NewItemTitle = "Opening panel";
        viewModel.NewItemBrief = "Opening visual for a panel-like sequence.";
        await viewModel.AddItemCommand.ExecuteAsync(null);

        var row = Assert.Single(viewModel.PlanRows);
        Assert.Equal("Kind", viewModel.PlanKindColumn);
        Assert.Equal("Standard", row.KindText);
    }

    [Fact]
    public async Task WorkflowGraphView_ShowsPlanAndCandidateNodes()
    {
        using var localStudioRoot = LocalStudioDataPathScope.Create();
        var viewModel = CreateViewModel();
        await viewModel.BackgroundTask;

        viewModel.NewProjectName = "Graph UI demo";
        await viewModel.CreateProjectCommand.ExecuteAsync(null);

        viewModel.NewSeriesTitle = "Lesson visuals";
        await viewModel.CreateSeriesCommand.ExecuteAsync(null);

        viewModel.NewItemTitle = "Opening frame";
        viewModel.NewItemBrief = "Opening visual for a lesson.";
        await viewModel.AddItemCommand.ExecuteAsync(null);

        viewModel.NewPromptText = "Create a clean opening frame.";
        await viewModel.CreatePromptVersionCommand.ExecuteAsync(null);
        await viewModel.RunFakeGenerationCommand.ExecuteAsync(null);

        try
        {
            Assert.Contains(
                viewModel.WorkbenchTabs,
                tab => tab.Kind is WorkbenchTabKind.Graph && tab.Title == "Graph");
            Assert.True(viewModel.HasWorkflowGraphRows);
            Assert.Contains(viewModel.WorkflowGraphRows, row => row.NodeType == "Project" && row.Title == "Graph UI demo");
            Assert.Contains(viewModel.WorkflowGraphRows, row => row.NodeType == "Series" && row.Title == "Lesson visuals");
            Assert.Contains(viewModel.WorkflowGraphRows, row => row.NodeType == "Item" && row.Title == "Opening frame");
            Assert.Contains(viewModel.WorkflowGraphRows, row => row.NodeType == "Prompt" && row.LinksTo == "Opening frame");
            Assert.Contains(viewModel.WorkflowGraphRows, row => row.NodeType == "Candidate" && row.LinksTo == "Opening frame");
        }
        finally
        {
            DeleteProjectOutputDirectories(viewModel.SelectedProject?.Id);
        }
    }

    [Fact]
    public async Task GalleryRows_TriggerThumbnailWarmupForVisibleCandidates()
    {
        using var localStudioRoot = LocalStudioDataPathScope.Create();
        var warmupService = new CapturingGalleryThumbnailWarmupService();
        var viewModel = CreateViewModel(galleryThumbnailWarmupService: warmupService);
        await viewModel.BackgroundTask;

        viewModel.NewProjectName = "Warmup UI demo";
        await viewModel.CreateProjectCommand.ExecuteAsync(null);

        viewModel.NewSeriesTitle = "Storyboard";
        await viewModel.CreateSeriesCommand.ExecuteAsync(null);

        viewModel.NewItemTitle = "Opening frame";
        viewModel.NewItemBrief = "Opening visual for a short series.";
        await viewModel.AddItemCommand.ExecuteAsync(null);

        viewModel.NewPromptText = "Create a clean opening frame.";
        await viewModel.CreatePromptVersionCommand.ExecuteAsync(null);

        for (var index = 0; index < 30; index++)
        {
            viewModel.NewItemTitle = $"Item {index:000}";
            viewModel.NewItemBrief = $"Brief {index:000}";
            await viewModel.AddItemCommand.ExecuteAsync(null);
            viewModel.NewPromptText = $"Prompt {index:000}";
            await viewModel.CreatePromptVersionCommand.ExecuteAsync(null);
        }

        await viewModel.RunFakeGenerationCommand.ExecuteAsync(null);

        try
        {
            Assert.NotEmpty(warmupService.LastWarmedPaths);
            Assert.True(warmupService.LastWarmedPaths.Count <= 24);
            Assert.All(warmupService.LastWarmedPaths, path => Assert.EndsWith(".png", path, StringComparison.OrdinalIgnoreCase));
        }
        finally
        {
            DeleteProjectOutputDirectories(viewModel.SelectedProject?.Id);
        }
    }

    [Fact]
    public async Task GalleryRows_ReplacesPreviousWarmupWithLatestVisibleCandidates()
    {
        using var localStudioRoot = LocalStudioDataPathScope.Create();
        var warmupService = new BlockingGalleryThumbnailWarmupService();
        var viewModel = CreateViewModel(galleryThumbnailWarmupService: warmupService);
        await viewModel.BackgroundTask;

        viewModel.NewProjectName = "Warmup cancellation demo";
        await viewModel.CreateProjectCommand.ExecuteAsync(null);

        viewModel.NewSeriesTitle = "Storyboard";
        await viewModel.CreateSeriesCommand.ExecuteAsync(null);

        viewModel.NewItemTitle = "First frame";
        viewModel.NewItemBrief = "First visual.";
        await viewModel.AddItemCommand.ExecuteAsync(null);
        viewModel.NewPromptText = "Prompt first.";
        await viewModel.CreatePromptVersionCommand.ExecuteAsync(null);
        await viewModel.RunFakeGenerationCommand.ExecuteAsync(null);

        await warmupService.WaitForInvocationCountAsync(1);
        var firstPaths = warmupService.StartedPaths.Single();

        viewModel.NewItemTitle = "Second frame";
        viewModel.NewItemBrief = "Second visual.";
        await viewModel.AddItemCommand.ExecuteAsync(null);
        viewModel.NewPromptText = "Prompt second.";
        await viewModel.CreatePromptVersionCommand.ExecuteAsync(null);
        await viewModel.RunFakeGenerationCommand.ExecuteAsync(null);

        await warmupService.WaitForInvocationCountAsync(2);
        var secondPaths = warmupService.StartedPaths.Last();

        Assert.NotEqual(firstPaths, secondPaths);
        Assert.Contains(warmupService.CompletedTokens, token => token.IsCancellationRequested);
        Assert.Contains(secondPaths, path => path.Contains("second", StringComparison.OrdinalIgnoreCase));

        warmupService.ReleaseAll();
        await viewModel.BackgroundTask;
        DeleteProjectOutputDirectories(viewModel.SelectedProject?.Id);
    }

    [Fact]
    public async Task ImageEditWorkflow_RunsFakeEditForSelectedGalleryRow()
    {
        using var localStudioRoot = LocalStudioDataPathScope.Create();
        var viewModel = CreateViewModel();
        await viewModel.BackgroundTask;

        viewModel.NewProjectName = "Edit UI demo";
        await viewModel.CreateProjectCommand.ExecuteAsync(null);

        viewModel.NewSeriesTitle = "Storyboard";
        await viewModel.CreateSeriesCommand.ExecuteAsync(null);

        viewModel.NewItemTitle = "Opening frame";
        viewModel.NewItemBrief = "Opening visual for a short series.";
        await viewModel.AddItemCommand.ExecuteAsync(null);

        viewModel.NewPromptText = "Create a clean opening frame.";
        await viewModel.CreatePromptVersionCommand.ExecuteAsync(null);
        await viewModel.RunFakeGenerationCommand.ExecuteAsync(null);

        var sourceRow = Assert.Single(viewModel.GalleryRows);
        Assert.Equal(sourceRow, viewModel.SelectedGalleryRow);

        viewModel.NewImageEditPrompt = "Clean the label area while preserving the composition.";
        Assert.True(viewModel.RunFakeImageEditCommand.CanExecute(null));

        try
        {
            await viewModel.RunFakeImageEditCommand.ExecuteAsync(null);

            Assert.Equal(2, viewModel.GalleryRows.Count);
            var editedRow = viewModel.GalleryRows.Single(row => row.CandidateImageId != sourceRow.CandidateImageId);
            Assert.Equal(sourceRow.SeriesItemId, editedRow.SeriesItemId);
            Assert.Contains("edited", editedRow.ItemTitle, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("Clean the label area", editedRow.PromptText);
            Assert.True(File.Exists(editedRow.AssetPath));
            Assert.True(File.Exists(editedRow.MetadataPath));
            Assert.Contains(viewModel.ImageEditResultText, viewModel.ActivityItems);
        }
        finally
        {
            DeleteProjectOutputDirectories(viewModel.SelectedProject?.Id);
        }
    }

    [Fact]
    public async Task SelectionDerivedSummaries_ShowCurrentItemStyleRecipeAndCandidate()
    {
        using var localStudioRoot = LocalStudioDataPathScope.Create();
        var viewModel = CreateViewModel();
        await viewModel.BackgroundTask;

        viewModel.NewProjectName = "Selection summary demo";
        await viewModel.CreateProjectCommand.ExecuteAsync(null);

        viewModel.NewSeriesTitle = "Storyboard";
        await viewModel.CreateSeriesCommand.ExecuteAsync(null);

        viewModel.NewItemTitle = "Opening frame";
        viewModel.NewItemBrief = "Opening visual for a short series.";
        await viewModel.AddItemCommand.ExecuteAsync(null);

        Assert.Equal("Opening frame", viewModel.SelectedSeriesItemTitleText);
        Assert.Equal("Educational poster / Default editorial guide / Fake standard PNG", viewModel.StyleRecipeSummaryText);

        viewModel.NewPromptText = "Create a clean opening frame.";
        await viewModel.CreatePromptVersionCommand.ExecuteAsync(null);
        await viewModel.RunFakeGenerationCommand.ExecuteAsync(null);

        var selectedCandidate = Assert.Single(viewModel.GalleryRows);
        Assert.Equal(
            $"{selectedCandidate.ItemTitle} ({selectedCandidate.CandidateImageId:N})",
            viewModel.SelectedCandidateSummary);
    }

    [Fact]
    public async Task FinalApprovalWorkflow_BlocksDeliveryUntilHumanApprovesReview()
    {
        using var localStudioRoot = LocalStudioDataPathScope.Create();
        var viewModel = CreateViewModel();
        await viewModel.BackgroundTask;

        viewModel.NewProjectName = "Approval UI demo";
        await viewModel.CreateProjectCommand.ExecuteAsync(null);

        viewModel.NewSeriesTitle = "Approval storyboard";
        await viewModel.CreateSeriesCommand.ExecuteAsync(null);

        viewModel.NewItemTitle = "Opening frame";
        viewModel.NewItemBrief = "Opening visual for approval.";
        await viewModel.AddItemCommand.ExecuteAsync(null);

        viewModel.NewPromptText = "Create a clean opening frame.";
        await viewModel.CreatePromptVersionCommand.ExecuteAsync(null);
        await viewModel.RunFakeGenerationCommand.ExecuteAsync(null);
        await viewModel.RunFakeReviewCommand.ExecuteAsync(null);

        try
        {
            var reviewRow = Assert.Single(viewModel.ReviewRows);
            Assert.False(reviewRow.HumanApproved);
            Assert.Equal("None", reviewRow.RouteSummary);
            Assert.False(viewModel.ExportDeliveryCommand.CanExecute(null));

            viewModel.SelectedReviewRow = reviewRow;
            viewModel.FinalApprovalReviewer = "Teacher";
            viewModel.FinalApprovalNotes = "Looks ready for delivery.";

            Assert.True(viewModel.ApproveSelectedReviewCommand.CanExecute(null));
            await viewModel.ApproveSelectedReviewCommand.ExecuteAsync(null);

            var approvedRow = Assert.Single(viewModel.ReviewRows);
            Assert.True(approvedRow.HumanApproved);
            Assert.Equal("Teacher", approvedRow.FinalReviewer);
            Assert.Contains("approved", approvedRow.HumanApprovalStatus, StringComparison.OrdinalIgnoreCase);
            Assert.True(viewModel.ExportDeliveryCommand.CanExecute(null));

            await viewModel.ExportDeliveryCommand.ExecuteAsync(null);

            var delivery = Assert.Single(viewModel.DeliveryRows);
            using var manifestStream = File.OpenRead(delivery.ManifestJsonPath);
            using var manifest = await JsonDocument.ParseAsync(manifestStream, cancellationToken: CancellationToken.None);
            var item = Assert.Single(manifest.RootElement.GetProperty("items").EnumerateArray());

            Assert.Equal("Teacher", item.GetProperty("finalReviewer").GetString());
            Assert.Equal("Looks ready for delivery.", item.GetProperty("finalApprovalNotes").GetString());
        }
        finally
        {
            DeleteProjectOutputDirectories(viewModel.SelectedProject?.Id);
        }
    }

    [Fact]
    public async Task ReviewWorkflow_ShowsRepairRouteSummary()
    {
        using var localStudioRoot = LocalStudioDataPathScope.Create();
        var viewModel = CreateViewModel(reviewPasses: false);
        await viewModel.BackgroundTask;

        viewModel.NewProjectName = "Review route UI demo";
        await viewModel.CreateProjectCommand.ExecuteAsync(null);

        viewModel.NewSeriesTitle = "Repair storyboard";
        await viewModel.CreateSeriesCommand.ExecuteAsync(null);

        viewModel.NewItemTitle = "Opening frame";
        viewModel.NewItemBrief = "Opening visual that will fail fake review.";
        await viewModel.AddItemCommand.ExecuteAsync(null);

        viewModel.NewPromptText = "Create a frame that will be routed for repair.";
        await viewModel.CreatePromptVersionCommand.ExecuteAsync(null);
        await viewModel.RunFakeGenerationCommand.ExecuteAsync(null);

        try
        {
            await viewModel.RunFakeReviewCommand.ExecuteAsync(null);

            var reviewRow = Assert.Single(viewModel.ReviewRows);
            Assert.Equal("Repair route", viewModel.ReviewRouteColumn);
            Assert.Contains("Brief", reviewRow.RouteSummary);
            Assert.Contains("Regenerate", reviewRow.RouteSummary);
        }
        finally
        {
            DeleteProjectOutputDirectories(viewModel.SelectedProject?.Id);
        }
    }

    [Fact]
    public async Task FinalApprovalWorkflow_ExportsPromotedBlueprintMetadata()
    {
        using var localStudioRoot = LocalStudioDataPathScope.Create();
        var viewModel = CreateViewModel();
        await viewModel.BackgroundTask;

        viewModel.NewProjectName = "Blueprint delivery demo";
        await viewModel.CreateProjectCommand.ExecuteAsync(null);

        viewModel.NewSeriesTitle = "Panel storyboard";
        await viewModel.CreateSeriesCommand.ExecuteAsync(null);

        viewModel.NewPlanningGoal = "panel narrative sequence";
        viewModel.NewPlanningAudience = "teachers";
        viewModel.NewPlanningStyleBrief = "consistent panel storytelling";
        await viewModel.GenerateDesignBlueprintsCommand.ExecuteAsync(null);

        viewModel.SelectedDesignBlueprint = viewModel.DesignBlueprintRows
            .Single(row => row.Key == "panel-narrative-sequence");
        await viewModel.PromoteDesignBlueprintCommand.ExecuteAsync(null);

        viewModel.NewItemTitle = "Opening panel";
        viewModel.NewItemBrief = "Opening visual for a panel-like sequence.";
        await viewModel.AddItemCommand.ExecuteAsync(null);

        viewModel.NewPromptText = "Create a consistent opening panel.";
        await viewModel.CreatePromptVersionCommand.ExecuteAsync(null);
        await viewModel.RunFakeGenerationCommand.ExecuteAsync(null);
        await viewModel.RunFakeReviewCommand.ExecuteAsync(null);

        try
        {
            viewModel.SelectedReviewRow = Assert.Single(viewModel.ReviewRows);
            viewModel.FinalApprovalReviewer = "Teacher";
            viewModel.FinalApprovalNotes = "Ready for package.";
            await viewModel.ApproveSelectedReviewCommand.ExecuteAsync(null);
            await viewModel.ExportDeliveryCommand.ExecuteAsync(null);

            var delivery = Assert.Single(viewModel.DeliveryRows);
            using var manifestStream = File.OpenRead(delivery.ManifestJsonPath);
            using var manifest = await JsonDocument.ParseAsync(manifestStream, cancellationToken: CancellationToken.None);
            var item = Assert.Single(manifest.RootElement.GetProperty("items").EnumerateArray());
            var blueprint = item.GetProperty("blueprint");

            Assert.Equal("panel-narrative-sequence", blueprint.GetProperty("key").GetString());
            Assert.Equal("Panel narrative sequence", blueprint.GetProperty("displayName").GetString());
            Assert.Equal("panel sequence", blueprint.GetProperty("sequenceMode").GetString());
            Assert.Contains("consistent panel storytelling", blueprint.GetProperty("consistencySummary").GetString());
            Assert.Equal("Teacher", item.GetProperty("finalReviewer").GetString());
            Assert.Equal("Ready for package.", item.GetProperty("finalApprovalNotes").GetString());
            Assert.True(item.GetProperty("finalApprovalDecidedAt").GetDateTimeOffset() > DateTimeOffset.MinValue);
        }
        finally
        {
            DeleteProjectOutputDirectories(viewModel.SelectedProject?.Id);
        }
    }

    [Fact]
    public async Task FinalApprovalWorkflow_RejectsReviewWithNotesAndKeepsDeliveryBlocked()
    {
        using var localStudioRoot = LocalStudioDataPathScope.Create();
        var viewModel = CreateViewModel();
        await viewModel.BackgroundTask;

        viewModel.NewProjectName = "Reject UI demo";
        await viewModel.CreateProjectCommand.ExecuteAsync(null);

        viewModel.NewSeriesTitle = "Reject storyboard";
        await viewModel.CreateSeriesCommand.ExecuteAsync(null);

        viewModel.NewItemTitle = "Opening frame";
        viewModel.NewItemBrief = "Opening visual for rejection.";
        await viewModel.AddItemCommand.ExecuteAsync(null);

        viewModel.NewPromptText = "Create a clean opening frame.";
        await viewModel.CreatePromptVersionCommand.ExecuteAsync(null);
        await viewModel.RunFakeGenerationCommand.ExecuteAsync(null);
        await viewModel.RunFakeReviewCommand.ExecuteAsync(null);

        try
        {
            var reviewRow = Assert.Single(viewModel.ReviewRows);
            viewModel.SelectedReviewRow = reviewRow;
            viewModel.FinalApprovalReviewer = "Teacher";
            viewModel.FinalApprovalNotes = "Needs a clearer composition.";

            Assert.True(viewModel.RejectSelectedReviewCommand.CanExecute(null));
            await viewModel.RejectSelectedReviewCommand.ExecuteAsync(null);

            var rejectedRow = Assert.Single(viewModel.ReviewRows);
            Assert.False(rejectedRow.HumanApproved);
            Assert.Equal("Teacher", rejectedRow.FinalReviewer);
            Assert.Contains("rejected", rejectedRow.HumanApprovalStatus, StringComparison.OrdinalIgnoreCase);
            Assert.False(viewModel.ExportDeliveryCommand.CanExecute(null));
        }
        finally
        {
            DeleteProjectOutputDirectories(viewModel.SelectedProject?.Id);
        }
    }

    [Fact]
    public async Task FinalApprovalWorkflow_ReloadedProjectRestoresPersistedHumanDecision()
    {
        using var localStudioRoot = LocalStudioDataPathScope.Create();
        var repository = new InMemoryProjectRepository();
        var viewModel = CreateViewModel(repository: repository);
        await viewModel.BackgroundTask;

        viewModel.NewProjectName = "Reload approval demo";
        await viewModel.CreateProjectCommand.ExecuteAsync(null);

        viewModel.NewSeriesTitle = "Reload storyboard";
        await viewModel.CreateSeriesCommand.ExecuteAsync(null);

        viewModel.NewItemTitle = "Opening frame";
        viewModel.NewItemBrief = "Opening visual for reload.";
        await viewModel.AddItemCommand.ExecuteAsync(null);

        viewModel.NewPromptText = "Create a clean opening frame.";
        await viewModel.CreatePromptVersionCommand.ExecuteAsync(null);
        await viewModel.RunFakeGenerationCommand.ExecuteAsync(null);
        await viewModel.RunFakeReviewCommand.ExecuteAsync(null);

        try
        {
            viewModel.SelectedReviewRow = Assert.Single(viewModel.ReviewRows);
            viewModel.FinalApprovalReviewer = "Teacher";
            viewModel.FinalApprovalNotes = "Approved after reload test.";
            await viewModel.ApproveSelectedReviewCommand.ExecuteAsync(null);

            viewModel.SelectedProject = null;
            await WaitForConditionAsync(() => viewModel.ReviewRows.Count == 0 && viewModel.GalleryRows.Count == 0);

            viewModel.SelectedProject = Assert.Single(viewModel.Projects);
            await WaitForConditionAsync(() => viewModel.ReviewRows.Count == 1 && viewModel.GalleryRows.Count == 1);

            var restoredRow = Assert.Single(viewModel.ReviewRows);
            Assert.True(restoredRow.HumanApproved);
            Assert.Equal("Teacher", restoredRow.FinalReviewer);
            Assert.Equal("Approved after reload test.", restoredRow.FinalApprovalNotes);
        }
        finally
        {
            DeleteProjectOutputDirectories(viewModel.SelectedProject?.Id);
        }
    }

    [Fact]
    public async Task BriefWorkflow_ShowsRecommendationRowsAndPromotesRecommendedSettings()
    {
        var viewModel = CreateViewModel();
        await viewModel.BackgroundTask;

        viewModel.NewProjectName = "Brief UI demo";
        await viewModel.CreateProjectCommand.ExecuteAsync(null);

        viewModel.NewSeriesTitle = "Article images";
        await viewModel.CreateSeriesCommand.ExecuteAsync(null);

        viewModel.NewItemTitle = "Opening";
        viewModel.NewItemBrief = "Opening visual for teachers.";
        await viewModel.AddItemCommand.ExecuteAsync(null);

        viewModel.NewPlanningGoal = "article illustration";
        viewModel.NewPlanningAudience = "teachers";
        viewModel.NewPlanningStyleBrief = "clean editorial";

        await viewModel.CreateBriefCommand.ExecuteAsync(null);
        await viewModel.GeneratePromptDirectionsCommand.ExecuteAsync(null);

        var row = Assert.Single(
            viewModel.PromptDirectionRows,
            direction => direction.DirectionKey == "conservative");
        Assert.Contains("article-inline-illustration", row.RecommendationSummary);
        Assert.Contains("1536x1024", row.RecommendationSummary);
        Assert.Contains("draft", row.RecommendationSummary);
        Assert.Contains("fake provider warning", row.CapabilityWarningSummary);

        viewModel.SelectedPromptDirection = row;
        await viewModel.PromotePromptDirectionCommand.ExecuteAsync(null);

        var promptRow = Assert.Single(viewModel.PromptRows);
        Assert.Equal("1536x1024 draft png", promptRow.SettingsSummary);
    }

    [Fact]
    public async Task BriefWorkflow_ShowsBlueprintRowsAndPromotesSelectedBlueprint()
    {
        var viewModel = CreateViewModel();
        await viewModel.BackgroundTask;

        viewModel.NewProjectName = "Blueprint UI demo";
        await viewModel.CreateProjectCommand.ExecuteAsync(null);

        viewModel.NewSeriesTitle = "Storyboard images";
        await viewModel.CreateSeriesCommand.ExecuteAsync(null);

        viewModel.NewItemTitle = "Opening";
        viewModel.NewItemBrief = "Opening visual with the same main character.";
        await viewModel.AddItemCommand.ExecuteAsync(null);

        viewModel.NewPlanningGoal = "panel story sequence";
        viewModel.NewPlanningAudience = "students";
        viewModel.NewPlanningStyleBrief = "clear visual storytelling";

        await viewModel.CreateBriefCommand.ExecuteAsync(null);

        Assert.True(viewModel.GenerateDesignBlueprintsCommand.CanExecute(null));
        await viewModel.GenerateDesignBlueprintsCommand.ExecuteAsync(null);

        var row = Assert.Single(
            viewModel.DesignBlueprintRows,
            blueprint => blueprint.Key == "panel-narrative-sequence");
        Assert.True(viewModel.HasDesignBlueprintRows);
        Assert.True(row.SequenceMode.Contains("panel", StringComparison.OrdinalIgnoreCase));
        Assert.Contains("same main character", row.ConsistencySummary, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("Candidate", row.PromotionStatus);

        viewModel.SelectedDesignBlueprint = row;
        Assert.True(viewModel.PromoteDesignBlueprintCommand.CanExecute(null));
        await viewModel.PromoteDesignBlueprintCommand.ExecuteAsync(null);

        var promotedRow = Assert.Single(
            viewModel.DesignBlueprintRows,
            blueprint => blueprint.Key == "panel-narrative-sequence");
        Assert.True(promotedRow.IsPromoted);
        Assert.Equal("Promoted", promotedRow.PromotionStatus);
        Assert.Equal(promotedRow.BlueprintId, viewModel.SelectedDesignBlueprint?.BlueprintId);
    }

    [Fact]
    public async Task DocumentIllustrationWorkflow_RunsFakePlanningFromInspectorInputs()
    {
        var viewModel = CreateViewModel();
        await viewModel.BackgroundTask;

        viewModel.NewProjectName = "Document UI demo";
        await viewModel.CreateProjectCommand.ExecuteAsync(null);

        viewModel.NewDocumentSourceText = "Teachers need a clean visual explanation of superposition.";
        viewModel.NewDocumentAudience = "physics teachers";
        viewModel.SelectedDocumentStrictnessOption = viewModel.DocumentStrictnessOptions
            .Single(option => option.Value == IllustrationStrictnessLevel.ScholarlyDraft);

        Assert.True(viewModel.RunFakeDocumentPlanningCommand.CanExecute(null));

        await viewModel.RunFakeDocumentPlanningCommand.ExecuteAsync(null);

        Assert.NotEmpty(viewModel.DocumentPlanningResultSummary);
        Assert.Contains("Approved targets:", viewModel.DocumentPlanningResultSummary);
        Assert.Contains(viewModel.DocumentPlanningResultSummary, viewModel.ActivityItems);
        Assert.Single(viewModel.Series);
        Assert.NotEmpty(viewModel.PlanRows);
        Assert.NotEmpty(viewModel.PromptRows);
        Assert.Contains(
            viewModel.PromptRows,
            row => row.PromptText.Contains(
                "Do not imply real experimental, clinical, archival, or field evidence",
                StringComparison.OrdinalIgnoreCase));
        Assert.Contains(viewModel.WorkflowGraphRows, row => row.NodeType == "Series" && row.Title.Contains("Document illustrations"));
        Assert.Contains(viewModel.WorkflowGraphRows, row => row.NodeType == "Prompt");
    }

    [Fact]
    public async Task DocumentIllustrationWorkflow_ImportsPdfSourceTextIntoInspectorInput()
    {
        var repository = new InMemoryProjectRepository();
        var rootDirectory = Path.Combine(Path.GetTempPath(), "ContentDeliveryStudio.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(rootDirectory);
        var pdfPath = Path.Combine(rootDirectory, "lesson.pdf");

        try
        {
            await BinaryDocumentTestFixtureBuilder.CreateSimplePdfAsync(
                pdfPath,
                "Imported PDF text should populate the document illustration source box.",
                CancellationToken.None);
            var viewModel = CreateViewModel(
                repository: repository,
                projectService: CreateProjectService(repository));
            await viewModel.BackgroundTask;

            viewModel.NewProjectName = "Import document source demo";
            await viewModel.CreateProjectCommand.ExecuteAsync(null);

            await viewModel.ImportDocumentSourceFileCommand.ExecuteAsync(pdfPath);

            Assert.Contains("Imported PDF text", viewModel.NewDocumentSourceText, StringComparison.OrdinalIgnoreCase);
            Assert.Equal(pdfPath, viewModel.ImportedDocumentSourcePath);
            Assert.True(viewModel.RunFakeDocumentPlanningCommand.CanExecute(null));
        }
        finally
        {
            if (Directory.Exists(rootDirectory))
            {
                Directory.Delete(rootDirectory, recursive: true);
            }
        }
    }

    [Fact]
    public async Task DocumentIllustrationWorkflow_BrowseDocumentSourceFileImportsPdfTextIntoInspectorInput()
    {
        var repository = new InMemoryProjectRepository();
        var rootDirectory = Path.Combine(Path.GetTempPath(), "ContentDeliveryStudio.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(rootDirectory);
        var pdfPath = Path.Combine(rootDirectory, "browse.pdf");

        try
        {
            await BinaryDocumentTestFixtureBuilder.CreateSimplePdfAsync(
                pdfPath,
                "Browse import should populate the source box from a local PDF.",
                CancellationToken.None);
            var viewModel = CreateViewModel(
                repository: repository,
                projectService: CreateProjectService(repository),
                documentSourceFilePickerService: new StaticDocumentSourceFilePickerService(pdfPath));
            await viewModel.BackgroundTask;

            viewModel.NewProjectName = "Browse document source demo";
            await viewModel.CreateProjectCommand.ExecuteAsync(null);

            await viewModel.BrowseDocumentSourceFileCommand.ExecuteAsync(null);

            Assert.Contains("Browse import", viewModel.NewDocumentSourceText, StringComparison.OrdinalIgnoreCase);
            Assert.Equal(pdfPath, viewModel.ImportedDocumentSourcePath);
            Assert.Equal(pdfPath, viewModel.NewDocumentSourceFilePath);
        }
        finally
        {
            if (Directory.Exists(rootDirectory))
            {
                Directory.Delete(rootDirectory, recursive: true);
            }
        }
    }

    [Fact]
    public async Task ProviderCenter_CanRefreshConfigurationFromMainWindow()
    {
        var providerCenter = new ProviderCenterViewModel(
            new StaticProviderCenterConfigurationService(
                new ProviderCenterSnapshot(
                    new ProviderEndpointConfigurationSnapshot(
                        "Text provider",
                        "TEXT_PROVIDER",
                        "openai_compatible",
                        "https://text.example/v1",
                        "gpt-5.5",
                        1,
                        UsesAppCredentials: false,
                        ConcurrencyPerKey: 1,
                        TotalConcurrency: 1),
                    new ProviderEndpointConfigurationSnapshot(
                        "Image provider",
                        "IMAGE_PROVIDER",
                        "openai_compatible_image_only",
                        "https://image.example/v1",
                        "image-model",
                        4,
                        UsesAppCredentials: true,
                        ConcurrencyPerKey: 10,
                        TotalConcurrency: 40),
                    [])));
        var viewModel = CreateViewModel(providerCenter: providerCenter);
        await viewModel.BackgroundTask;

        await viewModel.RefreshProviderCenterCommand.ExecuteAsync(null);

        Assert.Same(providerCenter, viewModel.ProviderCenter);
        Assert.Equal(
            "Providers ready: text key configured; image keys 4; total image concurrency 40.",
            viewModel.ProviderCenter.SummaryText);
        Assert.Contains(
            viewModel.ProviderCenter.ProviderRows,
            row => row.Title == "Image provider" && row.SecretSummary == "4 keys + app credentials");
    }

    [Fact]
    public void LanguageSwitch_RefreshesLocalizedShellPayload()
    {
        var viewModel = CreateViewModel();

        viewModel.SelectedDocumentStrictnessOption = viewModel.DocumentStrictnessOptions
            .Single(option => option.Value == IllustrationStrictnessLevel.ScholarlyDraft);

        viewModel.SelectedLanguageOption = viewModel.LanguageOptions
            .Single(option => option.Preference == LanguagePreference.Chinese);

        Assert.Equal("语言", viewModel.LanguageLabel);
        Assert.Equal(["工作区", "项目", "设置"], viewModel.NavigationItems);
        Assert.Equal("需求设计", viewModel.WorkbenchTabs.First(tab => tab.Kind is WorkbenchTabKind.Brief).Title);
        Assert.Equal("图视图", viewModel.WorkbenchTabs.First(tab => tab.Kind is WorkbenchTabKind.Graph).Title);
        Assert.Equal(["跟随系统", "中文", "英文"], viewModel.LanguageOptions.Select(option => option.DisplayName));
        Assert.Equal("学术草稿", viewModel.SelectedDocumentStrictnessOption?.DisplayName);
    }

    [Fact]
    public async Task StartupRefreshFailure_IsObservedWithoutThrowingFromConstructor()
    {
        var viewModel = CreateViewModel(repository: new ThrowingProjectRepository());

        await viewModel.BackgroundTask;

        Assert.Empty(viewModel.Projects);
    }

    private static MainWindowViewModel CreateViewModel(
        bool reviewPasses = true,
        ProviderCenterViewModel? providerCenter = null,
        IProjectRepository? repository = null,
        ProjectApplicationService? projectService = null,
        GalleryThumbnailWarmupService? galleryThumbnailWarmupService = null,
        IDocumentSourceFilePickerService? documentSourceFilePickerService = null)
    {
        var fakeImageProvider = new FakeImageGenerationProvider();

        return new MainWindowViewModel(
            new LocalizationService(() => new CultureInfo("en-US")),
            projectService ?? new ProjectApplicationService(
                repository ?? new InMemoryProjectRepository(),
                new FakeTextPlanningProvider(),
                fakeImageProvider,
                new FakeVisionReviewProvider(defaultPasses: reviewPasses),
                deliveryPackageWriter: new DeliveryPackageWriter(),
                imageEditProvider: fakeImageProvider),
            providerCenter ?? new ProviderCenterViewModel(
                new StaticProviderCenterConfigurationService(
                    ProviderCenterSnapshot.MissingEnvironmentFile(".env"))),
            galleryThumbnailWarmupService ?? new NoopGalleryThumbnailWarmupService(),
            documentSourceFilePickerService);
    }

    private static ProjectApplicationService CreateProjectService(IProjectRepository repository)
    {
        var fakeImageProvider = new FakeImageGenerationProvider();

        return new ProjectApplicationService(
            repository,
            new FakeTextPlanningProvider(),
            fakeImageProvider,
            new FakeVisionReviewProvider(defaultPasses: true),
            deliveryPackageWriter: new DeliveryPackageWriter(),
            imageEditProvider: fakeImageProvider,
                sourceIngestionApplicationService: new SourceIngestionApplicationService(
                repository,
                new SupportMatrixSourceIngestionProvider(
                    new LocalBinaryDocumentExtractionProvider(),
                    new FakeSourceIngestionProvider())));
    }

    private sealed class StaticDocumentSourceFilePickerService(string filePath)
        : IDocumentSourceFilePickerService
    {
        public Task<string?> PickAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            return Task.FromResult<string?>(filePath);
        }
    }

    private sealed class CapturingGalleryThumbnailWarmupService : GalleryThumbnailWarmupService
    {
        public IReadOnlyList<string> LastWarmedPaths { get; private set; } = Array.Empty<string>();

        public override Task WarmupAsync(IEnumerable<string> assetPaths, CancellationToken cancellationToken)
        {
            LastWarmedPaths = assetPaths.Take(24).ToArray();
            return Task.CompletedTask;
        }
    }

    private sealed class BlockingGalleryThumbnailWarmupService : GalleryThumbnailWarmupService
    {
        private readonly List<IReadOnlyList<string>> _startedPaths = [];
        private readonly List<CancellationToken> _completedTokens = [];
        private readonly List<TaskCompletionSource> _releaseSignals = [];

        public IReadOnlyList<IReadOnlyList<string>> StartedPaths => _startedPaths;

        public IReadOnlyList<CancellationToken> CompletedTokens => _completedTokens;

        public override async Task WarmupAsync(IEnumerable<string> assetPaths, CancellationToken cancellationToken)
        {
            var capturedPaths = assetPaths.ToArray();
            var releaseSignal = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

            lock (_startedPaths)
            {
                _startedPaths.Add(capturedPaths);
                _releaseSignals.Add(releaseSignal);
            }

            try
            {
                await releaseSignal.Task.WaitAsync(cancellationToken);
            }
            finally
            {
                lock (_completedTokens)
                {
                    _completedTokens.Add(cancellationToken);
                }
            }
        }

        public async Task WaitForInvocationCountAsync(int expectedCount, int timeoutMs = 2000)
        {
            await WaitForConditionAsync(() =>
            {
                lock (_startedPaths)
                {
                    return _startedPaths.Count >= expectedCount;
                }
            }, timeoutMs);
        }

        public void ReleaseAll()
        {
            lock (_releaseSignals)
            {
                foreach (var signal in _releaseSignals)
                {
                    signal.TrySetResult();
                }
            }
        }
    }

    private sealed class NoopGalleryThumbnailWarmupService : GalleryThumbnailWarmupService
    {
        public override Task WarmupAsync(IEnumerable<string> assetPaths, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }

    private sealed class ThrowingProjectRepository : IProjectRepository
    {
        public Task SaveAsync(ImageProject project, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public Task<ImageProject?> LoadAsync(Guid projectId, CancellationToken cancellationToken)
        {
            return Task.FromResult<ImageProject?>(null);
        }

        public Task<IReadOnlyList<ProjectSummary>> ListAsync(CancellationToken cancellationToken)
        {
            throw new InvalidOperationException("Simulated list failure.");
        }

        public Task SaveReviewResultAsync(Guid projectId, ReviewResult reviewResult, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public Task<ReviewResult?> LoadLatestReviewResultAsync(Guid candidateImageId, CancellationToken cancellationToken)
        {
            return Task.FromResult<ReviewResult?>(null);
        }
    }

    private static async Task WaitForConditionAsync(Func<bool> condition, int timeoutMs = 2000)
    {
        var startedAt = DateTime.UtcNow;

        while (!condition())
        {
            if ((DateTime.UtcNow - startedAt).TotalMilliseconds > timeoutMs)
            {
                throw new TimeoutException("Condition was not met before timeout.");
            }

            await Task.Delay(25);
        }
    }

    private sealed class StaticProviderCenterConfigurationService(ProviderCenterSnapshot snapshot)
        : IProviderCenterConfigurationService
    {
        public Task<ProviderCenterSnapshot> LoadAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            return Task.FromResult(snapshot);
        }
    }

    private static void DeleteProjectOutputDirectories(Guid? projectId)
    {
        if (projectId is null)
        {
            return;
        }

        foreach (var folder in new[] { "generated", "edited", "deliveries", "review-prep" })
        {
            var directory = LocalStudioDataPaths.ResolveProjectDirectory(folder, projectId.Value);
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, recursive: true);
            }
        }
    }

    private sealed class InMemoryProjectRepository : IProjectRepository
    {
        private readonly Dictionary<Guid, ImageProject> _projects = [];
        private readonly Dictionary<Guid, ReviewResult> _reviewResultsByCandidateId = [];

        public Task SaveAsync(ImageProject project, CancellationToken cancellationToken)
        {
            _projects[project.Id] = project;
            return Task.CompletedTask;
        }

        public Task<ImageProject?> LoadAsync(Guid projectId, CancellationToken cancellationToken)
        {
            _projects.TryGetValue(projectId, out var project);
            return Task.FromResult<ImageProject?>(project);
        }

        public Task<IReadOnlyList<ProjectSummary>> ListAsync(CancellationToken cancellationToken)
        {
            var summaries = _projects.Values
                .Select(project => new ProjectSummary(
                    project.Id,
                    project.Name,
                    project.CreatedAt,
                    project.UpdatedAt))
                .OrderByDescending(project => project.UpdatedAt)
                .ToArray();

            return Task.FromResult<IReadOnlyList<ProjectSummary>>(summaries);
        }

        public Task SaveReviewResultAsync(Guid projectId, ReviewResult reviewResult, CancellationToken cancellationToken)
        {
            if (_projects.TryGetValue(projectId, out var project))
            {
                project.UpsertReviewResult(
                    reviewResult,
                    reviewResult.FinalApprovalDecidedAt ?? reviewResult.CreatedAt);
            }

            _reviewResultsByCandidateId[reviewResult.CandidateImageId] = reviewResult;
            return Task.CompletedTask;
        }

        public Task<ReviewResult?> LoadLatestReviewResultAsync(Guid candidateImageId, CancellationToken cancellationToken)
        {
            _reviewResultsByCandidateId.TryGetValue(candidateImageId, out var review);
            return Task.FromResult<ReviewResult?>(review);
        }
    }
}

