using ContentDeliveryStudio.Application.Projects;
using ContentDeliveryStudio.Core.Projects;

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

    public async Task<Guid> CreateSeriesAsync(
        Guid projectId,
        string title,
        string description,
        CancellationToken cancellationToken)
    {
        var series = await _projectService.AddSeriesAsync(
            projectId,
            title.Trim(),
            description.Trim(),
            DateTimeOffset.UtcNow,
            cancellationToken);

        return series.Id;
    }

    public async Task<Guid> AddItemAsync(
        Guid projectId,
        Guid seriesId,
        string title,
        string brief,
        CancellationToken cancellationToken)
    {
        var item = await _projectService.AddItemAsync(
            projectId,
            seriesId,
            title.Trim(),
            brief.Trim(),
            DateTimeOffset.UtcNow,
            cancellationToken);

        return item.Id;
    }

    public async Task<Guid> CreatePromptVersionAsync(
        Guid projectId,
        Guid seriesItemId,
        string promptText,
        CancellationToken cancellationToken)
    {
        var prompt = await _projectService.AddPromptVersionAsync(
            projectId,
            seriesItemId,
            promptText.Trim(),
            new GenerationSettings(1024, 1024, "standard", "png"),
            providerProfileId: null,
            DateTimeOffset.UtcNow,
            cancellationToken);

        return prompt.Id;
    }
}

public sealed record ProjectWorkspaceResult(
    IReadOnlyList<ProjectSummaryViewModel> Projects,
    ProjectSummaryViewModel? SelectedProject);
