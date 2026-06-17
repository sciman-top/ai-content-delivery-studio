using System.Text.Json.Serialization;

namespace ContentDeliveryStudio.Core.Sources;

public sealed class SourceAsset
{
    private readonly List<ExtractedContent> _extractedContents = [];
    private readonly List<EvidenceAnchor> _evidenceAnchors = [];

    private SourceAsset()
    {
        DisplayName = string.Empty;
    }

    private SourceAsset(
        Guid id,
        Guid projectId,
        SourceAssetKind kind,
        string displayName,
        string? originalPath,
        string? mimeType,
        long? sizeBytes,
        string? sha256,
        DateTimeOffset createdAt)
    {
        Id = id;
        ProjectId = RequireNonEmptyId(projectId, nameof(projectId));
        Kind = RequireDefinedKind(kind);
        DisplayName = RequireText(displayName, nameof(displayName));
        OriginalPath = NormalizeOptionalText(originalPath);
        MimeType = NormalizeOptionalText(mimeType);
        SizeBytes = RequireValidSize(sizeBytes, nameof(sizeBytes));
        Sha256 = NormalizeOptionalText(sha256);
        CreatedAt = createdAt;
        UpdatedAt = createdAt;
    }

    public Guid Id { get; private set; }

    public Guid ProjectId { get; private set; }

    public SourceAssetKind Kind { get; private set; }

    public string DisplayName { get; private set; }

    public string? OriginalPath { get; private set; }

    public string? MimeType { get; private set; }

    public long? SizeBytes { get; private set; }

    public string? Sha256 { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public IReadOnlyCollection<ExtractedContent> ExtractedContents => _extractedContents.AsReadOnly();

    public IReadOnlyCollection<EvidenceAnchor> EvidenceAnchors => _evidenceAnchors.AsReadOnly();

    public static SourceAsset Create(
        Guid projectId,
        SourceAssetKind kind,
        string displayName,
        string? originalPath,
        string? mimeType,
        long? sizeBytes,
        string? sha256,
        DateTimeOffset createdAt)
    {
        return new SourceAsset(
            Guid.NewGuid(),
            projectId,
            kind,
            displayName,
            originalPath,
            mimeType,
            sizeBytes,
            sha256,
            createdAt);
    }

    public ExtractedContent AddExtractedContent(
        ExtractedContentKind kind,
        string text,
        string locationHint,
        int? pageNumber,
        int? startOffset,
        int? endOffset,
        DateTimeOffset timestamp)
    {
        var content = ExtractedContent.Create(
            Id,
            kind,
            text,
            locationHint,
            pageNumber,
            startOffset,
            endOffset,
            timestamp);

        _extractedContents.Add(content);
        UpdatedAt = timestamp;
        return content;
    }

    public EvidenceAnchor AddEvidenceAnchor(
        Guid extractedContentId,
        string label,
        string quote,
        string locationHint,
        DateTimeOffset timestamp)
    {
        if (extractedContentId == Guid.Empty)
        {
            throw new ArgumentException("Extracted content id cannot be empty.", nameof(extractedContentId));
        }

        if (_extractedContents.All(content => content.Id != extractedContentId))
        {
            throw new InvalidOperationException($"Extracted content not found for evidence anchor: {extractedContentId}");
        }

        var anchor = EvidenceAnchor.Create(
            Id,
            extractedContentId,
            label,
            quote,
            locationHint,
            timestamp);

        _evidenceAnchors.Add(anchor);
        UpdatedAt = timestamp;
        return anchor;
    }

    private static Guid RequireNonEmptyId(Guid value, string parameterName)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException("Value cannot be empty.", parameterName);
        }

        return value;
    }

    private static SourceAssetKind RequireDefinedKind(SourceAssetKind kind)
    {
        return Enum.IsDefined(typeof(SourceAssetKind), kind)
            ? kind
            : throw new ArgumentOutOfRangeException(nameof(kind), kind, "Source asset kind is not supported.");
    }

    private static string RequireText(string value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Value cannot be empty.", parameterName);
        }

        return value.Trim();
    }

    private static string? NormalizeOptionalText(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static long? RequireValidSize(long? sizeBytes, string parameterName)
    {
        if (sizeBytes is < 0)
        {
            throw new ArgumentOutOfRangeException(parameterName, sizeBytes, "Size cannot be negative.");
        }

        return sizeBytes;
    }
}

public enum SourceAssetKind
{
    Unknown = 0,
    Pdf = 1,
    Docx = 2,
    Image = 3,
    Markdown = 4,
    Text = 5,
    Paste = 6,
    Spreadsheet = 7,
    Presentation = 8,
    WebPage = 9,
}

public enum ExtractedContentKind
{
    PlainText = 0,
    Markdown = 1,
    OcrText = 2,
    ImageCaption = 3,
    Table = 4,
    FormulaLatex = 5,
    Metadata = 6,
}

public sealed class ExtractedContent
{
    private ExtractedContent()
    {
        Text = string.Empty;
        LocationHint = string.Empty;
    }

