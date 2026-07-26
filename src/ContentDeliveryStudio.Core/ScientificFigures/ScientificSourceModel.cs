namespace ContentDeliveryStudio.Core.ScientificFigures;

public sealed record ScientificCharacterRange
{
    private ScientificCharacterRange(int startOffset, int endOffset)
    {
        StartOffset = startOffset;
        EndOffset = endOffset;
    }

    public int StartOffset { get; }

    public int EndOffset { get; }

    public static ScientificCharacterRange Create(int startOffset, int endOffset)
    {
        if (startOffset < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(startOffset),
                startOffset,
                "Start offset cannot be negative.");
        }

        if (endOffset <= startOffset)
        {
            throw new ArgumentOutOfRangeException(
                nameof(endOffset),
                endOffset,
                "End offset must be after start offset.");
        }

        return new ScientificCharacterRange(startOffset, endOffset);
    }
}

public sealed record ScientificBoundingRegion
{
    private ScientificBoundingRegion(double x, double y, double width, double height)
    {
        X = x;
        Y = y;
        Width = width;
        Height = height;
    }

    public double X { get; }

    public double Y { get; }

    public double Width { get; }

    public double Height { get; }

    public static ScientificBoundingRegion Create(
        double x,
        double y,
        double width,
        double height)
    {
        if (!double.IsFinite(x) || x < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(x), x, "X must be finite and non-negative.");
        }

        if (!double.IsFinite(y) || y < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(y), y, "Y must be finite and non-negative.");
        }

        if (!double.IsFinite(width) || width <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(width), width, "Width must be finite and positive.");
        }

        if (!double.IsFinite(height) || height <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(height), height, "Height must be finite and positive.");
        }

        return new ScientificBoundingRegion(x, y, width, height);
    }
}

public sealed record ScientificSourceLocation
{
    private ScientificSourceLocation(
        int pageNumber,
        string section,
        ScientificBoundingRegion? boundingRegion,
        ScientificCharacterRange? characterRange)
    {
        PageNumber = pageNumber;
        Section = section;
        BoundingRegion = boundingRegion;
        CharacterRange = characterRange;
    }

    public int PageNumber { get; }

    public string Section { get; }

    public ScientificBoundingRegion? BoundingRegion { get; }

    public ScientificCharacterRange? CharacterRange { get; }

    public static ScientificSourceLocation Create(
        int pageNumber,
        string section,
        ScientificBoundingRegion? boundingRegion,
        ScientificCharacterRange? characterRange)
    {
        if (pageNumber < 1)
        {
            throw new ArgumentOutOfRangeException(
                nameof(pageNumber),
                pageNumber,
                "Page number must be positive.");
        }

        if (boundingRegion is null && characterRange is null)
        {
            throw new ArgumentException(
                "A scientific source location requires a bounding region or character range.",
                nameof(boundingRegion));
        }

        return new ScientificSourceLocation(
            pageNumber,
            ScientificSourceGuard.RequireText(section, nameof(section)),
            boundingRegion,
            characterRange);
    }
}

public sealed record ScientificSourceBlock
{
    private ScientificSourceBlock(
        string blockId,
        ScientificSourceBlockKind kind,
        ScientificSourceLocation location,
        string? originalText,
        bool isRequired,
        ScientificRecoveryStatus recoveryStatus)
    {
        BlockId = blockId;
        Kind = kind;
        Location = location;
        OriginalText = originalText;
        IsRequired = isRequired;
        RecoveryStatus = recoveryStatus;
    }

    public string BlockId { get; }

    public ScientificSourceBlockKind Kind { get; }

    public ScientificSourceLocation Location { get; }

    public string? OriginalText { get; }

    public bool IsRequired { get; }

    public ScientificRecoveryStatus RecoveryStatus { get; }

    public static ScientificSourceBlock Create(
        string blockId,
        ScientificSourceBlockKind kind,
        ScientificSourceLocation location,
        string? originalText,
        bool isRequired,
        ScientificRecoveryStatus recoveryStatus)
    {
        ScientificSourceGuard.RequireDefined(kind, nameof(kind));
        ScientificSourceGuard.RequireDefined(recoveryStatus, nameof(recoveryStatus));
        ArgumentNullException.ThrowIfNull(location);

        var requiresRecovery = kind is ScientificSourceBlockKind.Formula or ScientificSourceBlockKind.Table;
        if (requiresRecovery && recoveryStatus == ScientificRecoveryStatus.NotRequired)
        {
            throw new ArgumentException(
                "Formula and table blocks require an explicit recovery result.",
                nameof(recoveryStatus));
        }

        if (!requiresRecovery && recoveryStatus != ScientificRecoveryStatus.NotRequired)
        {
            throw new ArgumentException(
                "Recovery status applies only to formula and table blocks.",
                nameof(recoveryStatus));
        }

        var normalizedText = ScientificSourceGuard.NormalizeOptionalText(originalText);
        if (recoveryStatus is ScientificRecoveryStatus.NotRequired or ScientificRecoveryStatus.Recovered
            && normalizedText is null)
        {
            throw new ArgumentException(
                "Recovered source blocks must preserve original text.",
                nameof(originalText));
        }

        return new ScientificSourceBlock(
            ScientificSourceGuard.RequireText(blockId, nameof(blockId)),
            kind,
            location,
            normalizedText,
            isRequired,
            recoveryStatus);
    }
}

