using System.IO;
using Microsoft.Win32;

namespace ContentDeliveryStudio.App.Services;

public interface IFinalDeliveryRootPickerService
{
    Task<string?> PickAsync(string currentRoot, string title, CancellationToken cancellationToken);
}

public sealed class FinalDeliveryRootPickerService : IFinalDeliveryRootPickerService
{
    public Task<string?> PickAsync(string currentRoot, string title, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var dialog = new OpenFolderDialog
        {
            Multiselect = false,
            Title = title,
        };
        if (Directory.Exists(currentRoot))
        {
            dialog.InitialDirectory = currentRoot;
        }

        return Task.FromResult(dialog.ShowDialog() is true ? dialog.FolderName : null);
    }
}
