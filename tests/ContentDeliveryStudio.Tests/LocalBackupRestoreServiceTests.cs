using System.IO.Compression;
using System.Security.Cryptography;
using System.Text.Json;
using ContentDeliveryStudio.Infrastructure.Backups;

namespace ContentDeliveryStudio.Tests;

public sealed class LocalBackupRestoreServiceTests
{
    [Fact]
    public async Task CreateAndRestoreBackupAsync_UsesSafeDefaults()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), $"image-series-backup-{Guid.NewGuid():N}");
        var source = Path.Combine(tempRoot, "source");
        var restored = Path.Combine(tempRoot, "restored");
        var backupPath = Path.Combine(tempRoot, "backup.zip");

        try
        {
            Directory.CreateDirectory(source);
            Directory.CreateDirectory(Path.Combine(source, "workspace"));
            Directory.CreateDirectory(Path.Combine(source, "outputs"));
            await File.WriteAllTextAsync(Path.Combine(source, "project.json"), "project");
            await File.WriteAllTextAsync(Path.Combine(source, ".env"), "OPENAI_API_KEY=test-openai-key");
            await File.WriteAllTextAsync(Path.Combine(source, "studio.sqlite"), "sqlite");
            await File.WriteAllTextAsync(Path.Combine(source, "workspace", "local.txt"), "local");
            await File.WriteAllTextAsync(Path.Combine(source, "outputs", "image.png"), "image");

            var service = new LocalBackupRestoreService();
            var backup = await service.CreateBackupAsync(
                new(source, backupPath),
                CancellationToken.None);
            var restore = await service.RestoreBackupAsync(
                new(backupPath, restored),
                CancellationToken.None);

            Assert.Equal(1, backup.IncludedFileCount);
            Assert.Equal(4, backup.SkippedFileCount);
            Assert.Equal("backup-manifest.json", backup.ManifestEntryName);
            Assert.True(File.Exists(Path.Combine(restored, "project.json")));
            Assert.False(File.Exists(Path.Combine(restored, ".env")));
            Assert.False(File.Exists(Path.Combine(restored, "studio.sqlite")));
            Assert.False(Directory.Exists(Path.Combine(restored, "workspace")));
            Assert.False(Directory.Exists(Path.Combine(restored, "outputs")));
            Assert.Equal(1, restore.RestoredFileCount);

            using var archive = ZipFile.OpenRead(backupPath);
            var manifestEntry = Assert.Single(
                archive.Entries,
                entry => entry.FullName == backup.ManifestEntryName);
            using var manifestStream = manifestEntry.Open();
            using var manifest = await JsonDocument.ParseAsync(manifestStream);
            Assert.Equal(1, manifest.RootElement.GetProperty("schemaVersion").GetInt32());
            var file = Assert.Single(manifest.RootElement.GetProperty("files").EnumerateArray());
            Assert.Equal("project.json", file.GetProperty("path").GetString());
            Assert.Equal(
                Convert.ToHexString(SHA256.HashData("project"u8.ToArray())).ToLowerInvariant(),
                file.GetProperty("sha256").GetString());
        }
        finally
        {
            if (Directory.Exists(tempRoot))
            {
                Directory.Delete(tempRoot, recursive: true);
            }
        }
    }

    [Fact]
    public async Task RestoreBackupAsync_BlocksEntriesOutsideTargetDirectory()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), $"image-series-backup-slip-{Guid.NewGuid():N}");
        var backupPath = Path.Combine(tempRoot, "bad.zip");
        var restored = Path.Combine(tempRoot, "restored");

        try
        {
            Directory.CreateDirectory(tempRoot);
            using (var archive = ZipFile.Open(backupPath, ZipArchiveMode.Create))
            {
                var entry = archive.CreateEntry("../escape.txt");
                await using var stream = entry.Open();
                await using var writer = new StreamWriter(stream);
                await writer.WriteAsync("escape");
            }

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                new LocalBackupRestoreService().RestoreBackupAsync(
                    new(backupPath, restored),
                    CancellationToken.None));

            Assert.Contains("escapes target directory", exception.Message);
            Assert.False(File.Exists(Path.Combine(tempRoot, "escape.txt")));
            Assert.False(Directory.Exists(restored));
        }
        finally
        {
            if (Directory.Exists(tempRoot))
            {
                Directory.Delete(tempRoot, recursive: true);
            }
        }
    }

    [Fact]
    public async Task RestoreBackupAsync_RejectsTamperedPayloadBeforeWritingTarget()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), $"content-delivery-backup-tamper-{Guid.NewGuid():N}");
        var source = Path.Combine(tempRoot, "source");
        var backupPath = Path.Combine(tempRoot, "backup.zip");
        var restored = Path.Combine(tempRoot, "restored");

        try
        {
            Directory.CreateDirectory(source);
            await File.WriteAllTextAsync(Path.Combine(source, "first.txt"), "first");
            await File.WriteAllTextAsync(Path.Combine(source, "second.txt"), "second");
            var service = new LocalBackupRestoreService();
            await service.CreateBackupAsync(new(source, backupPath), CancellationToken.None);

            using (var archive = ZipFile.Open(backupPath, ZipArchiveMode.Update))
            {
                var entry = archive.GetEntry("second.txt")!;
                entry.Delete();
                entry = archive.CreateEntry("second.txt");
                await using var stream = entry.Open();
                await stream.WriteAsync("tampered"u8.ToArray());
            }

            var exception = await Assert.ThrowsAsync<InvalidDataException>(() =>
                service.RestoreBackupAsync(new(backupPath, restored), CancellationToken.None));

            Assert.Contains("does not match its manifest", exception.Message);
            Assert.False(Directory.Exists(restored));
        }
        finally
        {
            if (Directory.Exists(tempRoot))
            {
                Directory.Delete(tempRoot, recursive: true);
            }
        }
    }

    [Fact]
    public async Task RestoreBackupAsync_RequiresExactlyOneManifestAndUniquePayloadPaths()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), $"content-delivery-backup-structure-{Guid.NewGuid():N}");
        var missingManifestPath = Path.Combine(tempRoot, "missing-manifest.zip");
        var duplicatePath = Path.Combine(tempRoot, "duplicate.zip");

        try
        {
            Directory.CreateDirectory(tempRoot);
            using (var archive = ZipFile.Open(missingManifestPath, ZipArchiveMode.Create))
            {
                await WriteEntryAsync(archive, "file.txt", "content");
            }

            var service = new LocalBackupRestoreService();
            var missingException = await Assert.ThrowsAsync<InvalidDataException>(() =>
                service.RestoreBackupAsync(
                    new(missingManifestPath, Path.Combine(tempRoot, "missing-target")),
                    CancellationToken.None));
            Assert.Contains("exactly one backup-manifest.json", missingException.Message);

            using (var archive = ZipFile.Open(duplicatePath, ZipArchiveMode.Create))
            {
                await WriteEntryAsync(archive, "file.txt", "content");
                await WriteEntryAsync(archive, "FILE.txt", "content");
                await WriteEntryAsync(
                    archive,
                    "backup-manifest.json",
                    BuildManifest(("file.txt", "content"), ("FILE.txt", "content")));
            }

            var duplicateException = await Assert.ThrowsAsync<InvalidDataException>(() =>
                service.RestoreBackupAsync(
                    new(duplicatePath, Path.Combine(tempRoot, "duplicate-target")),
                    CancellationToken.None));
            Assert.Contains("duplicate entry path", duplicateException.Message);
            Assert.False(Directory.Exists(Path.Combine(tempRoot, "duplicate-target")));
        }
        finally
        {
            if (Directory.Exists(tempRoot))
            {
                Directory.Delete(tempRoot, recursive: true);
            }
        }
    }

    [Fact]
    public async Task RestoreBackupAsync_PreflightsAllTargetConflictsBeforeWriting()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), $"content-delivery-backup-conflict-{Guid.NewGuid():N}");
        var source = Path.Combine(tempRoot, "source");
        var restored = Path.Combine(tempRoot, "restored");
        var backupPath = Path.Combine(tempRoot, "backup.zip");

        try
        {
            Directory.CreateDirectory(source);
            Directory.CreateDirectory(restored);
            await File.WriteAllTextAsync(Path.Combine(source, "first.txt"), "first");
            await File.WriteAllTextAsync(Path.Combine(source, "second.txt"), "second");
            await File.WriteAllTextAsync(Path.Combine(restored, "second.txt"), "keep");

            var service = new LocalBackupRestoreService();
            await service.CreateBackupAsync(new(source, backupPath), CancellationToken.None);

            await Assert.ThrowsAsync<IOException>(() =>
                service.RestoreBackupAsync(new(backupPath, restored), CancellationToken.None));

            Assert.False(File.Exists(Path.Combine(restored, "first.txt")));
            Assert.Equal("keep", await File.ReadAllTextAsync(Path.Combine(restored, "second.txt")));
        }
        finally
        {
            if (Directory.Exists(tempRoot))
            {
                Directory.Delete(tempRoot, recursive: true);
            }
        }
    }

    private static async Task WriteEntryAsync(ZipArchive archive, string path, string content)
    {
        var entry = archive.CreateEntry(path);
        await using var stream = entry.Open();
        await using var writer = new StreamWriter(stream);
        await writer.WriteAsync(content);
    }

    private static string BuildManifest(params (string Path, string Content)[] files)
    {
        return JsonSerializer.Serialize(new
        {
            schemaVersion = 1,
            createdAt = DateTimeOffset.UtcNow,
            files = files.Select(file => new
            {
                path = file.Path,
                sizeBytes = System.Text.Encoding.UTF8.GetByteCount(file.Content),
                sha256 = Convert.ToHexString(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(file.Content)))
                    .ToLowerInvariant(),
            }),
            skippedFileCount = 0,
        });
    }
}
