using ContentDeliveryStudio.Core.Providers;

namespace ContentDeliveryStudio.Infrastructure.OpenAI;

public static class OpenAiProviderFailoverFactory
{
    /// <summary>Explicit per-role timeouts for the raw failover transports:
    /// reasoning text planning can take minutes; high-quality image generation
    /// longer still. Without these the transports silently inherit the 100s
    /// HttpClient default, which real image calls can exceed.</summary>
    public static readonly TimeSpan TextPlanningTimeout = TimeSpan.FromMinutes(5);
    public static readonly TimeSpan VisionReviewTimeout = TimeSpan.FromMinutes(10);
    public static readonly TimeSpan ImageGenerationTimeout = TimeSpan.FromMinutes(10);

    public static ITextPlanningProvider CreateTextPlanningProvider(
        ProviderEnvironmentConfiguration configuration,
        IOpenAiSecretStore secretStore,
        IProviderCallTelemetrySink? telemetrySink = null,
        bool realApiEnabled = true,
        Func<OpenAiProviderOptions, HttpClient>? httpClientFactory = null)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(secretStore);

        var providers = GetTextEndpoints(configuration)
            .Select(endpoint =>
            {
                var options = OpenAiProviderOptions.FromTextEndpointEnvironment(endpoint, realApiEnabled);
                return new OpenAiTextPlanningProvider(
                    CreateHttpClient(options, httpClientFactory, TextPlanningTimeout),
                    options,
                    secretStore,
                    telemetrySink);
            })
            .Cast<ITextPlanningProvider>()
            .ToArray();

        return providers.Length == 1
            ? providers[0]
            : new FailoverTextPlanningProvider(providers);
    }

    public static IVisionReviewProvider CreateVisionReviewProvider(
        ProviderEnvironmentConfiguration configuration,
        IOpenAiSecretStore secretStore,
        IProviderCallTelemetrySink? telemetrySink = null,
        bool realApiEnabled = true,
        Func<OpenAiProviderOptions, HttpClient>? httpClientFactory = null)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(secretStore);

        var providers = GetTextEndpoints(configuration)
            .Select(endpoint =>
            {
                var options = OpenAiProviderOptions.FromTextEndpointEnvironment(endpoint, realApiEnabled);
                return new OpenAiVisionReviewProvider(
                    CreateHttpClient(options, httpClientFactory, VisionReviewTimeout),
                    options,
                    secretStore,
                    telemetrySink);
            })
            .Cast<IVisionReviewProvider>()
            .ToArray();

        return providers.Length == 1
            ? providers[0]
            : new FailoverVisionReviewProvider(providers);
    }

    public static IImageGenerationProvider CreateImageGenerationProvider(
        ProviderEnvironmentConfiguration configuration,
        IOpenAiSecretStore secretStore,
        IProviderCallTelemetrySink? telemetrySink = null,
        bool realApiEnabled = true,
        Func<OpenAiProviderOptions, HttpClient>? httpClientFactory = null)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(secretStore);

        var providers = GetImageEndpoints(configuration)
            .Select(endpoint =>
            {
                var options = OpenAiProviderOptions.FromImageEndpointEnvironment(endpoint, realApiEnabled);
                return new OpenAiImageGenerationProvider(
                    CreateHttpClient(options, httpClientFactory, ImageGenerationTimeout),
                    options,
                    secretStore,
                    telemetrySink);
            })
            .Cast<IImageGenerationProvider>()
            .ToArray();

        return providers.Length == 1
            ? providers[0]
            : new FailoverImageGenerationProvider(providers);
    }

    private static IReadOnlyList<ProviderEndpointEnvironmentConfiguration> GetTextEndpoints(
        ProviderEnvironmentConfiguration configuration)
    {
        return [configuration.Text, .. configuration.TextFallbacks];
    }

    private static IReadOnlyList<ProviderEndpointEnvironmentConfiguration> GetImageEndpoints(
        ProviderEnvironmentConfiguration configuration)
    {
        return [configuration.Image, .. configuration.ImageFallbacks];
    }

    private static HttpClient CreateHttpClient(
        OpenAiProviderOptions options,
        Func<OpenAiProviderOptions, HttpClient>? httpClientFactory,
        TimeSpan timeout)
    {
        if (httpClientFactory is not null)
        {
            return httpClientFactory(options);
        }

        return new HttpClient
        {
            BaseAddress = options.BaseUri,
            Timeout = timeout,
        };
    }
}
