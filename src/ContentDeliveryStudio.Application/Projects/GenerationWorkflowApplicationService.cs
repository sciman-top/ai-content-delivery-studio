using ContentDeliveryStudio.Application.Diagnostics;
using ContentDeliveryStudio.Core.Generation;
using ContentDeliveryStudio.Core.Projects;
using ContentDeliveryStudio.Core.Providers;

namespace ContentDeliveryStudio.Application.Projects;

public sealed class GenerationWorkflowApplicationService
{
    private readonly IProjectRepository _repository;
    private readonly IImageGenerationProvider? _imageGenerationProvider;
    private readonly IImageEditProvider? _imageEditProvider;
    private readonly IDiagnosticsEventJournal _eventJournal;

    public GenerationWorkflowApplicationService(
        IProjectRepository repository,
        IImageGenerationProvider? imageGenerationProvider,
        IImageEditProvider? imageEditProvider,
        IDiagnosticsEventJournal? eventJournal = null)
    {
        _repository = repository;
        _imageGenerationProvider = imageGenerationProvider;
        _imageEditProvider = imageEditProvider;
        _eventJournal = eventJournal ?? NullDiagnosticsEventJournal.Instance;
    }

    public async Task<GenerationQueueRun> RunGenerationQueueAsync(
        Guid projectId,
        string outputDirectory,
        CancellationToken cancellationToken)
    {
        RequireFakeImageGenerationProvider();
        await PrepareGenerationQueueAsync(projectId, cancellationToken);
        return await ExecutePreparedGenerationQueueAsync(projectId, outputDirectory, cancellationToken);
    }

    public async Task<GenerationQueuePreparation> PrepareGenerationQueueAsync(
        Guid projectId,
        CancellationToken cancellationToken)
    {
        var project = await RequireProjectAsync(projectId, cancellationToken);
        var existingTasks = GetTaskEntries(project).Select(entry => entry.Task).ToArray();
        if (existingTasks.Any(task => task.Status is GenerationTaskStatus.Queued
                or GenerationTaskStatus.Paused
                or GenerationTaskStatus.Running))
        {
            throw new InvalidOperationException("The project already has active generation work.");
        }

        var nextPosition = existingTasks.Select(task => task.QueuePosition ?? 0).DefaultIfEmpty().Max() + 1;
        var queuedAt = DateTimeOffset.UtcNow;
        var taskIds = new List<Guid>();

        foreach (var item in project.Series.SelectMany(series => series.Items))
        {
            var prompt = item.PromptVersions.OrderByDescending(value => value.VersionNumber).FirstOrDefault();
            if (prompt is null)
            {
                continue;
            }

            var taskQueuedAt = queuedAt.AddTicks(taskIds.Count);
            var task = item.AddGenerationTask(
                new GenerationTask(
                    Guid.NewGuid(),
                    item.Id,
                    prompt.Id,
                    prompt.ProviderProfileId,
                    GenerationTaskStatus.Queued,
                    attemptCount: 0,
                    maxRetries: 0,
                    taskQueuedAt,
                    taskQueuedAt,
                    queuePosition: nextPosition++),
                taskQueuedAt);
            taskIds.Add(task.Id);
        }

        if (taskIds.Count == 0)
        {
            throw new InvalidOperationException("No prompt versions are available for queue preparation.");
        }

        await _repository.SaveAsync(project, cancellationToken);
        SafeRecord(new GenerationQueueDiagnosticsEvent(
            queuedAt,
            GenerationQueueDiagnosticsEventName.Prepared,
            projectId,
            ItemCount: taskIds.Count));
        return new GenerationQueuePreparation(taskIds);
    }

    public Task PauseGenerationTaskAsync(
        Guid projectId,
        Guid taskId,
        CancellationToken cancellationToken)
    {
        return MutateTaskAsync(
            projectId,
            taskId,
            task => task.Pause(GetCheckpointTimestamp(task)),
            GenerationQueueDiagnosticsEventName.Paused,
            cancellationToken);
    }

    public Task ResumeGenerationTaskAsync(
        Guid projectId,
        Guid taskId,
        CancellationToken cancellationToken)
    {
        return MutateTaskAsync(
            projectId,
            taskId,
            task => task.Resume(GetCheckpointTimestamp(task)),
            GenerationQueueDiagnosticsEventName.Resumed,
            cancellationToken);
    }

