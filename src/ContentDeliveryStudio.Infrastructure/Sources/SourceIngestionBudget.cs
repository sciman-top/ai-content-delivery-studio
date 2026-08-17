using System.IO.Compression;
using System.Xml.Linq;

namespace ContentDeliveryStudio.Infrastructure.Sources;

internal sealed class SourceIngestionBudget
{
    private const long Mebibyte = 1024L * 1024;

    public static SourceIngestionBudget Default { get; } = new();

    internal SourceIngestionBudget(
        long maxSourceFileBytes = 256 * Mebibyte,
        int maxPdfPageCount = 1_000,
        long maxDocxXmlBytes = 64 * Mebibyte,
        int maxXmlElementCount = 500_000,
        int maxExtractedTextCharacters = 10_000_000,
        int maxExtractedFigureCount = 1_000,
        long maxEncodedFigureBytes = 64 * Mebibyte,
        long maxTotalExtractedFigureBytes = 512 * Mebibyte,
        long maxFigurePixels = 40_000_000)
    {
        MaxSourceFileBytes = RequirePositive(maxSourceFileBytes, nameof(maxSourceFileBytes));
        MaxPdfPageCount = RequirePositive(maxPdfPageCount, nameof(maxPdfPageCount));
        MaxDocxXmlBytes = RequirePositive(maxDocxXmlBytes, nameof(maxDocxXmlBytes));
        MaxXmlElementCount = RequirePositive(maxXmlElementCount, nameof(maxXmlElementCount));
        MaxExtractedTextCharacters = RequirePositive(maxExtractedTextCharacters, nameof(maxExtractedTextCharacters));
        MaxExtractedFigureCount = RequirePositive(maxExtractedFigureCount, nameof(maxExtractedFigureCount));
        MaxEncodedFigureBytes = RequirePositive(maxEncodedFigureBytes, nameof(maxEncodedFigureBytes));
        MaxTotalExtractedFigureBytes = RequirePositive(maxTotalExtractedFigureBytes, nameof(maxTotalExtractedFigureBytes));
        MaxFigurePixels = RequirePositive(maxFigurePixels, nameof(maxFigurePixels));
    }

    private long MaxSourceFileBytes { get; }

    private int MaxPdfPageCount { get; }

    private long MaxDocxXmlBytes { get; }

    private int MaxXmlElementCount { get; }

    private int MaxExtractedTextCharacters { get; }

    private int MaxExtractedFigureCount { get; }

    private long MaxEncodedFigureBytes { get; }

    private long MaxTotalExtractedFigureBytes { get; }

    private long MaxFigurePixels { get; }

    public void ValidateSourceFile(string path)
    {
        var length = new FileInfo(path).Length;
        if (length > MaxSourceFileBytes)
        {
            throw new InvalidDataException(
                $"Source file exceeds the supported size limit of {MaxSourceFileBytes} bytes.");
        }
    }

    public void ValidatePdfPageCount(int pageCount)
    {
        if (pageCount > MaxPdfPageCount)
        {
            throw new InvalidDataException(
                $"PDF exceeds the supported page limit of {MaxPdfPageCount}.");
        }
    }

    public int AddExtractedText(int currentCharacterCount, int addedCharacterCount, int separatorCount = 0)
    {
        var next = checked((long)currentCharacterCount + addedCharacterCount + separatorCount);
        if (next > MaxExtractedTextCharacters)
        {
            throw new InvalidDataException(
                $"Extracted source text exceeds the supported limit of {MaxExtractedTextCharacters} characters.");
        }

        return checked((int)next);
    }

    public async Task<XDocument> LoadDocxBodyAsync(
        ZipArchiveEntry entry,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(entry);
        if (entry.Length > MaxDocxXmlBytes)
        {
            throw new InvalidDataException(
                $"DOCX body XML exceeds the supported size limit of {MaxDocxXmlBytes} bytes.");
        }

        await using var source = entry.Open();
        using var buffer = new MemoryStream();
        var chunk = new byte[81920];
        long totalBytes = 0;
        while (true)
        {
            var read = await source.ReadAsync(chunk, cancellationToken);
            if (read == 0)
            {
                break;
            }

            totalBytes = checked(totalBytes + read);
            if (totalBytes > MaxDocxXmlBytes)
            {
                throw new InvalidDataException(
                    $"DOCX body XML exceeds the supported size limit of {MaxDocxXmlBytes} bytes.");
            }

            await buffer.WriteAsync(chunk.AsMemory(0, read), cancellationToken);
        }

        buffer.Position = 0;
        var document = await XDocument.LoadAsync(buffer, LoadOptions.None, cancellationToken);
        if (document.Descendants().Take(MaxXmlElementCount + 1).Count() > MaxXmlElementCount)
        {
            throw new InvalidDataException(
                $"DOCX body XML exceeds the supported element limit of {MaxXmlElementCount}.");
        }

        return document;
    }

    public void ValidateFigureCandidate(int width, int height, int encodedByteCount)
    {
        var pixels = checked((long)width * height);
        if (pixels > MaxFigurePixels)
        {
            throw new InvalidDataException(
                $"Source figure exceeds the supported pixel limit of {MaxFigurePixels}.");
        }

        if (encodedByteCount > MaxEncodedFigureBytes)
        {
            throw new InvalidDataException(
                $"Source figure exceeds the supported encoded size limit of {MaxEncodedFigureBytes} bytes.");
        }
    }

    public (int Count, long TotalBytes) AddExtractedFigure(
        int currentCount,
        long currentTotalBytes,
        int addedBytes)
    {
        var nextCount = checked(currentCount + 1);
        if (nextCount > MaxExtractedFigureCount)
        {
            throw new InvalidDataException(
                $"Source figures exceed the supported count limit of {MaxExtractedFigureCount}.");
        }

        if (addedBytes > MaxEncodedFigureBytes)
        {
            throw new InvalidDataException(
                $"Extracted source figure exceeds the supported size limit of {MaxEncodedFigureBytes} bytes.");
        }

        var nextTotalBytes = checked(currentTotalBytes + addedBytes);
        if (nextTotalBytes > MaxTotalExtractedFigureBytes)
        {
            throw new InvalidDataException(
                $"Extracted source figures exceed the supported total size limit of {MaxTotalExtractedFigureBytes} bytes.");
        }

        return (nextCount, nextTotalBytes);
    }

    private static int RequirePositive(int value, string parameterName)
    {
        return value > 0
            ? value
            : throw new ArgumentOutOfRangeException(parameterName, value, "Source ingestion limits must be positive.");
    }

    private static long RequirePositive(long value, string parameterName)
    {
        return value > 0
            ? value
            : throw new ArgumentOutOfRangeException(parameterName, value, "Source ingestion limits must be positive.");
    }
}
