using ContentDeliveryStudio.Application.ScientificFigures;
using ContentDeliveryStudio.Core.ScientificFigures;
using ContentDeliveryStudio.Core.Sources;
using ContentDeliveryStudio.Infrastructure.ScientificFigures;

namespace ContentDeliveryStudio.Tests;

public sealed class ScientificDocumentExtractionTests
{
    private const string SourceHash =
        "sha256:a3f75487411ef13d0270c65fc801ceff2b28e6b339afed9b407fe477f7e8453e";

    [Fact]
    public async Task ExtractAsync_PreservesPdfPageAndCharacterProvenance()
    {
        await WithDirectoryAsync(async directory =>
        {
            var pdfPath = Path.Combine(directory, "mechanism.pdf");
            await BinaryDocumentTestFixtureBuilder.CreateSimplePdfAsync(
                pdfPath,
                "Net force causes acceleration for constant mass.",
                CancellationToken.None);

            var extraction = await new PdfPigScientificDocumentExtractor().ExtractAsync(
                Request(
                    SourceAssetKind.Pdf,
                    originalPath: pdfPath),
                CancellationToken.None);

            var block = Assert.Single(extraction.Blocks);
            Assert.Equal(ScientificExtractionStatus.Ready, extraction.Status);
            Assert.Equal(1, block.Location.PageNumber);
            Assert.Equal("page 1", block.Location.Section);
            Assert.Equal(0, block.Location.CharacterRange!.StartOffset);
            Assert.Equal(block.OriginalText!.Length, block.Location.CharacterRange.EndOffset);
            Assert.Equal(ScientificSourceBlockKind.Paragraph, block.Kind);
        });
    }

    [Fact]
    public async Task ExtractAsync_PreservesMarkdownBlocksAndExplicitFormulaRecovery()
    {
        const string markdown =
            """
            # Dynamics

            Net force causes acceleration for constant mass.

            $$ F = m a $$

            | quantity | unit |
            | --- | --- |
            | force | N |
            """;

        var extraction = await new PdfPigScientificDocumentExtractor().ExtractAsync(
            Request(
                SourceAssetKind.Markdown,
                sourceText: markdown,
                requiredContent:
                [
                    ScientificRequiredContentKind.Formula,
                    ScientificRequiredContentKind.Table,
                ]),
            CancellationToken.None);

        Assert.Equal(ScientificExtractionStatus.Ready, extraction.Status);
        Assert.Collection(
            extraction.Blocks,
            block => Assert.Equal(ScientificSourceBlockKind.Heading, block.Kind),
            block => Assert.Equal(ScientificSourceBlockKind.Paragraph, block.Kind),
            block =>
            {
                Assert.Equal(ScientificSourceBlockKind.Formula, block.Kind);
                Assert.Equal(ScientificRecoveryStatus.Recovered, block.RecoveryStatus);
            },
            block =>
            {
                Assert.Equal(ScientificSourceBlockKind.Table, block.Kind);
                Assert.Equal(ScientificRecoveryStatus.Recovered, block.RecoveryStatus);
            });
        Assert.All(
            extraction.Blocks,
            block => Assert.NotNull(block.Location.CharacterRange));
    }

    [Fact]
    public async Task ExtractAsync_ReturnsStructuredBlockedResultForUnsupportedOcr()
    {
        await WithDirectoryAsync(async directory =>
        {
            var pdfPath = Path.Combine(directory, "scanned.pdf");
            await BinaryDocumentTestFixtureBuilder.CreateSimplePdfAsync(
                pdfPath,
                "A text layer remains available but OCR was explicitly requested.",
                CancellationToken.None);

            var extraction = await new PdfPigScientificDocumentExtractor().ExtractAsync(
                Request(
                    SourceAssetKind.Pdf,
                    originalPath: pdfPath,
                    isScanned: true,
                    useOcr: true),
                CancellationToken.None);

            Assert.Equal(ScientificExtractionStatus.Blocked, extraction.Status);
            Assert.False(extraction.Quality.OcrApplied);
            Assert.Contains("ocr-not-supported", extraction.BlockingCodes);
            Assert.Contains("scanned-without-ocr", extraction.BlockingCodes);
            Assert.Contains(
                extraction.Diagnostics,
                diagnostic => diagnostic.Code == "ocr-not-supported"
                    && diagnostic.Severity == ScientificDiagnosticSeverity.Blocking);
        });
    }

