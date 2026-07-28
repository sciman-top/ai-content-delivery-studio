using Microsoft.Win32;

namespace ContentDeliveryStudio.App.Services;

public interface IDiagnosticsDirectoryPickerService
{
    Task<string?> PickAsync(CancellationToken cancellationToken);
}

public sealed class DiagnosticsDirectoryPickerService : IDiagnosticsDirectoryPickerService
{
    public Task<string?> PickAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var dialog = new OpenFolderDialog
        {
            Multiselect = false,
            Title = "Select a parent folder for local diagnostics",
        };

        var selected = dialog.ShowDialog() is true ? dialog.FolderName : null;
        return Task.FromResult(selected);
    }
}
