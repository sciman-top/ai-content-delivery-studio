using System.IO;
using System.Net.Http;
using ContentDeliveryStudio.Application.ScientificFigures;
using ContentDeliveryStudio.Core.Providers;
using ContentDeliveryStudio.Infrastructure.Fakes;
using ContentDeliveryStudio.Infrastructure.OpenAI;
using ContentDeliveryStudio.Infrastructure.ScientificFigures;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace ContentDeliveryStudio.App.Services;

public sealed record ProviderRuntimeRegistrationOptions(
    string? ProviderMode = null,
    string? EnvPath = null,
    string? SecretStore = null);

public static class ProviderRuntimeServiceCollectionExtensions
{
    private const string LiveProviderMode = "live";
    private const string DotEnvSecretStoreName = "dotenv";
    private const string DpapiSecretStoreName = "dpapi";

    public static IServiceCollection AddContentDeliveryStudioProviderRuntime(
        this IServiceCollection services,
        ProviderRuntimeRegistrationOptions options)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(options);

        return IsLiveMode(options)
            ? AddLiveProviders(services, options, ResolveEnvPath(options))
            : AddFakeProviders(services);
    }

    private static IServiceCollection AddFakeProviders(IServiceCollection services)
    {
        services.AddSingleton<ITextPlanningProvider, FakeTextPlanningProvider>();
        services.AddSingleton<FakeImageGenerationProvider>();
        services.AddSingleton<IImageGenerationProvider>(serviceProvider =>
            serviceProvider.GetRequiredService<FakeImageGenerationProvider>());
        services.AddSingleton<IImageEditProvider>(serviceProvider =>
            serviceProvider.GetRequiredService<FakeImageGenerationProvider>());
        services.AddSingleton<IVisionReviewProvider, FakeVisionReviewProvider>();
        services.AddSingleton<IScientificUnderstandingProvider, FakeScientificUnderstandingProvider>();
        services.AddSingleton<IScientificSemanticReviewProvider, FakeScientificSemanticReviewProvider>();
        services.AddSingleton<IScientificVisualReviewProvider, FakeScientificVisualReviewProvider>();

        return services;
    }

    private static IServiceCollection AddLiveProviders(
        IServiceCollection services,
        ProviderRuntimeRegistrationOptions options,
        string envPath)
    {
        if (!File.Exists(envPath))
        {
            throw new InvalidOperationException(
                $"live provider mode requires a readable .env file. Expected path: {envPath}");
        }

        var configuration = ProviderEnvironmentConfiguration
            .FromDotEnvFileAsync(envPath, CancellationToken.None)
            .GetAwaiter()
            .GetResult();
        var validationErrors = configuration.Validate();
        if (validationErrors.Count > 0)
        {
            throw new InvalidOperationException(
                "live provider mode configuration is invalid: " + string.Join(" ", validationErrors));
        }

        var secretStore = ResolveSecretStore(options, envPath);
        services.TryAddSingleton<IOpenAiScientificReviewCheckpointStore, JsonOpenAiScientificReviewCheckpointStore>();
        services.AddSingleton(configuration);
        services.AddSingleton<IOpenAiSecretStore>(secretStore);
        services.AddSingleton<OpenAiSdkClientFactory>();
        services.AddHttpClient(
            "openai-image-edit",
            client =>
            {
                client.BaseAddress = configuration.Image.BaseUri;
                // High-quality image edits routinely run for minutes; bound the
                // wait explicitly instead of relying on the 100s library default.
                client.Timeout = OpenAiProviderFailoverFactory.ImageGenerationTimeout;
            });
        services.AddSingleton<ITextPlanningProvider>(serviceProvider =>
            OpenAiProviderFailoverFactory.CreateTextPlanningProvider(
                configuration,
                secretStore,
                serviceProvider.GetService<IProviderCallTelemetrySink>(),
                realApiEnabled: true));
        services.AddSingleton<IImageGenerationProvider>(serviceProvider =>
            OpenAiProviderFailoverFactory.CreateImageGenerationProvider(
                configuration,
                secretStore,
                serviceProvider.GetService<IProviderCallTelemetrySink>(),
                realApiEnabled: true));
        services.AddSingleton<IImageEditProvider>(serviceProvider =>
            new OpenAiImageEditProvider(
                serviceProvider.GetRequiredService<IHttpClientFactory>().CreateClient("openai-image-edit"),
                OpenAiProviderOptions.FromImageProviderEnvironment(configuration, realApiEnabled: true),
                secretStore,
                serviceProvider.GetService<IProviderCallTelemetrySink>()));
        services.AddSingleton<IVisionReviewProvider>(serviceProvider =>
            OpenAiProviderFailoverFactory.CreateVisionReviewProvider(
                configuration,
                secretStore,
                serviceProvider.GetService<IProviderCallTelemetrySink>(),
                realApiEnabled: true));
        services.AddSingleton<IScientificUnderstandingProvider>(serviceProvider =>
            new OpenAiScientificUnderstandingProvider(
                OpenAiProviderOptions.FromTextProviderEnvironment(
                    configuration,
                    realApiEnabled: true),
                serviceProvider.GetRequiredService<OpenAiSdkClientFactory>(),
                secretStore,
                serviceProvider.GetService<IProviderCallTelemetrySink>()));
        services.AddSingleton<OpenAiScientificReviewProvider>(serviceProvider =>
            new OpenAiScientificReviewProvider(
                // Live scientific review must consume the same named client the
                // application registers (resilience pipeline, bounded timeouts)
                // instead of constructing its own unconfigured HttpClient.
                serviceProvider.GetRequiredService<IHttpClientFactory>()
                    .CreateClient(OpenAiHttpClientNames.Provider),
                OpenAiProviderOptions.FromTextProviderEnvironment(
                    configuration,
                    realApiEnabled: true),
                secretStore,
                serviceProvider.GetService<IProviderCallTelemetrySink>(),
                serviceProvider.GetRequiredService<IOpenAiScientificReviewCheckpointStore>()));
        services.AddSingleton<IScientificSemanticReviewProvider>(serviceProvider =>
            serviceProvider.GetRequiredService<OpenAiScientificReviewProvider>());
        services.AddSingleton<IScientificVisualReviewProvider>(serviceProvider =>
            serviceProvider.GetRequiredService<OpenAiScientificReviewProvider>());

        return services;
    }

    private static bool IsLiveMode(ProviderRuntimeRegistrationOptions options)
    {
        var providerMode = !string.IsNullOrWhiteSpace(options.ProviderMode)
            ? options.ProviderMode
            : Environment.GetEnvironmentVariable("PROVIDER_MODE");

        return string.Equals(providerMode, LiveProviderMode, StringComparison.OrdinalIgnoreCase);
    }

    private static string ResolveEnvPath(ProviderRuntimeRegistrationOptions options)
    {
        var envPath = !string.IsNullOrWhiteSpace(options.EnvPath)
            ? options.EnvPath
            : Path.Combine(Environment.CurrentDirectory, ".env");

        return Path.GetFullPath(envPath);
    }

    internal static IOpenAiSecretStore ResolveSecretStore(
        ProviderRuntimeRegistrationOptions options,
        string envPath)
    {
        ArgumentNullException.ThrowIfNull(options);

        var name = !string.IsNullOrWhiteSpace(options.SecretStore)
            ? options.SecretStore
            : Environment.GetEnvironmentVariable("PROVIDER_SECRET_STORE");

        return name?.Trim().ToLowerInvariant() switch
        {
            null or "" or DotEnvSecretStoreName => new DotEnvSecretStore(envPath),
            // DPAPI wins over .env so operators can migrate real keys out of the
            // plaintext file one secret at a time; the .env values keep defining
            // endpoint topology and remain the fallback while both channels exist.
            DpapiSecretStoreName => new CompositeOpenAiSecretStore(
            [
                new DpapiOpenAiSecretStore(),
                new DotEnvSecretStore(envPath),
            ]),
            var other => throw new InvalidOperationException(
                $"PROVIDER_SECRET_STORE '{other}' is invalid. Allowed values: {DotEnvSecretStoreName}, {DpapiSecretStoreName}."),
        };
    }
}
