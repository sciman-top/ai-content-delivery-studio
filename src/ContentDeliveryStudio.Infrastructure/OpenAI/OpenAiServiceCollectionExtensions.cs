using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Http.Resilience;

namespace ContentDeliveryStudio.Infrastructure.OpenAI;

public static class OpenAiHttpClientNames
{
    public const string Provider = "openai-provider";
}

public static class OpenAiServiceCollectionExtensions
{
    /// <summary>
    /// Raw-HTTP OpenAI calls are long-running: scientific review runs
    /// multi-minute reasoning, so every attempt gets an explicit bounded
    /// timeout instead of the library defaults. These values live on the
    /// standard resilience pipeline, which owns the effective request
    /// timeouts for this named client.
    /// </summary>
    public static readonly TimeSpan ScientificReviewAttemptTimeout = TimeSpan.FromMinutes(2);
    public static readonly TimeSpan ScientificReviewTotalTimeout = TimeSpan.FromMinutes(5);

    public static IHttpClientBuilder AddOpenAiProviderHttpClient(
        this IServiceCollection services,
        OpenAiProviderOptions providerOptions)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(providerOptions);

        services.AddSingleton(providerOptions);
        services.TryAddSingleton(_ => OpenAiSecretStores.CreateDefault());
        services.TryAddSingleton<IProviderCallTelemetrySink, DiagnosticProviderCallTelemetrySink>();
        services.TryAddSingleton<IOpenAiExecutionSlotScheduler, OpenAiExecutionSlotScheduler>();
        services.TryAddSingleton<IOpenAiActivePresetSetState, OpenAiActivePresetSetState>();
        services.TryAddSingleton<IOpenAiModelAvailabilityProbe>(serviceProvider =>
            new OpenAiModelAvailabilityProbe(
                serviceProvider.GetRequiredService<IHttpClientFactory>()
                    .CreateClient(OpenAiHttpClientNames.Provider),
                serviceProvider.GetRequiredService<IOpenAiSecretStore>()));
        services.TryAddSingleton<IOpenAiScientificReviewCheckpointStore, JsonOpenAiScientificReviewCheckpointStore>();
        services.TryAddSingleton<OpenAiSdkClientFactory>();
        services.TryAddTransient<OpenAiScientificUnderstandingProvider>();
        services.TryAddTransient(serviceProvider =>
            new OpenAiScientificReviewProvider(
                serviceProvider.GetRequiredService<IHttpClientFactory>()
                    .CreateClient(OpenAiHttpClientNames.Provider),
                providerOptions,
                serviceProvider.GetRequiredService<IOpenAiSecretStore>(),
                serviceProvider.GetService<IProviderCallTelemetrySink>(),
                serviceProvider.GetRequiredService<IOpenAiScientificReviewCheckpointStore>(),
                serviceProvider.GetService<IOpenAiModelAvailabilityProbe>(),
                serviceProvider.GetRequiredService<IOpenAiExecutionSlotScheduler>(),
                serviceProvider.GetRequiredService<IOpenAiActivePresetSetState>()));

        var builder = services.AddHttpClient(
            OpenAiHttpClientNames.Provider,
            client =>
            {
                client.BaseAddress = providerOptions.BaseUri;
            });

        // The standard resilience handler takes over HttpClient.Timeout (the
        // factory forces InfiniteTimeSpan), so the effective bounded timeouts
        // are the explicit attempt/total values configured below.
        builder.AddStandardResilienceHandler(ConfigureScientificReviewResilience);
        return builder;
    }

    internal static void ConfigureScientificReviewResilience(
        Microsoft.Extensions.Http.Resilience.HttpStandardResilienceOptions options)
    {
        options.AttemptTimeout.Timeout = ScientificReviewAttemptTimeout;
        options.TotalRequestTimeout.Timeout = ScientificReviewTotalTimeout;
        // Circuit-breaker sampling must be at least double the attempt
        // timeout (library validation); the default 30s does not fit a
        // 120s scientific-review attempt.
        options.CircuitBreaker.SamplingDuration = TimeSpan.FromMinutes(5);
        options.Retry.DisableForUnsafeHttpMethods();
    }
}
