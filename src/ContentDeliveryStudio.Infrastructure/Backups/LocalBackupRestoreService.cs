using System.IO.Compression;
using System.Security.Cryptography;
using System.Text.Json;
using ContentDeliveryStudio.Application.Backups;
using Microsoft.Data.Sqlite;

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
        var databaseSnapshots = new List<BackupManifestDatabase>();
        var skippedFileCount = 0;
        long includedSizeBytes = 0;
        var databaseFiles = new List<string>();
        var tempBackupPath = Path.Combine(
            backupDirectory,
            $".{Path.GetFileName(backupFilePath)}.{Guid.NewGuid():N}.tmp");
        var tempSnapshotDirectory = Path.Combine(
            backupDirectory,
            $".{Path.GetFileName(backupFilePath)}.{Guid.NewGuid():N}.db-snapshots");

        try
        {
            Directory.CreateDirectory(tempSnapshotDirectory);
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

                    // Databases never go through the plain-copy lane regardless
                    // of options: copying an active SQLite file (or its WAL
                    // sidecar) is not a consistent snapshot. They are routed to
                    // the VACUUM INTO snapshot lane after the file loop; the
                    // snapshot already contains the committed WAL content.
                    if (IsDatabaseFile(filePath))
                    {
                        databaseFiles.Add(filePath);
                        continue;
                    }

                    if (IsDatabaseSidecarFile(filePath))
                    {
                        skippedFileCount++;
                        continue;
                    }

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

                includedSizeBytes = await SnapshotDatabasesIntoArchiveAsync(
                    archive,
                    sourceRoot,
                    databaseFiles,
                    tempSnapshotDirectory,
                    includedFiles,
                    databaseSnapshots,
                    includedSizeBytes,
                    _limits,
                    cancellationToken);

                var manifest = new BackupManifest(
                    ManifestSchemaVersion,
                    DateTimeOffset.UtcNow,
                    includedFiles,
                    skippedFileCount,
                    databaseSnapshots.Count > 0 ? databaseSnapshots : null);

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

            if (Directory.Exists(tempSnapshotDirectory))
            {
                try
                {
                    Directory.Delete(tempSnapshotDirectory, recursive: true);
                }
                catch (IOException)
                {
                    // Best-effort cleanup only; the backup outcome above is the
                    // visible result and a leftover hidden temp dir is inert.
                }
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

        // A previous restore may have died mid-commit (for example on power loss),
        // leaving original files stranded inside a hidden transaction directory.
        // Repair those before staging anything new for the same target.
        RecoverInterruptedRestoreTransactions(request.TargetDirectory, cancellationToken);

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
            WriteTransactionJournal(transactionRoot, targetRoot);
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

    /// <summary>
    /// Repairs restore transactions for this target that were interrupted by a
    /// process crash or power loss: files stranded in a transaction's rollback
    /// area are moved back to the target (unless the target already has newer
    /// content), and the leftover hidden transaction directories are removed.
    /// </summary>
    public static InterruptedRestoreRecovery RecoverInterruptedRestoreTransactions(
        string targetDirectory,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(targetDirectory);

        cancellationToken.ThrowIfCancellationRequested();
        var trimmedTargetRoot = Path.GetFullPath(targetDirectory)
            .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var targetParent = Path.GetDirectoryName(trimmedTargetRoot)
            ?? throw new InvalidOperationException("Restore target cannot be a volume root.");
        var targetName = Path.GetFileName(trimmedTargetRoot);
        Directory.CreateDirectory(targetParent);

        var recoveredFiles = new List<string>();
        var cleanedUpTransactions = 0;
        foreach (var transactionDirectory in Directory
                     .EnumerateDirectories(targetParent, $".{targetName}.restore-*")
                     .OrderBy(path => path, StringComparer.OrdinalIgnoreCase))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var journal = TryReadTransactionJournal(transactionDirectory);
            if (journal is not null
                && !string.Equals(
                    journal.TargetRoot,
                    trimmedTargetRoot,
                    StringComparison.OrdinalIgnoreCase))
            {
                // The journal records a different target than the transaction
                // directory's location implies; never replay files elsewhere.
                throw new IOException(
                    $"Interrupted restore transaction {transactionDirectory} targets {journal.TargetRoot}, which does not match {trimmedTargetRoot}. Resolve it manually.");
            }

            var rollbackRoot = Path.Combine(transactionDirectory, "rollback");
            if (Directory.Exists(rollbackRoot))
            {
                foreach (var rollbackFile in Directory
                             .EnumerateFiles(rollbackRoot, "*", SearchOption.AllDirectories)
                             .OrderBy(path => path, StringComparer.OrdinalIgnoreCase))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var relativePath = Path.GetRelativePath(rollbackRoot, rollbackFile);
                    var destinationPath = Path.Combine(trimmedTargetRoot, relativePath);
                    Directory.CreateDirectory(Path.GetDirectoryName(destinationPath)!);
                    if (File.Exists(destinationPath))
                    {
                        // The target already has content at this path (from a
                        // later restore or the user); keep it, drop the stale copy.
                        File.Delete(rollbackFile);
                    }
                    else
                    {
                        File.Move(rollbackFile, destinationPath);
                        recoveredFiles.Add(relativePath.Replace('\\', '/'));
                    }
                }
            }

            TryDeleteDirectory(transactionDirectory);
            cleanedUpTransactions++;
        }

        return new InterruptedRestoreRecovery(trimmedTargetRoot, recoveredFiles, cleanedUpTransactions);
    }

    private static void WriteTransactionJournal(string transactionRoot, string targetRoot)
    {
        var journalPath = Path.Combine(transactionRoot, "transaction.json");
        File.WriteAllText(
            journalPath,
            JsonSerializer.Serialize(
                new TransactionJournal(TargetRoot: targetRoot, CreatedAtUtc: DateTimeOffset.UtcNow),
                JsonOptions));
    }

    private static TransactionJournal? TryReadTransactionJournal(string transactionRoot)
    {
        try
        {
            var journalPath = Path.Combine(transactionRoot, "transaction.json");
            return File.Exists(journalPath)
                ? JsonSerializer.Deserialize<TransactionJournal>(File.ReadAllText(journalPath))
                : null;
        }
        catch (Exception exception) when (exception is IOException or JsonException or UnauthorizedAccessException)
        {
            return null;
        }
    }

    private sealed record TransactionJournal(string TargetRoot, DateTimeOffset CreatedAtUtc);

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

    private static readonly string[] DatabaseExtensions = [".db", ".sqlite", ".sqlite3"];

    private static bool IsDatabaseFile(string filePath)
    {
        return DatabaseExtensions.Contains(
            Path.GetExtension(filePath),
            StringComparer.OrdinalIgnoreCase);
    }

    private static bool IsDatabaseSidecarFile(string filePath)
    {
        var fileName = Path.GetFileName(filePath);
        return fileName.EndsWith("-wal", StringComparison.OrdinalIgnoreCase)
            || fileName.EndsWith("-shm", StringComparison.OrdinalIgnoreCase)
            || fileName.EndsWith("-journal", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Snapshots every database found under the source directory into the
    /// archive with SQLite's VACUUM INTO, which produces a consistent point-in
    /// -time copy even while writers are active. Any snapshot failure fails the
    /// whole backup closed: a ZIP without the databases it was expected to
    /// carry must never look like a successful disaster-recovery artifact.
    /// </summary>
    private static async Task<long> SnapshotDatabasesIntoArchiveAsync(
        ZipArchive archive,
        string sourceRoot,
        IReadOnlyList<string> databaseFiles,
        string tempSnapshotDirectory,
        List<BackupManifestFile> includedFiles,
        List<BackupManifestDatabase> databaseSnapshots,
        long includedSizeBytes,
        LocalBackupRestoreLimits limits,
        CancellationToken cancellationToken)
    {
        foreach (var databasePath in databaseFiles
                     .OrderBy(path => path, StringComparer.OrdinalIgnoreCase))
        {
            cancellationToken.ThrowIfCancellationRequested();

            var relativePath = NormalizeArchivePath(Path.GetRelativePath(sourceRoot, databasePath));
            var snapshotPath = Path.Combine(
                tempSnapshotDirectory,
                $"{Guid.NewGuid():N}{Path.GetExtension(databasePath)}");
            SnapshotDatabaseWithVacuumInto(databasePath, snapshotPath);
            try
            {
                var snapshotInfo = new FileInfo(snapshotPath);
                if (snapshotInfo.Length > limits.MaximumEntrySizeBytes)
                {
                    throw new InvalidOperationException(
                        $"Database snapshot exceeds the supported size limit: {relativePath}");
                }

                var remainingTotalBytes = checked(limits.MaximumTotalSizeBytes - includedSizeBytes);
                if (snapshotInfo.Length > remainingTotalBytes)
                {
                    throw new InvalidOperationException(
                        $"Backup exceeds the supported total size limit of {limits.MaximumTotalSizeBytes} bytes.");
                }

                await using var source = File.OpenRead(snapshotPath);
                var entry = archive.CreateEntry(relativePath, CompressionLevel.Optimal);
                await using var destination = entry.Open();
                var (sizeBytes, sha256) = await CopySnapshotAndHashAsync(
                    source,
                    destination,
                    remainingTotalBytes,
                    limits,
                    cancellationToken);
                includedSizeBytes = checked(includedSizeBytes + sizeBytes);
                includedFiles.Add(new BackupManifestFile(relativePath, sizeBytes, sha256));
                databaseSnapshots.Add(new BackupManifestDatabase(relativePath, sizeBytes, sha256));
            }
            finally
            {
                if (File.Exists(snapshotPath))
                {
                    File.Delete(snapshotPath);
                }
            }
        }

        return includedSizeBytes;
    }

    private static void SnapshotDatabaseWithVacuumInto(string databasePath, string snapshotPath)
    {
        // Read-write mode is required even though VACUUM INTO never modifies
        // the source: when no other connection holds the database open, WAL
        // recovery on open needs write access to the -shm sidecar, which a
        // read-only connection cannot take. Pooling is disabled so the file
        // handle is released as soon as the snapshot completes.
        using var connection = new SqliteConnection(new SqliteConnectionStringBuilder
        {
            DataSource = databasePath,
            Mode = SqliteOpenMode.ReadWrite,
            Pooling = false,
        }.ToString());
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandTimeout = 30;
        command.CommandText = "VACUUM INTO @snapshotPath";
        command.Parameters.AddWithValue("@snapshotPath", snapshotPath);
        command.ExecuteNonQuery();
    }

    private static async Task<(long SizeBytes, string Sha256)> CopySnapshotAndHashAsync(
        Stream source,
        Stream destination,
        long maximumRemainingTotalBytes,
        LocalBackupRestoreLimits limits,
        CancellationToken cancellationToken)
    {
        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        var buffer = new byte[81920];
        long total = 0;
        int read;
        while ((read = await source.ReadAsync(buffer, cancellationToken)) > 0)
        {
            total = checked(total + read);
            if (total > limits.MaximumEntrySizeBytes)
            {
                throw new InvalidOperationException("Database snapshot exceeds the supported size limit.");
            }

            if (total > maximumRemainingTotalBytes)
            {
                throw new InvalidOperationException(
                    $"Backup exceeds the supported total size limit of {limits.MaximumTotalSizeBytes} bytes.");
            }

            hash.AppendData(buffer, 0, read);
            await destination.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
        }

        return (total, Convert.ToHexString(hash.GetHashAndReset()).ToLowerInvariant());
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

        if (manifest.Databases is not null)
        {
            foreach (var database in manifest.Databases)
            {
                var normalizedPath = NormalizeArchivePath(database.Path);
                if (!manifestFiles.TryGetValue(normalizedPath, out var file)
                    || file.SizeBytes != database.SizeBytes
                    || !string.Equals(file.Sha256, database.Sha256, StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidDataException(
                        $"Backup manifest database entry does not match its file entry: {normalizedPath}");
                }
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
    int SkippedFileCount,
    IReadOnlyList<BackupManifestDatabase>? Databases = null);

internal sealed record BackupManifestFile(
    string Path,
    long SizeBytes,
    string Sha256);

/// <summary>Labels the manifest entries that are consistent database
/// snapshots (VACUUM INTO), so restores and tooling can tell them apart from
/// plain copied files. Every database entry also appears in
/// <see cref="BackupManifest.Files"/>.</summary>
internal sealed record BackupManifestDatabase(
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

/// <summary>Outcome of repairing interrupted restore transactions for one target.</summary>
public sealed record InterruptedRestoreRecovery(
    string TargetDirectory,
    IReadOnlyList<string> RecoveredFiles,
    int CleanedUpTransactions);