public sealed record ScientificExtractorIdentity
{
    private ScientificExtractorIdentity(string providerId, string version)
    {
        ProviderId = providerId;
        Version = version;
    }

    public string ProviderId { get; }

    public string Version { get; }

    public static ScientificExtractorIdentity Create(string providerId, string version)
    {
        return new ScientificExtractorIdentity(
            ScientificSourceGuard.RequireText(providerId, nameof(providerId)),
            ScientificSourceGuard.RequireText(version, nameof(version)));
    }
}

public sealed record ScientificExtractionQuality
{
    private ScientificExtractionQuality(
        bool isScanned,
        bool ocrApplied,
        ScientificReadingOrderStatus readingOrder,
        ScientificRequiredContentStatus requiredContent)
    {
        IsScanned = isScanned;
        OcrApplied = ocrApplied;
        ReadingOrder = readingOrder;
        RequiredContent = requiredContent;
    }

    public bool IsScanned { get; }

    public bool OcrApplied { get; }

    public ScientificReadingOrderStatus ReadingOrder { get; }

    public ScientificRequiredContentStatus RequiredContent { get; }

    public static ScientificExtractionQuality Create(
        bool isScanned,
        bool ocrApplied,
        ScientificReadingOrderStatus readingOrder,
        ScientificRequiredContentStatus requiredContent)
    {
        ScientificSourceGuard.RequireDefined(readingOrder, nameof(readingOrder));
        ScientificSourceGuard.RequireDefined(requiredContent, nameof(requiredContent));

        return new ScientificExtractionQuality(
            isScanned,
            ocrApplied,
            readingOrder,
            requiredContent);
    }
}

public sealed record ScientificExtractionDiagnostic
{
    private ScientificExtractionDiagnostic(
        string code,
        ScientificDiagnosticSeverity severity,
        string message)
    {
        Code = code;
        Severity = severity;
        Message = message;
    }

    public string Code { get; }

    public ScientificDiagnosticSeverity Severity { get; }

    public string Message { get; }

    public static ScientificExtractionDiagnostic Create(
        string code,
        ScientificDiagnosticSeverity severity,
        string message)
    {
        ScientificSourceGuard.RequireDefined(severity, nameof(severity));

        return new ScientificExtractionDiagnostic(
            ScientificSourceGuard.RequireText(code, nameof(code)).ToLowerInvariant(),
            severity,
            ScientificSourceGuard.RequireText(message, nameof(message)));
    }
}

public sealed record ScientificDocumentExtraction
{
    private ScientificDocumentExtraction(
        Guid sourceAssetId,
        string sourceSha256,
        ScientificExtractorIdentity extractor,
        ScientificExtractionQuality quality,
        IReadOnlyList<ScientificSourceBlock> blocks,
        IReadOnlyList<ScientificExtractionDiagnostic> diagnostics,
        IReadOnlyList<string> blockingCodes)
    {
        SourceAssetId = sourceAssetId;
        SourceSha256 = sourceSha256;
        Extractor = extractor;
        Quality = quality;
        Blocks = blocks;
        Diagnostics = diagnostics;
        BlockingCodes = blockingCodes;
        Status = blockingCodes.Count == 0
            ? ScientificExtractionStatus.Ready
            : ScientificExtractionStatus.Blocked;
    }

    public Guid SourceAssetId { get; }

    public string SourceSha256 { get; }

    public ScientificExtractorIdentity Extractor { get; }

    public ScientificExtractionQuality Quality { get; }

    public IReadOnlyList<ScientificSourceBlock> Blocks { get; }

    public IReadOnlyList<ScientificExtractionDiagnostic> Diagnostics { get; }

    public ScientificExtractionStatus Status { get; }

    public IReadOnlyList<string> BlockingCodes { get; }

