using ContentDeliveryStudio.App.Services;

namespace ContentDeliveryStudio.App.ViewModels;

public sealed class ProviderCenterPresentationCoordinator
{
    public IReadOnlyList<ProviderEndpointRowViewModel> BuildProviderRows(ProviderCenterSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        return
        [
            ProviderEndpointRowViewModel.FromSnapshot(snapshot.Text),
            .. snapshot.TextFallbacks.Select(ProviderEndpointRowViewModel.FromSnapshot),
            ProviderEndpointRowViewModel.FromSnapshot(snapshot.Image),
            .. snapshot.ImageFallbacks.Select(ProviderEndpointRowViewModel.FromSnapshot),
        ];
    }

    public IReadOnlyList<ProviderEndpointRowViewModel> UpdateHealthRows(
        IReadOnlyList<ProviderEndpointRowViewModel> providerRows,
        ProviderCenterHealthSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(providerRows);
        ArgumentNullException.ThrowIfNull(snapshot);

        return providerRows
            .Select(row => row with { HealthSummary = BuildEndpointHealthSummary(snapshot.ForPrefix(row.Prefix)) })
            .ToArray();
    }

    public string BuildSummary(ProviderCenterSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        if (snapshot.ValidationMessages.Count > 0)
        {
            return $"Provider configuration needs attention: {snapshot.ValidationMessages.Count} issue(s).";
        }

        var textKeyCount = snapshot.Text.ApiKeyCount + snapshot.TextFallbacks.Sum(item => item.ApiKeyCount);
        var imageKeyCount = snapshot.Image.ApiKeyCount + snapshot.ImageFallbacks.Sum(item => item.ApiKeyCount);
        var imageConcurrency = snapshot.Image.TotalConcurrency + snapshot.ImageFallbacks.Sum(item => item.TotalConcurrency);
        var textSecret = textKeyCount == 1 ? "text key configured" : $"text keys {textKeyCount}";
        return $"Providers ready: {textSecret}; image keys {imageKeyCount}; total image concurrency {imageConcurrency}.";
    }

    public string BuildHealthSummary(ProviderCenterHealthSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

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
