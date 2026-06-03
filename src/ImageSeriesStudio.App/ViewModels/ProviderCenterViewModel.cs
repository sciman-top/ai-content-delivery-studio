using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using ImageSeriesStudio.Infrastructure.OpenAI;

namespace ImageSeriesStudio.App.ViewModels;

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

public sealed partial class ProviderCenterViewModel : ObservableObject
{
    private readonly IProviderCenterConfigurationService _configurationService;
    private readonly IProviderCenterHealthCheckService? _healthCheckService;
    private string _summaryText = "Provider configuration not loaded.";
    private IReadOnlyList<ProviderEndpointRowViewModel> _providerRows = [];
    private IReadOnlyList<string> _validationMessages = [];

    public ProviderCenterViewModel(
        IProviderCenterConfigurationService configurationService,
        IProviderCenterHealthCheckService? healthCheckService = null)
    {
        _configurationService = configurationService;
        _healthCheckService = healthCheckService;
    }

    public string SummaryText
    {
        get => _summaryText;
        private set => SetProperty(ref _summaryText, value);
    }

    public IReadOnlyList<ProviderEndpointRowViewModel> ProviderRows
    {
        get => _providerRows;
        private set => SetProperty(ref _providerRows, value);
    }

    public IReadOnlyList<string> ValidationMessages
    {
        get => _validationMessages;
        private set => SetProperty(ref _validationMessages, value);
    }

    public async Task RefreshAsync(CancellationToken cancellationToken)
    {
        var snapshot = await _configurationService.LoadAsync(cancellationToken);

        ProviderRows =
        [
            ProviderEndpointRowViewModel.FromSnapshot(snapshot.Text),
            ProviderEndpointRowViewModel.FromSnapshot(snapshot.Image),
        ];
        ValidationMessages = snapshot.ValidationMessages;
        SummaryText = BuildSummary(snapshot);
    }

    public async Task CheckHealthAsync(CancellationToken cancellationToken)
    {
        if (_healthCheckService is null)
        {
            SummaryText = "Provider health check service is not configured.";
            return;
        }

        var snapshot = await _healthCheckService.CheckAsync(cancellationToken);
        ProviderRows = ProviderRows
            .Select(row => row with { HealthSummary = BuildEndpointHealthSummary(snapshot.ForPrefix(row.Prefix)) })
            .ToArray();
        SummaryText = BuildHealthSummary(snapshot);
    }

    private static string BuildSummary(ProviderCenterSnapshot snapshot)
    {
        if (snapshot.ValidationMessages.Count > 0)
        {
            return $"Provider configuration needs attention: {snapshot.ValidationMessages.Count} issue(s).";
        }

        var textSecret = snapshot.Text.ApiKeyCount == 1 ? "text key configured" : $"text keys {snapshot.Text.ApiKeyCount}";
        return $"Providers ready: {textSecret}; image keys {snapshot.Image.ApiKeyCount}; total image concurrency {snapshot.Image.TotalConcurrency}.";
    }

    private static string BuildHealthSummary(ProviderCenterHealthSnapshot snapshot)
    {
        return $"Provider health: text {BuildEndpointHealthSummary(snapshot.Text)}; image {BuildEndpointHealthSummary(snapshot.Image)}.";
    }

    private static string BuildEndpointHealthSummary(IReadOnlyList<ProviderKeyHealthSnapshot> entries)
    {
        if (entries.Count == 0)
        {
            return "Not checked";
        }

        if (entries.Count == 1)
        {
            return entries[0].Status;
        }

        return string.Join(
            ", ",
            entries
                .GroupBy(entry => entry.Status)
                .Select(group => $"{group.Count()} {group.Key}"));
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

public sealed record ProviderEndpointRowViewModel(
    string Title,
    string Prefix,
    string Kind,
    string BaseUrl,
    string Model,
    string SecretSummary,
    string ConcurrencySummary,
    string HealthSummary)
{
    public static ProviderEndpointRowViewModel FromSnapshot(ProviderEndpointConfigurationSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        return new ProviderEndpointRowViewModel(
            snapshot.Title,
            snapshot.Prefix,
            snapshot.Kind,
            snapshot.BaseUrl,
            snapshot.Model,
            BuildSecretSummary(snapshot),
            $"{snapshot.ConcurrencyPerKey} per key / {snapshot.TotalConcurrency} total",
            "Not checked");
    }

    private static string BuildSecretSummary(ProviderEndpointConfigurationSnapshot snapshot)
    {
        var keySummary = snapshot.ApiKeyCount switch
        {
            0 => "no key",
            1 => "1 key",
            _ => $"{snapshot.ApiKeyCount} keys",
        };

        return snapshot.UsesAppCredentials
            ? $"{keySummary} + app credentials"
            : keySummary;
    }
}
