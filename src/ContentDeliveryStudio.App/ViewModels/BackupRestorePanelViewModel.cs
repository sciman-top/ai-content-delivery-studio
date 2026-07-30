using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ContentDeliveryStudio.App.Services;
using ContentDeliveryStudio.Application.Backups;
using ContentDeliveryStudio.Application.Localization;

namespace ContentDeliveryStudio.App.ViewModels;

public sealed class BackupRestorePanelViewModel : ObservableObject
{
    private readonly LocalizationService _localization;
    private readonly IBackupRestoreService _backupRestoreService;
    private readonly IBackupRestorePickerService _picker;
    private string _backupSourceDirectory = string.Empty;
    private string _backupFilePath = string.Empty;
    private string _restoreBackupFilePath = string.Empty;
    private string _restoreTargetDirectory = string.Empty;
    private string _statusText = string.Empty;
    private LocalizationKey _statusKey = LocalizationKey.BackupRestoreSelectInputsStatus;
    private object[] _statusArguments = [];
    private bool _isBusy;

    public BackupRestorePanelViewModel(
        LocalizationService localization,
        IBackupRestoreService backupRestoreService,
        IBackupRestorePickerService picker)
    {
        _localization = localization;
        _backupRestoreService = backupRestoreService;
        _picker = picker;
        BrowseBackupSourceCommand = new AsyncRelayCommand(BrowseBackupSourceAsync, () => !IsBusy);
        BrowseBackupDestinationCommand = new AsyncRelayCommand(BrowseBackupDestinationAsync, () => !IsBusy);
        CreateBackupCommand = new AsyncRelayCommand(CreateBackupAsync, CanCreateBackup);
        BrowseRestoreBackupCommand = new AsyncRelayCommand(BrowseRestoreBackupAsync, () => !IsBusy);
        BrowseRestoreTargetCommand = new AsyncRelayCommand(BrowseRestoreTargetAsync, () => !IsBusy);
        RestoreBackupCommand = new AsyncRelayCommand(RestoreBackupAsync, CanRestoreBackup);
        RefreshLocalizedText();
    }

    public IAsyncRelayCommand BrowseBackupSourceCommand { get; }

    public IAsyncRelayCommand BrowseBackupDestinationCommand { get; }

    public IAsyncRelayCommand CreateBackupCommand { get; }

    public IAsyncRelayCommand BrowseRestoreBackupCommand { get; }

    public IAsyncRelayCommand BrowseRestoreTargetCommand { get; }

    public IAsyncRelayCommand RestoreBackupCommand { get; }

    public string Title => Text(LocalizationKey.BackupRestoreTitle);

    public string SafetySummary => Text(LocalizationKey.BackupRestoreSafetySummary);

    public string BackupSourceDirectoryLabel => Text(LocalizationKey.BackupSourceDirectory);

    public string BackupDestinationFileLabel => Text(LocalizationKey.BackupDestinationFile);

    public string BrowseBackupSourceText => Text(LocalizationKey.BackupBrowseSource);

    public string BrowseBackupDestinationText => Text(LocalizationKey.BackupBrowseDestination);

    public string CreateBackupText => Text(LocalizationKey.BackupCreate);

    public string RestoreBackupFileLabel => Text(LocalizationKey.RestoreBackupFile);

    public string RestoreTargetDirectoryLabel => Text(LocalizationKey.RestoreTargetDirectory);

    public string BrowseRestoreBackupText => Text(LocalizationKey.RestoreBrowseBackup);

    public string BrowseRestoreTargetText => Text(LocalizationKey.RestoreBrowseTarget);

    public string RestoreBackupText => Text(LocalizationKey.RestoreCreate);

    public string BackupSourceDirectory
    {
        get => _backupSourceDirectory;
        private set
        {
            if (SetProperty(ref _backupSourceDirectory, value))
            {
                CreateBackupCommand.NotifyCanExecuteChanged();
            }
        }
    }

    public string BackupFilePath
    {
        get => _backupFilePath;
        private set
        {
            if (SetProperty(ref _backupFilePath, value))
            {
                CreateBackupCommand.NotifyCanExecuteChanged();
            }
        }
    }

    public string RestoreBackupFilePath
    {
        get => _restoreBackupFilePath;
        private set
        {
            if (SetProperty(ref _restoreBackupFilePath, value))
            {
                RestoreBackupCommand.NotifyCanExecuteChanged();
            }
        }
    }

    public string RestoreTargetDirectory
    {
        get => _restoreTargetDirectory;
        private set
        {
            if (SetProperty(ref _restoreTargetDirectory, value))
            {
                RestoreBackupCommand.NotifyCanExecuteChanged();
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
                BrowseBackupSourceCommand.NotifyCanExecuteChanged();
                BrowseBackupDestinationCommand.NotifyCanExecuteChanged();
                CreateBackupCommand.NotifyCanExecuteChanged();
                BrowseRestoreBackupCommand.NotifyCanExecuteChanged();
                BrowseRestoreTargetCommand.NotifyCanExecuteChanged();
                RestoreBackupCommand.NotifyCanExecuteChanged();
            }
        }
    }

