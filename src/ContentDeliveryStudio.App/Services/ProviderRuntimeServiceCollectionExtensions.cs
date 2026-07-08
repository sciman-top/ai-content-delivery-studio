using System.IO;
using ContentDeliveryStudio.Core.Providers;
using ContentDeliveryStudio.Infrastructure.Fakes;
using ContentDeliveryStudio.Infrastructure.OpenAI;
using Microsoft.Extensions.DependencyInjection;

namespace ContentDeliveryStudio.App.Services;

public sealed record ProviderRuntimeRegistrationOptions(
    string? ProviderMode = null,
    string? EnvPath = null);

public static class ProviderRuntimeServiceCollectionExtensions
{
    private const string LiveProviderMode = "live";

    public static IServiceCollection AddContentDeliveryStudioProviderRuntime(
        this IServiceCollection services,
        ProviderRuntimeRegistrationOptions options)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(options);

        return IsLiveMode(options)
            ? AddLiveProviders(services, ResolveEnvPath(options))
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

        return services;
    }

    private static IServiceCollection AddLiveProviders(IServiceCollection services, string envPath)
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

        var secretStore = new DotEnvSecretStore(envPath);
        services.AddSingleton(configuration);
        services.AddSingleton<IOpenAiSecretStore>(secretStore);
        services.AddSingleton<FakeImageGenerationProvider>();
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
            serviceProvider.GetRequiredService<FakeImageGenerationProvider>());
        services.AddSingleton<IVisionReviewProvider>(serviceProvider =>
            OpenAiProviderFailoverFactory.CreateVisionReviewProvider(
                configuration,
                secretStore,
                serviceProvider.GetService<IProviderCallTelemetrySink>(),
                realApiEnabled: true));

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
}
