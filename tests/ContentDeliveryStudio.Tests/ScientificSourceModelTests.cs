using ContentDeliveryStudio.Core.ScientificFigures;

namespace ContentDeliveryStudio.Tests;

public sealed class ScientificSourceModelTests
{
    private const string SourceHash =
        "sha256:a3f75487411ef13d0270c65fc801ceff2b28e6b339afed9b407fe477f7e8453e";

    [Fact]
    public void ScientificDocumentExtraction_PreservesSourceAuthorityAndLocations()
    {
        var sourceAssetId = Guid.NewGuid();
        var location = ScientificSourceLocation.Create(
            pageNumber: 12,
            section: "3.2 Energy balance",
            ScientificBoundingRegion.Create(72, 144, 320, 48),
            ScientificCharacterRange.Create(120, 168));
        var block = ScientificSourceBlock.Create(
            "block-energy-balance",
            ScientificSourceBlockKind.Paragraph,
            location,
            "For a complete cycle, the internal energy returns to its initial value.",
            isRequired: true,
            ScientificRecoveryStatus.NotRequired);
        var diagnostic = ScientificExtractionDiagnostic.Create(
            "layout-columns-detected",
            ScientificDiagnosticSeverity.Information,
            "Two columns were detected and normalized into stable reading order.");

        var extraction = ScientificDocumentExtraction.Create(
            sourceAssetId,
            SourceHash,
            ScientificExtractorIdentity.Create("pdftotext", "24.04.0"),
            ScientificExtractionQuality.Create(
                isScanned: false,
                ocrApplied: false,
                ScientificReadingOrderStatus.Reliable,
                ScientificRequiredContentStatus.Complete),
            [block],
            [diagnostic]);

        Assert.Equal(sourceAssetId, extraction.SourceAssetId);
        Assert.Equal(SourceHash, extraction.SourceSha256);
        Assert.Equal("pdftotext", extraction.Extractor.ProviderId);
        Assert.Equal("24.04.0", extraction.Extractor.Version);
        Assert.Equal(ScientificExtractionStatus.Ready, extraction.Status);
        Assert.Empty(extraction.BlockingCodes);

        var persistedBlock = Assert.Single(extraction.Blocks);
        Assert.Equal("block-energy-balance", persistedBlock.BlockId);
        Assert.Equal(block.OriginalText, persistedBlock.OriginalText);
        Assert.Equal(12, persistedBlock.Location.PageNumber);
        Assert.Equal("3.2 Energy balance", persistedBlock.Location.Section);
        Assert.Equal(72, persistedBlock.Location.BoundingRegion!.X);
        Assert.Equal(120, persistedBlock.Location.CharacterRange!.StartOffset);
        Assert.Equal(168, persistedBlock.Location.CharacterRange.EndOffset);
        Assert.Equal("layout-columns-detected", Assert.Single(extraction.Diagnostics).Code);
    }

    [Theory]
    [InlineData("")]
    [InlineData("abc123")]
    [InlineData("sha256:not-a-hash")]
    public void ScientificDocumentExtraction_RejectsMissingOrInvalidSourceHash(string sourceHash)
    {
        Assert.Throws<ArgumentException>(() =>
            ScientificDocumentExtraction.Create(
                Guid.NewGuid(),
                sourceHash,
                ScientificExtractorIdentity.Create("extractor", "1.0"),
                ReliableQuality(),
                [ParagraphBlock()],
                []));
    }

