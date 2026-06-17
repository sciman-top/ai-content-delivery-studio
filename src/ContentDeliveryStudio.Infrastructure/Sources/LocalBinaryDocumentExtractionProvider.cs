using System.IO.Compression;
using System.Text;
using System.Xml.Linq;
using ContentDeliveryStudio.Application.Sources;
using ContentDeliveryStudio.Core.Sources;
using UglyToad.PdfPig;

namespace ContentDeliveryStudio.Infrastructure.Sources;

public sealed class LocalBinaryDocumentExtractionProvider : IDocumentExtractionProvider
{
    private static readonly SourceAssetKind[] SupportedKinds =
    [
        SourceAssetKind.Pdf,
        SourceAssetKind.Docx,
    ];

    public DocumentExtractionProviderCapabilities Capabilities { get; } = new(
        "local-binary-document-extraction",
        "Local Binary Document Extraction Provider",
        SupportedKinds,
        SupportsOcr: false);

    public async Task<DocumentExtractionResult> ExtractAsync(
        DocumentExtractionRequest request,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!SupportedKinds.Contains(request.SourceKind))
        {
            throw new InvalidOperationException($"Source kind is not supported by local binary extraction: {request.SourceKind}.");
        }

        if (request.UseOcr)
        {
            throw new InvalidOperationException("OCR is outside the current supported boundary for local binary document extraction.");
        }

        if (string.IsNullOrWhiteSpace(request.OriginalPath) || !File.Exists(request.OriginalPath))
        {
            throw new FileNotFoundException("Binary document extraction requires an existing local file path.", request.OriginalPath);
        }

        return request.SourceKind switch
        {
            SourceAssetKind.Pdf => await ExtractPdfAsync(request, cancellationToken),
            SourceAssetKind.Docx => await ExtractDocxAsync(request, cancellationToken),
            _ => throw new InvalidOperationException($"Source kind is not supported by local binary extraction: {request.SourceKind}."),
        };
    }

    private static Task<DocumentExtractionResult> ExtractPdfAsync(
        DocumentExtractionRequest request,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var fileName = Path.GetFileName(request.OriginalPath!);
        var extractedContents = new List<ExtractedContentDraft>();
        var anchors = new List<EvidenceAnchorDraft>();
        var offset = 0;

        using var document = PdfDocument.Open(request.OriginalPath!);
        foreach (var page in document.GetPages())
        {
            cancellationToken.ThrowIfCancellationRequested();

            var text = NormalizeExtractedText(page.Text);
            if (string.IsNullOrWhiteSpace(text))
            {
                continue;
            }

            var locationHint = $"{fileName}: page {page.Number}";
            extractedContents.Add(new ExtractedContentDraft(
                ExtractedContentKind.PlainText,
                text,
                locationHint,
                page.Number,
                offset,
                offset + text.Length));
            anchors.Add(new EvidenceAnchorDraft(
                extractedContents.Count - 1,
                $"pdf-page-{page.Number}",
                SelectAnchorQuote(text),
                locationHint));
            offset += text.Length;
        }

        EnsureHasExtractedContent(extractedContents, fileName, "PDF");
        return Task.FromResult(new DocumentExtractionResult(
            extractedContents,
            anchors,
            "local-binary-pdf-extraction"));
    }

    private static async Task<DocumentExtractionResult> ExtractDocxAsync(
        DocumentExtractionRequest request,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var fileName = Path.GetFileName(request.OriginalPath!);
        var extractedContents = new List<ExtractedContentDraft>();
        var anchors = new List<EvidenceAnchorDraft>();
        var offset = 0;

        using var archive = ZipFile.OpenRead(request.OriginalPath!);
        var entry = archive.GetEntry("word/document.xml")
            ?? throw new InvalidOperationException("DOCX body XML was not found. High-fidelity or unsupported DOCX extraction is outside the current supported boundary.");
        await using var stream = entry.Open();
        var document = await XDocument.LoadAsync(stream, LoadOptions.None, cancellationToken);
        XNamespace wordNamespace = "http://schemas.openxmlformats.org/wordprocessingml/2006/main";

        var paragraphIndex = 0;
        foreach (var paragraph in document.Descendants(wordNamespace + "p"))
        {
            cancellationToken.ThrowIfCancellationRequested();

            var text = NormalizeExtractedText(string.Concat(
                paragraph
                    .Descendants(wordNamespace + "t")
                    .Select(node => node.Value)));
            if (string.IsNullOrWhiteSpace(text))
            {
                continue;
            }

            paragraphIndex++;
            var locationHint = $"{fileName}: paragraph {paragraphIndex}";
            extractedContents.Add(new ExtractedContentDraft(
                ExtractedContentKind.PlainText,
                text,
                locationHint,
                null,
                offset,
                offset + text.Length));
            anchors.Add(new EvidenceAnchorDraft(
                extractedContents.Count - 1,
                $"docx-paragraph-{paragraphIndex}",
                SelectAnchorQuote(text),
                locationHint));
            offset += text.Length;
        }

        EnsureHasExtractedContent(extractedContents, fileName, "DOCX");
        return new DocumentExtractionResult(
            extractedContents,
            anchors,
            "local-binary-docx-extraction");
    }

    private static string NormalizeExtractedText(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return string.Empty;
        }

        var builder = new StringBuilder();
        var sawWhitespace = false;
        foreach (var character in text)
        {
            if (char.IsWhiteSpace(character))
            {
                sawWhitespace = true;
                continue;
            }

            if (sawWhitespace && builder.Length > 0)
            {
                builder.Append(' ');
            }

            builder.Append(character);
            sawWhitespace = false;
        }

        return builder.ToString().Trim();
    }

    private static void EnsureHasExtractedContent(
        IReadOnlyCollection<ExtractedContentDraft> extractedContents,
        string fileName,
        string sourceFamily)
    {
        if (extractedContents.Count > 0)
        {
            return;
        }

        throw new InvalidOperationException(
            $"{sourceFamily} extraction produced no usable text for {fileName}. OCR-heavy, image-only, or high-fidelity binary extraction is outside the current supported boundary.");
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
