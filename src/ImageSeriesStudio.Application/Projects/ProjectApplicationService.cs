using ImageSeriesStudio.Core.Projects;

namespace ImageSeriesStudio.Application.Projects;

public sealed class ProjectApplicationService
{
    private readonly IProjectRepository _repository;

    public ProjectApplicationService(IProjectRepository repository)
    {
        _repository = repository;
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

    public Task<ImageProject?> LoadProjectAsync(Guid projectId, CancellationToken cancellationToken)
    {
        return _repository.LoadAsync(projectId, cancellationToken);
    }

    public Task<IReadOnlyList<ProjectSummary>> ListProjectsAsync(CancellationToken cancellationToken)
    {
        return _repository.ListAsync(cancellationToken);
    }
}

public interface IProjectRepository
{
    Task SaveAsync(ImageProject project, CancellationToken cancellationToken);

    Task<ImageProject?> LoadAsync(Guid projectId, CancellationToken cancellationToken);

    Task<IReadOnlyList<ProjectSummary>> ListAsync(CancellationToken cancellationToken);
}

public sealed record ProjectSummary(
    Guid Id,
    string Name,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
