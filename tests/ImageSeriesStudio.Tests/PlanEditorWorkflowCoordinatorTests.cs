using ImageSeriesStudio.Application.Projects;
using ImageSeriesStudio.App.ViewModels;
using ImageSeriesStudio.Core.Projects;

namespace ImageSeriesStudio.Tests;

public sealed class PlanEditorWorkflowCoordinatorTests
{
    [Fact]
    public async Task CreateSeriesAndAddItemAsync_PersistsSeriesAndItem()
    {
        var repository = new InMemoryProjectRepository();
        var projectService = new ProjectApplicationService(repository);
        var coordinator = new PlanEditorWorkflowCoordinator(projectService);
        var timestamp = DateTimeOffset.Parse("2026-06-08T14:00:00Z");
        var project = await projectService.CreateProjectAsync("Plan editor demo", timestamp, CancellationToken.None);

        var seriesId = await coordinator.CreateSeriesAsync(
            project.Id,
            "Storyboard",
            "Panel sequence",
            CancellationToken.None);

        var itemId = await coordinator.AddItemAsync(
            project.Id,
            seriesId,
            "Opening panel",
            "Opening visual for a panel-like sequence.",
            CancellationToken.None);

        var loaded = await projectService.LoadProjectAsync(project.Id, CancellationToken.None);
        var loadedSeries = Assert.Single(loaded!.Series);
        var loadedItem = Assert.Single(loadedSeries.Items);

        Assert.Equal(seriesId, loadedSeries.Id);
        Assert.Equal(itemId, loadedItem.Id);
        Assert.Equal("Opening panel", loadedItem.Title);
    }

    [Fact]
    public async Task CreatePromptVersionAsync_UsesDefaultGenerationSettings()
    {
        var repository = new InMemoryProjectRepository();
        var projectService = new ProjectApplicationService(repository);
        var coordinator = new PlanEditorWorkflowCoordinator(projectService);
        var timestamp = DateTimeOffset.Parse("2026-06-08T15:00:00Z");
        var project = await projectService.CreateProjectAsync("Prompt editor demo", timestamp, CancellationToken.None);
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

        var promptId = await coordinator.CreatePromptVersionAsync(
            project.Id,
            item.Id,
            "Create a clean opening frame.",
            CancellationToken.None);

        var loaded = await projectService.LoadProjectAsync(project.Id, CancellationToken.None);
        var loadedPrompt = Assert.Single(loaded!.Series.Single().Items.Single().PromptVersions);

        Assert.Equal(promptId, loadedPrompt.Id);
        Assert.Equal(1024, loadedPrompt.Settings.Width);
        Assert.Equal(1024, loadedPrompt.Settings.Height);
        Assert.Equal("standard", loadedPrompt.Settings.Quality);
        Assert.Equal("png", loadedPrompt.Settings.OutputFormat);
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
