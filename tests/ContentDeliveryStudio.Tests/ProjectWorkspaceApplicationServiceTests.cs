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

    [Fact]
    public async Task LoadProjectAsync_RecoversIncompleteTasksWithoutProviderDispatch()
    {
        var repository = new InMemoryProjectRepository();
        var recoveryTimestamp = DateTimeOffset.Parse("2026-07-28T17:00:00Z");
        var service = new ProjectWorkspaceApplicationService(
            repository,
            new FixedTimeProvider(recoveryTimestamp));
        var project = CreateProjectWithTasks(
            GenerationTaskStatus.Queued,
            GenerationTaskStatus.Running);
        await repository.SaveAsync(project, CancellationToken.None);
        repository.ResetSaveCount();

        var loaded = await service.LoadProjectAsync(project.Id, CancellationToken.None);

        var tasks = loaded!.Series.Single().Items.Single().GenerationTasks.OrderBy(task => task.CreatedAt).ToArray();
        Assert.Equal(GenerationTaskStatus.Cancelled, tasks[0].Status);
        Assert.Contains("not dispatched", tasks[0].ErrorMessage, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(GenerationTaskStatus.Failed, tasks[1].Status);
        Assert.Contains("interrupted", tasks[1].ErrorMessage, StringComparison.OrdinalIgnoreCase);
        Assert.All(tasks, task => Assert.Equal(recoveryTimestamp, task.UpdatedAt));
        Assert.Equal(1, repository.SaveCount);
    }

    [Fact]
    public async Task LoadProjectAsync_DoesNotSaveProjectWithOnlyTerminalTasks()
    {
        var repository = new InMemoryProjectRepository();
        var service = new ProjectWorkspaceApplicationService(repository);
        var project = CreateProjectWithTasks(
            GenerationTaskStatus.Succeeded,
            GenerationTaskStatus.Failed,
            GenerationTaskStatus.Cancelled);
        await repository.SaveAsync(project, CancellationToken.None);
        repository.ResetSaveCount();

        _ = await service.LoadProjectAsync(project.Id, CancellationToken.None);

        Assert.Equal(0, repository.SaveCount);
    }

    private static ImageProject CreateProjectWithTasks(params GenerationTaskStatus[] statuses)
    {
        var timestamp = DateTimeOffset.Parse("2026-07-28T16:30:00Z");
        var project = ImageProject.Create("Recovery project", timestamp);
        var series = project.AddSeries("Series", "Recovery", timestamp.AddMinutes(1));
        var item = series.AddItem("Item", "Recovery item", timestamp.AddMinutes(2));
        var profile = project.AddProviderProfile("Fake provider", ProviderKind.Fake, timestamp.AddMinutes(3));
        var prompt = item.AddPromptVersion(
            "Recovery prompt",
            new GenerationSettings(1024, 1024, "standard", "png"),
            profile.Id,
            timestamp.AddMinutes(4));

        for (var index = 0; index < statuses.Length; index++)
        {
            var status = statuses[index];
            item.AddGenerationTask(
                new GenerationTask(
                    Guid.NewGuid(),
                    item.Id,
                    prompt.Id,
                    profile.Id,
                    status,
                    attemptCount: status is GenerationTaskStatus.Queued ? 0 : 1,
                    maxRetries: 0,
                    timestamp.AddMinutes(5 + index),
                    timestamp.AddMinutes(5 + index),
                    status is GenerationTaskStatus.Failed or GenerationTaskStatus.Cancelled
                        ? "Historical terminal reason."
                        : null),
                timestamp.AddMinutes(5 + index));
        }

        return project;
    }

    private sealed class InMemoryProjectRepository : IProjectRepository
    {
        private readonly Dictionary<Guid, ImageProject> _projects = [];

        public int SaveCount { get; private set; }

        public Task SaveAsync(ImageProject project, CancellationToken cancellationToken)
        {
            _projects[project.Id] = project;
            SaveCount++;
            return Task.CompletedTask;
        }

        public void ResetSaveCount()
        {
            SaveCount = 0;
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

    private sealed class FixedTimeProvider : TimeProvider
    {
        private readonly DateTimeOffset _timestamp;

        public FixedTimeProvider(DateTimeOffset timestamp)
        {
            _timestamp = timestamp;
        }

        public override DateTimeOffset GetUtcNow() => _timestamp;
    }
}
