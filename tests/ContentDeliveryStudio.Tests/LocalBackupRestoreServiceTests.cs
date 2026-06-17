using System.IO.Compression;
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
            Assert.True(File.Exists(Path.Combine(restored, "project.json")));
            Assert.False(File.Exists(Path.Combine(restored, ".env")));
            Assert.False(File.Exists(Path.Combine(restored, "studio.sqlite")));
            Assert.False(Directory.Exists(Path.Combine(restored, "workspace")));
            Assert.False(Directory.Exists(Path.Combine(restored, "outputs")));
            Assert.Equal(1, restore.RestoredFileCount);
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
        }
        finally
        {
            if (Directory.Exists(tempRoot))
            {
                Directory.Delete(tempRoot, recursive: true);
            }
        }
    }
}
