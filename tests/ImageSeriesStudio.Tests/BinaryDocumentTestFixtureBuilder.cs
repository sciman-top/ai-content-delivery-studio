using System.IO.Compression;
using System.Text;

namespace ImageSeriesStudio.Tests;

internal static class BinaryDocumentTestFixtureBuilder
{
    public static async Task CreateSimplePdfAsync(string outputPath, string text, CancellationToken cancellationToken)
    {
        await Task.Run(() =>
        {
            using var stream = File.Create(outputPath);
            using var writer = new BinaryWriter(stream);
            WritePdfDocument(writer, text);
        }, cancellationToken);
    }

    public static async Task CreateSimpleDocxAsync(
        string outputPath,
        IReadOnlyList<string> paragraphs,
        CancellationToken cancellationToken)
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
        using var archive = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: false);
        await WriteZipEntryAsync(archive, "[Content_Types].xml", contentTypesXml, cancellationToken);
        await WriteZipEntryAsync(archive, "_rels/.rels", relationshipsXml, cancellationToken);
        await WriteZipEntryAsync(archive, "word/document.xml", documentXml, cancellationToken);
    }

    private static async Task WriteZipEntryAsync(
        ZipArchive archive,
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
            binaryWriter.Write(Encoding.ASCII.GetBytes($"{objectNumber} 0 obj\n{body}\nendobj\n"));
            return offset;
        }

        writer.Write(Encoding.ASCII.GetBytes("%PDF-1.4\n"));
        var offsets = new List<int> { 0 };

        var escapedText = text.Replace("\\", "\\\\").Replace("(", "\\(").Replace(")", "\\)");
        var contentStream = $"BT /F1 24 Tf 72 140 Td ({escapedText}) Tj ET";

        offsets.Add(WriteObject(writer, 1, "<< /Type /Catalog /Pages 2 0 R >>"));
        offsets.Add(WriteObject(writer, 2, "<< /Type /Pages /Count 1 /Kids [3 0 R] >>"));
        offsets.Add(WriteObject(writer, 3, "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] /Resources << /Font << /F1 5 0 R >> >> /Contents 4 0 R >>"));
        offsets.Add(WriteObject(writer, 4, $"<< /Length {Encoding.ASCII.GetByteCount(contentStream)} >>\nstream\n{contentStream}\nendstream"));
        offsets.Add(WriteObject(writer, 5, "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>"));

        var xrefOffset = (int)writer.BaseStream.Position;
        writer.Write(Encoding.ASCII.GetBytes($"xref\n0 {offsets.Count}\n"));
        writer.Write(Encoding.ASCII.GetBytes("0000000000 65535 f \n"));
        foreach (var offset in offsets.Skip(1))
        {
            writer.Write(Encoding.ASCII.GetBytes($"{offset:D10} 00000 n \n"));
        }

        writer.Write(Encoding.ASCII.GetBytes(
            $"trailer\n<< /Size {offsets.Count} /Root 1 0 R >>\nstartxref\n{xrefOffset}\n%%EOF"));
    }
}
