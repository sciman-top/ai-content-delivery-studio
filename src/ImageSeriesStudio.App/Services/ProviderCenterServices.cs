using System.IO;
using ImageSeriesStudio.Infrastructure.OpenAI;

namespace ImageSeriesStudio.App.Services;

public interface IProviderCenterConfigurationService
{
    Task<ProviderCenterSnapshot> LoadAsync(CancellationToken cancellationToken);
}

public interface IProviderCenterHealthCheckService
{
    Task<ProviderCenterHealthSnapshot> CheckAsync(CancellationToken cancellationToken);
}

public sealed class DotEnvProviderCenterConfigurationService : IProviderCenterConfigurationService
{
    private readonly string _envPath;

    public DotEnvProviderCenterConfigurationService(string? envPath = null)
    {
        _envPath = string.IsNullOrWhiteSpace(envPath)
            ? Path.Combine(Environment.CurrentDirectory, ".env")
            : envPath;
    }

    public async Task<ProviderCenterSnapshot> LoadAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!File.Exists(_envPath))
        {
            return ProviderCenterSnapshot.MissingEnvironmentFile(_envPath);
        }

        var configuration = await ProviderEnvironmentConfiguration.FromDotEnvFileAsync(_envPath, cancellationToken);
        return ProviderCenterSnapshot.FromConfiguration(configuration);
    }
}

public sealed class DotEnvProviderCenterHealthCheckService : IProviderCenterHealthCheckService
{
    private readonly string _envPath;
    private readonly ProviderHealthCheckService _healthCheckService;

    public DotEnvProviderCenterHealthCheckService(
        ProviderHealthCheckService healthCheckService,
        string? envPath = null)
    {
        _healthCheckService = healthCheckService;
        _envPath = string.IsNullOrWhiteSpace(envPath)
            ? Path.Combine(Environment.CurrentDirectory, ".env")
            : envPath;
    }

    public async Task<ProviderCenterHealthSnapshot> CheckAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!File.Exists(_envPath))
        {
            return new ProviderCenterHealthSnapshot([], []);
        }

        var configuration = await ProviderEnvironmentConfiguration.FromDotEnvFileAsync(_envPath, cancellationToken);
        var textResults = configuration.Text.ApiKeySecretName is null
            ? []
            : new[]
            {
                ProviderKeyHealthSnapshot.FromResult(
                    await _healthCheckService.CheckModelsEndpointAsync(configuration.Text, cancellationToken)),
            };
        var imageResults = await _healthCheckService.CheckKeyPoolModelsEndpointAsync(configuration.Image, cancellationToken);

        return new ProviderCenterHealthSnapshot(
            textResults,
            imageResults.Select(ProviderKeyHealthSnapshot.FromResult).ToArray());
    }
}

public sealed record ProviderCenterSnapshot(
    ProviderEndpointConfigurationSnapshot Text,
    ProviderEndpointConfigurationSnapshot Image,
    IReadOnlyList<string> ValidationMessages)
{
    public static ProviderCenterSnapshot FromConfiguration(ProviderEnvironmentConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        return new ProviderCenterSnapshot(
            ProviderEndpointConfigurationSnapshot.FromConfiguration("Text provider", configuration.Text),
            ProviderEndpointConfigurationSnapshot.FromConfiguration("Image provider", configuration.Image),
            configuration.Validate());
    }

    public static ProviderCenterSnapshot MissingEnvironmentFile(string envPath)
    {
        return new ProviderCenterSnapshot(
            ProviderEndpointConfigurationSnapshot.Empty("Text provider", "TEXT_PROVIDER"),
            ProviderEndpointConfigurationSnapshot.Empty("Image provider", "IMAGE_PROVIDER"),
            [$"Provider environment file was not found: {envPath}"]);
    }
}

public sealed record ProviderEndpointConfigurationSnapshot(
    string Title,
    string Prefix,
    string Kind,
    string BaseUrl,
    string Model,
    int ApiKeyCount,
    bool UsesAppCredentials,
    int ConcurrencyPerKey,
    int TotalConcurrency)
{
    public static ProviderEndpointConfigurationSnapshot FromConfiguration(
        string title,
        ProviderEndpointEnvironmentConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        return new ProviderEndpointConfigurationSnapshot(
            title,
            configuration.Prefix,
            configuration.Kind,
            configuration.BaseUri?.ToString() ?? string.Empty,
            configuration.Model,
            configuration.ApiKeySecretNames.Count,
            configuration.AppIdSecretName is not null && configuration.AppSecretSecretName is not null,
            configuration.ConcurrencyPerKey,
            configuration.TotalConcurrency);
    }

    public static ProviderEndpointConfigurationSnapshot Empty(string title, string prefix)
    {
        return new ProviderEndpointConfigurationSnapshot(
            title,
            prefix,
            string.Empty,
            string.Empty,
            string.Empty,
            0,
            false,
            0,
            0);
    }
}

public sealed record ProviderCenterHealthSnapshot(
    IReadOnlyList<ProviderKeyHealthSnapshot> Text,
    IReadOnlyList<ProviderKeyHealthSnapshot> Image)
{
    public IReadOnlyList<ProviderKeyHealthSnapshot> ForPrefix(string prefix)
    {
        return prefix switch
        {
            "TEXT_PROVIDER" => Text,
            "IMAGE_PROVIDER" => Image,
            _ => [],
        };
    }
}

public sealed record ProviderKeyHealthSnapshot(
    string ProviderPrefix,
    string ApiKeySecretName,
    string Status,
    int? HttpStatusCode)
{
    public static ProviderKeyHealthSnapshot FromResult(ProviderHealthCheckResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        return new ProviderKeyHealthSnapshot(
            result.ProviderPrefix,
            result.ApiKeySecretName,
            result.Status.ToString(),
            result.HttpStatusCode is null ? null : (int)result.HttpStatusCode.Value);
    }
}
