using System.Globalization;
using ContentDeliveryStudio.Application.Localization;
using ContentDeliveryStudio.Application.Projects;
using ContentDeliveryStudio.App.ViewModels;
using ContentDeliveryStudio.Core.Projects;
using ContentDeliveryStudio.Core.Styles;
using ContentDeliveryStudio.Infrastructure.Fakes;

namespace ContentDeliveryStudio.Tests;

public sealed class ProjectWorkbenchStateCoordinatorTests
{
    [Fact]
    public async Task LoadAsync_BuildsWorkbenchStateAndRestoresActiveBriefSelections()
    {
        var repository = new InMemoryProjectRepository();
        var localizationService = new LocalizationService(() => new CultureInfo("en-US"));
        var projectService = new ProjectApplicationService(repository, new FakeTextPlanningProvider());
        var projectionCoordinator = new ProjectWorkbenchProjectionCoordinator(localizationService, projectService);
        var coordinator = new ProjectWorkbenchStateCoordinator(localizationService, projectService, projectionCoordinator);
        var timestamp = DateTimeOffset.Parse("2026-06-14T08:00:00Z");
        var project = await projectService.CreateProjectAsync("Workbench state demo", timestamp, CancellationToken.None);
        var series = await projectService.AddSeriesAsync(
            project.Id,
            "Lesson visuals",
            "Series",
            timestamp.AddMinutes(1),
            CancellationToken.None);
        var item = await projectService.AddItemAsync(
            project.Id,
            series.Id,
            "Opening frame",
            "Opening visual for a lesson.",
            timestamp.AddMinutes(2),
            CancellationToken.None);
        await projectService.AddPromptVersionAsync(
            project.Id,
            item.Id,
            "Create a clean opening frame.",
            new GenerationSettings(1024, 1024, "standard", "png"),
            providerProfileId: null,
            timestamp.AddMinutes(3),
            CancellationToken.None);

        var firstBrief = await projectService.CreateCreativeBriefAsync(
            project.Id,
            series.Id,
            "article illustration",
            "teachers",
            ImageTextPolicy.Hybrid,
            "clean editorial",
            ["Opening frame"],
            ["unreadable small text"],
            timestamp.AddMinutes(4),
            CancellationToken.None);
        await projectService.CreatePromptDirectionsAsync(
            project.Id,
            firstBrief.Id,
            timestamp.AddMinutes(5),
            CancellationToken.None);
        await projectService.CreateDesignBlueprintsAsync(
            project.Id,
            firstBrief.Id,
            timestamp.AddMinutes(6),
            CancellationToken.None);

        var secondBrief = await projectService.CreateCreativeBriefAsync(
            project.Id,
            series.Id,
            "panel narrative sequence",
            "students",
            ImageTextPolicy.Hybrid,
            "clear visual storytelling",
            ["Opening frame"],
            ["unreadable small text"],
            timestamp.AddMinutes(7),
            CancellationToken.None);
        await projectService.CreatePromptDirectionsAsync(
            project.Id,
            secondBrief.Id,
            timestamp.AddMinutes(8),
            CancellationToken.None);
        await projectService.CreateDesignBlueprintsAsync(
            project.Id,
            secondBrief.Id,
            timestamp.AddMinutes(9),
            CancellationToken.None);

        var state = await coordinator.LoadAsync(
            project.Id,
            series.Id,
            item.Id,
            secondBrief.Id,
            noItemsInSeriesText: "No items",
            CancellationToken.None);

        Assert.Equal(series.Id, state.SelectedSeries?.Id);
        Assert.Equal(item.Id, state.SelectedSeriesItem?.Id);
        Assert.Single(state.PlanRows);
        Assert.Single(state.PromptRows);
        Assert.Contains(state.DesignBlueprintRows, row => row.CreativeBriefId == firstBrief.Id);
        Assert.Contains(state.DesignBlueprintRows, row => row.CreativeBriefId == secondBrief.Id);
        Assert.Contains(state.PromptDirectionRows, row => row.CreativeBriefId == firstBrief.Id);
        Assert.Contains(state.PromptDirectionRows, row => row.CreativeBriefId == secondBrief.Id);
        Assert.Equal(secondBrief.Id, state.SelectedDesignBlueprint?.CreativeBriefId);
        Assert.Equal(secondBrief.Id, state.SelectedPromptDirection?.CreativeBriefId);
    }

