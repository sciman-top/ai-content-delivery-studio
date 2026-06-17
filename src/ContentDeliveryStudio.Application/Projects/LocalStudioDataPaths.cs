namespace ContentDeliveryStudio.Application.Projects;

public static class LocalStudioDataPaths
{
    internal const string CurrentStudioFolderName = "ContentDeliveryStudio";
    internal const string LegacyStudioFolderName = "ImageSeriesStudio";
    private static readonly AsyncLocal<string?> RootOverride = new();

    public static string ResolveStudioRoot()
    {
        var overrideRoot = RootOverride.Value;
        var root = string.IsNullOrWhiteSpace(overrideRoot)
            ? ResolveStudioRootPath(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData))
            : Path.GetFullPath(overrideRoot);

        return root;
    }

    internal static string ResolveStudioRootPath(string localAppDataRoot)
    {
        if (string.IsNullOrWhiteSpace(localAppDataRoot))
        {
            throw new ArgumentException("Local app data root cannot be empty.", nameof(localAppDataRoot));
        }

        var normalizedBaseRoot = Path.GetFullPath(localAppDataRoot);
        var currentRoot = Path.Combine(normalizedBaseRoot, CurrentStudioFolderName);
        if (Directory.Exists(currentRoot))
        {
            return currentRoot;
        }

        var legacyRoot = Path.Combine(normalizedBaseRoot, LegacyStudioFolderName);
        if (Directory.Exists(legacyRoot))
        {
            return legacyRoot;
        }

        return currentRoot;
    }

    public static string ResolveProjectDirectory(string areaName, Guid projectId)
    {
        if (string.IsNullOrWhiteSpace(areaName))
        {
            throw new ArgumentException("Area name cannot be empty.", nameof(areaName));
        }

        return Path.Combine(
            ResolveStudioRoot(),
            areaName.Trim(),
            projectId.ToString("N"));
    }

    public static string ResolveTimestampedProjectDirectory(
        string areaName,
        Guid projectId,
        DateTimeOffset timestamp)
    {
        return Path.Combine(
            ResolveProjectDirectory(areaName, projectId),
            timestamp.ToString("yyyyMMdd-HHmmss"));
    }

    public static IDisposable PushRootOverride(string rootPath)
    {
        if (string.IsNullOrWhiteSpace(rootPath))
        {
            throw new ArgumentException("Root path cannot be empty.", nameof(rootPath));
        }

        var previousRoot = RootOverride.Value;
        RootOverride.Value = Path.GetFullPath(rootPath);
        return new RootOverrideScope(previousRoot);
    }

    private sealed class RootOverrideScope(string? previousRoot) : IDisposable
    {
        private bool _disposed;

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            RootOverride.Value = previousRoot;
            _disposed = true;
        }
    }
}