    [JsonConstructor]
    public ExtractedContent(
        Guid id,
        Guid sourceAssetId,
        ExtractedContentKind kind,
        string text,
        string locationHint,
        int? pageNumber,
        int? startOffset,
        int? endOffset,
        DateTimeOffset createdAt)
    {
        Id = RequireNonEmptyId(id, nameof(id));
        SourceAssetId = RequireNonEmptyId(sourceAssetId, nameof(sourceAssetId));
        Kind = RequireDefinedKind(kind);
        Text = RequireText(text, nameof(text));
        LocationHint = NormalizeOptionalText(locationHint) ?? string.Empty;
        PageNumber = RequirePositivePageNumber(pageNumber, nameof(pageNumber));
        StartOffset = RequireNonNegativeOffset(startOffset, nameof(startOffset));
        EndOffset = RequireEndOffset(endOffset, StartOffset, nameof(endOffset));
        CreatedAt = createdAt;
    }

    public Guid Id { get; private set; }

    public Guid SourceAssetId { get; private set; }

    public ExtractedContentKind Kind { get; private set; }

    public string Text { get; private set; }

    public string LocationHint { get; private set; }

    public int? PageNumber { get; private set; }

    public int? StartOffset { get; private set; }

    public int? EndOffset { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public static ExtractedContent Create(
        Guid sourceAssetId,
        ExtractedContentKind kind,
        string text,
        string locationHint,
        int? pageNumber,
        int? startOffset,
        int? endOffset,
        DateTimeOffset createdAt)
    {
        return new ExtractedContent(
            Guid.NewGuid(),
            sourceAssetId,
            kind,
            text,
            locationHint,
            pageNumber,
            startOffset,
            endOffset,
            createdAt);
    }

    private static Guid RequireNonEmptyId(Guid value, string parameterName)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException("Value cannot be empty.", parameterName);
        }

        return value;
    }

    private static ExtractedContentKind RequireDefinedKind(ExtractedContentKind kind)
    {
        return Enum.IsDefined(typeof(ExtractedContentKind), kind)
            ? kind
            : throw new ArgumentOutOfRangeException(nameof(kind), kind, "Extracted content kind is not supported.");
    }

    private static string RequireText(string value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Value cannot be empty.", parameterName);
        }

        return value.Trim();
    }

    private static string? NormalizeOptionalText(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static int? RequirePositivePageNumber(int? pageNumber, string parameterName)
    {
        if (pageNumber is < 1)
        {
            throw new ArgumentOutOfRangeException(parameterName, pageNumber, "Page number must be positive.");
        }

        return pageNumber;
    }

    private static int? RequireNonNegativeOffset(int? offset, string parameterName)
    {
        if (offset is < 0)
        {
            throw new ArgumentOutOfRangeException(parameterName, offset, "Offset cannot be negative.");
        }

        return offset;
    }

    private static int? RequireEndOffset(int? endOffset, int? startOffset, string parameterName)
    {
        if (endOffset is < 0)
        {
            throw new ArgumentOutOfRangeException(parameterName, endOffset, "Offset cannot be negative.");
        }

        if (startOffset is { } start && endOffset is { } end && end < start)
        {
            throw new ArgumentOutOfRangeException(parameterName, endOffset, "End offset cannot be before start offset.");
        }

        return endOffset;
    }
}

public sealed class EvidenceAnchor
{
    private EvidenceAnchor()
    {
        Label = string.Empty;
        Quote = string.Empty;
        LocationHint = string.Empty;
    }

    [JsonConstructor]
    public EvidenceAnchor(
        Guid id,
        Guid sourceAssetId,
        Guid extractedContentId,
        string label,
        string quote,
        string locationHint,
        DateTimeOffset createdAt)
    {
        Id = RequireNonEmptyId(id, nameof(id));
        SourceAssetId = RequireNonEmptyId(sourceAssetId, nameof(sourceAssetId));
        ExtractedContentId = RequireNonEmptyId(extractedContentId, nameof(extractedContentId));
        Label = RequireText(label, nameof(label));
        Quote = RequireText(quote, nameof(quote));
        LocationHint = NormalizeOptionalText(locationHint) ?? string.Empty;
        CreatedAt = createdAt;
    }

    public Guid Id { get; private set; }

    public Guid SourceAssetId { get; private set; }

    public Guid ExtractedContentId { get; private set; }

    public string Label { get; private set; }

    public string Quote { get; private set; }

    public string LocationHint { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public static EvidenceAnchor Create(
        Guid sourceAssetId,
        Guid extractedContentId,
        string label,
        string quote,
        string locationHint,
        DateTimeOffset createdAt)
    {
        return new EvidenceAnchor(
            Guid.NewGuid(),
            sourceAssetId,
            extractedContentId,
            label,
            quote,
            locationHint,
            createdAt);
    }

    private static Guid RequireNonEmptyId(Guid value, string parameterName)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException("Value cannot be empty.", parameterName);
        }

        return value;
    }

    private static string RequireText(string value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Value cannot be empty.", parameterName);
        }

        return value.Trim();
    }

    private static string? NormalizeOptionalText(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
