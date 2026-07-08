using ContentDeliveryStudio.Core.Providers;

namespace ContentDeliveryStudio.Infrastructure.OpenAI;

public static class OpenAiProviderFailoverFactory
{
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
                    CreateHttpClient(options, httpClientFactory),
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
                    CreateHttpClient(options, httpClientFactory),
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
                    CreateHttpClient(options, httpClientFactory),
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
        Func<OpenAiProviderOptions, HttpClient>? httpClientFactory)
    {
        if (httpClientFactory is not null)
        {
            return httpClientFactory(options);
        }

        return new HttpClient
        {
            BaseAddress = options.BaseUri,
        };
    }
}
