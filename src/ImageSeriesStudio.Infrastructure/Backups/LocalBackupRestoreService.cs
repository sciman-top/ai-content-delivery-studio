using System.IO.Compression;
using System.Text.Json;
using ImageSeriesStudio.Application.Backups;

namespace ImageSeriesStudio.Infrastructure.Backups;

public sealed class LocalBackupRestoreService : IBackupRestoreService
{
    private const string ManifestEntryName = "backup-manifest.json";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
    };

    public async Task<BackupResult> CreateBackupAsync(
        BackupRequest request,
        CancellationToken cancellationToken)
    {
        var sourceRoot = GetExistingDirectory(request.SourceDirectory, nameof(request.SourceDirectory));
        var backupFilePath = Path.GetFullPath(request.BackupFilePath);
        var backupDirectory = Path.GetDirectoryName(backupFilePath);

        if (string.IsNullOrWhiteSpace(backupDirectory))
        {
            throw new ArgumentException("Backup file path must include a directory.", nameof(request));
        }

        Directory.CreateDirectory(backupDirectory);

        var options = request.Options ?? BackupOptions.SafeDefaults;
        var includedFiles = new List<BackupManifestFile>();
        var skippedFileCount = 0;

        if (File.Exists(backupFilePath))
        {
            File.Delete(backupFilePath);
        }

        using var archive = ZipFile.Open(backupFilePath, ZipArchiveMode.Create);

        foreach (var filePath in Directory.EnumerateFiles(sourceRoot, "*", SearchOption.AllDirectories))
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (ShouldSkip(sourceRoot, filePath, options))
            {
                skippedFileCount++;
                continue;
            }

            var relativePath = ToArchivePath(Path.GetRelativePath(sourceRoot, filePath));
            archive.CreateEntryFromFile(filePath, relativePath, CompressionLevel.Optimal);
            includedFiles.Add(new BackupManifestFile(relativePath, new FileInfo(filePath).Length));
        }

        var manifest = new BackupManifest(
            DateTimeOffset.UtcNow,
            sourceRoot,
            includedFiles,
            skippedFileCount);

        var manifestEntry = archive.CreateEntry(ManifestEntryName, CompressionLevel.Optimal);
        await using (var stream = manifestEntry.Open())
        {
            await JsonSerializer.SerializeAsync(stream, manifest, JsonOptions, cancellationToken);
        }

        return new BackupResult(
            backupFilePath,
            includedFiles.Count,
            skippedFileCount,
            ManifestEntryName);
    }

    public async Task<RestoreResult> RestoreBackupAsync(
        RestoreRequest request,
        CancellationToken cancellationToken)
    {
        var backupFilePath = Path.GetFullPath(request.BackupFilePath);
        if (!File.Exists(backupFilePath))
        {
            throw new FileNotFoundException("Backup file does not exist.", backupFilePath);
        }

        var targetRoot = Path.GetFullPath(request.TargetDirectory);
        Directory.CreateDirectory(targetRoot);

        var restoredCount = 0;

        using var archive = ZipFile.OpenRead(backupFilePath);
        foreach (var entry in archive.Entries)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (entry.FullName.Length == 0 || entry.FullName.EndsWith('/'))
            {
                continue;
            }

            if (string.Equals(entry.FullName, ManifestEntryName, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var destinationPath = ResolveInsideRoot(targetRoot, entry.FullName);
            Directory.CreateDirectory(Path.GetDirectoryName(destinationPath)!);

            if (File.Exists(destinationPath) && !request.Overwrite)
            {
                throw new IOException($"Restore target already exists: {destinationPath}");
            }

            await using var source = entry.Open();
            await using var destination = File.Create(destinationPath);
            await source.CopyToAsync(destination, cancellationToken);
            restoredCount++;
        }

        return new RestoreResult(targetRoot, restoredCount);
    }

    private static bool ShouldSkip(string sourceRoot, string filePath, BackupOptions options)
    {
        var relativePath = Path.GetRelativePath(sourceRoot, filePath);
        var parts = relativePath.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

        if (parts.Any(part => options.ExcludedDirectoryNames?.Contains(part) == true))
        {
            return true;
        }

        var fileName = Path.GetFileName(filePath);
        if (options.ExcludedFileNames?.Contains(fileName) == true)
        {
            return true;
        }

        var extension = Path.GetExtension(filePath);
        return options.ExcludedFileExtensions?.Contains(extension) == true;
    }

    private static string GetExistingDirectory(string path, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException("Directory path cannot be empty.", parameterName);
        }

        var fullPath = Path.GetFullPath(path);
        if (!Directory.Exists(fullPath))
        {
            throw new DirectoryNotFoundException(fullPath);
        }

        return fullPath;
    }

    private static string ResolveInsideRoot(string rootDirectory, string relativePath)
    {
        var destinationPath = Path.GetFullPath(Path.Combine(rootDirectory, relativePath));
        var rootWithSeparator = rootDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
            + Path.DirectorySeparatorChar;

        if (!destinationPath.StartsWith(rootWithSeparator, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"Backup entry escapes target directory: {relativePath}");
        }

        return destinationPath;
    }

    private static string ToArchivePath(string relativePath)
    {
        return relativePath.Replace('\\', '/');
    }
}

internal sealed record BackupManifest(
    DateTimeOffset CreatedAt,
    string SourceDirectory,
    IReadOnlyList<BackupManifestFile> Files,
    int SkippedFileCount);

internal sealed record BackupManifestFile(
    string Path,
    long SizeBytes);
