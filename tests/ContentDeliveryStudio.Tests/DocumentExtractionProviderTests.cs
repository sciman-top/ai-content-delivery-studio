using ContentDeliveryStudio.Application.Sources;
using ContentDeliveryStudio.Core.Sources;
using ContentDeliveryStudio.Infrastructure.Sources;
using System.Text;

namespace ContentDeliveryStudio.Tests;

public sealed class DocumentExtractionProviderTests
{
    [Theory]
    [InlineData(SourceAssetKind.Pdf, ExtractedContentKind.PlainText)]
    [InlineData(SourceAssetKind.Docx, ExtractedContentKind.PlainText)]
    [InlineData(SourceAssetKind.Presentation, ExtractedContentKind.PlainText)]
    [InlineData(SourceAssetKind.Markdown, ExtractedContentKind.Markdown)]
    [InlineData(SourceAssetKind.Image, ExtractedContentKind.OcrText)]
    public async Task FakeDocumentExtractionProvider_ExtractsFixtureTextForSupportedKinds(
        SourceAssetKind sourceKind,
        ExtractedContentKind expectedContentKind)
    {
        var provider = new FakeDocumentExtractionProvider();

        var result = await provider.ExtractAsync(
            new DocumentExtractionRequest(
                sourceKind,
                "source-file",
                "Key claim: keep generated artifacts traceable to source evidence.",
                OriginalPath: @"C:\uploads\source-file",
                UseOcr: sourceKind == SourceAssetKind.Image),
            CancellationToken.None);

        var content = Assert.Single(result.ExtractedContents);
        var anchor = Assert.Single(result.EvidenceAnchors);

        Assert.Equal("fake-document-extraction", result.ProviderTraceId);
        Assert.Equal(expectedContentKind, content.Kind);
        Assert.Contains("traceable", content.Text);
        Assert.Equal(0, content.StartOffset);
        Assert.Equal(content.Text.Length, content.EndOffset);
        Assert.Equal(0, anchor.ExtractedContentIndex);
        Assert.Contains("traceable", anchor.Quote);
        Assert.Contains(sourceKind, provider.Capabilities.SupportedSourceKinds);
    }

    [Fact]
    public void FakeDocumentExtractionProvider_DeclaresProviderNeutralCapabilities()
    {
        var provider = new FakeDocumentExtractionProvider();

        Assert.Equal("fake-document-extraction", provider.Capabilities.ProviderId);
        Assert.False(provider.Capabilities.RequiresExplicitApproval);
        Assert.True(provider.Capabilities.SupportsOcr);
        Assert.Contains(SourceAssetKind.Pdf, provider.Capabilities.SupportedSourceKinds);
        Assert.Contains(SourceAssetKind.Docx, provider.Capabilities.SupportedSourceKinds);
        Assert.Contains(SourceAssetKind.Presentation, provider.Capabilities.SupportedSourceKinds);
        Assert.Contains(SourceAssetKind.Markdown, provider.Capabilities.SupportedSourceKinds);
        Assert.Contains(SourceAssetKind.Image, provider.Capabilities.SupportedSourceKinds);
    }

    [Fact]
    public async Task FakeDocumentExtractionProvider_RejectsUnsupportedSourceKind()
    {
        var provider = new FakeDocumentExtractionProvider();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            provider.ExtractAsync(
                new DocumentExtractionRequest(
                    SourceAssetKind.WebPage,
                    "page.html",
                    "Web extraction is not part of the fake document extractor."),
                CancellationToken.None));

