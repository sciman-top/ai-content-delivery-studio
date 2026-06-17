using ContentDeliveryStudio.Application.Sources;
using ContentDeliveryStudio.Core.Sources;

namespace ContentDeliveryStudio.Infrastructure.Sources;

public sealed class FakeDocumentExtractionProvider : IDocumentExtractionProvider
{
    private static readonly SourceAssetKind[] SupportedKinds =
    [
        SourceAssetKind.Pdf,
        SourceAssetKind.Docx,
        SourceAssetKind.Presentation,
        SourceAssetKind.Markdown,
        SourceAssetKind.Text,
        SourceAssetKind.Image,
    ];

    public DocumentExtractionProviderCapabilities Capabilities { get; } = new(
        "fake-document-extraction",
        "Fake Document Extraction Provider",
        SupportedKinds,
        SupportsOcr: true);

    public Task<DocumentExtractionResult> ExtractAsync(
        DocumentExtractionRequest request,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!SupportedKinds.Contains(request.SourceKind))
        {
            throw new InvalidOperationException($"Source kind is not supported by fake document extraction: {request.SourceKind}.");
        }

        if (request.SourceKind is SourceAssetKind.Image && request.UseOcr is false)
        {
            throw new InvalidOperationException("Image extraction requires OCR in the fake document extraction provider.");
        }

        var text = EnsureText(request.FixtureText, $"No extractable text supplied for {request.DisplayName}.");
        var contentKind = SelectContentKind(request.SourceKind);
        var locationHint = SelectLocationHint(request.SourceKind, request.OriginalPath);
        var content = new ExtractedContentDraft(
            contentKind,
            text,
            locationHint,
            PageNumber: request.SourceKind is SourceAssetKind.Pdf ? 1 : null,
            StartOffset: 0,
            EndOffset: text.Length);
        var anchor = new EvidenceAnchorDraft(
            ExtractedContentIndex: 0,
            Label: "fake-document-evidence",
            Quote: SelectAnchorQuote(text),
            LocationHint: locationHint);

        return Task.FromResult(new DocumentExtractionResult(
            [content],
            [anchor],
            "fake-document-extraction"));
    }

    private static ExtractedContentKind SelectContentKind(SourceAssetKind sourceKind)
    {
        return sourceKind switch
        {
            SourceAssetKind.Markdown => ExtractedContentKind.Markdown,
            SourceAssetKind.Image => ExtractedContentKind.OcrText,
            _ => ExtractedContentKind.PlainText,
        };
    }

    private static string SelectLocationHint(SourceAssetKind sourceKind, string? originalPath)
    {
        var fileName = string.IsNullOrWhiteSpace(originalPath)
            ? "fixture"
            : Path.GetFileName(originalPath);

        return sourceKind switch
        {
            SourceAssetKind.Pdf => $"{fileName}: page 1",
            SourceAssetKind.Docx => $"{fileName}: document body",
            SourceAssetKind.Presentation => $"{fileName}: slide 1",
            SourceAssetKind.Markdown => $"{fileName}: markdown body",
            SourceAssetKind.Image => $"{fileName}: OCR layer",
            _ => $"{fileName}: text body",
        };
    }

    private static string EnsureText(string value, string fallback)
    {
        return string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
    }

    private static string SelectAnchorQuote(string text)
    {
        var firstLine = text
            .Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .FirstOrDefault();
        var quote = string.IsNullOrWhiteSpace(firstLine) ? text.Trim() : firstLine.Trim();

        return quote.Length <= 160 ? quote : quote[..160].Trim();
    }
}
