using System.Net;
using ContentDeliveryStudio.Core.Documents;
using ContentDeliveryStudio.Core.Projects;
using ContentDeliveryStudio.Core.Providers;
using ContentDeliveryStudio.Infrastructure.OpenAI;

namespace ContentDeliveryStudio.Tests;

public sealed class ProviderFailoverTests
{
    [Fact]
    public void ProviderFailoverFactory_CreatesFailoverProvidersFromConfiguredFallbacks()
    {
        var configuration = ProviderEnvironmentConfiguration.FromValues(
            new Dictionary<string, string?>
            {
                ["TEXT_PROVIDER_BASE_URL"] = "https://input.example/v1",
                ["TEXT_PROVIDER_API_KEY"] = "sk-input",
                ["TEXT_PROVIDER_MODEL"] = "gpt-5.5",
                ["TEXT_PROVIDER_FALLBACK_1_BASE_URL"] = "https://ciii.example/v1",
                ["TEXT_PROVIDER_FALLBACK_1_API_KEY"] = "sk-ciii",
                ["TEXT_PROVIDER_FALLBACK_1_MODEL"] = "gpt-5.5",
                ["IMAGE_PROVIDER_BASE_URL"] = "https://input.example/v1",
                ["IMAGE_PROVIDER_MODEL"] = "gpt-image-2",
                ["IMAGE_PROVIDER_IMAGE_SURFACE"] = "responses",
                ["IMAGE_PROVIDER_RESPONSES_MODEL"] = "gpt-5.5",
                ["IMAGE_PROVIDER_API_KEY_1"] = "sk-input",
                ["IMAGE_PROVIDER_FALLBACK_1_BASE_URL"] = "https://ciii.example/v1",
                ["IMAGE_PROVIDER_FALLBACK_1_MODEL"] = "gpt-image-2",
                ["IMAGE_PROVIDER_FALLBACK_1_IMAGE_SURFACE"] = "images",
                ["IMAGE_PROVIDER_FALLBACK_1_API_KEY_1"] = "sk-ciii",
            });

        var secretStore = new StaticOpenAiSecretStore();

        var textProvider = OpenAiProviderFailoverFactory.CreateTextPlanningProvider(configuration, secretStore);
        var visionProvider = OpenAiProviderFailoverFactory.CreateVisionReviewProvider(configuration, secretStore);
        var imageProvider = OpenAiProviderFailoverFactory.CreateImageGenerationProvider(configuration, secretStore);

        Assert.IsType<FailoverTextPlanningProvider>(textProvider);
        Assert.IsType<FailoverVisionReviewProvider>(visionProvider);
        Assert.IsType<FailoverImageGenerationProvider>(imageProvider);
        Assert.Contains("gpt-5.5", textProvider.Capabilities.ModelIds);
        Assert.Contains("gpt-5.5", visionProvider.Capabilities.ModelIds);
        Assert.Contains("gpt-image-2", imageProvider.Capabilities.ModelIds);
    }

    [Fact]
    public async Task TextPlanningProvider_FallsBackAfterConnectionFailure()
    {
        var primary = new ThrowingTextPlanningProvider(new HttpRequestException("No such host is known."));
        var backup = new StaticTextPlanningProvider("backup-trace");
        var provider = new FailoverTextPlanningProvider([primary, backup]);

        var result = await provider.CreatePlanAsync(
            new PlanningRequest("Create a blue square.", "QA", 1, "Minimal."),
            CancellationToken.None);

        Assert.Equal("backup-trace", result.ProviderTraceId);
        Assert.Equal(1, primary.CreatePlanCallCount);
        Assert.Equal(1, backup.CreatePlanCallCount);
    }

