using System.IO;
using ContentDeliveryStudio.Application.Projects;
using Microsoft.Win32;

namespace ContentDeliveryStudio.App.Services;

public interface IScientificDeliveryPackageSaveService
{
    void SavePackage(byte[] packageBytes);
}

public sealed class ScientificDeliveryPackageSaveService : IScientificDeliveryPackageSaveService
{
    public void SavePackage(byte[] packageBytes)
    {
        ArgumentNullException.ThrowIfNull(packageBytes);
        if (packageBytes.Length == 0)
        {
            throw new ArgumentException("Scientific delivery package cannot be empty.", nameof(packageBytes));
        }

        var defaultDirectory = LocalStudioDataPaths.ResolveFinalDeliveryCategoryRoot(
            FinalImageDeliveryCategory.ScientificFigures);
        Directory.CreateDirectory(defaultDirectory);
        var dialog = new SaveFileDialog
        {
            Filter = "ZIP (*.zip)|*.zip",
            DefaultExt = ".zip",
            AddExtension = true,
            FileName = $"scientific-figure-{DateTimeOffset.Now:yyyyMMdd-HHmmss}.zip",
            InitialDirectory = defaultDirectory,
        };
        if (dialog.ShowDialog() == true)
        {
            File.WriteAllBytes(dialog.FileName, packageBytes);
        }
    }
}