    [Fact]
    public async Task ExtractAsync_ReturnsUnrecoverableRegionWhenPdfHasNoUsableText()
    {
        await WithDirectoryAsync(async directory =>
        {
            var pdfPath = Path.Combine(directory, "image-only.pdf");
            await BinaryDocumentTestFixtureBuilder.CreateSimplePdfAsync(
                pdfPath,
                " ",
                CancellationToken.None);

            var extraction = await new PdfPigScientificDocumentExtractor().ExtractAsync(
                Request(
                    SourceAssetKind.Pdf,
                    originalPath: pdfPath,
                    isScanned: true),
                CancellationToken.None);

            var block = Assert.Single(extraction.Blocks);
            Assert.Equal(ScientificSourceBlockKind.UnrecoverableRegion, block.Kind);
            Assert.Equal(ScientificRecoveryStatus.Missing, block.RecoveryStatus);
            Assert.Null(block.OriginalText);
            Assert.Equal(ScientificExtractionStatus.Blocked, extraction.Status);
            Assert.Contains("no-usable-text", extraction.BlockingCodes);
        });
    }

    [Fact]
    public async Task ExtractAsync_BlocksCorruptedReadingOrder()
    {
        var extraction = await new PdfPigScientificDocumentExtractor().ExtractAsync(
            Request(
                SourceAssetKind.Text,
                sourceText: "First fragment. Second fragment.",
                readingOrder: ScientificReadingOrderStatus.Corrupted),
            CancellationToken.None);

        Assert.Equal(ScientificExtractionStatus.Blocked, extraction.Status);
        Assert.Contains("corrupted-reading-order", extraction.BlockingCodes);
        Assert.Contains(
            extraction.Diagnostics,
            diagnostic => diagnostic.Code == "reading-order-corrupted");
    }

    [Fact]
    public async Task ExtractAsync_DoesNotPromoteUnmarkedPossibleFormulaToRecoveredEvidence()
    {
        var extraction = await new PdfPigScientificDocumentExtractor().ExtractAsync(
            Request(
                SourceAssetKind.Text,
                sourceText: "The source may imply that force equals mass times acceleration.",
                requiredContent: [ScientificRequiredContentKind.Formula]),
            CancellationToken.None);

        var missingFormula = Assert.Single(
            extraction.Blocks,
            block => block.Kind == ScientificSourceBlockKind.Formula);
        Assert.Equal(ScientificRecoveryStatus.Missing, missingFormula.RecoveryStatus);
        Assert.Null(missingFormula.OriginalText);
        Assert.Equal(ScientificExtractionStatus.Blocked, extraction.Status);
        Assert.Contains("required-formula-missing", extraction.BlockingCodes);
    }

    private static ScientificDocumentExtractionRequest Request(
        SourceAssetKind sourceKind,
        string sourceText = "",
        string? originalPath = null,
        bool isScanned = false,
        bool useOcr = false,
        ScientificReadingOrderStatus readingOrder = ScientificReadingOrderStatus.Reliable,
        IReadOnlyList<ScientificRequiredContentKind>? requiredContent = null)
    {
        return new ScientificDocumentExtractionRequest(
            Guid.NewGuid(),
            SourceHash,
            sourceKind,
            "scientific-source",
            sourceText,
            originalPath,
            isScanned,
            useOcr,
            readingOrder,
            requiredContent ?? []);
    }

    private static async Task WithDirectoryAsync(Func<string, Task> test)
    {
        var directory = Path.Combine(
            Path.GetTempPath(),
            "ContentDeliveryStudio.Tests",
            Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            await test(directory);
        }
        finally
        {
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, recursive: true);
            }
        }
    }
}
