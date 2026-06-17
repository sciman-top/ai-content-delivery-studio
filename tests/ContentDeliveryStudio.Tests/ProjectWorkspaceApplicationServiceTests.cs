using ContentDeliveryStudio.Application.Projects;
using ContentDeliveryStudio.Core.Projects;

namespace ContentDeliveryStudio.Tests;

public sealed class ProjectWorkspaceApplicationServiceTests
{
    [Fact]
    public async Task ProjectWorkspaceApplicationService_CreatesAndLoadsProject()
    {
        var repository = new InMemoryProjectRepository();
        var service = new ProjectWorkspaceApplicationService(repository);
        var timestamp = DateTimeOffset.Parse("2026-06-07T20:00:00Z");

        var created = await service.CreateProjectAsync("Workspace demo", timestamp, CancellationToken.None);
        var loaded = await service.LoadProjectAsync(created.Id, CancellationToken.None);

        Assert.NotNull(loaded);
        Assert.Equal(created.Id, loaded!.Id);
        Assert.Equal("Workspace demo", loaded.Name);
        Assert.Equal(timestamp, loaded.CreatedAt);
    }

    [Fact]
    public async Task ProjectWorkspaceApplicationService_ListsProjectsInUpdatedOrder()
    {
        var repository = new InMemoryProjectRepository();
        var service = new ProjectWorkspaceApplicationService(repository);

        var first = await service.CreateProjectAsync(
            "First project",
            DateTimeOffset.Parse("2026-06-07T20:00:00Z"),
            CancellationToken.None);
        var second = await service.CreateProjectAsync(
            "Second project",
            DateTimeOffset.Parse("2026-06-07T21:00:00Z"),
            CancellationToken.None);

        var projects = await service.ListProjectsAsync(CancellationToken.None);

        Assert.Collection(
            projects,
            project => Assert.Equal(second.Id, project.Id),
            project => Assert.Equal(first.Id, project.Id));
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
