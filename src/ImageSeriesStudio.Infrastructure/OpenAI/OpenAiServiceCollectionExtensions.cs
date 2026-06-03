using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Http.Resilience;

namespace ImageSeriesStudio.Infrastructure.OpenAI;

public static class OpenAiHttpClientNames
{
    public const string Provider = "openai-provider";
}

public static class OpenAiServiceCollectionExtensions
{
    public static IHttpClientBuilder AddOpenAiProviderHttpClient(
        this IServiceCollection services,
        OpenAiProviderOptions providerOptions)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(providerOptions);

        services.AddSingleton(providerOptions);
        services.TryAddSingleton(_ => OpenAiSecretStores.CreateDefault());

        var builder = services.AddHttpClient(
            OpenAiHttpClientNames.Provider,
            client =>
            {
                client.BaseAddress = providerOptions.BaseUri;
            });

        builder.AddStandardResilienceHandler(options =>
        {
            options.Retry.DisableForUnsafeHttpMethods();
        });

        return builder;
    }
}
