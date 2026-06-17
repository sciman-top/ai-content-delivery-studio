using Microsoft.Win32;

namespace ContentDeliveryStudio.App.Services;

public interface IDocumentSourceFilePickerService
{
    Task<string?> PickAsync(CancellationToken cancellationToken);
}

public sealed class DocumentSourceFilePickerService : IDocumentSourceFilePickerService
{
    public Task<string?> PickAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var dialog = new OpenFileDialog
        {
            CheckFileExists = true,
            CheckPathExists = true,
            Filter = "Document sources (*.pdf;*.docx)|*.pdf;*.docx",
            Multiselect = false,
            Title = "Select a document source file",
        };

        var selected = dialog.ShowDialog() is true ? dialog.FileName : null;
        return Task.FromResult(selected);
    }
}
