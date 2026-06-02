using ImageSeriesStudio.Core.Providers;
using ImageSeriesStudio.Infrastructure.Fakes;
using ImageSeriesStudio.Infrastructure.OpenAI;

namespace ImageSeriesStudio.Tests;

public sealed class ProviderCapabilityValidatorTests
{
    [Fact]
    public void ValidateTextPlanningProvider_AcceptsFakeAndOpenAiProviders()
    {
        using var httpClient = new HttpClient(new StaticHttpHandler());
        var openAiProvider = new OpenAiTextPlanningProvider(
            httpClient,
            new OpenAiProviderOptions(),
            new StaticSecretStore());

        Assert.Empty(ProviderCapabilityValidator.ValidateTextPlanningProvider(new FakeTextPlanningProvider()));
        Assert.Empty(ProviderCapabilityValidator.ValidateTextPlanningProvider(openAiProvider));
    }

    [Fact]
    public void ValidateImageGenerationProvider_AcceptsFakeAndOpenAiProviders()
    {
        using var httpClient = new HttpClient(new StaticHttpHandler());
        var openAiProvider = new OpenAiImageGenerationProvider(
            httpClient,
            new OpenAiProviderOptions(),
            new StaticSecretStore());

        var fakeProvider = new FakeImageGenerationProvider();

        Assert.Empty(ProviderCapabilityValidator.ValidateImageGenerationProvider(fakeProvider));
        Assert.Empty(ProviderCapabilityValidator.ValidateImageGenerationProvider(openAiProvider));
        Assert.Contains(fakeProvider.Capabilities.SupportedSizes, size => size.Width == 1024 && size.Height == 1024);
        Assert.Contains("png", fakeProvider.Capabilities.SupportedOutputFormats);
        Assert.Contains("standard", fakeProvider.Capabilities.SupportedQualities);
        Assert.Contains("auto", fakeProvider.Capabilities.SupportedBackgroundModes);
        Assert.NotEmpty(fakeProvider.Capabilities.CostHints);
    }

    [Fact]
    public void ValidateImageEditProvider_AcceptsFakeProvider()
    {
        var errors = ProviderCapabilityValidator.ValidateImageEditProvider(new FakeImageGenerationProvider());

        Assert.Empty(errors);
    }

    [Fact]
    public void ValidateImageGenerationProvider_RejectsMissingOutputSettings()
    {
        var provider = new InvalidImageGenerationProvider(
            new ProviderCapabilities(
                "invalid-image",
                "Invalid Image Provider",
                ["image-model"],
                SupportsTextPlanning: false,
                SupportsImageGeneration: true,
                SupportsVisionReview: false,
                SupportsImageEditing: false,
                SupportsStreaming: false));

        var errors = ProviderCapabilityValidator.ValidateImageGenerationProvider(provider);

        Assert.Contains(errors, error => error.Contains("size", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(errors, error => error.Contains("quality", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(errors, error => error.Contains("format", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(errors, error => error.Contains("background", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(errors, error => error.Contains("cost", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void ValidateVisionReviewProvider_AcceptsFakeAndOpenAiProviders()
    {
        using var httpClient = new HttpClient(new StaticHttpHandler());
        var openAiProvider = new OpenAiVisionReviewProvider(
            httpClient,
            new OpenAiProviderOptions(),
            new StaticSecretStore());

        Assert.Empty(ProviderCapabilityValidator.ValidateVisionReviewProvider(new FakeVisionReviewProvider()));
        Assert.Empty(ProviderCapabilityValidator.ValidateVisionReviewProvider(openAiProvider));
    }

    [Fact]
    public void ValidateImageGenerationProvider_RejectsWrongCapabilityAndInvalidModels()
    {
        var provider = new InvalidImageGenerationProvider(
            new ProviderCapabilities(
                "",
                " ",
                ["gpt-image-2", "GPT-IMAGE-2", ""],
                SupportsTextPlanning: true,
                SupportsImageGeneration: false,
                SupportsVisionReview: false,
                SupportsImageEditing: false,
                SupportsStreaming: false));

        var errors = ProviderCapabilityValidator.ValidateImageGenerationProvider(provider);

        Assert.Contains(errors, error => error.Contains("Provider id", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(errors, error => error.Contains("display name", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(errors, error => error.Contains("image generation", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(errors, error => error.Contains("blank", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(errors, error => error.Contains("unique", StringComparison.OrdinalIgnoreCase));
    }

    private sealed class StaticSecretStore : IOpenAiSecretStore
    {
        public Task<string?> GetSecretAsync(string secretName, CancellationToken cancellationToken)
        {
            return Task.FromResult<string?>("test-key");
        }
    }

    private sealed class StaticHttpHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK));
        }
    }

    private sealed class InvalidImageGenerationProvider(IProviderCapabilities capabilities) : IImageGenerationProvider
    {
        public IProviderCapabilities Capabilities { get; } = capabilities;

        public Task<ImageGenerationResult> GenerateImageAsync(
            ImageGenerationRequest request,
            CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }
    }
}
