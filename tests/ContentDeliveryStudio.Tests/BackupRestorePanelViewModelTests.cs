using ContentDeliveryStudio.App.Services;
using ContentDeliveryStudio.App.ViewModels;
using ContentDeliveryStudio.Application.Backups;
using ContentDeliveryStudio.Application.Localization;
using ContentDeliveryStudio.Core.Projects;

namespace ContentDeliveryStudio.Tests;

public sealed class BackupRestorePanelViewModelTests
{
    [Fact]
    public async Task BrowseCreateAndRestore_UseSafeNonOverwriteContracts()
    {
        var localization = new LocalizationService();
        localization.SetLanguage(LanguagePreference.English);
        var service = new RecordingBackupRestoreService();
        var picker = new QueuePicker(
            ["source", "restore-target"],
            ["backup.zip", "backup.zip"]);
        var viewModel = new BackupRestorePanelViewModel(localization, service, picker);

        await viewModel.BrowseBackupSourceCommand.ExecuteAsync(null);
        await viewModel.BrowseBackupDestinationCommand.ExecuteAsync(null);
        Assert.True(viewModel.CreateBackupCommand.CanExecute(null));
        await viewModel.CreateBackupCommand.ExecuteAsync(null);

        var backupRequest = Assert.Single(service.BackupRequests);
        Assert.Same(BackupOptions.SafeDefaults, backupRequest.Options);
        Assert.Contains("Backup created with 2 files", viewModel.StatusText, StringComparison.Ordinal);

        await viewModel.BrowseRestoreBackupCommand.ExecuteAsync(null);
        await viewModel.BrowseRestoreTargetCommand.ExecuteAsync(null);
        Assert.True(viewModel.RestoreBackupCommand.CanExecute(null));
        await viewModel.RestoreBackupCommand.ExecuteAsync(null);

        var restoreRequest = Assert.Single(service.RestoreRequests);
        Assert.False(restoreRequest.Overwrite);
        Assert.Equal("restore-target", restoreRequest.TargetDirectory);
        Assert.Contains("Restored 2 files", viewModel.StatusText, StringComparison.Ordinal);
        Assert.False(viewModel.IsBusy);
    }

    [Fact]
    public void RefreshLocalizedText_UpdatesSafetyAndCommands()
    {
        var localization = new LocalizationService();
        var viewModel = new BackupRestorePanelViewModel(
            localization,
            new RecordingBackupRestoreService(),
            new QueuePicker([], []));

        localization.SetLanguage(LanguagePreference.Chinese);
        viewModel.RefreshLocalizedText();

        Assert.Equal("安全备份与恢复", viewModel.Title);
        Assert.Equal("创建备份", viewModel.CreateBackupText);
        Assert.Equal("恢复备份", viewModel.RestoreBackupText);
        Assert.Contains("请选择备份或恢复位置", viewModel.StatusText, StringComparison.Ordinal);
    }

    private sealed class QueuePicker(
        IEnumerable<string> directories,
        IEnumerable<string> zipFiles) : IBackupRestorePickerService
    {
        private readonly Queue<string> _directories = new(directories);
        private readonly Queue<string> _zipFiles = new(zipFiles);

        public Task<string?> PickDirectoryAsync(string title, CancellationToken cancellationToken) =>
            Task.FromResult<string?>(_directories.Dequeue());

        public Task<string?> PickZipFileAsync(string title, bool save, CancellationToken cancellationToken) =>
            Task.FromResult<string?>(_zipFiles.Dequeue());
    }

    private sealed class RecordingBackupRestoreService : IBackupRestoreService
    {
        public List<BackupRequest> BackupRequests { get; } = [];

        public List<RestoreRequest> RestoreRequests { get; } = [];

        public Task<BackupResult> CreateBackupAsync(BackupRequest request, CancellationToken cancellationToken)
        {
            BackupRequests.Add(request);
            return Task.FromResult(new BackupResult(request.BackupFilePath, 2, 0, "backup-manifest.json"));
        }

        public Task<RestoreResult> RestoreBackupAsync(RestoreRequest request, CancellationToken cancellationToken)
        {
            RestoreRequests.Add(request);
            return Task.FromResult(new RestoreResult(request.TargetDirectory, 2));
        }
    }
}
