using ContentDeliveryStudio.Core.Generation;
using ContentDeliveryStudio.Core.Projects;
using ContentDeliveryStudio.Core.Providers;

namespace ContentDeliveryStudio.Application.Projects;

public sealed class GenerationWorkflowApplicationService
{
    private readonly IProjectRepository _repository;
    private readonly IImageGenerationProvider? _imageGenerationProvider;
    private readonly IImageEditProvider? _imageEditProvider;

    public GenerationWorkflowApplicationService(
        IProjectRepository repository,
        IImageGenerationProvider? imageGenerationProvider,
        IImageEditProvider? imageEditProvider)
    {
        _repository = repository;
        _imageGenerationProvider = imageGenerationProvider;
        _imageEditProvider = imageEditProvider;
    }

    public async Task<GenerationQueueRun> RunGenerationQueueAsync(
        Guid projectId,
        string outputDirectory,
        CancellationToken cancellationToken)
    {
        if (_imageGenerationProvider is null)
        {
            throw new InvalidOperationException("Image generation provider is not registered.");
        }

        if (!_imageGenerationProvider.Capabilities.ProviderId.StartsWith("fake", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Real image generation requires explicit approval.");
        }

        var project = await RequireProjectAsync(projectId, cancellationToken);
        var requests = CreateGenerationRequests(project, outputDirectory);
        var queue = new GenerationQueue(
            _imageGenerationProvider,
            new GenerationQueueOptions(MaxConcurrency: 1, MaxRetries: 0));
        var workItems = CreateDurableWorkItems(project, requests, DateTimeOffset.UtcNow);
        await _repository.SaveAsync(project, cancellationToken);

        var taskResults = new List<GenerationQueueTaskResult>(workItems.Count);
        var images = new List<ImageGenerationResult>(workItems.Count);

        foreach (var workItem in workItems)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                CancelUndispatchedTasks(workItems, taskResults, DateTimeOffset.UtcNow);
                await _repository.SaveAsync(project, CancellationToken.None);
                break;
            }

            workItem.Task.Start(GetCheckpointTimestamp(workItem.Task));
            await _repository.SaveAsync(project, cancellationToken);

            var itemRun = await queue.RunAsync([workItem.Request], cancellationToken);
            var queueResult = itemRun.Tasks.Single();
            var result = queueResult with { Id = workItem.Task.Id };
            PersistTerminalResult(
                workItem,
                result,
                itemRun.Images.SingleOrDefault(),
                GetCheckpointTimestamp(workItem.Task));
            await _repository.SaveAsync(project, CancellationToken.None);

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

    private static IReadOnlyList<ImageGenerationRequest> CreateGenerationRequests(
        ImageProject project,
        string outputDirectory)
    {
        var index = 0;
        return project.Series
            .SelectMany(series => series.Items)
            .Select(item => new
            {
                Item = item,
                Prompt = item.PromptVersions.OrderByDescending(prompt => prompt.VersionNumber).FirstOrDefault(),
            })
            .Where(value => value.Prompt is not null)
            .Select(value =>
            {
                index++;
                return new ImageGenerationRequest(
                    value.Item.Id,
                    value.Prompt!.Id,
                    value.Prompt.PromptText,
                    value.Prompt.Settings,
                    outputDirectory,
                    $"{index:000}-{SanitizeFileName(value.Item.Title)}.png");
            })
            .ToArray();
    }

    private static IReadOnlyList<DurableGenerationWorkItem> CreateDurableWorkItems(
        ImageProject project,
        IReadOnlyList<ImageGenerationRequest> requests,
        DateTimeOffset queuedAt)
    {
        var itemsById = project.Series
            .SelectMany(series => series.Items)
            .ToDictionary(item => item.Id);
        var workItems = new List<DurableGenerationWorkItem>(requests.Count);

        foreach (var request in requests)
        {
            if (!itemsById.TryGetValue(request.SeriesItemId, out var item))
            {
                continue;
            }

            var prompt = item.PromptVersions.SingleOrDefault(existing => existing.Id == request.PromptVersionId);
            if (prompt is null)
            {
                continue;
            }

            var taskQueuedAt = queuedAt.AddTicks(workItems.Count);
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
                    taskQueuedAt),
                taskQueuedAt);
            workItems.Add(new DurableGenerationWorkItem(request, task, item, prompt));
        }

        return workItems;
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

    private static void CancelUndispatchedTasks(
        IReadOnlyList<DurableGenerationWorkItem> workItems,
        ICollection<GenerationQueueTaskResult> results,
        DateTimeOffset timestamp)
    {
        foreach (var workItem in workItems.Where(item => item.Task.Status is GenerationTaskStatus.Queued))
        {
            const string reason = "Generation was cancelled before provider dispatch.";
            var cancellationTimestamp = timestamp < workItem.Task.UpdatedAt
                ? workItem.Task.UpdatedAt
                : timestamp;
            workItem.Task.Cancel(reason, cancellationTimestamp);
            results.Add(new GenerationQueueTaskResult(
                workItem.Task.Id,
                workItem.Item.Id,
                workItem.Prompt.Id,
                GenerationTaskStatus.Cancelled,
                workItem.Task.AttemptCount,
                reason));
        }
    }

    private static DateTimeOffset GetCheckpointTimestamp(GenerationTask task)
    {
        var timestamp = DateTimeOffset.UtcNow;
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
}
