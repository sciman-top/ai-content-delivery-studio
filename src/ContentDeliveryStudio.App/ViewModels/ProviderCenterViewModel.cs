using CommunityToolkit.Mvvm.ComponentModel;
using ContentDeliveryStudio.App.Services;

namespace ContentDeliveryStudio.App.ViewModels;

public sealed partial class ProviderCenterViewModel : ObservableObject
{
    private readonly IProviderCenterConfigurationService _configurationService;
    private readonly IProviderCenterHealthCheckService? _healthCheckService;
    private readonly ProviderCenterPresentationCoordinator _presentationCoordinator = new();
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

        ProviderRows = _presentationCoordinator.BuildProviderRows(snapshot);
        ValidationMessages = snapshot.ValidationMessages;
        SummaryText = _presentationCoordinator.BuildSummary(snapshot);
    }

    public async Task CheckHealthAsync(CancellationToken cancellationToken)
    {
        if (_healthCheckService is null)
        {
            SummaryText = "Provider health check service is not configured.";
            return;
        }

        var snapshot = await _healthCheckService.CheckAsync(cancellationToken);
        ProviderRows = _presentationCoordinator.UpdateHealthRows(ProviderRows, snapshot);
        SummaryText = _presentationCoordinator.BuildHealthSummary(snapshot);
    }
}
