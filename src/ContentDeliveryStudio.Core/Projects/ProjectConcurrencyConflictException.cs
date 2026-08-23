namespace ContentDeliveryStudio.Core.Projects;

/// <summary>
/// Thrown when a project aggregate save loses an optimistic-concurrency race.
/// Callers must reload the aggregate and re-apply their decision instead of
/// overwriting the concurrent change.
/// </summary>
public sealed class ProjectConcurrencyConflictException : Exception
{
    public ProjectConcurrencyConflictException(Guid projectId, Exception? innerException = null)
        : base(
            $"Project {projectId} was modified by another operation. Reload the project and retry.",
            innerException)
    {
        ProjectId = projectId;
    }

    public Guid ProjectId { get; }
}
