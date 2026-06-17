namespace ContentDeliveryStudio.Application.Backups;

public interface IBackupRestoreService
{
    Task<BackupResult> CreateBackupAsync(BackupRequest request, CancellationToken cancellationToken);

    Task<RestoreResult> RestoreBackupAsync(RestoreRequest request, CancellationToken cancellationToken);
}

public sealed record BackupRequest(
    string SourceDirectory,
    string BackupFilePath,
    BackupOptions? Options = null);

public sealed record BackupOptions(
    IReadOnlySet<string>? ExcludedDirectoryNames = null,
    IReadOnlySet<string>? ExcludedFileNames = null,
    IReadOnlySet<string>? ExcludedFileExtensions = null)
{
    public static BackupOptions SafeDefaults { get; } = new(
        new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "bin",
            "obj",
            "workspace",
            "outputs",
        },
        new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ".env",
            "appsettings.local.json",
        },
        new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ".db",
            ".sqlite",
            ".sqlite3",
        });
}

public sealed record BackupResult(
    string BackupFilePath,
    int IncludedFileCount,
    int SkippedFileCount,
    string ManifestEntryName);

public sealed record RestoreRequest(
    string BackupFilePath,
    string TargetDirectory,
    bool Overwrite = false);

public sealed record RestoreResult(
    string TargetDirectory,
    int RestoredFileCount);
