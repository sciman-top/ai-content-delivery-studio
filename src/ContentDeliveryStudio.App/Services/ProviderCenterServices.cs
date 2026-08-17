using System.IO;
using ContentDeliveryStudio.Infrastructure.OpenAI;

namespace ContentDeliveryStudio.App.Services;

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
        var textResults = await CheckEndpointsAsync(
            [configuration.Text, .. configuration.TextFallbacks],
            cancellationToken);
        var imageResults = await CheckEndpointsAsync(
            [configuration.Image, .. configuration.ImageFallbacks],
            cancellationToken);

        return new ProviderCenterHealthSnapshot(
            textResults,
            imageResults);
    }

    private async Task<IReadOnlyList<ProviderKeyHealthSnapshot>> CheckEndpointsAsync(
        IReadOnlyList<ProviderEndpointEnvironmentConfiguration> endpoints,
        CancellationToken cancellationToken)
    {
        var results = new List<ProviderKeyHealthSnapshot>();
        foreach (var endpoint in endpoints)
        {
            var endpointResults = await _healthCheckService.CheckKeyPoolModelsEndpointAsync(
                endpoint,
                cancellationToken);
            results.AddRange(endpointResults.Select(ProviderKeyHealthSnapshot.FromResult));
        }

        return results;
    }
}

public sealed record ProviderCenterSnapshot(
    ProviderEndpointConfigurationSnapshot Text,
    ProviderEndpointConfigurationSnapshot Image,
    IReadOnlyList<ProviderEndpointConfigurationSnapshot> TextFallbacks,
    IReadOnlyList<ProviderEndpointConfigurationSnapshot> ImageFallbacks,
    IReadOnlyList<string> ValidationMessages)
{
    public ProviderCenterSnapshot(
        ProviderEndpointConfigurationSnapshot text,
        ProviderEndpointConfigurationSnapshot image,
        IReadOnlyList<string> validationMessages)
        : this(text, image, [], [], validationMessages)
    {
    }

    public static ProviderCenterSnapshot FromConfiguration(ProviderEnvironmentConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        return new ProviderCenterSnapshot(
            ProviderEndpointConfigurationSnapshot.FromConfiguration("Text provider", configuration.Text),
            ProviderEndpointConfigurationSnapshot.FromConfiguration("Image provider", configuration.Image),
            configuration.TextFallbacks
                .Select((item, index) => ProviderEndpointConfigurationSnapshot.FromConfiguration(
                    $"Text provider fallback {index + 1}",
                    item))
                .ToArray(),
            configuration.ImageFallbacks
                .Select((item, index) => ProviderEndpointConfigurationSnapshot.FromConfiguration(
                    $"Image provider fallback {index + 1}",
                    item))
                .ToArray(),
            configuration.Validate());
    }

    public static ProviderCenterSnapshot MissingEnvironmentFile(string envPath)
    {
        return new ProviderCenterSnapshot(
            ProviderEndpointConfigurationSnapshot.Empty("Text provider", "TEXT_PROVIDER"),
            ProviderEndpointConfigurationSnapshot.Empty("Image provider", "IMAGE_PROVIDER"),
            [],
            [],
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
        return Text
            .Concat(Image)
            .Where(item => string.Equals(item.ProviderPrefix, prefix, StringComparison.Ordinal))
            .ToArray();
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
