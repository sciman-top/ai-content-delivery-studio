using ImageSeriesStudio.Core.Projects;
using ImageSeriesStudio.Core.Providers;
using ImageSeriesStudio.Core.Styles;

namespace ImageSeriesStudio.Core.Generation;

public sealed class GenerationQueue
{
    private readonly IImageGenerationProvider _provider;
    private readonly GenerationQueueOptions _options;

    public GenerationQueue(IImageGenerationProvider provider, GenerationQueueOptions? options = null)
    {
        _provider = provider;
        _options = options ?? new GenerationQueueOptions();

        if (_options.MaxConcurrency < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(options), "MaxConcurrency must be at least 1.");
        }

        if (_options.MaxRetries < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(options), "MaxRetries cannot be negative.");
        }
    }

    public async Task<GenerationQueueRun> RunAsync(
        IReadOnlyList<ImageGenerationRequest> requests,
        CancellationToken cancellationToken)
    {
        using var concurrency = new SemaphoreSlim(_options.MaxConcurrency);

        var executions = requests
            .Select(request =>
            {
                var validationErrors = ValidateRecipe(request);
                return validationErrors.Count == 0
                    ? RunWithThrottleAsync(request, concurrency, cancellationToken)
                    : Task.FromResult(GenerationQueueExecution.Failed(
                        request,
                        attemptCount: 0,
                        string.Join(" ", validationErrors)));
            })
            .ToArray();

        var results = await Task.WhenAll(executions);

        return new GenerationQueueRun(
            results.Select(result => result.Task).ToArray(),
            results.Where(result => result.Image is not null).Select(result => result.Image!).ToArray());
    }

    private async Task<GenerationQueueExecution> RunWithThrottleAsync(
        ImageGenerationRequest request,
        SemaphoreSlim concurrency,
        CancellationToken cancellationToken)
    {
        var entered = false;

        try
        {
            await concurrency.WaitAsync(cancellationToken);
            entered = true;
            return await ExecuteWithRetryAsync(request, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            return GenerationQueueExecution.Cancelled(request, attemptCount: 0);
        }
        finally
        {
            if (entered)
            {
                concurrency.Release();
            }
        }
    }

    private async Task<GenerationQueueExecution> ExecuteWithRetryAsync(
        ImageGenerationRequest request,
        CancellationToken cancellationToken)
    {
        var attemptCount = 0;
        Exception? lastError = null;

        while (attemptCount <= _options.MaxRetries)
        {
            attemptCount++;

            try
            {
                using var attemptCancellation = CreateAttemptCancellation(cancellationToken);
                var image = await _provider.GenerateImageAsync(request, attemptCancellation.Token);
                return GenerationQueueExecution.Succeeded(request, attemptCount, image);
            }
            catch (OperationCanceledException)
            {
                return GenerationQueueExecution.Cancelled(request, attemptCount);
            }
            catch (Exception exception) when (attemptCount <= _options.MaxRetries)
            {
                lastError = exception;
            }
            catch (Exception exception)
            {
                lastError = exception;
                break;
            }
        }

        return GenerationQueueExecution.Failed(request, attemptCount, lastError?.Message ?? "Generation failed.");
    }

    private CancellationTokenSource CreateAttemptCancellation(CancellationToken cancellationToken)
    {
        var attemptCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        if (_options.Timeout is { } timeout)
        {
            attemptCancellation.CancelAfter(timeout);
        }

        return attemptCancellation;
    }

    private IReadOnlyList<string> ValidateRecipe(ImageGenerationRequest request)
    {
        if (request.Recipe is null)
        {
            return [];
        }

        var errors = new List<string>();
        var capabilities = _provider.Capabilities;
        var recipe = request.Recipe;

        if (!capabilities.ModelIds.Contains(recipe.ModelId, StringComparer.OrdinalIgnoreCase))
        {
            errors.Add($"Unsupported model: {recipe.ModelId}.");
        }

        if (!capabilities.SupportedSizes.Any(size => size.Width == recipe.Width && size.Height == recipe.Height))
        {
            errors.Add($"Unsupported output size: {recipe.Width}x{recipe.Height}.");
        }

        if (!capabilities.SupportedQualities.Contains(recipe.Quality, StringComparer.OrdinalIgnoreCase))
        {
            errors.Add($"Unsupported quality: {recipe.Quality}.");
        }

        if (!capabilities.SupportedOutputFormats.Contains(recipe.OutputFormat, StringComparer.OrdinalIgnoreCase))
        {
            errors.Add($"Unsupported output format: {recipe.OutputFormat}.");
        }

        var backgroundMode = FormatBackgroundMode(recipe.Background);
        if (!capabilities.SupportedBackgroundModes.Contains(backgroundMode, StringComparer.OrdinalIgnoreCase))
        {
            errors.Add($"Unsupported background mode: {backgroundMode}.");
        }

        return errors;
    }

    private static string FormatBackgroundMode(ImageBackgroundMode background)
    {
        return background switch
        {
            ImageBackgroundMode.Opaque => "opaque",
            ImageBackgroundMode.Transparent => "transparent",
            _ => "auto",
        };
    }
}

public sealed record GenerationQueueOptions(
    int MaxConcurrency = 1,
    int MaxRetries = 0,
    TimeSpan? Timeout = null);

public sealed record GenerationQueueRun(
    IReadOnlyList<GenerationQueueTaskResult> Tasks,
    IReadOnlyList<ImageGenerationResult> Images);

public sealed record GenerationQueueTaskResult(
    Guid Id,
    Guid SeriesItemId,
    Guid PromptVersionId,
    GenerationTaskStatus Status,
    int AttemptCount,
    string? ErrorMessage);

internal sealed record GenerationQueueExecution(
    GenerationQueueTaskResult Task,
    ImageGenerationResult? Image)
{
    public static GenerationQueueExecution Succeeded(
        ImageGenerationRequest request,
        int attemptCount,
        ImageGenerationResult image)
    {
        return new GenerationQueueExecution(
            CreateTask(request, GenerationTaskStatus.Succeeded, attemptCount, errorMessage: null),
            image);
    }

    public static GenerationQueueExecution Failed(
        ImageGenerationRequest request,
        int attemptCount,
        string errorMessage)
    {
        return new GenerationQueueExecution(
            CreateTask(request, GenerationTaskStatus.Failed, attemptCount, errorMessage),
            Image: null);
    }

    public static GenerationQueueExecution Cancelled(ImageGenerationRequest request, int attemptCount)
    {
        return new GenerationQueueExecution(
            CreateTask(request, GenerationTaskStatus.Cancelled, attemptCount, "Generation cancelled."),
            Image: null);
    }

    private static GenerationQueueTaskResult CreateTask(
        ImageGenerationRequest request,
        GenerationTaskStatus status,
        int attemptCount,
        string? errorMessage)
    {
        return new GenerationQueueTaskResult(
            Guid.NewGuid(),
            request.SeriesItemId,
            request.PromptVersionId,
            status,
            attemptCount,
            errorMessage);
    }
}
