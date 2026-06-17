using ContentDeliveryStudio.Application.Projects;
using ContentDeliveryStudio.App.ViewModels;
using ContentDeliveryStudio.Core.Projects;

namespace ContentDeliveryStudio.Tests;

public sealed class ProjectWorkspaceCoordinatorTests
{
    [Fact]
    public async Task CreateProjectAsync_CreatesProjectAndReturnsProjectedSelection()
    {
        var repository = new InMemoryProjectRepository();
        var projectService = new ProjectApplicationService(repository);
        var coordinator = new ProjectWorkspaceCoordinator(projectService);

        var result = await coordinator.CreateProjectAsync(
            "Workspace UI demo",
            CancellationToken.None);

        var selected = Assert.Single(result.Projects);
        Assert.Equal("Workspace UI demo", selected.Name);
        Assert.Equal(selected.Id, result.SelectedProject?.Id);
    }

    [Fact]
    public async Task RefreshProjectsAsync_OrdersProjectsByUpdatedAtAndPreservesSelection()
    {
        var repository = new InMemoryProjectRepository();
        var projectService = new ProjectApplicationService(repository);
        var coordinator = new ProjectWorkspaceCoordinator(projectService);
        var first = await projectService.CreateProjectAsync(
            "First project",
            DateTimeOffset.Parse("2026-06-09T08:00:00Z"),
            CancellationToken.None);
        var second = await projectService.CreateProjectAsync(
            "Second project",
            DateTimeOffset.Parse("2026-06-09T09:00:00Z"),
            CancellationToken.None);

        var result = await coordinator.RefreshProjectsAsync(second.Id, CancellationToken.None);

        Assert.Collection(
            result.Projects,
            project => Assert.Equal(second.Id, project.Id),
            project => Assert.Equal(first.Id, project.Id));
        Assert.Equal(second.Id, result.SelectedProject?.Id);
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
