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

    public async Task<ImageSeries> AddSeriesAsync(
        Guid projectId,
        string title,
        string description,
        DateTimeOffset timestamp,
        CancellationToken cancellationToken)
    {
        var project = await RequireProjectAsync(projectId, cancellationToken);
        var series = project.AddSeries(title, description, timestamp);
        await _repository.SaveAsync(project, cancellationToken);
        return series;
    }

    public async Task<SeriesItem> AddItemAsync(
        Guid projectId,
        Guid seriesId,
        string title,
        string brief,
        DateTimeOffset timestamp,
        CancellationToken cancellationToken)
    {
        var project = await RequireProjectAsync(projectId, cancellationToken);
        var series = project.Series.SingleOrDefault(series => series.Id == seriesId)
            ?? throw new InvalidOperationException($"Series not found: {seriesId}");
        var item = series.AddItem(title, brief, timestamp);
        await _repository.SaveAsync(project, cancellationToken);
        return item;
    }

    private async Task<ImageProject> RequireProjectAsync(Guid projectId, CancellationToken cancellationToken)
    {
        return await _repository.LoadAsync(projectId, cancellationToken)
            ?? throw new InvalidOperationException($"Project not found: {projectId}");
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
