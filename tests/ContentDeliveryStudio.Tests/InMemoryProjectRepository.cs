using ContentDeliveryStudio.Application.Projects;
using ContentDeliveryStudio.Core.Projects;

namespace ContentDeliveryStudio.Tests;

/// <summary>
/// Canonical in-memory <see cref="IProjectRepository"/> double for tests that
/// do not need persistence-specific behavior. Files needing different save/load
/// semantics keep their own private fakes.
/// </summary>
internal sealed class InMemoryProjectRepository : IProjectRepository
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
