using System.Globalization;
using System.Text.Json;
using ImageSeriesStudio.Application.Localization;
using ImageSeriesStudio.Application.Projects;
using ImageSeriesStudio.App.ViewModels;
using ImageSeriesStudio.Core.Documents;
using ImageSeriesStudio.Core.Projects;
using ImageSeriesStudio.Core.Providers;
using ImageSeriesStudio.Infrastructure.Delivery;
using ImageSeriesStudio.Infrastructure.Fakes;

namespace ImageSeriesStudio.Tests;

public sealed class MainWindowViewModelTests
{
    [Fact]
    public async Task PlanRows_ShowLocalizedSeriesItemKind()
    {
        var viewModel = CreateViewModel();

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
        var viewModel = CreateViewModel();

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
    public async Task ImageEditWorkflow_RunsFakeEditForSelectedGalleryRow()
    {
        var viewModel = CreateViewModel();

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
    public async Task FinalApprovalWorkflow_BlocksDeliveryUntilHumanApprovesReview()
    {
        var viewModel = CreateViewModel();

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

            Assert.Single(viewModel.DeliveryRows);
        }
        finally
        {
            DeleteProjectOutputDirectories(viewModel.SelectedProject?.Id);
        }
    }

    [Fact]
    public async Task ReviewWorkflow_ShowsRepairRouteSummary()
    {
        var viewModel = CreateViewModel(reviewPasses: false);

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
        var viewModel = CreateViewModel();

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
        }
        finally
        {
            DeleteProjectOutputDirectories(viewModel.SelectedProject?.Id);
        }
    }

    [Fact]
    public async Task FinalApprovalWorkflow_RejectsReviewWithNotesAndKeepsDeliveryBlocked()
    {
        var viewModel = CreateViewModel();

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
    public async Task BriefWorkflow_ShowsRecommendationRowsAndPromotesRecommendedSettings()
    {
        var viewModel = CreateViewModel();

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

        await viewModel.RefreshProviderCenterCommand.ExecuteAsync(null);

        Assert.Same(providerCenter, viewModel.ProviderCenter);
        Assert.Equal(
            "Providers ready: text key configured; image keys 4; total image concurrency 40.",
            viewModel.ProviderCenter.SummaryText);
        Assert.Contains(
            viewModel.ProviderCenter.ProviderRows,
            row => row.Title == "Image provider" && row.SecretSummary == "4 keys + app credentials");
    }

    private static MainWindowViewModel CreateViewModel(
        bool reviewPasses = true,
        ProviderCenterViewModel? providerCenter = null)
    {
        var fakeImageProvider = new FakeImageGenerationProvider();

        return new MainWindowViewModel(
            new LocalizationService(() => new CultureInfo("en-US")),
            new ProjectApplicationService(
                new InMemoryProjectRepository(),
                new FakeTextPlanningProvider(),
                fakeImageProvider,
                new FakeVisionReviewProvider(defaultPasses: reviewPasses),
                deliveryPackageWriter: new DeliveryPackageWriter(),
                imageEditProvider: fakeImageProvider),
            providerCenter ?? new ProviderCenterViewModel(
                new StaticProviderCenterConfigurationService(
                    ProviderCenterSnapshot.MissingEnvironmentFile(".env"))));
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

        var appDataDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "ImageSeriesStudio");

        foreach (var folder in new[] { "generated", "edited", "deliveries" })
        {
            var directory = Path.Combine(appDataDirectory, folder, projectId.Value.ToString("N"));
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, recursive: true);
            }
        }
    }

    private sealed class InMemoryProjectRepository : IProjectRepository
    {
        private readonly Dictionary<Guid, ImageProject> _projects = [];

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
    }
}
