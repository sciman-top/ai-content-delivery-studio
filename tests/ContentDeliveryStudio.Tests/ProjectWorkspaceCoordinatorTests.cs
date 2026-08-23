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
}
