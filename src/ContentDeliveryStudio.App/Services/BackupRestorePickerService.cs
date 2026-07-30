using Microsoft.Win32;

namespace ContentDeliveryStudio.App.Services;

public interface IBackupRestorePickerService
{
    Task<string?> PickDirectoryAsync(string title, CancellationToken cancellationToken);

    Task<string?> PickZipFileAsync(string title, bool save, CancellationToken cancellationToken);
}

public sealed class BackupRestorePickerService : IBackupRestorePickerService
{
    public Task<string?> PickDirectoryAsync(string title, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var dialog = new OpenFolderDialog
        {
            Multiselect = false,
            Title = title,
        };
        return Task.FromResult(dialog.ShowDialog() is true ? dialog.FolderName : null);
    }

    public Task<string?> PickZipFileAsync(string title, bool save, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        FileDialog dialog = save
            ? new SaveFileDialog
            {
                AddExtension = true,
                DefaultExt = ".zip",
                OverwritePrompt = true,
            }
            : new OpenFileDialog
            {
                CheckFileExists = true,
                Multiselect = false,
            };
        dialog.Title = title;
        dialog.Filter = "ZIP archive (*.zip)|*.zip";
        return Task.FromResult(dialog.ShowDialog() is true ? dialog.FileName : null);
    }
}
