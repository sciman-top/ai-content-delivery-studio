using ContentDeliveryStudio.Application.Projects;

namespace ContentDeliveryStudio.App.ViewModels;

public sealed class ProjectWorkspaceCoordinator
{
    private readonly ProjectApplicationService _projectService;

    public ProjectWorkspaceCoordinator(ProjectApplicationService projectService)
    {
        _projectService = projectService;
    }

    public async Task<ProjectWorkspaceResult> CreateProjectAsync(
        string projectName,
        CancellationToken cancellationToken)
    {
        var project = await _projectService.CreateProjectAsync(
            projectName.Trim(),
            DateTimeOffset.UtcNow,
            cancellationToken);

        return await RefreshProjectsAsync(project.Id, cancellationToken);
    }

    public async Task<ProjectWorkspaceResult> RefreshProjectsAsync(
        Guid? selectedProjectId,
        CancellationToken cancellationToken)
    {
        var projectSummaries = await _projectService.ListProjectsAsync(cancellationToken);
        var projects = projectSummaries
            .Select(project => new ProjectSummaryViewModel(project.Id, project.Name, project.UpdatedAt))
            .ToArray();

        var selectedProject = selectedProjectId is null
            ? projects.FirstOrDefault()
            : projects.FirstOrDefault(project => project.Id == selectedProjectId);

        return new ProjectWorkspaceResult(projects, selectedProject);
    }
}

public sealed record ProjectWorkspaceResult(
    IReadOnlyList<ProjectSummaryViewModel> Projects,
    ProjectSummaryViewModel? SelectedProject);
