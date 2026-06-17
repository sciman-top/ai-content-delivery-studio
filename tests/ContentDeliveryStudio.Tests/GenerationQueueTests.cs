using ContentDeliveryStudio.Core.Generation;
using ContentDeliveryStudio.Core.Projects;
using ContentDeliveryStudio.Core.Providers;
using ContentDeliveryStudio.Core.Styles;

namespace ContentDeliveryStudio.Tests;

public sealed class GenerationQueueTests
{
    [Fact]
    public async Task GenerationQueue_RetriesFailedTaskAndReturnsSucceededState()
    {
        var provider = new FailOnceImageGenerationProvider();
        var queue = new GenerationQueue(provider, new GenerationQueueOptions(MaxConcurrency: 1, MaxRetries: 1));

        var run = await queue.RunAsync([CreateRequest()], CancellationToken.None);

        var task = Assert.Single(run.Tasks);
        Assert.Equal(GenerationTaskStatus.Succeeded, task.Status);
        Assert.Equal(2, task.AttemptCount);
        Assert.Null(task.ErrorMessage);
        Assert.Single(run.Images);
        Assert.Equal(2, provider.CallCount);
    }

    [Fact]
    public async Task GenerationQueue_MarksTaskCancelledWhenProviderObservesCancellation()
    {
        var provider = new DelayingImageGenerationProvider(TimeSpan.FromSeconds(10));
        var queue = new GenerationQueue(provider, new GenerationQueueOptions(MaxConcurrency: 1, MaxRetries: 0));

        using var cancellation = new CancellationTokenSource(TimeSpan.FromMilliseconds(25));
        var run = await queue.RunAsync([CreateRequest()], cancellation.Token);

        var task = Assert.Single(run.Tasks);
        Assert.Equal(GenerationTaskStatus.Cancelled, task.Status);
        Assert.Equal(1, task.AttemptCount);
        Assert.Empty(run.Images);
    }

    [Fact]
    public async Task GenerationQueue_FailsBeforeProviderCallWhenRecipeUnsupportedByCapabilities()
    {
        var provider = new CapabilityBoundImageGenerationProvider();
        var queue = new GenerationQueue(provider);
        var recipe = GenerationRecipe.Create(
            Guid.NewGuid(),
            "test",
            ImageTypePresetCatalog.ArticleCover,
            1024,
            1024,
            "high",
            "webp",
            ImageBackgroundMode.Transparent,
            compression: null,
            ImageModerationMode.Auto,
            seed: null,
            []);
        var request = new ImageGenerationRequest(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "A clean blue poster background",
            new GenerationSettings(1024, 1024, "high", "webp"),
            Path.GetTempPath(),
            "queue-test.webp",
            recipe);

        var run = await queue.RunAsync([request], CancellationToken.None);

        var task = Assert.Single(run.Tasks);
        Assert.Equal(GenerationTaskStatus.Failed, task.Status);
        Assert.Equal(0, task.AttemptCount);
        Assert.Contains("size", task.ErrorMessage, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("quality", task.ErrorMessage, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("format", task.ErrorMessage, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("background", task.ErrorMessage, StringComparison.OrdinalIgnoreCase);
        Assert.Empty(run.Images);
        Assert.Equal(0, provider.CallCount);
    }

    private static ImageGenerationRequest CreateRequest()
    {
        return new ImageGenerationRequest(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "A clean blue poster background",
            new GenerationSettings(512, 512, "standard", "png"),
            Path.GetTempPath(),
            "queue-test.png");
    }

    private sealed class FailOnceImageGenerationProvider : IImageGenerationProvider
    {
        public int CallCount { get; private set; }

        public IProviderCapabilities Capabilities { get; } = new ProviderCapabilities(
            "test-fail-once",
            "Test fail once provider",
            ["test"],
            SupportsTextPlanning: false,
            SupportsImageGeneration: true,
            SupportsVisionReview: false,
            SupportsImageEditing: false,
            SupportsStreaming: false);

        public Task<ImageGenerationResult> GenerateImageAsync(
            ImageGenerationRequest request,
            CancellationToken cancellationToken)
        {
            CallCount++;

            if (CallCount == 1)
            {
                throw new InvalidOperationException("planned failure");
            }

            return Task.FromResult(new ImageGenerationResult(
                Guid.NewGuid(),
                Path.Combine(request.OutputDirectory, request.OutputFileName),
                Path.Combine(request.OutputDirectory, "queue-test.json"),
                "test-success",
                DateTimeOffset.UtcNow));
        }
    }

    private sealed class DelayingImageGenerationProvider : IImageGenerationProvider
    {
        private readonly TimeSpan _delay;

        public DelayingImageGenerationProvider(TimeSpan delay)
        {
            _delay = delay;
        }

        public IProviderCapabilities Capabilities { get; } = new ProviderCapabilities(
            "test-delay",
            "Test delay provider",
            ["test"],
            SupportsTextPlanning: false,
            SupportsImageGeneration: true,
            SupportsVisionReview: false,
            SupportsImageEditing: false,
            SupportsStreaming: false);

        public async Task<ImageGenerationResult> GenerateImageAsync(
            ImageGenerationRequest request,
            CancellationToken cancellationToken)
        {
            await Task.Delay(_delay, cancellationToken);

            return new ImageGenerationResult(
                Guid.NewGuid(),
                Path.Combine(request.OutputDirectory, request.OutputFileName),
                Path.Combine(request.OutputDirectory, "queue-test.json"),
                "test-delay-success",
                DateTimeOffset.UtcNow);
        }
    }

    private sealed class CapabilityBoundImageGenerationProvider : IImageGenerationProvider
    {
        public int CallCount { get; private set; }

        public IProviderCapabilities Capabilities { get; } = new ProviderCapabilities(
            "test-capability-bound",
            "Test capability bound provider",
            ["test"],
            SupportsTextPlanning: false,
            SupportsImageGeneration: true,
            SupportsVisionReview: false,
            SupportsImageEditing: false,
            SupportsStreaming: false,
            supportedSizes: [new ImageOutputSize(512, 512)],
            supportedQualities: ["standard"],
            supportedOutputFormats: ["png"],
            supportedBackgroundModes: ["auto"],
            costHints: [new ProviderCostHint("test", "free")]);

        public Task<ImageGenerationResult> GenerateImageAsync(
            ImageGenerationRequest request,
            CancellationToken cancellationToken)
        {
            CallCount++;
            throw new InvalidOperationException("Provider should not be called for unsupported recipes.");
        }
    }
}
