using ImageSeriesStudio.Application.Projects;

namespace ImageSeriesStudio.Tests;

internal sealed class LocalStudioDataPathScope : IDisposable
{
    private readonly IDisposable _overrideScope;
    private bool _disposed;

    private LocalStudioDataPathScope(string rootPath, IDisposable overrideScope)
    {
        RootPath = rootPath;
        _overrideScope = overrideScope;
    }

    public string RootPath { get; }

    public static LocalStudioDataPathScope Create()
    {
        var rootPath = Path.Combine(
            Path.GetTempPath(),
            "ImageSeriesStudio.Tests",
            Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(rootPath);

        return new LocalStudioDataPathScope(
            rootPath,
            LocalStudioDataPaths.PushRootOverride(rootPath));
    }

    public string GetProjectDirectory(string areaName, Guid projectId)
    {
        return Path.Combine(RootPath, areaName, projectId.ToString("N"));
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _overrideScope.Dispose();

        if (Directory.Exists(RootPath))
        {
            Directory.Delete(RootPath, recursive: true);
        }

        _disposed = true;
    }
}
