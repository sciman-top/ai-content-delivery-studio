using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ContentDeliveryStudio.App.Services;
using ContentDeliveryStudio.Application.Diagnostics;
using ContentDeliveryStudio.Application.Localization;

namespace ContentDeliveryStudio.App.ViewModels;

public sealed class DiagnosticsPanelViewModel : ObservableObject
{
    private readonly LocalizationService _localization;
    private readonly DiagnosticsPackageApplicationService _applicationService;
    private readonly IDesktopDiagnosticsSnapshotFactory _snapshotFactory;
    private readonly IDiagnosticsDirectoryPickerService _directoryPicker;
    private string _outputParentDirectory = string.Empty;
    private string _statusText = string.Empty;
    private LocalizationKey _statusKey = LocalizationKey.DiagnosticsSelectDirectoryStatus;
    private string? _statusArgument;
    private bool _isBusy;

    public DiagnosticsPanelViewModel(
        LocalizationService localization,
        DiagnosticsPackageApplicationService applicationService,
        IDesktopDiagnosticsSnapshotFactory snapshotFactory,
        IDiagnosticsDirectoryPickerService directoryPicker)
    {
        _localization = localization;
        _applicationService = applicationService;
        _snapshotFactory = snapshotFactory;
        _directoryPicker = directoryPicker;
        BrowseCommand = new AsyncRelayCommand(BrowseAsync, () => !IsBusy);
        ExportCommand = new AsyncRelayCommand(ExportAsync, CanExport);
        RefreshLocalizedText();
    }

    public IAsyncRelayCommand BrowseCommand { get; }

    public IAsyncRelayCommand ExportCommand { get; }

    public string Title => Text(LocalizationKey.DiagnosticsExportTitle);

    public string PrivacySummary => Text(LocalizationKey.DiagnosticsPrivacySummary);

    public string OutputDirectoryLabel => Text(LocalizationKey.DiagnosticsOutputDirectory);

    public string BrowseText => Text(LocalizationKey.DiagnosticsBrowse);

    public string ExportText => Text(LocalizationKey.DiagnosticsExport);

    public string OutputParentDirectory
    {
        get => _outputParentDirectory;
        set
        {
            if (SetProperty(ref _outputParentDirectory, value))
            {
                ExportCommand.NotifyCanExecuteChanged();
            }
        }
    }

    public string StatusText
    {
        get => _statusText;
        private set => SetProperty(ref _statusText, value);
    }

    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (SetProperty(ref _isBusy, value))
            {
                BrowseCommand.NotifyCanExecuteChanged();
                ExportCommand.NotifyCanExecuteChanged();
            }
        }
    }

    public void RefreshLocalizedText()
    {
        OnPropertyChanged(nameof(Title));
        OnPropertyChanged(nameof(PrivacySummary));
        OnPropertyChanged(nameof(OutputDirectoryLabel));
        OnPropertyChanged(nameof(BrowseText));
        OnPropertyChanged(nameof(ExportText));

        UpdateStatusText();
    }

    private async Task BrowseAsync()
    {
        var selected = await _directoryPicker.PickAsync(CancellationToken.None);
        if (!string.IsNullOrWhiteSpace(selected))
        {
            OutputParentDirectory = selected;
            SetStatus(LocalizationKey.DiagnosticsReadyStatus);
        }
    }

    private bool CanExport() => !IsBusy && !string.IsNullOrWhiteSpace(OutputParentDirectory);

    private async Task ExportAsync()
    {
        IsBusy = true;
        SetStatus(LocalizationKey.DiagnosticsExportingStatus);
        try
        {
            var snapshots = await _snapshotFactory.CreateAsync(CancellationToken.None);
            var result = await _applicationService.ExportAsync(
                new DiagnosticsPackageApplicationRequest(
                    OutputParentDirectory,
                    snapshots.Application,
                    snapshots.Machine,
                    snapshots.Providers,
                    snapshots.Secrets),
                CancellationToken.None);
            SetStatus(LocalizationKey.DiagnosticsExportSucceededTemplate, result.PackageDirectory);
        }
        catch (OperationCanceledException)
        {
            SetStatus(LocalizationKey.DiagnosticsExportCancelledStatus);
        }
        catch (Exception exception)
        {
            var message = exception.Message.Length <= 200
                ? exception.Message
                : exception.Message[..200];
            SetStatus(LocalizationKey.DiagnosticsExportFailedTemplate, message);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private string Text(LocalizationKey key) => _localization.GetText(key);

    private void SetStatus(LocalizationKey key, string? argument = null)
    {
        _statusKey = key;
        _statusArgument = argument;
        UpdateStatusText();
    }

    private void UpdateStatusText()
    {
        var text = Text(_statusKey);
        StatusText = _statusArgument is null
            ? text
            : string.Format(System.Globalization.CultureInfo.CurrentCulture, text, _statusArgument);
    }
}
