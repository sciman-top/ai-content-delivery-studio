using System.IO.Compression;
using System.Security.Cryptography;
using System.Text.Json;
using ContentDeliveryStudio.Application.Backups;

namespace ContentDeliveryStudio.Infrastructure.Backups;

public sealed class LocalBackupRestoreService : IBackupRestoreService
{
    private const int ManifestSchemaVersion = 1;
    private const string ManifestEntryName = "backup-manifest.json";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
    };

    private readonly LocalBackupRestoreLimits _limits;

    public LocalBackupRestoreService()
        : this(new LocalBackupRestoreLimits())
    {
    }

    internal LocalBackupRestoreService(LocalBackupRestoreLimits limits)
    {
        _limits = limits ?? throw new ArgumentNullException(nameof(limits));
        _limits.Validate();
    }

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

        if (IsInsideRoot(sourceRoot, backupFilePath))
        {
            throw new ArgumentException("Backup file path must be outside the source directory.", nameof(request));
        }

        Directory.CreateDirectory(backupDirectory);

        var options = request.Options ?? BackupOptions.SafeDefaults;
        var includedFiles = new List<BackupManifestFile>();
        var skippedFileCount = 0;
        long includedSizeBytes = 0;
        var tempBackupPath = Path.Combine(
            backupDirectory,
            $".{Path.GetFileName(backupFilePath)}.{Guid.NewGuid():N}.tmp");

        try
        {
            using (var archive = ZipFile.Open(tempBackupPath, ZipArchiveMode.Create))
            {
                var enumerationOptions = new EnumerationOptions
                {
                    RecurseSubdirectories = true,
                    IgnoreInaccessible = false,
                    ReturnSpecialDirectories = false,
                    AttributesToSkip = FileAttributes.ReparsePoint,
                };
                foreach (var filePath in Directory
                             .EnumerateFiles(sourceRoot, "*", enumerationOptions)
                             .OrderBy(path => path, StringComparer.OrdinalIgnoreCase))
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    if (ShouldSkip(sourceRoot, filePath, options))
                    {
                        skippedFileCount++;
                        continue;
                    }

                    if (includedFiles.Count >= _limits.MaximumEntryCount)
                    {
                        throw new InvalidOperationException(
                            $"Backup exceeds the supported file limit of {_limits.MaximumEntryCount}.");
                    }

                    var relativePath = NormalizeArchivePath(Path.GetRelativePath(sourceRoot, filePath));
                    await using var source = File.OpenRead(filePath);
                    if (source.Length > _limits.MaximumEntrySizeBytes)
                    {
                        throw new InvalidOperationException(
                            $"Backup file exceeds the supported size limit: {relativePath}");
                    }

                    var remainingTotalBytes = checked(_limits.MaximumTotalSizeBytes - includedSizeBytes);
                    if (source.Length > remainingTotalBytes)
                    {
                        throw new InvalidOperationException(
                            $"Backup exceeds the supported total size limit of {_limits.MaximumTotalSizeBytes} bytes.");
                    }

                    var entry = archive.CreateEntry(relativePath, CompressionLevel.Optimal);
                    await using var destination = entry.Open();
                    var (sizeBytes, sha256) = await CopyAndHashAsync(
                        source,
                        destination,
                        remainingTotalBytes,
                        cancellationToken);
                    includedSizeBytes = checked(includedSizeBytes + sizeBytes);
                    includedFiles.Add(new BackupManifestFile(relativePath, sizeBytes, sha256));
                }

                var manifest = new BackupManifest(
                    ManifestSchemaVersion,
                    DateTimeOffset.UtcNow,
                    includedFiles,
                    skippedFileCount);

                var manifestEntry = archive.CreateEntry(ManifestEntryName, CompressionLevel.Optimal);
                await using var stream = manifestEntry.Open();
                await JsonSerializer.SerializeAsync(stream, manifest, JsonOptions, cancellationToken);
            }

            File.Move(tempBackupPath, backupFilePath, overwrite: true);
        }
        finally
        {
            if (File.Exists(tempBackupPath))
            {
                File.Delete(tempBackupPath);
            }
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
        if (File.Exists(targetRoot))
        {
            throw new IOException($"Restore target directory is occupied by a file: {targetRoot}");
        }

        using var archive = ZipFile.OpenRead(backupFilePath);
        var validatedFiles = await ValidateArchiveAsync(
            archive,
            targetRoot,
            request.Overwrite,
            cancellationToken);

        var trimmedTargetRoot = targetRoot.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var targetParent = Path.GetDirectoryName(trimmedTargetRoot)
            ?? throw new InvalidOperationException("Restore target cannot be a volume root.");
        var targetName = Path.GetFileName(trimmedTargetRoot);
        Directory.CreateDirectory(targetParent);
        var transactionRoot = Path.Combine(targetParent, $".{targetName}.restore-{Guid.NewGuid():N}");
        var stagingRoot = Path.Combine(transactionRoot, "payload");
        var rollbackRoot = Path.Combine(transactionRoot, "rollback");
        var cleanupTransaction = true;

        try
        {
            Directory.CreateDirectory(stagingRoot);
            foreach (var validatedFile in validatedFiles)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var stagingPath = ResolveInsideRoot(stagingRoot, validatedFile.RelativePath);
                Directory.CreateDirectory(Path.GetDirectoryName(stagingPath)!);

                await using var source = validatedFile.Entry.Open();
                await using var destination = new FileStream(
                    stagingPath,
                    FileMode.CreateNew,
                    FileAccess.Write,
                    FileShare.None);
                await source.CopyToAsync(destination, cancellationToken);
            }

            cancellationToken.ThrowIfCancellationRequested();
            if (!Directory.Exists(targetRoot))
            {
                Directory.Move(stagingRoot, targetRoot);
            }
            else
            {
                CommitStagedFiles(
                    stagingRoot,
                    rollbackRoot,
                    targetRoot,
                    validatedFiles,
                    request.Overwrite);
            }

            return new RestoreResult(targetRoot, validatedFiles.Count);
        }
        catch (RestoreRollbackException exception)
        {
            cleanupTransaction = false;
            throw new IOException(
                $"Restore failed and rollback was incomplete. Recovery data was preserved at: {transactionRoot}",
                exception);
        }
        finally
        {
            if (cleanupTransaction)
            {
                TryDeleteDirectory(transactionRoot);
            }
        }
    }

    private static void CommitStagedFiles(
        string stagingRoot,
        string rollbackRoot,
        string targetRoot,
        IReadOnlyList<ValidatedBackupFile> files,
        bool overwrite)
    {
        var committed = new List<CommittedRestoreFile>(files.Count);
        var createdDirectories = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        try
        {
            foreach (var file in files)
            {
                var stagingPath = ResolveInsideRoot(stagingRoot, file.RelativePath);
                var destinationPath = file.DestinationPath;
                CreateMissingTargetDirectories(
                    Path.GetDirectoryName(destinationPath)!,
                    targetRoot,
                    createdDirectories);

                string? rollbackPath = null;
                try
                {
                    if (File.Exists(destinationPath))
                    {
                        if (!overwrite)
                        {
                            throw new IOException($"Restore target already exists: {destinationPath}");
                        }

                        rollbackPath = ResolveInsideRoot(rollbackRoot, file.RelativePath);
                        Directory.CreateDirectory(Path.GetDirectoryName(rollbackPath)!);
                        File.Move(destinationPath, rollbackPath);
                    }

                    File.Move(stagingPath, destinationPath);
                    committed.Add(new CommittedRestoreFile(destinationPath, rollbackPath));
                }
                catch (Exception commitException)
                {
                    if (rollbackPath is not null
                        && File.Exists(rollbackPath)
                        && !File.Exists(destinationPath))
                    {
                        try
                        {
                            File.Move(rollbackPath, destinationPath);
                        }
                        catch (Exception rollbackException)
                        {
                            throw new RestoreRollbackException(commitException, rollbackException);
                        }
                    }

                    throw;
                }
            }
        }
        catch (Exception commitException)
        {
            try
            {
                RollBackCommittedFiles(committed, createdDirectories);
            }
            catch (Exception rollbackException)
            {
                throw new RestoreRollbackException(commitException, rollbackException);
            }

            throw;
        }
    }

    private static void CreateMissingTargetDirectories(
        string directory,
        string targetRoot,
        ISet<string> createdDirectories)
    {
        var missing = new Stack<string>();
        for (var current = directory;
             !string.Equals(current, targetRoot, StringComparison.OrdinalIgnoreCase) && !Directory.Exists(current);
             current = Path.GetDirectoryName(current)!)
        {
            missing.Push(current);
        }

        while (missing.Count > 0)
        {
            var current = missing.Pop();
            Directory.CreateDirectory(current);
            createdDirectories.Add(current);
        }
    }

    private static void RollBackCommittedFiles(
        IReadOnlyList<CommittedRestoreFile> committed,
        IEnumerable<string> createdDirectories)
    {
        foreach (var file in committed.Reverse())
        {
            if (File.Exists(file.DestinationPath))
            {
                File.Delete(file.DestinationPath);
            }

            if (file.RollbackPath is not null && File.Exists(file.RollbackPath))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(file.DestinationPath)!);
                File.Move(file.RollbackPath, file.DestinationPath);
            }
        }

        foreach (var directory in createdDirectories.OrderByDescending(path => path.Length))
        {
            if (Directory.Exists(directory) && !Directory.EnumerateFileSystemEntries(directory).Any())
            {
                Directory.Delete(directory);
            }
        }
    }

    private static void TryDeleteDirectory(string path)
    {
        try
        {
            if (Directory.Exists(path))
            {
                Directory.Delete(path, recursive: true);
            }
        }
        catch
        {
            // Cleanup is best-effort; preserve the original restore result or failure.
        }
    }

    private async Task<IReadOnlyList<ValidatedBackupFile>> ValidateArchiveAsync(
        ZipArchive archive,
        string targetRoot,
        bool overwrite,
        CancellationToken cancellationToken)
    {
        if (archive.Entries.Count > _limits.MaximumEntryCount + 1)
        {
            throw new InvalidDataException($"Backup exceeds the supported entry limit of {_limits.MaximumEntryCount}.");
        }

        var normalizedEntries = new Dictionary<string, ZipArchiveEntry>(StringComparer.OrdinalIgnoreCase);
        foreach (var entry in archive.Entries)
        {
            cancellationToken.ThrowIfCancellationRequested();
            RejectUnsupportedEntry(entry);
            var normalizedPath = NormalizeArchivePath(entry.FullName);
            if (!normalizedEntries.TryAdd(normalizedPath, entry))
            {
                throw new InvalidDataException($"Backup contains a duplicate entry path: {normalizedPath}");
            }
        }

        var manifestEntries = normalizedEntries
            .Where(pair => string.Equals(pair.Key, ManifestEntryName, StringComparison.OrdinalIgnoreCase))
            .Select(pair => pair.Value)
            .ToArray();
        if (manifestEntries.Length != 1)
        {
            throw new InvalidDataException($"Backup must contain exactly one {ManifestEntryName} entry.");
        }

        var manifestEntry = manifestEntries[0];
        if (manifestEntry.Length > _limits.MaximumManifestSizeBytes)
        {
            throw new InvalidDataException("Backup manifest exceeds the supported size limit.");
        }

        BackupManifest manifest;
        try
        {
            await using var manifestStream = manifestEntry.Open();
            manifest = await JsonSerializer.DeserializeAsync<BackupManifest>(
                           manifestStream,
                           JsonOptions,
                           cancellationToken)
                       ?? throw new InvalidDataException("Backup manifest is empty.");
        }
        catch (JsonException exception)
        {
            throw new InvalidDataException("Backup manifest is not valid JSON.", exception);
        }

        if (manifest.SchemaVersion != ManifestSchemaVersion)
        {
            throw new InvalidDataException(
                $"Backup manifest schema {manifest.SchemaVersion} is not supported.");
        }

        if (manifest.Files is null || manifest.Files.Count > _limits.MaximumEntryCount)
        {
            throw new InvalidDataException("Backup manifest has an invalid file count.");
        }

        var manifestFiles = new Dictionary<string, BackupManifestFile>(StringComparer.OrdinalIgnoreCase);
        long totalSizeBytes = 0;
        foreach (var file in manifest.Files)
        {
            var normalizedPath = NormalizeArchivePath(file.Path);
            if (string.Equals(normalizedPath, ManifestEntryName, StringComparison.OrdinalIgnoreCase)
                || !manifestFiles.TryAdd(normalizedPath, file))
            {
                throw new InvalidDataException($"Backup manifest contains a duplicate entry path: {normalizedPath}");
            }

            if (file.SizeBytes < 0 || file.SizeBytes > _limits.MaximumEntrySizeBytes)
            {
                throw new InvalidDataException($"Backup entry has an unsupported size: {normalizedPath}");
            }

            totalSizeBytes = checked(totalSizeBytes + file.SizeBytes);
            if (totalSizeBytes > _limits.MaximumTotalSizeBytes)
            {
                throw new InvalidDataException("Backup exceeds the supported total size limit.");
            }

            if (!IsSha256(file.Sha256))
            {
                throw new InvalidDataException($"Backup entry has an invalid SHA-256: {normalizedPath}");
            }
        }

        var payloadEntries = normalizedEntries
            .Where(pair => !string.Equals(pair.Key, ManifestEntryName, StringComparison.OrdinalIgnoreCase))
            .ToArray();
        if (payloadEntries.Length != manifestFiles.Count)
        {
            throw new InvalidDataException("Backup payload membership does not match its manifest.");
        }

        var validatedFiles = new List<ValidatedBackupFile>(payloadEntries.Length);
        foreach (var (normalizedPath, entry) in payloadEntries)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!manifestFiles.TryGetValue(normalizedPath, out var manifestFile)
                || entry.Length != manifestFile.SizeBytes)
            {
                throw new InvalidDataException($"Backup entry does not match its manifest: {normalizedPath}");
            }

            await using var stream = entry.Open();
            var actualHash = Convert.ToHexString(await SHA256.HashDataAsync(stream, cancellationToken))
                .ToLowerInvariant();
            if (!string.Equals(actualHash, manifestFile.Sha256, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidDataException($"Backup entry does not match its manifest: {normalizedPath}");
            }

            var destinationPath = ResolveInsideRoot(targetRoot, normalizedPath);
            EnsureNoReparsePoints(targetRoot, destinationPath);
            if (!overwrite && File.Exists(destinationPath))
            {
                throw new IOException($"Restore target already exists: {destinationPath}");
            }

            validatedFiles.Add(new ValidatedBackupFile(entry, normalizedPath, destinationPath));
        }

        return validatedFiles;
    }

    private async Task<(long SizeBytes, string Sha256)> CopyAndHashAsync(
        Stream source,
        Stream destination,
        long maximumRemainingTotalBytes,
        CancellationToken cancellationToken)
    {
        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        var buffer = new byte[81920];
        long total = 0;
        int read;
        while ((read = await source.ReadAsync(buffer, cancellationToken)) > 0)
        {
            total = checked(total + read);
            if (total > _limits.MaximumEntrySizeBytes)
            {
                throw new InvalidOperationException("Backup file exceeds the supported size limit.");
            }

            if (total > maximumRemainingTotalBytes)
            {
                throw new InvalidOperationException(
                    $"Backup exceeds the supported total size limit of {_limits.MaximumTotalSizeBytes} bytes.");
            }

            hash.AppendData(buffer, 0, read);
            await destination.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
        }

        return (total, Convert.ToHexString(hash.GetHashAndReset()).ToLowerInvariant());
    }

    private static void RejectUnsupportedEntry(ZipArchiveEntry entry)
    {
        if (string.IsNullOrWhiteSpace(entry.FullName)
            || entry.FullName.EndsWith('/')
            || entry.FullName.EndsWith('\\'))
        {
            throw new InvalidDataException("Backup contains an unsupported directory entry.");
        }

        var unixFileType = (entry.ExternalAttributes >> 16) & 0xF000;
        if (unixFileType == 0xA000
            || ((FileAttributes)entry.ExternalAttributes).HasFlag(FileAttributes.ReparsePoint))
        {
            throw new InvalidDataException($"Backup contains an unsupported link entry: {entry.FullName}");
        }
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

        if (File.GetAttributes(fullPath).HasFlag(FileAttributes.ReparsePoint))
        {
            throw new InvalidOperationException($"Directory cannot be a link or reparse point: {fullPath}");
        }

        return fullPath;
    }

    private static string ResolveInsideRoot(string rootDirectory, string relativePath)
    {
        var destinationPath = Path.GetFullPath(
            Path.Combine(rootDirectory, relativePath.Replace('/', Path.DirectorySeparatorChar)));
        if (!IsInsideRoot(rootDirectory, destinationPath))
        {
            throw new InvalidOperationException($"Backup entry escapes target directory: {relativePath}");
        }

        return destinationPath;
    }

    private static bool IsInsideRoot(string rootDirectory, string path)
    {
        var rootWithSeparator = Path.GetFullPath(rootDirectory)
                .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
            + Path.DirectorySeparatorChar;
        return Path.GetFullPath(path).StartsWith(rootWithSeparator, StringComparison.OrdinalIgnoreCase);
    }

    private static void EnsureNoReparsePoints(string targetRoot, string destinationPath)
    {
        for (var current = new DirectoryInfo(targetRoot); current is not null; current = current.Parent)
        {
            if (current.Exists && current.Attributes.HasFlag(FileAttributes.ReparsePoint))
            {
                throw new InvalidOperationException(
                    $"Restore target cannot contain a link or reparse point: {current.FullName}");
            }
        }

        var relativeParent = Path.GetRelativePath(targetRoot, Path.GetDirectoryName(destinationPath)!);
        var currentPath = targetRoot;
        foreach (var part in relativeParent.Split(
                     Path.DirectorySeparatorChar,
                     Path.AltDirectorySeparatorChar,
                     StringSplitOptions.RemoveEmptyEntries))
        {
            if (part == ".")
            {
                continue;
            }

            currentPath = Path.Combine(currentPath, part);
            if (File.Exists(currentPath))
            {
                throw new IOException($"Restore directory path is occupied by a file: {currentPath}");
            }

            if (Directory.Exists(currentPath)
                && File.GetAttributes(currentPath).HasFlag(FileAttributes.ReparsePoint))
            {
                throw new InvalidOperationException(
                    $"Restore target cannot contain a link or reparse point: {currentPath}");
            }
        }
    }

    private static string NormalizeArchivePath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new InvalidDataException("Backup contains an empty entry path.");
        }

        var normalized = path.Replace('\\', '/');
        if (normalized.StartsWith('/')
            || Path.IsPathRooted(normalized)
            || normalized.Contains(':'))
        {
            throw new InvalidOperationException($"Backup entry escapes target directory: {path}");
        }

        var parts = normalized.Split('/');
        if (parts.Any(part => string.IsNullOrWhiteSpace(part) || part is "." or ".."))
        {
            throw new InvalidOperationException($"Backup entry escapes target directory: {path}");
        }

        return string.Join('/', parts);
    }

    private static bool IsSha256(string? value)
    {
        return value is { Length: 64 } && value.All(character =>
            character is >= '0' and <= '9'
            or >= 'a' and <= 'f'
            or >= 'A' and <= 'F');
    }

    private sealed record ValidatedBackupFile(
        ZipArchiveEntry Entry,
        string RelativePath,
        string DestinationPath);

    private sealed record CommittedRestoreFile(
        string DestinationPath,
        string? RollbackPath);

    private sealed class RestoreRollbackException(Exception commitException, Exception rollbackException)
        : IOException(
            "Restore commit failed and the attempted rollback was incomplete.",
            new AggregateException(commitException, rollbackException));
}

internal sealed record BackupManifest(
    int SchemaVersion,
    DateTimeOffset CreatedAt,
    IReadOnlyList<BackupManifestFile> Files,
    int SkippedFileCount);

internal sealed record BackupManifestFile(
    string Path,
    long SizeBytes,
    string Sha256);

internal sealed record LocalBackupRestoreLimits(
    int MaximumEntryCount = 10_000,
    long MaximumEntrySizeBytes = 512L * 1024 * 1024,
    long MaximumTotalSizeBytes = 4L * 1024 * 1024 * 1024,
    long MaximumManifestSizeBytes = 4L * 1024 * 1024)
{
    public void Validate()
    {
        if (MaximumEntryCount <= 0
            || MaximumEntrySizeBytes <= 0
            || MaximumTotalSizeBytes <= 0
            || MaximumManifestSizeBytes <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(LocalBackupRestoreLimits), "Backup limits must be positive.");
        }
    }
}
