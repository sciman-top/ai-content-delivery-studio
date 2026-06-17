namespace ContentDeliveryStudio.Infrastructure.Workspaces;

public sealed class WorkspaceFolderService
{
    public WorkspaceFolders EnsureWorkspace(string rootPath)
    {
        if (string.IsNullOrWhiteSpace(rootPath))
        {
            throw new ArgumentException("Workspace root path cannot be empty.", nameof(rootPath));
        }

        var root = Path.GetFullPath(rootPath);
        var folders = new WorkspaceFolders(
            root,
            Path.Combine(root, "projects"),
            Path.Combine(root, "assets"),
            Path.Combine(root, "deliveries"),
            Path.Combine(root, "logs"));

        Directory.CreateDirectory(folders.RootPath);
        Directory.CreateDirectory(folders.ProjectsPath);
        Directory.CreateDirectory(folders.AssetsPath);
        Directory.CreateDirectory(folders.DeliveriesPath);
        Directory.CreateDirectory(folders.LogsPath);

        return folders;
    }
}

public sealed record WorkspaceFolders(
    string RootPath,
    string ProjectsPath,
    string AssetsPath,
    string DeliveriesPath,
    string LogsPath);