    [Fact]
    public void ScientificSourceRecords_RejectInvalidRangesAndUndefinedRecovery()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            ScientificCharacterRange.Create(8, 7));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            ScientificBoundingRegion.Create(0, 0, width: 0, height: 10));
        Assert.Throws<ArgumentException>(() =>
            ScientificSourceLocation.Create(
                pageNumber: 1,
                section: "Introduction",
                boundingRegion: null,
                characterRange: null));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            ScientificSourceBlock.Create(
                "formula",
                ScientificSourceBlockKind.Formula,
                ValidLocation(),
                "E = mc^2",
                isRequired: true,
                (ScientificRecoveryStatus)999));
    }

    [Theory]
    [InlineData(true, false, ScientificReadingOrderStatus.Reliable, ScientificRequiredContentStatus.Complete, "scanned-without-ocr")]
    [InlineData(false, false, ScientificReadingOrderStatus.Corrupted, ScientificRequiredContentStatus.Complete, "corrupted-reading-order")]
    [InlineData(false, false, ScientificReadingOrderStatus.Reliable, ScientificRequiredContentStatus.Missing, "missing-required-content")]
    public void ScientificDocumentExtraction_ProducesExplicitBlockedOutcomes(
        bool isScanned,
        bool ocrApplied,
        ScientificReadingOrderStatus readingOrder,
        ScientificRequiredContentStatus requiredContent,
        string expectedCode)
    {
        var extraction = ScientificDocumentExtraction.Create(
            Guid.NewGuid(),
            SourceHash,
            ScientificExtractorIdentity.Create("bounded-extractor", "1.0"),
            ScientificExtractionQuality.Create(
                isScanned,
                ocrApplied,
                readingOrder,
                requiredContent),
            [ParagraphBlock()],
            []);

        Assert.Equal(ScientificExtractionStatus.Blocked, extraction.Status);
        Assert.Contains(expectedCode, extraction.BlockingCodes);
    }

    [Theory]
    [InlineData(ScientificSourceBlockKind.Formula, ScientificRecoveryStatus.Missing, "required-formula-missing")]
    [InlineData(ScientificSourceBlockKind.Table, ScientificRecoveryStatus.Uncertain, "required-table-uncertain")]
    public void ScientificDocumentExtraction_BlocksRequiredFormulaOrTableRecovery(
        ScientificSourceBlockKind kind,
        ScientificRecoveryStatus recoveryStatus,
        string expectedCode)
    {
        var block = ScientificSourceBlock.Create(
            $"required-{kind.ToString().ToLowerInvariant()}",
            kind,
            ValidLocation(),
            originalText: null,
            isRequired: true,
            recoveryStatus);

        var extraction = ScientificDocumentExtraction.Create(
            Guid.NewGuid(),
            SourceHash,
            ScientificExtractorIdentity.Create("bounded-extractor", "1.0"),
            ReliableQuality(),
            [block],
            []);

        Assert.Equal(ScientificExtractionStatus.Blocked, extraction.Status);
        Assert.Contains(expectedCode, extraction.BlockingCodes);
    }

    [Fact]
    public void ScientificDocumentExtraction_PreservesExplicitBlockingDiagnostics()
    {
        var extraction = ScientificDocumentExtraction.Create(
            Guid.NewGuid(),
            SourceHash,
            ScientificExtractorIdentity.Create("bounded-extractor", "1.0"),
            ReliableQuality(),
            [ParagraphBlock()],
            [
                ScientificExtractionDiagnostic.Create(
                    "encoding-loss",
                    ScientificDiagnosticSeverity.Blocking,
                    "Required mathematical symbols were lost during extraction."),
            ]);

        Assert.Equal(ScientificExtractionStatus.Blocked, extraction.Status);
        Assert.Contains("encoding-loss", extraction.BlockingCodes);
    }

    [Fact]
    public void ScientificDocumentExtraction_ExposesImmutableSnapshots()
    {
        var blocks = new List<ScientificSourceBlock> { ParagraphBlock() };
        var diagnostics = new List<ScientificExtractionDiagnostic>
        {
            ScientificExtractionDiagnostic.Create(
                "layout-reviewed",
                ScientificDiagnosticSeverity.Information,
                "Layout was reviewed."),
        };
        var extraction = ScientificDocumentExtraction.Create(
            Guid.NewGuid(),
            SourceHash,
            ScientificExtractorIdentity.Create("bounded-extractor", "1.0"),
            ReliableQuality(),
            blocks,
            diagnostics);

        blocks.Clear();
        diagnostics.Clear();

        Assert.Single(extraction.Blocks);
        Assert.Single(extraction.Diagnostics);
        var exposedBlocks = Assert.IsAssignableFrom<IList<ScientificSourceBlock>>(extraction.Blocks);
        var exposedDiagnostics =
            Assert.IsAssignableFrom<IList<ScientificExtractionDiagnostic>>(extraction.Diagnostics);
        Assert.Throws<NotSupportedException>(() => exposedBlocks[0] = ParagraphBlock());
        Assert.Throws<NotSupportedException>(() =>
            exposedDiagnostics[0] = ScientificExtractionDiagnostic.Create(
                "replacement",
                ScientificDiagnosticSeverity.Warning,
                "Replacement must be rejected."));
    }

    private static ScientificExtractionQuality ReliableQuality()
    {
        return ScientificExtractionQuality.Create(
            isScanned: false,
            ocrApplied: false,
            ScientificReadingOrderStatus.Reliable,
            ScientificRequiredContentStatus.Complete);
    }

    private static ScientificSourceBlock ParagraphBlock()
    {
        return ScientificSourceBlock.Create(
            "paragraph-1",
            ScientificSourceBlockKind.Paragraph,
            ValidLocation(),
            "A required source paragraph.",
            isRequired: true,
            ScientificRecoveryStatus.NotRequired);
    }

    private static ScientificSourceLocation ValidLocation()
    {
        return ScientificSourceLocation.Create(
            pageNumber: 1,
            section: "Introduction",
            boundingRegion: null,
            ScientificCharacterRange.Create(0, 28));
    }
}