    public async Task MoveGenerationTaskAsync(
        Guid projectId,
        Guid taskId,
        GenerationTaskMoveDirection direction,
        CancellationToken cancellationToken)
    {
        var project = await RequireProjectAsync(projectId, cancellationToken);
        var allEntries = GetTaskEntries(project);
        var activeEntries = allEntries
            .Where(entry => entry.Task.Status is GenerationTaskStatus.Queued or GenerationTaskStatus.Paused)
            .OrderBy(entry => entry.Task.QueuePosition ?? int.MaxValue)
            .ThenBy(entry => entry.Task.CreatedAt)
            .ThenBy(entry => entry.Task.Id)
            .ToArray();
        var currentIndex = Array.FindIndex(activeEntries, entry => entry.Task.Id == taskId);
        if (currentIndex < 0)
        {
            throw new InvalidOperationException($"Active generation task not found: {taskId}");
        }

        var targetIndex = direction switch
        {
            GenerationTaskMoveDirection.Up => currentIndex - 1,
            GenerationTaskMoveDirection.Down => currentIndex + 1,
            _ => throw new ArgumentOutOfRangeException(nameof(direction)),
        };
        if (targetIndex < 0 || targetIndex >= activeEntries.Length)
        {
            throw new InvalidOperationException("The generation task cannot move beyond the active queue boundary.");
        }

        var nextPosition = allEntries.Select(entry => entry.Task.QueuePosition ?? 0).DefaultIfEmpty().Max() + 1;
        var timestamp = DateTimeOffset.UtcNow;
        foreach (var entry in activeEntries.Where(entry => entry.Task.QueuePosition is null))
        {
            entry.Task.MoveTo(nextPosition++, GetNonDecreasingTimestamp(entry.Task, timestamp));
        }

        var current = activeEntries[currentIndex].Task;
        var target = activeEntries[targetIndex].Task;
        var currentPosition = current.QueuePosition!.Value;
        current.MoveTo(target.QueuePosition!.Value, GetNonDecreasingTimestamp(current, timestamp));
        target.MoveTo(currentPosition, GetNonDecreasingTimestamp(target, timestamp));

        await _repository.SaveAsync(project, cancellationToken);
        SafeRecord(new GenerationQueueDiagnosticsEvent(
            current.UpdatedAt,
            GenerationQueueDiagnosticsEventName.Moved,
            projectId,
            current.Id,
            current.Status.ToString(),
            current.QueuePosition,
            Direction: direction.ToString()));
    }

    public async Task<Guid> RetryGenerationTaskAsync(
        Guid projectId,
        Guid taskId,
        CancellationToken cancellationToken)
    {
        var project = await RequireProjectAsync(projectId, cancellationToken);
        var original = RequireTaskEntry(project, taskId);
        if (original.Task.Status is not (GenerationTaskStatus.Failed or GenerationTaskStatus.Cancelled))
        {
            throw new InvalidOperationException("Only failed or cancelled generation tasks can be retried.");
        }

        var queuedAt = DateTimeOffset.UtcNow;
        var nextPosition = GetTaskEntries(project)
            .Select(entry => entry.Task.QueuePosition ?? 0)
            .DefaultIfEmpty()
            .Max() + 1;
        var retry = original.Item.AddGenerationTask(
            new GenerationTask(
                Guid.NewGuid(),
                original.Item.Id,
                original.Prompt.Id,
                original.Task.ProviderProfileId,
                GenerationTaskStatus.Queued,
                attemptCount: 0,
                original.Task.MaxRetries,
                queuedAt,
                queuedAt,
                queuePosition: nextPosition,
                retryOfTaskId: original.Task.Id),
            queuedAt);

        await _repository.SaveAsync(project, cancellationToken);
        SafeRecord(new GenerationQueueDiagnosticsEvent(
            retry.UpdatedAt,
            GenerationQueueDiagnosticsEventName.Retried,
            projectId,
            retry.Id,
            retry.Status.ToString(),
            retry.QueuePosition,
            RetryOfTaskId: original.Task.Id));
        return retry.Id;
    }

    public async Task<GenerationQueueRun> ExecutePreparedGenerationQueueAsync(
        Guid projectId,
        string outputDirectory,
        CancellationToken cancellationToken)
    {
        var imageGenerationProvider = RequireFakeImageGenerationProvider();

        var project = await RequireProjectAsync(projectId, cancellationToken);
        var queue = new GenerationQueue(
            imageGenerationProvider,
            new GenerationQueueOptions(MaxConcurrency: 1, MaxRetries: 0));
        var workItems = GetTaskEntries(project)
            .Where(entry => entry.Task.Status is GenerationTaskStatus.Queued)
            .OrderBy(entry => entry.Task.QueuePosition ?? int.MaxValue)
            .ThenBy(entry => entry.Task.CreatedAt)
            .ThenBy(entry => entry.Task.Id)
            .Select((entry, index) => new DurableGenerationWorkItem(
                CreateGenerationRequest(
                    entry,
                    outputDirectory,
                    entry.Task.QueuePosition ?? index + 1),
                entry.Task,
                entry.Item,
                entry.Prompt))
            .ToArray();

        var taskResults = new List<GenerationQueueTaskResult>(workItems.Length);
        var images = new List<ImageGenerationResult>(workItems.Length);

        foreach (var workItem in workItems)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                break;
            }

