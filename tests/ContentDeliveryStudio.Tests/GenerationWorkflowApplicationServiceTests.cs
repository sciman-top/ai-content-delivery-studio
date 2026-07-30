using ContentDeliveryStudio.Application.Diagnostics;
using ContentDeliveryStudio.Application.Projects;
using ContentDeliveryStudio.Core.Projects;
using ContentDeliveryStudio.Core.Providers;
using ContentDeliveryStudio.Infrastructure.Fakes;

namespace ContentDeliveryStudio.Tests;

public sealed class GenerationWorkflowApplicationServiceTests
{
    [Fact]
    public async Task CompatibilityRun_RejectsNonFakeProviderBeforePreparingTasks()
    {
        var repository = new InMemoryProjectRepository();
        var provider = new NonFakeImageGenerationProvider();
        var service = new GenerationWorkflowApplicationService(repository, provider, imageEditProvider: null);
        var project = await SeedGenerationProjectAsync(repository, itemCount: 1);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.RunGenerationQueueAsync(
            project.Id,
            Path.GetTempPath(),
            CancellationToken.None));

        var loaded = await repository.LoadAsync(project.Id, CancellationToken.None);
        Assert.Empty(loaded!.Series.Single().Items.Single().GenerationTasks);
        Assert.Equal(0, provider.CallCount);
    }

    [Fact]
    public async Task PrepareGenerationQueue_JournalFailureDoesNotChangePersistedWorkflow()
    {
        var repository = new InMemoryProjectRepository();
        var provider = new RecordingImageGenerationProvider(repository);
        var service = new GenerationWorkflowApplicationService(
            repository,
            provider,
            imageEditProvider: null,
            new ThrowingDiagnosticsEventJournal());
        var project = await SeedGenerationProjectAsync(repository, itemCount: 1);

        var prepared = await service.PrepareGenerationQueueAsync(project.Id, CancellationToken.None);

        var loaded = await repository.LoadAsync(project.Id, CancellationToken.None);
        Assert.Single(prepared.TaskIds);
        Assert.Equal(GenerationTaskStatus.Queued, loaded!.Series.Single().Items.Single().GenerationTasks.Single().Status);
        Assert.Equal(0, provider.CallCount);
    }

    [Fact]
    public async Task PrepareAndOperatorMutations_PersistWithoutProviderCalls()
    {
        var repository = new InMemoryProjectRepository();
        var provider = new RecordingImageGenerationProvider(repository);
        var journal = new RecordingDiagnosticsEventJournal();
        var service = new GenerationWorkflowApplicationService(repository, provider, imageEditProvider: null, journal);
        var project = await SeedGenerationProjectAsync(repository, itemCount: 3);

        var prepared = await service.PrepareGenerationQueueAsync(project.Id, CancellationToken.None);
        await service.PauseGenerationTaskAsync(project.Id, prepared.TaskIds[1], CancellationToken.None);
        await service.MoveGenerationTaskAsync(
            project.Id,
            prepared.TaskIds[2],
            GenerationTaskMoveDirection.Up,
            CancellationToken.None);
        await service.ResumeGenerationTaskAsync(project.Id, prepared.TaskIds[1], CancellationToken.None);

        var loaded = await repository.LoadAsync(project.Id, CancellationToken.None);
        var tasks = loaded!.Series.Single().Items
            .SelectMany(item => item.GenerationTasks)
            .OrderBy(task => task.QueuePosition)
            .ToArray();

        Assert.Equal(0, provider.CallCount);
        Assert.Equal(3, tasks.Length);
        Assert.All(tasks, task => Assert.Equal(GenerationTaskStatus.Queued, task.Status));
        Assert.Equal(prepared.TaskIds[2], tasks[1].Id);
        Assert.Equal(prepared.TaskIds[1], tasks[2].Id);
        Assert.Equal(
            [
                GenerationQueueDiagnosticsEventName.Prepared,
                GenerationQueueDiagnosticsEventName.Paused,
                GenerationQueueDiagnosticsEventName.Moved,
                GenerationQueueDiagnosticsEventName.Resumed,
            ],
            journal.QueueEvents.Select(value => value.EventName));
    }

    [Fact]
    public async Task RetryGenerationTask_CreatesLinkedQueuedTaskWithoutProviderCall()
    {
        var repository = new InMemoryProjectRepository();
        var provider = new RecordingImageGenerationProvider(repository);
        var journal = new RecordingDiagnosticsEventJournal();
        var service = new GenerationWorkflowApplicationService(repository, provider, imageEditProvider: null, journal);
        var project = await SeedGenerationProjectAsync(repository, itemCount: 1);
        var item = project.Series.Single().Items.Single();
        var prompt = item.PromptVersions.Single();
        var timestamp = DateTimeOffset.Parse("2026-07-29T14:00:00Z");
        var failedTask = item.AddGenerationTask(
            new GenerationTask(
                Guid.NewGuid(),
                item.Id,
                prompt.Id,
                prompt.ProviderProfileId,
                GenerationTaskStatus.Running,
                attemptCount: 1,
                maxRetries: 0,
                timestamp,
                timestamp,
                queuePosition: 1),
            timestamp);
        failedTask.Fail("provider failure", timestamp.AddSeconds(1));
        await repository.SaveAsync(project, CancellationToken.None);

        var retryTaskId = await service.RetryGenerationTaskAsync(
            project.Id,
            failedTask.Id,
            CancellationToken.None);

        var loaded = await repository.LoadAsync(project.Id, CancellationToken.None);
        var tasks = loaded!.Series.Single().Items.Single().GenerationTasks.OrderBy(task => task.CreatedAt).ToArray();
        var retry = tasks.Single(task => task.Id == retryTaskId);

        Assert.Equal(0, provider.CallCount);
        Assert.Equal(GenerationTaskStatus.Failed, tasks.Single(task => task.Id == failedTask.Id).Status);
        Assert.Equal(GenerationTaskStatus.Queued, retry.Status);
        Assert.Equal(failedTask.Id, retry.RetryOfTaskId);
        Assert.Equal(2, retry.QueuePosition);
        var retryEvent = Assert.Single(journal.QueueEvents);
        Assert.Equal(GenerationQueueDiagnosticsEventName.Retried, retryEvent.EventName);
        Assert.Equal(failedTask.Id, retryEvent.RetryOfTaskId);
    }

    [Fact]
    public async Task ExecutePreparedGenerationQueue_UsesOperatorOrderAndSkipsPausedWork()
    {
        var repository = new InMemoryProjectRepository();
        var provider = new RecordingImageGenerationProvider(repository);
        var journal = new RecordingDiagnosticsEventJournal();
        var service = new GenerationWorkflowApplicationService(repository, provider, imageEditProvider: null, journal);
        var project = await SeedGenerationProjectAsync(repository, itemCount: 3);
        var prepared = await service.PrepareGenerationQueueAsync(project.Id, CancellationToken.None);
        await service.PauseGenerationTaskAsync(project.Id, prepared.TaskIds[1], CancellationToken.None);
        await service.MoveGenerationTaskAsync(
            project.Id,
            prepared.TaskIds[2],
            GenerationTaskMoveDirection.Up,
            CancellationToken.None);

        var outputDirectory = Path.Combine(Path.GetTempPath(), "ContentDeliveryStudio.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(outputDirectory);

        try
        {
            var run = await service.ExecutePreparedGenerationQueueAsync(
                project.Id,
                outputDirectory,
                CancellationToken.None);

            var loaded = await repository.LoadAsync(project.Id, CancellationToken.None);
            var tasks = loaded!.Series.Single().Items.SelectMany(item => item.GenerationTasks).ToArray();

            Assert.Equal(2, provider.CallCount);
            Assert.Equal(2, run.Tasks.Count);
            Assert.Equal(
                [prepared.TaskIds[0], prepared.TaskIds[2]],
                run.Tasks.Select(task => task.Id));
            Assert.Equal(GenerationTaskStatus.Paused, tasks.Single(task => task.Id == prepared.TaskIds[1]).Status);
            Assert.Equal(2, journal.QueueEvents.Count(value =>
                value.EventName == GenerationQueueDiagnosticsEventName.ExecutionStarted));
            Assert.Equal(2, journal.QueueEvents.Count(value =>
                value.EventName == GenerationQueueDiagnosticsEventName.ExecutionSucceeded));
        }
        finally
        {
            if (Directory.Exists(outputDirectory))
            {
                Directory.Delete(outputDirectory, recursive: true);
            }
        }
    }

    [Fact]
    public async Task ExecutePreparedGenerationQueue_UsesDurablePositionsAcrossSeparateRuns()
    {
        var repository = new InMemoryProjectRepository();
        var provider = new RecordingImageGenerationProvider(repository);
        var service = new GenerationWorkflowApplicationService(repository, provider, imageEditProvider: null);
        var project = await SeedGenerationProjectAsync(repository, itemCount: 2, _ => "Repeated title");
        var prepared = await service.PrepareGenerationQueueAsync(project.Id, CancellationToken.None);
        await service.PauseGenerationTaskAsync(project.Id, prepared.TaskIds[0], CancellationToken.None);

        var outputDirectory = Path.Combine(Path.GetTempPath(), "ContentDeliveryStudio.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(outputDirectory);

        try
        {
            var firstRun = await service.ExecutePreparedGenerationQueueAsync(
                project.Id,
                outputDirectory,
                CancellationToken.None);
            await service.ResumeGenerationTaskAsync(project.Id, prepared.TaskIds[0], CancellationToken.None);
            var secondRun = await service.ExecutePreparedGenerationQueueAsync(
                project.Id,
                outputDirectory,
                CancellationToken.None);

            Assert.StartsWith("002-", Path.GetFileName(firstRun.Images.Single().AssetPath), StringComparison.Ordinal);
            Assert.StartsWith("001-", Path.GetFileName(secondRun.Images.Single().AssetPath), StringComparison.Ordinal);
            Assert.NotEqual(firstRun.Images.Single().AssetPath, secondRun.Images.Single().AssetPath);
        }
        finally
        {
            if (Directory.Exists(outputDirectory))
            {
                Directory.Delete(outputDirectory, recursive: true);
            }
        }
    }

    [Fact]
    public async Task MoveGenerationTask_InvalidLegacyBoundaryDoesNotMutateMissingPositions()
    {
        var repository = new InMemoryProjectRepository();
        var provider = new RecordingImageGenerationProvider(repository);
        var service = new GenerationWorkflowApplicationService(repository, provider, imageEditProvider: null);
        var project = await SeedGenerationProjectAsync(repository, itemCount: 2);
        var timestamp = DateTimeOffset.Parse("2026-07-29T14:30:00Z");
        var tasks = project.Series.Single().Items.Select((item, index) => item.AddGenerationTask(
            new GenerationTask(
                Guid.NewGuid(),
                item.Id,
                item.PromptVersions.Single().Id,
                item.PromptVersions.Single().ProviderProfileId,
                GenerationTaskStatus.Queued,
                attemptCount: 0,
                maxRetries: 0,
                timestamp.AddTicks(index),
                timestamp.AddTicks(index)),
            timestamp.AddTicks(index))).ToArray();
        await repository.SaveAsync(project, CancellationToken.None);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.MoveGenerationTaskAsync(
            project.Id,
            tasks[0].Id,
            GenerationTaskMoveDirection.Up,
            CancellationToken.None));

        Assert.All(tasks, task => Assert.Null(task.QueuePosition));
    }

    [Fact]
    public async Task GenerationWorkflowApplicationService_RunsGenerationQueueWithFakeProvider()
    {
        var repository = new InMemoryProjectRepository();
        var provider = new FakeImageGenerationProvider();
        var service = new GenerationWorkflowApplicationService(repository, provider, provider);
        var timestamp = DateTimeOffset.Parse("2026-06-07T12:00:00Z");
        var project = ImageProject.Create("Generation workflow demo", timestamp);
        var series = project.AddSeries("Series", "Fake queue", timestamp.AddMinutes(1));
        var item = series.AddItem("Opening image", "Opening visual", timestamp.AddMinutes(2));
        var profile = project.AddProviderProfile("Fake provider", ProviderKind.Fake, timestamp.AddMinutes(3));
        item.AddPromptVersion(
            "Create a clean editorial image.",
            new GenerationSettings(1024, 1024, "standard", "png"),
            profile.Id,
            timestamp.AddMinutes(4));
        var secondItem = series.AddItem("Detail image", "Detail visual", timestamp.AddMinutes(5));
        secondItem.AddPromptVersion(
            "Create a detailed editorial image.",
            new GenerationSettings(1024, 1024, "standard", "png"),
            profile.Id,
            timestamp.AddMinutes(6));
        var thirdItem = series.AddItem("Closing image", "Closing visual", timestamp.AddMinutes(7));
        thirdItem.AddPromptVersion(
            "Create a closing editorial image.",
            new GenerationSettings(1024, 1024, "standard", "png"),
            profile.Id,
            timestamp.AddMinutes(8));
        await repository.SaveAsync(project, CancellationToken.None);

        var outputDirectory = Path.Combine(Path.GetTempPath(), "ContentDeliveryStudio.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(outputDirectory);

        try
        {
            var run = await service.RunGenerationQueueAsync(project.Id, outputDirectory, CancellationToken.None);

            var loaded = await repository.LoadAsync(project.Id, CancellationToken.None);
            var loadedItems = loaded!.Series.Single().Items;

            Assert.Equal(3, run.Tasks.Count);
            Assert.All(run.Tasks, task => Assert.Equal(GenerationTaskStatus.Succeeded, task.Status));
            Assert.Equal(3, run.Images.Count);
            Assert.Equal(3, loadedItems.Sum(loadedItem => loadedItem.GenerationTasks.Count));
            Assert.Equal(3, loadedItems.Sum(loadedItem => loadedItem.CandidateImages.Count));
            Assert.All(run.Images, image => Assert.True(File.Exists(image.AssetPath)));
            Assert.Equal(
                ["Opening image", "Detail image", "Closing image"],
                loadedItems
                    .SelectMany(loadedItem => loadedItem.GenerationTasks.Select(task => new { loadedItem.Title, Task = task }))
                    .OrderBy(entry => entry.Task.CreatedAt)
                    .Select(entry => entry.Title));
        }
        finally
        {
            if (Directory.Exists(outputDirectory))
            {
                Directory.Delete(outputDirectory, recursive: true);
            }
        }
    }

    [Fact]
    public async Task GenerationWorkflowApplicationService_PersistsRunningBeforeProviderDispatch()
    {
        var repository = new InMemoryProjectRepository();
        var provider = new RecordingImageGenerationProvider(repository);
        var service = new GenerationWorkflowApplicationService(repository, provider, imageEditProvider: null);
        var timestamp = DateTimeOffset.Parse("2026-07-28T16:00:00Z");
        var project = ImageProject.Create("Durable generation", timestamp);
        var series = project.AddSeries("Series", "Durable queue", timestamp.AddMinutes(1));
        var item = series.AddItem("Opening", "Opening visual", timestamp.AddMinutes(2));
        var profile = project.AddProviderProfile("Fake provider", ProviderKind.Fake, timestamp.AddMinutes(3));
        item.AddPromptVersion(
            "Create a durable image.",
            new GenerationSettings(1024, 1024, "standard", "png"),
            profile.Id,
            timestamp.AddMinutes(4));
        await repository.SaveAsync(project, CancellationToken.None);

        var outputDirectory = Path.Combine(Path.GetTempPath(), "ContentDeliveryStudio.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(outputDirectory);

        try
        {
            await service.RunGenerationQueueAsync(project.Id, outputDirectory, CancellationToken.None);

            Assert.True(provider.SawPersistedRunningTask);
            Assert.Equal(
                [GenerationTaskStatus.Queued, GenerationTaskStatus.Running, GenerationTaskStatus.Succeeded],
                repository.GenerationTaskSaveStates);
        }
        finally
        {
            if (Directory.Exists(outputDirectory))
            {
                Directory.Delete(outputDirectory, recursive: true);
            }
        }
    }

    [Fact]
    public async Task GenerationWorkflowApplicationService_RunsMaskEditWithFakeProvider()
    {
        var repository = new InMemoryProjectRepository();
        var provider = new FakeImageGenerationProvider();
        var service = new GenerationWorkflowApplicationService(repository, provider, provider);
        var timestamp = DateTimeOffset.Parse("2026-06-07T13:00:00Z");
        var project = ImageProject.Create("Image edit workflow demo", timestamp);
        await repository.SaveAsync(project, CancellationToken.None);

        var workingDirectory = Path.Combine(Path.GetTempPath(), "ContentDeliveryStudio.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(workingDirectory);
        var sourcePath = Path.Combine(workingDirectory, "source.png");
        var maskPath = Path.Combine(workingDirectory, "mask.png");
        var outputDirectory = Path.Combine(workingDirectory, "edited");
        await File.WriteAllBytesAsync(sourcePath, [1, 2, 3], CancellationToken.None);
        await File.WriteAllBytesAsync(maskPath, [4, 5, 6], CancellationToken.None);

        try
        {
            var result = await service.RunImageEditAsync(
                new ImageEditWorkflowRequest(
                    project.Id,
                    Guid.NewGuid(),
                    Guid.NewGuid(),
                    sourcePath,
                    maskPath,
                    "Clean the masked area.",
                    new GenerationSettings(1024, 1024, "standard", "png"),
                    outputDirectory,
                    "edited.png"),
                CancellationToken.None);

            Assert.True(File.Exists(result.AssetPath));
            Assert.Equal("fake-image-edit", result.ProviderTraceId);
        }
        finally
        {
            if (Directory.Exists(workingDirectory))
            {
                Directory.Delete(workingDirectory, recursive: true);
            }
        }
    }

    private static async Task<ImageProject> SeedGenerationProjectAsync(
        InMemoryProjectRepository repository,
        int itemCount,
        Func<int, string>? itemTitleFactory = null)
    {
        var timestamp = DateTimeOffset.Parse("2026-07-29T12:00:00Z");
        var project = ImageProject.Create("Operator queue", timestamp);
        var series = project.AddSeries("Series", "Operator queue", timestamp.AddMinutes(1));
        var profile = project.AddProviderProfile("Fake provider", ProviderKind.Fake, timestamp.AddMinutes(2));

        for (var index = 0; index < itemCount; index++)
        {
            var item = series.AddItem(
                itemTitleFactory?.Invoke(index) ?? $"Item {index + 1}",
                $"Visual {index + 1}",
                timestamp.AddMinutes(3 + index));
            item.AddPromptVersion(
                $"Create visual {index + 1}.",
                new GenerationSettings(1024, 1024, "standard", "png"),
                profile.Id,
                timestamp.AddMinutes(10 + index));
        }

        await repository.SaveAsync(project, CancellationToken.None);
        return project;
    }

    private sealed class InMemoryProjectRepository : IProjectRepository
    {
        private readonly Dictionary<Guid, ImageProject> _projects = [];

        public List<GenerationTaskStatus> GenerationTaskSaveStates { get; } = [];

        public Task SaveAsync(ImageProject project, CancellationToken cancellationToken)
        {
            _projects[project.Id] = project;
            var tasks = project.Series.SelectMany(series => series.Items).SelectMany(item => item.GenerationTasks).ToArray();
            if (tasks.Length == 1)
            {
                GenerationTaskSaveStates.Add(tasks.Single().Status);
            }

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

    private sealed class RecordingImageGenerationProvider : IImageGenerationProvider
    {
        private readonly InMemoryProjectRepository _repository;
        private readonly FakeImageGenerationProvider _inner = new();

        public RecordingImageGenerationProvider(InMemoryProjectRepository repository)
        {
            _repository = repository;
        }

        public IProviderCapabilities Capabilities => _inner.Capabilities;

        public bool SawPersistedRunningTask { get; private set; }

        public int CallCount { get; private set; }

        public Task<ImageGenerationResult> GenerateImageAsync(
            ImageGenerationRequest request,
            CancellationToken cancellationToken)
        {
            CallCount++;
            SawPersistedRunningTask = _repository.GenerationTaskSaveStates.LastOrDefault()
                is GenerationTaskStatus.Running;
            return _inner.GenerateImageAsync(request, cancellationToken);
        }
    }

    private sealed class NonFakeImageGenerationProvider : IImageGenerationProvider
    {
        public IProviderCapabilities Capabilities { get; } = new ProviderCapabilities(
            "paid-image",
            "Paid image provider",
            ["paid-image-v1"],
            SupportsTextPlanning: false,
            SupportsImageGeneration: true,
            SupportsVisionReview: false,
            SupportsImageEditing: false,
            SupportsStreaming: false);

        public int CallCount { get; private set; }

        public Task<ImageGenerationResult> GenerateImageAsync(
            ImageGenerationRequest request,
            CancellationToken cancellationToken)
        {
            CallCount++;
            throw new InvalidOperationException("This provider must not be called by the test.");
        }
    }

    private sealed class RecordingDiagnosticsEventJournal : IDiagnosticsEventJournal
    {
        public List<GenerationQueueDiagnosticsEvent> QueueEvents { get; } = [];

        public void Record(GenerationQueueDiagnosticsEvent value)
        {
            QueueEvents.Add(value);
        }

        public void Record(ProviderCallDiagnosticsEvent value)
        {
        }

        public Task<DiagnosticsLogReadResult> ReadRecentAsync(int maxCount, CancellationToken cancellationToken)
        {
            return Task.FromResult(new DiagnosticsLogReadResult([], 0, 0));
        }
    }

    private sealed class ThrowingDiagnosticsEventJournal : IDiagnosticsEventJournal
    {
        public void Record(GenerationQueueDiagnosticsEvent value) => throw new IOException("simulated journal failure");

        public void Record(ProviderCallDiagnosticsEvent value) => throw new IOException("simulated journal failure");

        public Task<DiagnosticsLogReadResult> ReadRecentAsync(int maxCount, CancellationToken cancellationToken) =>
            throw new IOException("simulated journal failure");
    }
}