    [Fact]
    public async Task ImageGenerationProvider_FallsBackAfterServerFailure()
    {
        var primary = new ThrowingImageGenerationProvider(
            new HttpRequestException("OpenAI image generation request failed with status 502 Bad Gateway."));
        var backup = new StaticImageGenerationProvider("backup-image-trace");
        var provider = new FailoverImageGenerationProvider([primary, backup]);

        var result = await provider.GenerateImageAsync(
            new ImageGenerationRequest(
                Guid.NewGuid(),
                Guid.NewGuid(),
                "Create a blue square.",
                new GenerationSettings(1024, 1024, "low", "png"),
                Path.GetTempPath()),
            CancellationToken.None);

        Assert.Equal("backup-image-trace", result.ProviderTraceId);
        Assert.Equal(1, primary.CallCount);
        Assert.Equal(1, backup.CallCount);
    }

    [Fact]
    public async Task VisionReviewProvider_DoesNotFallbackAfterAuthFailure()
    {
        var primary = new ThrowingVisionReviewProvider(
            new HttpRequestException(
                "OpenAI vision review request failed with status 401 Unauthorized.",
                inner: null,
                HttpStatusCode.Unauthorized));
        var backup = new StaticVisionReviewProvider();
        var provider = new FailoverVisionReviewProvider([primary, backup]);

        await Assert.ThrowsAsync<HttpRequestException>(() =>
            provider.ReviewAsync(CreateVisionReviewRequest(), CancellationToken.None));

        Assert.Equal(1, primary.CallCount);
        Assert.Equal(0, backup.CallCount);
    }

    [Fact]
    public void FailoverPolicy_TreatsRateLimitAsEligibleAndBadRequestAsIneligible()
    {
        Assert.True(ProviderFailoverPolicy.IsFailoverEligible(
            new HttpRequestException("rate limited", inner: null, (HttpStatusCode)429),
            CancellationToken.None));
        Assert.False(ProviderFailoverPolicy.IsFailoverEligible(
            new HttpRequestException("bad request", inner: null, HttpStatusCode.BadRequest),
            CancellationToken.None));
    }

    private static VisionReviewRequest CreateVisionReviewRequest()
    {
        return new VisionReviewRequest(
            Guid.NewGuid(),
            Path.Combine(Path.GetTempPath(), "candidate.png"),
            new ReviewRubric(
                Guid.NewGuid(),
                Guid.NewGuid(),
                "Default",
                [new ReviewRubricDimension("match", "Image should match the prompt.", 1)],
                DateTimeOffset.UtcNow),
            "Create a blue square.");
    }

    private static ProviderCapabilities CreateCapabilities(
        string providerId,
        bool supportsTextPlanning,
        bool supportsImageGeneration,
        bool supportsVisionReview)
    {
        return new ProviderCapabilities(
            providerId,
            providerId,
            ["gpt-5.5"],
            supportsTextPlanning,
            supportsImageGeneration,
            supportsVisionReview,
            SupportsImageEditing: false,
            SupportsStreaming: false,
            supportedSizes: [new ImageOutputSize(1024, 1024)],
            supportedQualities: ["low"],
            supportedOutputFormats: ["png"],
            supportedBackgroundModes: ["opaque"]);
    }

    private sealed class ThrowingTextPlanningProvider(Exception exception) : ITextPlanningProvider
    {
        public int CreatePlanCallCount { get; private set; }

        public IProviderCapabilities Capabilities { get; } = CreateCapabilities(
            "primary-text",
            supportsTextPlanning: true,
            supportsImageGeneration: false,
            supportsVisionReview: false);

        public Task<SeriesPlanResult> CreatePlanAsync(PlanningRequest request, CancellationToken cancellationToken)
        {
            CreatePlanCallCount++;
            return Task.FromException<SeriesPlanResult>(exception);
        }