            workItem.Task.Start(GetCheckpointTimestamp(workItem.Task));
            await _repository.SaveAsync(project, cancellationToken);
            SafeRecord(new GenerationQueueDiagnosticsEvent(
                workItem.Task.UpdatedAt,
                GenerationQueueDiagnosticsEventName.ExecutionStarted,
                projectId,
                workItem.Task.Id,
                workItem.Task.Status.ToString(),
                workItem.Task.QueuePosition));

            var itemRun = await queue.RunAsync([workItem.Request], cancellationToken);
            var queueResult = itemRun.Tasks.Single();
            var result = queueResult with { Id = workItem.Task.Id };
            PersistTerminalResult(
                workItem,
                result,
                itemRun.Images.SingleOrDefault(),
                GetCheckpointTimestamp(workItem.Task));
            await _repository.SaveAsync(project, CancellationToken.None);
            SafeRecord(new GenerationQueueDiagnosticsEvent(
                workItem.Task.UpdatedAt,
                ToTerminalEventName(workItem.Task.Status),
                projectId,
                workItem.Task.Id,
                workItem.Task.Status.ToString(),
                workItem.Task.QueuePosition));

            taskResults.Add(result);
            images.AddRange(itemRun.Images);
        }

        return new GenerationQueueRun(taskResults, images);
    }

    public async Task<ImageGenerationResult> RunImageEditAsync(
        ImageEditWorkflowRequest request,
        CancellationToken cancellationToken)
    {
        if (_imageEditProvider is null)
        {
            throw new InvalidOperationException("Image edit provider is not registered.");
        }

        if (!_imageEditProvider.Capabilities.ProviderId.StartsWith("fake", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Real image editing requires explicit approval.");
        }

        if (!_imageEditProvider.Capabilities.SupportsImageEditing)
        {
            throw new InvalidOperationException("Provider does not support image editing.");
        }

        _ = await RequireProjectAsync(request.ProjectId, cancellationToken);

        return await _imageEditProvider.EditImageAsync(
            new ImageEditRequest(
                request.SeriesItemId,
                request.SourceCandidateImageId,
                request.SourceImagePath,
                request.MaskImagePath,
                request.PromptText,
                request.Settings,
                request.OutputDirectory,
                request.OutputFileName,
                request.Recipe),
            cancellationToken);
    }

    private async Task<ImageProject> RequireProjectAsync(Guid projectId, CancellationToken cancellationToken)
    {
        return await _repository.LoadAsync(projectId, cancellationToken)
            ?? throw new InvalidOperationException($"Project not found: {projectId}");
    }

    private async Task MutateTaskAsync(
        Guid projectId,
        Guid taskId,
        Action<GenerationTask> mutation,
        GenerationQueueDiagnosticsEventName eventName,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(mutation);
        var project = await RequireProjectAsync(projectId, cancellationToken);
        var entry = RequireTaskEntry(project, taskId);
        mutation(entry.Task);
        await _repository.SaveAsync(project, cancellationToken);
        SafeRecord(new GenerationQueueDiagnosticsEvent(
            entry.Task.UpdatedAt,
            eventName,
            projectId,
            entry.Task.Id,
            entry.Task.Status.ToString(),
            entry.Task.QueuePosition));
    }

    private void SafeRecord(GenerationQueueDiagnosticsEvent value)
    {
        try
        {
            _eventJournal.Record(value);
        }
        catch (Exception exception) when (exception is not StackOverflowException)
        {
            // Diagnostics must never change queue behavior or provider authorization.
        }
    }

    private static GenerationQueueDiagnosticsEventName ToTerminalEventName(GenerationTaskStatus status)
    {
        return status switch
        {
            GenerationTaskStatus.Succeeded => GenerationQueueDiagnosticsEventName.ExecutionSucceeded,
            GenerationTaskStatus.Failed => GenerationQueueDiagnosticsEventName.ExecutionFailed,
            GenerationTaskStatus.Cancelled => GenerationQueueDiagnosticsEventName.ExecutionCancelled,
            _ => throw new InvalidOperationException($"Generation status {status} is not terminal."),
        };
    }

    private IImageGenerationProvider RequireFakeImageGenerationProvider()
    {
        if (_imageGenerationProvider is null)
        {
            throw new InvalidOperationException("Image generation provider is not registered.");
        }

        if (!_imageGenerationProvider.Capabilities.ProviderId.StartsWith("fake", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Real image generation requires explicit approval.");
        }

        return _imageGenerationProvider;
    }

    private static IReadOnlyList<GenerationTaskEntry> GetTaskEntries(ImageProject project)
    {
        return project.Series
            .SelectMany(series => series.Items)
            .SelectMany(item => item.GenerationTasks.Select(task => new GenerationTaskEntry(
                task,
                item,
                item.PromptVersions.Single(prompt => prompt.Id == task.PromptVersionId))))
            .ToArray();
    }

    private static GenerationTaskEntry RequireTaskEntry(ImageProject project, Guid taskId)
    {
        return GetTaskEntries(project).SingleOrDefault(entry => entry.Task.Id == taskId)
            ?? throw new InvalidOperationException($"Generation task not found: {taskId}");
    }

    private static ImageGenerationRequest CreateGenerationRequest(
        GenerationTaskEntry entry,
        string outputDirectory,
        int executionIndex)
    {
        return new ImageGenerationRequest(
            entry.Item.Id,
            entry.Prompt.Id,
            entry.Prompt.PromptText,
            entry.Prompt.Settings,
            outputDirectory,
            $"{executionIndex:000}-{SanitizeFileName(entry.Item.Title)}.png");
    }

    private static void PersistTerminalResult(
        DurableGenerationWorkItem workItem,
        GenerationQueueTaskResult result,
        ImageGenerationResult? generatedImage,
        DateTimeOffset persistedAt)
    {
        if (result.Status is GenerationTaskStatus.Succeeded && generatedImage is null)
        {
            throw new InvalidOperationException("A succeeded queue result must include a generated image.");
        }

        if (result.Status is not GenerationTaskStatus.Succeeded && generatedImage is not null)
        {
            throw new InvalidOperationException("Only a succeeded queue result may include a generated image.");
        }

        switch (result.Status)
        {
            case GenerationTaskStatus.Succeeded:
                workItem.Task.Succeed(persistedAt);
                break;
            case GenerationTaskStatus.Failed:
                workItem.Task.Fail(result.ErrorMessage ?? "Generation failed.", persistedAt);
                break;
            case GenerationTaskStatus.Cancelled:
                workItem.Task.Cancel(result.ErrorMessage ?? "Generation cancelled.", persistedAt);
                break;
            default:
                throw new InvalidOperationException($"Queue returned non-terminal status {result.Status}.");
        }

        if (generatedImage is null)
        {
            return;
        }

        workItem.Item.AddCandidateImage(
                new CandidateImage(
                    generatedImage.CandidateImageId,
                    workItem.Item.Id,
                    workItem.Prompt.Id,
                    workItem.Task.Id,
                    workItem.Prompt.ProviderProfileId,
                    CandidateImageStatus.ReviewPending,
                    generatedImage.AssetPath,
                    generatedImage.MetadataPath,
                    generatedImage.GeneratedAt),
                generatedImage.GeneratedAt);
    }

    private static DateTimeOffset GetCheckpointTimestamp(GenerationTask task)
    {
        var timestamp = DateTimeOffset.UtcNow;
        return timestamp < task.UpdatedAt ? task.UpdatedAt : timestamp;
    }

    private static DateTimeOffset GetNonDecreasingTimestamp(GenerationTask task, DateTimeOffset timestamp)
    {
        return timestamp < task.UpdatedAt ? task.UpdatedAt : timestamp;
    }

    private static string SanitizeFileName(string value)
    {
        var invalidChars = Path.GetInvalidFileNameChars();
        var sanitized = new string(value.Select(character => invalidChars.Contains(character) ? '-' : character).ToArray());
        return string.IsNullOrWhiteSpace(sanitized) ? "image" : sanitized.Trim();
    }

    private sealed record DurableGenerationWorkItem(
        ImageGenerationRequest Request,
        GenerationTask Task,
        SeriesItem Item,
        PromptVersion Prompt);

    private sealed record GenerationTaskEntry(
        GenerationTask Task,
        SeriesItem Item,
        PromptVersion Prompt);
}

public sealed record GenerationQueuePreparation(IReadOnlyList<Guid> TaskIds);

public enum GenerationTaskMoveDirection
{
    Up = 0,
    Down = 1,
}
