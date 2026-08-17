using ContentDeliveryStudio.Application.Diagnostics;
using ContentDeliveryStudio.Core.Generation;
using ContentDeliveryStudio.Core.Projects;
using ContentDeliveryStudio.Core.Providers;
using ContentDeliveryStudio.Core.References;
using System.Security.Cryptography;
using System.Text;

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
        foreach (var entry in activeEntries.Where(entry => entry.Task.ApprovalReceipt is not null))
        {
            entry.Task.InvalidateApproval(GetNonDecreasingTimestamp(entry.Task, timestamp));
        }

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

        return await ExecuteQueueAsync(
            projectId,
            outputDirectory,
            imageGenerationProvider,
            approvalReceiptId: null,
            cancellationToken);
    }

    public async Task<GenerationApprovalReceipt> ApprovePreparedLiveGenerationQueueAsync(
        Guid projectId,
        GenerationQueueApprovalRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var imageGenerationProvider = RequireLiveImageGenerationProvider();
        if (!request.ExplicitPaidAuthorityConfirmed)
        {
            throw new InvalidOperationException("Explicit paid-provider authority is required.");
        }

        if (!imageGenerationProvider.Capabilities.ModelIds.Contains(request.ModelId, StringComparer.Ordinal))
        {
            throw new InvalidOperationException($"Provider does not expose approved model: {request.ModelId}");
        }

        var project = await RequireProjectAsync(projectId, cancellationToken);
        var entries = GetTaskEntries(project)
            .Where(entry => entry.Task.Status is GenerationTaskStatus.Queued or GenerationTaskStatus.Paused)
            .OrderBy(entry => entry.Task.QueuePosition ?? int.MaxValue)
            .ThenBy(entry => entry.Task.CreatedAt)
            .ThenBy(entry => entry.Task.Id)
            .ToArray();
        if (entries.Length == 0)
        {
            throw new InvalidOperationException("No prepared generation operations are available for approval.");
        }

        var now = DateTimeOffset.UtcNow;
        var requestSet = CreateApprovalRequestSet(
            project.Id,
            entries,
            imageGenerationProvider,
            request.ModelId,
            request.EstimatedCostPerOperationUsd);
        var receipt = GenerationApprovalReceipt.Issue(
            requestSet,
            request.EstimatedCostPerOperationUsd * entries.Length,
            request.MaximumCostUsd,
            request.ApprovalSource,
            request.AuthorityReference,
            now,
            request.ExpiresAt);
        foreach (var entry in entries)
        {
            entry.Task.AttachApproval(receipt, GetNonDecreasingTimestamp(entry.Task, now));
        }

        await _repository.SaveAsync(project, cancellationToken);
        return receipt;
    }

    public async Task<GenerationQueueRun> ExecuteApprovedLiveGenerationQueueAsync(
        Guid projectId,
        string outputDirectory,
        Guid approvalReceiptId,
        CancellationToken cancellationToken)
    {
        if (approvalReceiptId == Guid.Empty)
        {
            throw new ArgumentException("Approval receipt id is required.", nameof(approvalReceiptId));
        }

        return await ExecuteQueueAsync(
            projectId,
            outputDirectory,
            RequireLiveImageGenerationProvider(),
            approvalReceiptId,
            cancellationToken);
    }

    private async Task<GenerationQueueRun> ExecuteQueueAsync(
        Guid projectId,
        string outputDirectory,
        IImageGenerationProvider imageGenerationProvider,
        Guid? approvalReceiptId,
        CancellationToken cancellationToken)
    {

        var project = await RequireProjectAsync(projectId, cancellationToken);
        var queue = new GenerationQueue(
            imageGenerationProvider,
            new GenerationQueueOptions(MaxConcurrency: 1, MaxRetries: 0));
        var workItems = GetTaskEntries(project)
            .Where(entry => entry.Task.Status is GenerationTaskStatus.Queued
                && (approvalReceiptId is null || entry.Task.ApprovalReceipt?.Id == approvalReceiptId))
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

        if (approvalReceiptId is not null && workItems.Length == 0)
        {
            throw new InvalidOperationException("No queued generation operations match the approval receipt.");
        }

        foreach (var workItem in workItems)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                break;
            }

            if (workItem.Task.Status is not GenerationTaskStatus.Queued)
            {
                continue;
            }

            if (approvalReceiptId is { } receiptId)
            {
                ValidateLiveApprovalBeforeDispatch(project, imageGenerationProvider, receiptId);
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

    public async Task<ImageEditApprovalReceipt> ApproveImageEditAsync(
        ImageEditWorkflowRequest request,
        ImageEditApprovalRequest approval,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(approval);
        var provider = RequireLiveImageEditProvider();
        if (!approval.ExplicitPaidAuthorityConfirmed)
        {
            throw new InvalidOperationException("Explicit paid-provider authority is required.");
        }

        ValidateImageEditCapabilities(provider, request);
        if (!provider.Capabilities.ModelIds.Contains(approval.ModelId, StringComparer.Ordinal))
        {
            throw new InvalidOperationException($"Provider does not expose approved edit model: {approval.ModelId}");
        }

        var project = await RequireProjectAsync(request.ProjectId, cancellationToken);
        var context = RequireImageEditContext(project, request);
        var requestSet = CreateImageEditApprovalRequestSet(
            project,
            context,
            request,
            provider,
            approval.ModelId,
            approval.EstimatedCostUsd);
        var now = DateTimeOffset.UtcNow;
        return ImageEditApprovalReceipt.Issue(
            requestSet,
            approval.MaximumCostUsd,
            approval.ApprovedBy,
            approval.AuthorityReference,
            now,
            approval.ExpiresAt);
    }

    public async Task<ImageGenerationResult> RunApprovedImageEditAsync(
        ImageEditWorkflowRequest request,
        ImageEditApprovalReceipt receipt,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(receipt);
        var provider = RequireLiveImageEditProvider();
        ValidateImageEditCapabilities(provider, request);
        var project = await RequireProjectAsync(request.ProjectId, cancellationToken);
        var context = RequireImageEditContext(project, request);
        var modelId = ResolveSingleEditModelId(provider);
        var requestSet = CreateImageEditApprovalRequestSet(
            project,
            context,
            request,
            provider,
            modelId,
            receipt.EstimatedCostUsd);

        receipt.Validate(requestSet, DateTimeOffset.UtcNow);
        var references = new[]
        {
            new ImageEditReferenceInput(
                context.SourceCandidate.Id,
                ReferenceImageRole.Subject,
                context.SourceCandidate.AssetPath,
                requestSet.SourceSha256),
        };
        var result = await provider.EditImageAsync(
            new ImageEditRequest(
                request.SeriesItemId,
                request.SourceCandidateImageId,
                context.SourceCandidate.AssetPath,
                request.MaskImagePath,
                request.PromptText,
                request.Settings,
                request.OutputDirectory,
                request.OutputFileName,
                request.Recipe,
                references),
            cancellationToken);

        var provenance = new CandidateImageEditProvenance(
            Guid.NewGuid(),
            context.SourceCandidate.Id,
            requestSet.SourceSha256,
            requestSet.MaskSha256,
            requestSet.InstructionSha256,
            requestSet.ProviderId,
            requestSet.EndpointClass,
            requestSet.ModelId,
            requestSet.References.Select(reference => new CandidateImageEditReferenceProvenance(
                reference.ReferenceId,
                reference.Role.ToString(),
                reference.Sha256)).ToArray(),
            receipt.Id,
            receipt.RequestSetHash,
            result.GeneratedAt);
        context.Item.AddCandidateImage(
            new CandidateImage(
                result.CandidateImageId,
                context.Item.Id,
                context.SourceCandidate.PromptVersionId,
                generationTaskId: null,
                context.SourceCandidate.ProviderProfileId,
                CandidateImageStatus.ReviewPending,
                result.AssetPath,
                result.MetadataPath,
                result.GeneratedAt,
                provenance),
            result.GeneratedAt);
        await _repository.SaveAsync(project, CancellationToken.None);
        return result;
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

    private IImageEditProvider RequireLiveImageEditProvider()
    {
        if (_imageEditProvider is null)
        {
            throw new InvalidOperationException("Image edit provider is not registered.");
        }

        if (_imageEditProvider.Capabilities.ProviderId.StartsWith("fake", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Approved image editing requires a real provider.");
        }

        return _imageEditProvider;
    }

    private static void ValidateImageEditCapabilities(
        IImageEditProvider provider,
        ImageEditWorkflowRequest request)
    {
        var capabilities = provider.Capabilities;
        if (!capabilities.SupportsImageEditing
            || !capabilities.SupportsReferenceImages
            || capabilities.MaxReferenceImageCount < 1
            || !capabilities.SupportedReferenceImageRoles.Contains(ReferenceImageRole.Subject))
        {
            throw new InvalidOperationException("Provider does not support the required subject-reference image edit contract.");
        }

        if (!string.IsNullOrWhiteSpace(request.MaskImagePath) && !capabilities.SupportsMaskEditing)
        {
            throw new InvalidOperationException("Provider does not support mask editing.");
        }

        if (string.IsNullOrWhiteSpace(request.PromptText))
        {
            throw new ArgumentException("Image edit instruction is required.", nameof(request));
        }

        if (!capabilities.SupportedSizes.Any(size =>
                size.Width == request.Settings.Width && size.Height == request.Settings.Height))
        {
            throw new InvalidOperationException("Provider does not support the requested image edit size.");
        }

        var quality = request.Settings.Quality.Trim();
        if (!capabilities.SupportedQualities.Contains(quality, StringComparer.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Provider does not support the requested image edit quality.");
        }

        var outputFormat = request.Settings.OutputFormat.Trim().Equals("jpg", StringComparison.OrdinalIgnoreCase)
            ? "jpeg"
            : request.Settings.OutputFormat.Trim();
        if (!capabilities.SupportedOutputFormats.Contains(outputFormat, StringComparer.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Provider does not support the requested image edit output format.");
        }
    }

    private static ImageEditContext RequireImageEditContext(
        ImageProject project,
        ImageEditWorkflowRequest request)
    {
        var item = project.Series
            .SelectMany(series => series.Items)
            .SingleOrDefault(candidate => candidate.Id == request.SeriesItemId)
            ?? throw new InvalidOperationException($"Series item not found: {request.SeriesItemId}");
        var sourceCandidate = item.CandidateImages
            .SingleOrDefault(candidate => candidate.Id == request.SourceCandidateImageId)
            ?? throw new InvalidOperationException($"Source candidate image not found: {request.SourceCandidateImageId}");
        if (!Path.GetFullPath(sourceCandidate.AssetPath)
            .Equals(Path.GetFullPath(request.SourceImagePath), StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Source image path does not match the persisted candidate lineage.");
        }

        if (!File.Exists(sourceCandidate.AssetPath))
        {
            throw new FileNotFoundException("Persisted source candidate image was not found.", sourceCandidate.AssetPath);
        }

        return new ImageEditContext(item, sourceCandidate);
    }

    private static ImageEditApprovalRequestSet CreateImageEditApprovalRequestSet(
        ImageProject project,
        ImageEditContext context,
        ImageEditWorkflowRequest request,
        IImageEditProvider provider,
        string modelId,
        decimal estimatedCostUsd)
    {
        if (estimatedCostUsd < 0m)
        {
            throw new ArgumentOutOfRangeException(nameof(estimatedCostUsd));
        }

        var sourceSha256 = ComputeFileSha256(context.SourceCandidate.AssetPath);
        var sourceLength = new FileInfo(context.SourceCandidate.AssetPath).Length;
        if (provider.Capabilities.MaxReferenceImageBytes is { } maximumBytes
            && sourceLength > maximumBytes)
        {
            throw new InvalidOperationException("Source candidate exceeds the provider reference-image size limit.");
        }

        var maskSha256 = string.IsNullOrWhiteSpace(request.MaskImagePath)
            ? null
            : ComputeFileSha256(request.MaskImagePath);
        return new ImageEditApprovalRequestSet(
            project.Id,
            context.Item.Id,
            context.SourceCandidate.Id,
            sourceSha256,
            maskSha256,
            ComputeTextSha256(request.PromptText),
            provider.Capabilities.ProviderId,
            "images/edits",
            modelId,
            request.Settings.Width,
            request.Settings.Height,
            request.Settings.Quality,
            request.Settings.OutputFormat,
            [new ImageEditApprovalReference(context.SourceCandidate.Id, ReferenceImageRole.Subject, sourceSha256)],
            estimatedCostUsd);
    }

    private static string ResolveSingleEditModelId(IImageEditProvider provider)
    {
        return provider.Capabilities.ModelIds.Count == 1
            ? provider.Capabilities.ModelIds[0]
            : throw new InvalidOperationException("Approved image edit execution requires one unambiguous provider model.");
    }

    private static string ComputeFileSha256(string path)
    {
        return Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(path)));
    }

    private static string ComputeTextSha256(string value)
    {
        return Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
    }

    private IImageGenerationProvider RequireLiveImageGenerationProvider()
    {
        if (_imageGenerationProvider is null)
        {
            throw new InvalidOperationException("Image generation provider is not registered.");
        }

        if (_imageGenerationProvider.Capabilities.ProviderId.StartsWith("fake", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Live generation requires a non-fake image provider.");
        }

        if (!_imageGenerationProvider.Capabilities.IsDirectProviderIdentity)
        {
            throw new InvalidOperationException(
                "Live approval requires a direct provider identity; failover destinations need separate receipt coverage.");
        }

        return _imageGenerationProvider;
    }

    private static void ValidateLiveApprovalBeforeDispatch(
        ImageProject project,
        IImageGenerationProvider provider,
        Guid approvalReceiptId)
    {
        var approvedEntries = GetTaskEntries(project)
            .Where(entry => entry.Task.ApprovalReceipt?.Id == approvalReceiptId)
            .OrderBy(entry => entry.Task.QueuePosition ?? int.MaxValue)
            .ThenBy(entry => entry.Task.CreatedAt)
            .ThenBy(entry => entry.Task.Id)
            .ToArray();
        var receipt = approvedEntries.Select(entry => entry.Task.ApprovalReceipt).FirstOrDefault()
            ?? throw new InvalidOperationException("Generation approval receipt is absent.");
        if (approvedEntries.Any(entry => entry.Task.ApprovalReceipt?.Id != receipt.Id
                || entry.Task.ApprovalReceipt?.RequestSetHash != receipt.RequestSetHash))
        {
            throw new InvalidOperationException("Generation approval receipt is inconsistent across the request set.");
        }

        receipt.Validate(
            CreateApprovalRequestSet(
                project.Id,
                approvedEntries,
                provider,
                receipt.ModelId,
                receipt.EstimatedCostPerOperationUsd),
            DateTimeOffset.UtcNow);
    }

    private static GenerationApprovalRequestSet CreateApprovalRequestSet(
        Guid projectId,
        IReadOnlyList<GenerationTaskEntry> entries,
        IImageGenerationProvider provider,
        string modelId,
        decimal estimatedCostPerOperationUsd)
    {
        return new GenerationApprovalRequestSet(
            projectId,
            provider.Capabilities.ProviderId,
            "images",
            modelId,
            entries.Select(entry => new GenerationApprovalOperation(
                entry.Task.Id,
                entry.Item.SeriesId ?? throw new InvalidOperationException("Generation item is not attached to a series."),
                entry.Prompt.Id,
                entry.Task.ProviderProfileId,
                entry.Prompt.PromptText,
                Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(entry.Prompt.PromptText))),
                entry.Prompt.Settings.Width,
                entry.Prompt.Settings.Height,
                entry.Prompt.Settings.Quality,
                entry.Prompt.Settings.OutputFormat,
                "auto",
                entry.Prompt.Settings.Seed,
                entry.Task.MaxRetries,
                estimatedCostPerOperationUsd)).ToArray());
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

    private sealed record ImageEditContext(
        SeriesItem Item,
        CandidateImage SourceCandidate);
}

public sealed record GenerationQueuePreparation(IReadOnlyList<Guid> TaskIds);

public sealed record GenerationQueueApprovalRequest(
    string ModelId,
    decimal EstimatedCostPerOperationUsd,
    decimal MaximumCostUsd,
    string ApprovalSource,
    string AuthorityReference,
    DateTimeOffset ExpiresAt,
    bool ExplicitPaidAuthorityConfirmed);

public sealed record ImageEditApprovalRequest(
    string ModelId,
    decimal EstimatedCostUsd,
    decimal MaximumCostUsd,
    string ApprovedBy,
    string AuthorityReference,
    DateTimeOffset ExpiresAt,
    bool ExplicitPaidAuthorityConfirmed);

public enum GenerationTaskMoveDirection
{
    Up = 0,
    Down = 1,
}
