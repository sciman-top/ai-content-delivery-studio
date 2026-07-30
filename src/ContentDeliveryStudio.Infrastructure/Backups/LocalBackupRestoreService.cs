using System.IO.Compression;
using System.Security.Cryptography;
using System.Text.Json;
using ContentDeliveryStudio.Application.Backups;

namespace ContentDeliveryStudio.Infrastructure.Backups;

public sealed class LocalBackupRestoreService : IBackupRestoreService
{
    private const int ManifestSchemaVersion = 1;
    private const string ManifestEntryName = "backup-manifest.json";
    private const int MaximumEntryCount = 10_000;
    private const long MaximumEntrySizeBytes = 512L * 1024 * 1024;
    private const long MaximumTotalSizeBytes = 4L * 1024 * 1024 * 1024;
    private const long MaximumManifestSizeBytes = 4L * 1024 * 1024;

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

        if (IsInsideRoot(sourceRoot, backupFilePath))
        {
            throw new ArgumentException("Backup file path must be outside the source directory.", nameof(request));
        }

        Directory.CreateDirectory(backupDirectory);

        var options = request.Options ?? BackupOptions.SafeDefaults;
        var includedFiles = new List<BackupManifestFile>();
        var skippedFileCount = 0;
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

                    if (includedFiles.Count >= MaximumEntryCount)
                    {
                        throw new InvalidOperationException(
                            $"Backup exceeds the supported file limit of {MaximumEntryCount}.");
                    }

                    var relativePath = NormalizeArchivePath(Path.GetRelativePath(sourceRoot, filePath));
                    var entry = archive.CreateEntry(relativePath, CompressionLevel.Optimal);
                    await using var source = File.OpenRead(filePath);
                    if (source.Length > MaximumEntrySizeBytes)
                    {
                        throw new InvalidOperationException(
                            $"Backup file exceeds the supported size limit: {relativePath}");
                    }

                    await using var destination = entry.Open();
                    var (sizeBytes, sha256) = await CopyAndHashAsync(source, destination, cancellationToken);
                    includedFiles.Add(new BackupManifestFile(relativePath, sizeBytes, sha256));
                }

                if (includedFiles.Sum(file => file.SizeBytes) > MaximumTotalSizeBytes)
                {
                    throw new InvalidOperationException(
                        $"Backup exceeds the supported total size limit of {MaximumTotalSizeBytes} bytes.");
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
        using var archive = ZipFile.OpenRead(backupFilePath);
        var validatedFiles = await ValidateArchiveAsync(
            archive,
            targetRoot,
            request.Overwrite,
            cancellationToken);

        Directory.CreateDirectory(targetRoot);
        foreach (var validatedFile in validatedFiles)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Directory.CreateDirectory(Path.GetDirectoryName(validatedFile.DestinationPath)!);

            await using var source = validatedFile.Entry.Open();
            await using var destination = new FileStream(
                validatedFile.DestinationPath,
                request.Overwrite ? FileMode.Create : FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None);
            await source.CopyToAsync(destination, cancellationToken);
        }

        return new RestoreResult(targetRoot, validatedFiles.Count);
    }

    private static async Task<IReadOnlyList<ValidatedBackupFile>> ValidateArchiveAsync(
        ZipArchive archive,
        string targetRoot,
        bool overwrite,
        CancellationToken cancellationToken)
    {
        if (archive.Entries.Count > MaximumEntryCount + 1)
        {
            throw new InvalidDataException($"Backup exceeds the supported entry limit of {MaximumEntryCount}.");
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
        if (manifestEntry.Length > MaximumManifestSizeBytes)
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

        if (manifest.Files is null || manifest.Files.Count > MaximumEntryCount)
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

            if (file.SizeBytes < 0 || file.SizeBytes > MaximumEntrySizeBytes)
            {
                throw new InvalidDataException($"Backup entry has an unsupported size: {normalizedPath}");
            }

            totalSizeBytes = checked(totalSizeBytes + file.SizeBytes);
            if (totalSizeBytes > MaximumTotalSizeBytes)
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

            validatedFiles.Add(new ValidatedBackupFile(entry, destinationPath));
        }

        return validatedFiles;
    }

    private static async Task<(long SizeBytes, string Sha256)> CopyAndHashAsync(
        Stream source,
        Stream destination,
        CancellationToken cancellationToken)
    {
        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        var buffer = new byte[81920];
        long total = 0;
        int read;
        while ((read = await source.ReadAsync(buffer, cancellationToken)) > 0)
        {
            total = checked(total + read);
            if (total > MaximumEntrySizeBytes)
            {
                throw new InvalidOperationException("Backup file exceeds the supported size limit.");
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
        string DestinationPath);
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