        Assert.Contains("not supported", exception.Message);
    }

    [Fact]
    public async Task LocalBinaryDocumentExtractionProvider_ExtractsPdfTextWithPageHints()
    {
        var rootDirectory = Path.Combine(Path.GetTempPath(), "ContentDeliveryStudio.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(rootDirectory);
        var pdfPath = Path.Combine(rootDirectory, "lesson.pdf");

        try
        {
            await CreateSimplePdfAsync(
                pdfPath,
                "Binary extraction keeps source evidence reviewable for planning.",
                CancellationToken.None);
            var provider = new LocalBinaryDocumentExtractionProvider();

            var result = await provider.ExtractAsync(
                new DocumentExtractionRequest(
                    SourceAssetKind.Pdf,
                    "lesson.pdf",
                    string.Empty,
                    OriginalPath: pdfPath),
                CancellationToken.None);

            var content = Assert.Single(result.ExtractedContents);
            var anchor = Assert.Single(result.EvidenceAnchors);

            Assert.Equal("local-binary-pdf-extraction", result.ProviderTraceId);
            Assert.Equal(ExtractedContentKind.PlainText, content.Kind);
            Assert.Equal("lesson.pdf: page 1", content.LocationHint);
            Assert.Equal(1, content.PageNumber);
            Assert.Contains("source evidence reviewable", content.Text, StringComparison.OrdinalIgnoreCase);
            Assert.Equal("lesson.pdf: page 1", anchor.LocationHint);
        }
        finally
        {
            if (Directory.Exists(rootDirectory))
            {
                Directory.Delete(rootDirectory, recursive: true);
            }
        }
    }

    [Fact]
    public async Task LocalBinaryDocumentExtractionProvider_ExtractsDocxTextWithParagraphHints()
    {
        var rootDirectory = Path.Combine(Path.GetTempPath(), "ContentDeliveryStudio.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(rootDirectory);
        var docxPath = Path.Combine(rootDirectory, "brief.docx");

        try
        {
            await CreateSimpleDocxAsync(
                docxPath,
                [
                    "First paragraph grounds the main claim in source evidence.",
                    "Second paragraph preserves the supporting explanation.",
                ],
                CancellationToken.None);
            var provider = new LocalBinaryDocumentExtractionProvider();

            var result = await provider.ExtractAsync(
                new DocumentExtractionRequest(
                    SourceAssetKind.Docx,
                    "brief.docx",
                    string.Empty,
                    OriginalPath: docxPath),
                CancellationToken.None);

            Assert.Equal("local-binary-docx-extraction", result.ProviderTraceId);
            Assert.Equal(2, result.ExtractedContents.Count);
            Assert.Equal("brief.docx: paragraph 1", result.ExtractedContents[0].LocationHint);
            Assert.Equal("brief.docx: paragraph 2", result.ExtractedContents[1].LocationHint);
            Assert.Contains("main claim", result.ExtractedContents[0].Text, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("supporting explanation", result.ExtractedContents[1].Text, StringComparison.OrdinalIgnoreCase);
            Assert.Equal("brief.docx: paragraph 1", result.EvidenceAnchors[0].LocationHint);
        }
        finally
        {
            if (Directory.Exists(rootDirectory))
            {
                Directory.Delete(rootDirectory, recursive: true);
            }
        }
    }

    [Fact]
    public async Task LocalBinaryDocumentExtractionProvider_FailsClosedWhenPdfHasNoUsableText()
    {
        var rootDirectory = Path.Combine(Path.GetTempPath(), "ContentDeliveryStudio.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(rootDirectory);
        var pdfPath = Path.Combine(rootDirectory, "empty.pdf");

        try
        {
            await CreateSimplePdfAsync(pdfPath, " ", CancellationToken.None);
            var provider = new LocalBinaryDocumentExtractionProvider();

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                provider.ExtractAsync(
                    new DocumentExtractionRequest(
                        SourceAssetKind.Pdf,
                        "empty.pdf",
                        string.Empty,
                        OriginalPath: pdfPath),
                    CancellationToken.None));

            Assert.Contains("produced no usable text", exception.Message, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("outside the current supported boundary", exception.Message, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            if (Directory.Exists(rootDirectory))
            {
                Directory.Delete(rootDirectory, recursive: true);
            }
        }
    }

    [Fact]
    public async Task LocalBinaryDocumentExtractionProvider_RejectsOcrOutsideCurrentSupportedBoundary()
    {
        var rootDirectory = Path.Combine(Path.GetTempPath(), "ContentDeliveryStudio.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(rootDirectory);
        var pdfPath = Path.Combine(rootDirectory, "ocr-request.pdf");

        try
        {
            await CreateSimplePdfAsync(
                pdfPath,
                "OCR is not part of the supported local binary extraction path.",
                CancellationToken.None);
            var provider = new LocalBinaryDocumentExtractionProvider();

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                provider.ExtractAsync(
                    new DocumentExtractionRequest(
                        SourceAssetKind.Pdf,
                        "ocr-request.pdf",
                        string.Empty,
                        OriginalPath: pdfPath,
                        UseOcr: true),
                    CancellationToken.None));

            Assert.Contains("OCR is outside the current supported boundary", exception.Message, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            if (Directory.Exists(rootDirectory))
            {
                Directory.Delete(rootDirectory, recursive: true);
            }
        }
    }

    private static async Task CreateSimplePdfAsync(string outputPath, string text, CancellationToken cancellationToken)
    {
        await Task.Run(() =>
        {
            using var stream = File.Create(outputPath);
            using var writer = new BinaryWriter(stream);
            WritePdfDocument(writer, text);
        }, cancellationToken);
    }

    private static async Task CreateSimpleDocxAsync(string outputPath, IReadOnlyList<string> paragraphs, CancellationToken cancellationToken)
    {
        const string contentTypesXml =
            """
<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<Types xmlns="http://schemas.openxmlformats.org/package/2006/content-types">
  <Default Extension="rels" ContentType="application/vnd.openxmlformats-package.relationships+xml"/>
  <Default Extension="xml" ContentType="application/xml"/>
  <Override PartName="/word/document.xml" ContentType="application/vnd.openxmlformats-officedocument.wordprocessingml.document.main+xml"/>
</Types>
""";
        const string relationshipsXml =
            """
<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">
  <Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument" Target="word/document.xml"/>
</Relationships>
""";

        var documentXml = $$"""
<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<w:document xmlns:w="http://schemas.openxmlformats.org/wordprocessingml/2006/main">
  <w:body>
{{string.Join(Environment.NewLine, paragraphs.Select(paragraph => $"    <w:p><w:r><w:t>{System.Security.SecurityElement.Escape(paragraph)}</w:t></w:r></w:p>"))}}
    <w:sectPr />
  </w:body>
</w:document>
""";

        await using var stream = File.Create(outputPath);
        using var archive = new System.IO.Compression.ZipArchive(stream, System.IO.Compression.ZipArchiveMode.Create, leaveOpen: false);
        await WriteZipEntryAsync(archive, "[Content_Types].xml", contentTypesXml, cancellationToken);
        await WriteZipEntryAsync(archive, "_rels/.rels", relationshipsXml, cancellationToken);
        await WriteZipEntryAsync(archive, "word/document.xml", documentXml, cancellationToken);
    }

    private static async Task WriteZipEntryAsync(
        System.IO.Compression.ZipArchive archive,
        string entryName,
        string content,
        CancellationToken cancellationToken)
    {
        var entry = archive.CreateEntry(entryName);
        await using var stream = entry.Open();
        await using var writer = new StreamWriter(stream);
        await writer.WriteAsync(content.AsMemory(), cancellationToken);
    }

    private static void WritePdfDocument(BinaryWriter writer, string text)
    {
        static int WriteObject(BinaryWriter binaryWriter, int objectNumber, string body)
        {
            var offset = (int)binaryWriter.BaseStream.Position;
            binaryWriter.Write(System.Text.Encoding.ASCII.GetBytes($"{objectNumber} 0 obj\n{body}\nendobj\n"));
            return offset;
        }

        writer.Write(System.Text.Encoding.ASCII.GetBytes("%PDF-1.4\n"));
        var offsets = new List<int> { 0 };

        var escapedText = text.Replace("\\", "\\\\").Replace("(", "\\(").Replace(")", "\\)");
        var contentStream = $"BT /F1 24 Tf 72 140 Td ({escapedText}) Tj ET";

        offsets.Add(WriteObject(writer, 1, "<< /Type /Catalog /Pages 2 0 R >>"));
        offsets.Add(WriteObject(writer, 2, "<< /Type /Pages /Count 1 /Kids [3 0 R] >>"));
        offsets.Add(WriteObject(writer, 3, "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] /Resources << /Font << /F1 5 0 R >> >> /Contents 4 0 R >>"));
        offsets.Add(WriteObject(writer, 4, $"<< /Length {Encoding.ASCII.GetByteCount(contentStream)} >>\nstream\n{contentStream}\nendstream"));
        offsets.Add(WriteObject(writer, 5, "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>"));

        var xrefOffset = (int)writer.BaseStream.Position;
        writer.Write(System.Text.Encoding.ASCII.GetBytes($"xref\n0 {offsets.Count}\n"));
        writer.Write(System.Text.Encoding.ASCII.GetBytes("0000000000 65535 f \n"));
        foreach (var offset in offsets.Skip(1))
        {
            writer.Write(System.Text.Encoding.ASCII.GetBytes($"{offset:D10} 00000 n \n"));
        }

        writer.Write(System.Text.Encoding.ASCII.GetBytes(
            $"trailer\n<< /Size {offsets.Count} /Root 1 0 R >>\nstartxref\n{xrefOffset}\n%%EOF"));
    }
}