        public Task<BriefPlanningResult> CreatePromptDirectionsAsync(
            BriefPlanningRequest request,
            CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public Task<BlueprintPlanningResult> CreateDesignBlueprintsAsync(
            BlueprintPlanningRequest request,
            CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public Task<DocumentIllustrationPlanningResult> CreateDocumentIllustrationPlanAsync(
            DocumentIllustrationPlanningRequest request,
            CancellationToken cancellationToken)
            => throw new NotSupportedException();
    }

    private sealed class StaticOpenAiSecretStore : IOpenAiSecretStore
    {
        public Task<string?> GetSecretAsync(string secretName, CancellationToken cancellationToken)
            => Task.FromResult<string?>("test-secret");
    }

    private sealed class StaticTextPlanningProvider(string providerTraceId) : ITextPlanningProvider
    {
        public int CreatePlanCallCount { get; private set; }

        public IProviderCapabilities Capabilities { get; } = CreateCapabilities(
            "backup-text",
            supportsTextPlanning: true,
            supportsImageGeneration: false,
            supportsVisionReview: false);

        public Task<SeriesPlanResult> CreatePlanAsync(PlanningRequest request, CancellationToken cancellationToken)
        {
            CreatePlanCallCount++;
            return Task.FromResult(new SeriesPlanResult(
                "Backup plan",
                [new SeriesPlanItem("Item", "Brief", "Prompt")],
                providerTraceId));
        }

        public Task<BriefPlanningResult> CreatePromptDirectionsAsync(
            BriefPlanningRequest request,
            CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public Task<BlueprintPlanningResult> CreateDesignBlueprintsAsync(
            BlueprintPlanningRequest request,
            CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public Task<DocumentIllustrationPlanningResult> CreateDocumentIllustrationPlanAsync(
            DocumentIllustrationPlanningRequest request,
            CancellationToken cancellationToken)
            => throw new NotSupportedException();
    }

    private sealed class ThrowingImageGenerationProvider(Exception exception) : IImageGenerationProvider
    {
        public int CallCount { get; private set; }

        public IProviderCapabilities Capabilities { get; } = CreateCapabilities(
            "primary-image",
            supportsTextPlanning: false,
            supportsImageGeneration: true,
            supportsVisionReview: false);

        public Task<ImageGenerationResult> GenerateImageAsync(
            ImageGenerationRequest request,
            CancellationToken cancellationToken)
        {
            CallCount++;
            return Task.FromException<ImageGenerationResult>(exception);
        }
    }

    private sealed class StaticImageGenerationProvider(string providerTraceId) : IImageGenerationProvider
    {
        public int CallCount { get; private set; }

        public IProviderCapabilities Capabilities { get; } = CreateCapabilities(
            "backup-image",
            supportsTextPlanning: false,
            supportsImageGeneration: true,
            supportsVisionReview: false);

        public Task<ImageGenerationResult> GenerateImageAsync(
            ImageGenerationRequest request,
            CancellationToken cancellationToken)
        {
            CallCount++;
            return Task.FromResult(new ImageGenerationResult(
                Guid.NewGuid(),
                Path.Combine(request.OutputDirectory, "image.png"),
                Path.Combine(request.OutputDirectory, "image.json"),
                providerTraceId,
                DateTimeOffset.UtcNow));
        }
    }

    private sealed class ThrowingVisionReviewProvider(Exception exception) : IVisionReviewProvider
    {
        public int CallCount { get; private set; }

        public IProviderCapabilities Capabilities { get; } = CreateCapabilities(
            "primary-vision",
            supportsTextPlanning: false,
            supportsImageGeneration: false,
            supportsVisionReview: true);

        public Task<VisionReviewResult> ReviewAsync(VisionReviewRequest request, CancellationToken cancellationToken)
        {
            CallCount++;
            return Task.FromException<VisionReviewResult>(exception);
        }
    }

    private sealed class StaticVisionReviewProvider : IVisionReviewProvider
    {
        public int CallCount { get; private set; }

        public IProviderCapabilities Capabilities { get; } = CreateCapabilities(
            "backup-vision",
            supportsTextPlanning: false,
            supportsImageGeneration: false,
            supportsVisionReview: true);

        public Task<VisionReviewResult> ReviewAsync(VisionReviewRequest request, CancellationToken cancellationToken)
        {
            CallCount++;
            return Task.FromResult(new VisionReviewResult(
                request.CandidateImageId,
                ReviewDecision.Pass,
                new Dictionary<string, int> { ["match"] = 5 },
                [],
                "Backup review passed.",
                SuggestedFix: null));
        }
    }
}
