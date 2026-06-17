using CommunityToolkit.Mvvm.ComponentModel;
using ContentDeliveryStudio.App.Services;

namespace ContentDeliveryStudio.App.ViewModels;

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