    public void RefreshLocalizedText()
    {
        OnPropertyChanged(nameof(Title));
        OnPropertyChanged(nameof(SafetySummary));
        OnPropertyChanged(nameof(BackupSourceDirectoryLabel));
        OnPropertyChanged(nameof(BackupDestinationFileLabel));
        OnPropertyChanged(nameof(BrowseBackupSourceText));
        OnPropertyChanged(nameof(BrowseBackupDestinationText));
        OnPropertyChanged(nameof(CreateBackupText));
        OnPropertyChanged(nameof(RestoreBackupFileLabel));
        OnPropertyChanged(nameof(RestoreTargetDirectoryLabel));
        OnPropertyChanged(nameof(BrowseRestoreBackupText));
        OnPropertyChanged(nameof(BrowseRestoreTargetText));
        OnPropertyChanged(nameof(RestoreBackupText));
        UpdateStatusText();
    }

    private async Task BrowseBackupSourceAsync()
    {
        var selected = await _picker.PickDirectoryAsync(BackupSourceDirectoryLabel, CancellationToken.None);
        if (!string.IsNullOrWhiteSpace(selected))
        {
            BackupSourceDirectory = selected;
        }
    }

    private async Task BrowseBackupDestinationAsync()
    {
        var selected = await _picker.PickZipFileAsync(BackupDestinationFileLabel, save: true, CancellationToken.None);
        if (!string.IsNullOrWhiteSpace(selected))
        {
            BackupFilePath = selected;
        }
    }

    private async Task BrowseRestoreBackupAsync()
    {
        var selected = await _picker.PickZipFileAsync(RestoreBackupFileLabel, save: false, CancellationToken.None);
        if (!string.IsNullOrWhiteSpace(selected))
        {
            RestoreBackupFilePath = selected;
        }
    }

    private async Task BrowseRestoreTargetAsync()
    {
        var selected = await _picker.PickDirectoryAsync(RestoreTargetDirectoryLabel, CancellationToken.None);
        if (!string.IsNullOrWhiteSpace(selected))
        {
            RestoreTargetDirectory = selected;
        }
    }

    private bool CanCreateBackup() => !IsBusy
        && !string.IsNullOrWhiteSpace(BackupSourceDirectory)
        && !string.IsNullOrWhiteSpace(BackupFilePath);

    private bool CanRestoreBackup() => !IsBusy
        && !string.IsNullOrWhiteSpace(RestoreBackupFilePath)
        && !string.IsNullOrWhiteSpace(RestoreTargetDirectory);

    private async Task CreateBackupAsync()
    {
        IsBusy = true;
        SetStatus(LocalizationKey.BackupCreatingStatus);
        try
        {
            var result = await _backupRestoreService.CreateBackupAsync(
                new BackupRequest(BackupSourceDirectory, BackupFilePath, BackupOptions.SafeDefaults),
                CancellationToken.None);
            SetStatus(LocalizationKey.BackupSucceededTemplate, result.IncludedFileCount, result.BackupFilePath);
        }
        catch (Exception exception)
        {
            SetStatus(LocalizationKey.BackupFailedTemplate, BoundedMessage(exception));
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task RestoreBackupAsync()
    {
        IsBusy = true;
        SetStatus(LocalizationKey.RestoreCreatingStatus);
        try
        {
            var result = await _backupRestoreService.RestoreBackupAsync(
                new RestoreRequest(RestoreBackupFilePath, RestoreTargetDirectory, Overwrite: false),
                CancellationToken.None);
            SetStatus(LocalizationKey.RestoreSucceededTemplate, result.RestoredFileCount, result.TargetDirectory);
        }
        catch (Exception exception)
        {
            SetStatus(LocalizationKey.RestoreFailedTemplate, BoundedMessage(exception));
        }
        finally
        {
            IsBusy = false;
        }
    }

    private string Text(LocalizationKey key) => _localization.GetText(key);

    private void SetStatus(LocalizationKey key, params object[] arguments)
    {
        _statusKey = key;
        _statusArguments = arguments;
        UpdateStatusText();
    }

    private void UpdateStatusText()
    {
        var text = Text(_statusKey);
        StatusText = _statusArguments.Length == 0
            ? text
            : string.Format(System.Globalization.CultureInfo.CurrentCulture, text, _statusArguments);
    }

    private static string BoundedMessage(Exception exception)
    {
        return exception.Message.Length <= 200 ? exception.Message : exception.Message[..200];
    }
}
