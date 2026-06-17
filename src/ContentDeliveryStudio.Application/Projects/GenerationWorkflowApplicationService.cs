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

        var run = await queue.RunAsync(requests, cancellationToken);
        PersistGenerationRun(project, run, DateTimeOffset.UtcNow);
        await _repository.SaveAsync(project, cancellationToken);

        return run;
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

    private static void PersistGenerationRun(
        ImageProject project,
        GenerationQueueRun run,
        DateTimeOffset persistedAt)
    {
        var succeededTasks = run.Tasks
            .Where(task => task.Status is GenerationTaskStatus.Succeeded)
            .ToArray();
        var imagesByTaskId = succeededTasks
            .Zip(run.Images, (task, image) => new { task.Id, Image = image })
            .ToDictionary(entry => entry.Id, entry => entry.Image);
        var itemsById = project.Series
            .SelectMany(series => series.Items)
            .ToDictionary(item => item.Id);

        foreach (var taskResult in run.Tasks)
        {
            if (!itemsById.TryGetValue(taskResult.SeriesItemId, out var item))
            {
                continue;
            }

            var prompt = item.PromptVersions.SingleOrDefault(existing => existing.Id == taskResult.PromptVersionId);
            if (prompt is null)
            {
                continue;
            }

            var timestamp = imagesByTaskId.TryGetValue(taskResult.Id, out var generatedImage)
                ? generatedImage.GeneratedAt
                : persistedAt;

            item.AddGenerationTask(
                new GenerationTask(
                    taskResult.Id,
                    item.Id,
                    prompt.Id,
                    prompt.ProviderProfileId,
                    taskResult.Status,
                    taskResult.AttemptCount,
                    maxRetries: 0,
                    timestamp,
                    timestamp),
                timestamp);

            if (generatedImage is null)
            {
                continue;
            }

            item.AddCandidateImage(
                new CandidateImage(
                    generatedImage.CandidateImageId,
                    item.Id,
                    prompt.Id,
                    taskResult.Id,
                    prompt.ProviderProfileId,
                    CandidateImageStatus.ReviewPending,
                    generatedImage.AssetPath,
                    generatedImage.MetadataPath,
                    generatedImage.GeneratedAt),
                generatedImage.GeneratedAt);
        }
    }

    private static string SanitizeFileName(string value)
    {
        var invalidChars = Path.GetInvalidFileNameChars();
        var sanitized = new string(value.Select(character => invalidChars.Contains(character) ? '-' : character).ToArray());
        return string.IsNullOrWhiteSpace(sanitized) ? "image" : sanitized.Trim();
    }
}