    public static ScientificDocumentExtraction Create(
        Guid sourceAssetId,
        string sourceSha256,
        ScientificExtractorIdentity extractor,
        ScientificExtractionQuality quality,
        IReadOnlyList<ScientificSourceBlock> blocks,
        IReadOnlyList<ScientificExtractionDiagnostic> diagnostics)
    {
        if (sourceAssetId == Guid.Empty)
        {
            throw new ArgumentException("Source asset id cannot be empty.", nameof(sourceAssetId));
        }

        ArgumentNullException.ThrowIfNull(extractor);
        ArgumentNullException.ThrowIfNull(quality);
        ArgumentNullException.ThrowIfNull(blocks);
        ArgumentNullException.ThrowIfNull(diagnostics);
        if (blocks.Count == 0)
        {
            throw new ArgumentException("At least one scientific source block is required.", nameof(blocks));
        }

        var duplicateBlockId = blocks
            .GroupBy(block => block.BlockId, StringComparer.Ordinal)
            .FirstOrDefault(group => group.Count() > 1)
            ?.Key;
        if (duplicateBlockId is not null)
        {
            throw new ArgumentException($"Duplicate scientific source block id: {duplicateBlockId}", nameof(blocks));
        }

        var normalizedHash = ScientificSourceGuard.RequireSha256(sourceSha256, nameof(sourceSha256));
        var blockSnapshot = Array.AsReadOnly(blocks.ToArray());
        var diagnosticSnapshot = Array.AsReadOnly(diagnostics.ToArray());
        var blockingCodes = BuildBlockingCodes(quality, blockSnapshot, diagnosticSnapshot);

        return new ScientificDocumentExtraction(
            sourceAssetId,
            normalizedHash,
            extractor,
            quality,
            blockSnapshot,
            diagnosticSnapshot,
            blockingCodes);
    }

    private static IReadOnlyList<string> BuildBlockingCodes(
        ScientificExtractionQuality quality,
        IReadOnlyList<ScientificSourceBlock> blocks,
        IReadOnlyList<ScientificExtractionDiagnostic> diagnostics)
    {
        var codes = new List<string>();
        if (quality.IsScanned && !quality.OcrApplied)
        {
            codes.Add("scanned-without-ocr");
        }

        if (quality.ReadingOrder == ScientificReadingOrderStatus.Corrupted)
        {
            codes.Add("corrupted-reading-order");
        }

        if (quality.RequiredContent == ScientificRequiredContentStatus.Missing)
        {
            codes.Add("missing-required-content");
        }

        foreach (var block in blocks.Where(block =>
                     block.IsRequired
                     && block.Kind is ScientificSourceBlockKind.Formula or ScientificSourceBlockKind.Table
                     && block.RecoveryStatus is ScientificRecoveryStatus.Missing or ScientificRecoveryStatus.Uncertain))
        {
            codes.Add(
                $"required-{block.Kind.ToString().ToLowerInvariant()}-{block.RecoveryStatus.ToString().ToLowerInvariant()}");
        }

        codes.AddRange(
            diagnostics
                .Where(diagnostic => diagnostic.Severity == ScientificDiagnosticSeverity.Blocking)
                .Select(diagnostic => diagnostic.Code));

        return Array.AsReadOnly(codes.Distinct(StringComparer.Ordinal).ToArray());
    }
}

public enum ScientificSourceBlockKind
{
    Heading = 0,
    Paragraph = 1,
    Caption = 2,
    Footnote = 3,
    Reference = 4,
    SupplementalStatement = 5,
    Formula = 6,
    Table = 7,
}

public enum ScientificRecoveryStatus
{
    NotRequired = 0,
    Recovered = 1,
    Missing = 2,
    Uncertain = 3,
}

public enum ScientificReadingOrderStatus
{
    Reliable = 0,
    Corrupted = 1,
}

public enum ScientificRequiredContentStatus
{
    Complete = 0,
    Missing = 1,
}

public enum ScientificDiagnosticSeverity
{
    Information = 0,
    Warning = 1,
    Blocking = 2,
}

public enum ScientificExtractionStatus
{
    Ready = 0,
    Blocked = 1,
}

internal static class ScientificSourceGuard
{
    public static string RequireText(string? value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Value cannot be empty.", parameterName);
        }

        return value.Trim();
    }

    public static string? NormalizeOptionalText(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    public static string RequireSha256(string? value, string parameterName)
    {
        var hash = RequireText(value, parameterName).ToLowerInvariant();
        const string prefix = "sha256:";
        if (!hash.StartsWith(prefix, StringComparison.Ordinal)
            || hash.Length != prefix.Length + 64
            || hash[prefix.Length..].Any(character =>
                !char.IsAsciiDigit(character) && character is < 'a' or > 'f'))
        {
            throw new ArgumentException(
                "Source hash must use the form sha256:<64 lowercase hexadecimal characters>.",
                parameterName);
        }

        return hash;
    }

    public static void RequireDefined<TEnum>(TEnum value, string parameterName)
        where TEnum : struct, Enum
    {
        if (!Enum.IsDefined(value))
        {
            throw new ArgumentOutOfRangeException(parameterName, value, "Enum value is not supported.");
        }
    }
}
