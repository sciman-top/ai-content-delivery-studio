using ContentDeliveryStudio.Core.Projects;

namespace ContentDeliveryStudio.Application.Projects;

public sealed class ProjectWorkspaceApplicationService
{
    private readonly IProjectRepository _repository;
    private readonly TimeProvider _timeProvider;

    public ProjectWorkspaceApplicationService(
        IProjectRepository repository,
        TimeProvider? timeProvider = null)
    {
        _repository = repository;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    public async Task<ImageProject> CreateProjectAsync(
        string name,
        DateTimeOffset timestamp,
        CancellationToken cancellationToken)
    {
        var project = ImageProject.Create(name, timestamp);
        await _repository.SaveAsync(project, cancellationToken);
        return project;
    }

    public async Task<ImageProject?> LoadProjectAsync(Guid projectId, CancellationToken cancellationToken)
    {
        var project = await _repository.LoadAsync(projectId, cancellationToken);
        if (project is null)
        {
            return null;
        }

        var recoveryTimestamp = _timeProvider.GetUtcNow();
        var recovered = false;
        foreach (var task in project.Series
                     .SelectMany(series => series.Items)
                     .SelectMany(item => item.GenerationTasks))
        {
            var taskRecoveryTimestamp = recoveryTimestamp < task.UpdatedAt
                ? task.UpdatedAt
                : recoveryTimestamp;
            recovered = task.RecoverInterrupted(taskRecoveryTimestamp) || recovered;
        }

        if (recovered)
        {
            await _repository.SaveAsync(project, cancellationToken);
        }

        return project;
    }

    public Task<IReadOnlyList<ProjectSummary>> ListProjectsAsync(CancellationToken cancellationToken)
    {
        return _repository.ListAsync(cancellationToken);
    }
}