    [Fact]
    public async Task LoadAsync_ReturnsEmptyStateForMissingProject()
    {
        var localizationService = new LocalizationService(() => new CultureInfo("en-US"));
        var projectService = new ProjectApplicationService(new InMemoryProjectRepository(), new FakeTextPlanningProvider());
        var projectionCoordinator = new ProjectWorkbenchProjectionCoordinator(localizationService, projectService);
        var coordinator = new ProjectWorkbenchStateCoordinator(localizationService, projectService, projectionCoordinator);

        var state = await coordinator.LoadAsync(
            Guid.NewGuid(),
            selectedSeriesId: null,
            selectedSeriesItemId: null,
            activeCreativeBriefId: null,
            noItemsInSeriesText: "No items",
            CancellationToken.None);

        Assert.Empty(state.Series);
        Assert.Empty(state.PlanRows);
        Assert.Empty(state.PromptRows);
        Assert.Empty(state.QueueRows);
        Assert.Empty(state.GalleryRows);
        Assert.Empty(state.ReviewRows);
        Assert.Empty(state.DeliveryRows);
        Assert.Null(state.SelectedSeries);
        Assert.Null(state.SelectedSeriesItem);
        Assert.Null(state.SelectedDesignBlueprint);
        Assert.Null(state.SelectedPromptDirection);
    }

    [Fact]
    public async Task LoadAsync_RestoresPersistedQueueRowsAndCandidatePath()
    {
        var repository = new InMemoryProjectRepository();
        var localizationService = new LocalizationService(() => new CultureInfo("en-US"));
        var projectService = new ProjectApplicationService(repository, new FakeTextPlanningProvider());
        var projectionCoordinator = new ProjectWorkbenchProjectionCoordinator(localizationService, projectService);
        var coordinator = new ProjectWorkbenchStateCoordinator(localizationService, projectService, projectionCoordinator);
        var timestamp = DateTimeOffset.Parse("2026-07-28T18:00:00Z");
        var project = ImageProject.Create("Queue projection", timestamp);
        var series = project.AddSeries("Series", "Queue projection", timestamp.AddMinutes(1));
        var item = series.AddItem("Opening", "Opening", timestamp.AddMinutes(2));
        var profile = project.AddProviderProfile("Fake provider", ProviderKind.Fake, timestamp.AddMinutes(3));
        var prompt = item.AddPromptVersion(
            "Queue prompt",
            new GenerationSettings(1024, 1024, "standard", "png"),
            profile.Id,
            timestamp.AddMinutes(4));
        var laterItem = series.AddItem("Closing", "Closing", timestamp.AddMinutes(5));
        var laterPrompt = laterItem.AddPromptVersion(
            "Closing prompt",
            new GenerationSettings(1024, 1024, "standard", "png"),
            profile.Id,
            timestamp.AddMinutes(6));
        laterItem.AddGenerationTask(
            new GenerationTask(
                Guid.NewGuid(), laterItem.Id, laterPrompt.Id, profile.Id,
                GenerationTaskStatus.Cancelled, 0, 0,
                timestamp.AddMinutes(7), timestamp.AddMinutes(7),
                "Cancelled before dispatch."),
            timestamp.AddMinutes(7));
        var succeeded = item.AddGenerationTask(
            new GenerationTask(
                Guid.NewGuid(), item.Id, prompt.Id, profile.Id,
                GenerationTaskStatus.Succeeded, 1, 0,
                timestamp.AddMinutes(8), timestamp.AddMinutes(8)),
            timestamp.AddMinutes(8));
        item.AddCandidateImage(
            new CandidateImage(
                Guid.NewGuid(), item.Id, prompt.Id, succeeded.Id, profile.Id,
                CandidateImageStatus.ReviewPending,
                "outputs/queue/succeeded.png",
                "outputs/queue/succeeded.json",
                timestamp.AddMinutes(8)),
            timestamp.AddMinutes(8));
        item.AddGenerationTask(
            new GenerationTask(
                Guid.NewGuid(), item.Id, prompt.Id, profile.Id,
                GenerationTaskStatus.Failed, 1, 0,
                timestamp.AddMinutes(9), timestamp.AddMinutes(9),
                "Provider failed."),
            timestamp.AddMinutes(9));
        await repository.SaveAsync(project, CancellationToken.None);

        var state = await coordinator.LoadAsync(
            project.Id,
            series.Id,
            item.Id,
            activeCreativeBriefId: null,
            noItemsInSeriesText: "No items",
            CancellationToken.None);

        Assert.Collection(
            state.QueueRows,
            row =>
            {
                Assert.Equal("Closing", row.ItemTitle);
                Assert.Equal("Cancelled", row.Status);
            },
            row =>
            {
                Assert.Equal("Succeeded", row.Status);
                Assert.Equal("outputs/queue/succeeded.png", row.OutputPath);
                Assert.Empty(row.ErrorMessage);
            },
            row =>
            {
                Assert.Equal("Failed", row.Status);
                Assert.Equal("Provider failed.", row.ErrorMessage);
                Assert.Empty(row.OutputPath);
            });
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
            return Task.FromResult(project);
        }

        public Task<IReadOnlyList<ProjectSummary>> ListAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult<IReadOnlyList<ProjectSummary>>(
                _projects.Values
                    .OrderByDescending(project => project.UpdatedAt)
                    .Select(project => new ProjectSummary(
                        project.Id,
                        project.Name,
                        project.CreatedAt,
                        project.UpdatedAt))
                    .ToArray());
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
}
