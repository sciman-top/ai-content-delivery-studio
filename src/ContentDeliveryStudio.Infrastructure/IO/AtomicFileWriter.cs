using System.Text;

namespace ContentDeliveryStudio.Infrastructure.IO;

public static class AtomicFileWriter
{
    public static async Task WriteAllTextAsync(
        string path,
        string contents,
        CancellationToken cancellationToken,
        Encoding? encoding = null)
    {
        var selectedEncoding = encoding ?? Encoding.UTF8;
        var bytes = selectedEncoding.GetBytes(contents);
        await WriteAllBytesAsync(path, bytes, cancellationToken);
    }

    public static async Task WriteAllBytesAsync(
        string path,
        byte[] contents,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(contents);

        await WriteAtomicAsync(
            path,
            async temporaryPath =>
            {
                await File.WriteAllBytesAsync(temporaryPath, contents, cancellationToken);
            },
            cancellationToken);
    }

    public static void WriteAllBytes(
        string path,
        byte[] contents)
    {
        ArgumentNullException.ThrowIfNull(contents);

        WriteAtomic(
            path,
            temporaryPath => File.WriteAllBytes(temporaryPath, contents));
    }

    public static async Task CopyFileAsync(
        string sourcePath,
        string destinationPath,
        CancellationToken cancellationToken)
    {
        var normalizedSourcePath = NormalizeExistingSourcePath(sourcePath);
        await WriteAtomicAsync(
            destinationPath,
            async temporaryPath =>
            {
                await using var sourceStream = new FileStream(
                    normalizedSourcePath,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.Read,
                    bufferSize: 81920,
                    FileOptions.Asynchronous | FileOptions.SequentialScan);
                await using var destinationStream = new FileStream(
                    temporaryPath,
                    FileMode.Create,
                    FileAccess.Write,
                    FileShare.None,
                    bufferSize: 81920,
                    FileOptions.Asynchronous);
                await sourceStream.CopyToAsync(destinationStream, cancellationToken);
            },
            cancellationToken);
    }

    private static async Task WriteAtomicAsync(
        string path,
        Func<string, Task> writeTemporaryAsync,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(writeTemporaryAsync);

        var normalizedPath = NormalizeDestinationPath(path);
        var directoryPath = Path.GetDirectoryName(normalizedPath)
            ?? throw new InvalidOperationException($"Could not resolve the directory for path '{path}'.");
        Directory.CreateDirectory(directoryPath);

        var temporaryPath = CreateTemporaryPath(normalizedPath);
        try
        {
            await writeTemporaryAsync(temporaryPath);
            cancellationToken.ThrowIfCancellationRequested();
            File.Move(temporaryPath, normalizedPath, overwrite: true);
        }
        finally
        {
            TryDeleteTemporaryFile(temporaryPath);
        }
    }

    private static void WriteAtomic(
        string path,
        Action<string> writeTemporary)
    {
        ArgumentNullException.ThrowIfNull(writeTemporary);

        var normalizedPath = NormalizeDestinationPath(path);
        var directoryPath = Path.GetDirectoryName(normalizedPath)
            ?? throw new InvalidOperationException($"Could not resolve the directory for path '{path}'.");
        Directory.CreateDirectory(directoryPath);

        var temporaryPath = CreateTemporaryPath(normalizedPath);
        try
        {
            writeTemporary(temporaryPath);
            File.Move(temporaryPath, normalizedPath, overwrite: true);
        }
        finally
        {
            TryDeleteTemporaryFile(temporaryPath);
        }
    }

    private static string NormalizeDestinationPath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException("Destination path cannot be empty.", nameof(path));
        }

        return Path.GetFullPath(path.Trim());
    }

    private static string NormalizeExistingSourcePath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException("Source path cannot be empty.", nameof(path));
        }

        var normalizedPath = Path.GetFullPath(path.Trim());
        if (!File.Exists(normalizedPath))
        {
            throw new FileNotFoundException("Source file was not found.", normalizedPath);
        }

        return normalizedPath;
    }

    private static string CreateTemporaryPath(string destinationPath)
    {
        var directoryPath = Path.GetDirectoryName(destinationPath)
            ?? throw new InvalidOperationException($"Could not resolve the directory for path '{destinationPath}'.");
        var fileName = Path.GetFileName(destinationPath);
        return Path.Combine(directoryPath, $".{fileName}.{Guid.NewGuid():N}.tmp");
    }

    private static void TryDeleteTemporaryFile(string temporaryPath)
    {
        try
        {
            if (File.Exists(temporaryPath))
            {
                File.Delete(temporaryPath);
            }
        }
        catch
        {
            // Best-effort cleanup only. The original failure should remain the visible outcome.
        }
    }
}
