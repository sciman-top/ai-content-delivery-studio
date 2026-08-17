using ContentDeliveryStudio.Application.ScientificFigures;
using ContentDeliveryStudio.Application.Sources;
using ContentDeliveryStudio.Core.ScientificFigures;
using ContentDeliveryStudio.Core.Sources;
using ContentDeliveryStudio.Infrastructure.ScientificFigures;
using ContentDeliveryStudio.Infrastructure.Sources;

namespace ContentDeliveryStudio.Tests;

public sealed class SourceIngestionBudgetTests
{
    [Fact]
    public async Task LocalDocxExtraction_RejectsBodyXmlBeforeUnboundedParsing()
    {
        await WithDirectoryAsync(async directory =>
        {
            var path = Path.Combine(directory, "oversized-body.docx");
            await BinaryDocumentTestFixtureBuilder.CreateSimpleDocxAsync(
                path,
                ["This paragraph is deliberately larger than the tiny test budget."],
                CancellationToken.None);
            var provider = new LocalBinaryDocumentExtractionProvider(
                new SourceIngestionBudget(maxDocxXmlBytes: 64));

            var exception = await Assert.ThrowsAsync<InvalidDataException>(() =>
                provider.ExtractAsync(
                    new DocumentExtractionRequest(
                        SourceAssetKind.Docx,
                        "oversized-body.docx",
                        string.Empty,
                        OriginalPath: path),
                    CancellationToken.None));

            Assert.Contains("DOCX body XML", exception.Message, StringComparison.OrdinalIgnoreCase);
        });
    }

    [Fact]
    public async Task ScientificPdfExtraction_RejectsCumulativeTextOverBudget()
    {
        await WithDirectoryAsync(async directory =>
        {
            var path = Path.Combine(directory, "oversized-text.pdf");
            await BinaryDocumentTestFixtureBuilder.CreateSimplePdfAsync(
                path,
                "Scientific source text exceeds the deliberately tiny test budget.",
                CancellationToken.None);
            var extractor = new PdfPigScientificDocumentExtractor(
                new SourceIngestionBudget(maxExtractedTextCharacters: 8));

            var exception = await Assert.ThrowsAsync<InvalidDataException>(() =>
                extractor.ExtractAsync(
                    new ScientificDocumentExtractionRequest(
                        Guid.NewGuid(),
                        "sha256:test",
                        SourceAssetKind.Pdf,
                        "oversized-text.pdf",
                        string.Empty,
                        path,
                        IsScanned: false,
                        UseOcr: false,
                        ScientificReadingOrderStatus.Reliable,
                        []),
                    CancellationToken.None));

            Assert.Contains("source text", exception.Message, StringComparison.OrdinalIgnoreCase);
        });
    }

    [Fact]
    public void ArticleFigureExtraction_RejectsOversizedSourceBeforePdfParsing()
    {
        var path = Path.Combine(Path.GetTempPath(), "ContentDeliveryStudio.Tests", Guid.NewGuid().ToString("N"), "oversized.pdf");
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllBytes(path, [1, 2]);

        try
        {
            var extractor = new PdfPigArticleSourceFigureExtractor(
                new SourceIngestionBudget(maxSourceFileBytes: 1));

            var exception = Assert.Throws<InvalidDataException>(() => extractor.Extract(path));

            Assert.Contains("source file", exception.Message, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            Directory.Delete(Path.GetDirectoryName(path)!, recursive: true);
        }
    }

    [Fact]
    public void FigureBudget_RejectsPageCountAndCumulativeDecodedOutput()
    {
        var budget = new SourceIngestionBudget(
            maxPdfPageCount: 1,
            maxExtractedFigureCount: 2,
            maxEncodedFigureBytes: 8,
            maxTotalExtractedFigureBytes: 10);

        Assert.Throws<InvalidDataException>(() => budget.ValidatePdfPageCount(2));
        var state = budget.AddExtractedFigure(0, 0, 6);
        Assert.Throws<InvalidDataException>(() => budget.AddExtractedFigure(state.Count, state.TotalBytes, 5));
    }

    private static async Task WithDirectoryAsync(Func<string, Task> action)
    {
        var directory = Path.Combine(Path.GetTempPath(), "ContentDeliveryStudio.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            await action(directory);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }
}
