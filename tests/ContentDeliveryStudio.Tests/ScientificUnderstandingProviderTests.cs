using ContentDeliveryStudio.Application.ScientificFigures;
using ContentDeliveryStudio.Core.ScientificFigures;
using ContentDeliveryStudio.Core.Sources;
using ContentDeliveryStudio.Infrastructure.ScientificFigures;

namespace ContentDeliveryStudio.Tests;

public sealed class ScientificUnderstandingProviderTests
{
    [Fact]
    public async Task FakeProvider_ProducesExactEvidenceLinksWithoutNetwork()
    {
        var extraction = Extraction(
            ("block-force", "Net force causes acceleration for constant mass."));
        var provider = new FakeScientificUnderstandingProvider();

        var result = await provider.AnalyzeChunkAsync(
            new ScientificUnderstandingChunkRequest(
                extraction,
                "Explain the mechanism.",
                ChunkIndex: 0,
                ChunkCount: 1,
                extraction.Blocks),
            CancellationToken.None);

        var claim = Assert.Single(result.Claims);
        Assert.Equal("block-force", claim.SourceBlockId);
        Assert.Equal(
            "Net force causes acceleration for constant mass.",
            claim.QuotedText);
        Assert.Equal(ScientificClaimCategory.CausalRelation, claim.Category);
        Assert.Equal(1, provider.InvocationCount);
        Assert.Equal("fake-scientific-understanding", result.ProviderTraceId);
    }

    [Fact]
    public async Task ApplicationService_ChunksOverlongSourcesWithinPolicy()
    {
        var extraction = Extraction(
            ("block-1", "Force changes motion."),
            ("block-2", "Mass constrains acceleration."),
            ("block-3", "Direction follows net force."));
        var provider = new FakeScientificUnderstandingProvider();
        var service = new ScientificFigureApplicationService(
            provider,
            new InMemoryScientificFigureWorkflowRepository(),
            new ScientificUnderstandingChunkPolicy(
                MaxBlocksPerChunk: 1,
                MaxCharactersPerChunk: 200));

        var draft = await service.CreateDraftAsync(
            Guid.NewGuid(),
            extraction,
            DraftRequest(),
            DateTimeOffset.Parse("2026-07-26T14:00:00Z"),
            CancellationToken.None);

        Assert.Equal(3, provider.InvocationCount);
        Assert.Equal(3, draft.Understanding.Claims.Count);
        Assert.Equal(ScientificUnderstandingStatus.ReadyForApproval, draft.Understanding.Status);
        Assert.Null(draft.Workflow.Gate1Approval);
    }

    [Fact]
    public async Task ApplicationService_ReportsCrossChunkConflictAndBlocksGate1()
    {
        var extraction = Extraction(
            ("block-a", "CLAIM[direction]: Acceleration follows the net force."),
            ("block-b", "CLAIM[direction]: Acceleration opposes the net force."));
        var repository = new InMemoryScientificFigureWorkflowRepository();
        var service = new ScientificFigureApplicationService(
            new FakeScientificUnderstandingProvider(),
            repository,
            new ScientificUnderstandingChunkPolicy(1, 200));
        var draft = await service.CreateDraftAsync(
            Guid.NewGuid(),
            extraction,
            DraftRequest(),
            DateTimeOffset.Parse("2026-07-26T14:00:00Z"),
            CancellationToken.None);

        var conflict = Assert.Single(draft.Understanding.Conflicts);
        Assert.Equal(ScientificConflictStatus.Unresolved, conflict.Status);
        Assert.Equal(ScientificUnderstandingStatus.Blocked, draft.Understanding.Status);
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.DecideGateOneAsync(
                draft.Id,
                new ScientificGateOneDecision(
                    Approved: true,
                    "human-reviewer",
                    "Conflict is unresolved.",
                    DateTimeOffset.Parse("2026-07-26T14:10:00Z")),
                CancellationToken.None));
    }

    [Fact]
    public async Task CheckpointOne_TextBearingPdfReachesReviewableDraft()
    {
        var directory = Path.Combine(
            Path.GetTempPath(),
            "ContentDeliveryStudio.Tests",
            Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            var pdfPath = Path.Combine(directory, "checkpoint-one.pdf");
            await BinaryDocumentTestFixtureBuilder.CreateSimplePdfAsync(
                pdfPath,
                "Net force causes acceleration for constant mass.",
                CancellationToken.None);
            var extraction = await new PdfPigScientificDocumentExtractor().ExtractAsync(
                new ScientificDocumentExtractionRequest(
                    Guid.NewGuid(),
                    "sha256:a3f75487411ef13d0270c65fc801ceff2b28e6b339afed9b407fe477f7e8453e",
                    SourceAssetKind.Pdf,
                    "checkpoint-one.pdf",
                    SourceText: string.Empty,
                    OriginalPath: pdfPath,
                    IsScanned: false,
                    UseOcr: false,
                    ScientificReadingOrderStatus.Reliable,
                    RequiredContent: []),
                CancellationToken.None);
            var service = new ScientificFigureApplicationService(
                new FakeScientificUnderstandingProvider(),
                new InMemoryScientificFigureWorkflowRepository());

            var draft = await service.CreateDraftAsync(
                Guid.NewGuid(),
                extraction,
                DraftRequest(),
                DateTimeOffset.Parse("2026-07-26T14:00:00Z"),
                CancellationToken.None);

            Assert.Equal(ScientificUnderstandingStatus.ReadyForApproval, draft.Understanding.Status);
            Assert.Equal(ScientificFigureSpecStatus.ReadyForGate1, draft.Workflow.Specification.Status);
            Assert.Null(draft.Workflow.Gate1Approval);
        }
        finally
        {
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, recursive: true);
            }
        }
    }

    internal static ScientificDocumentExtraction Extraction(
        params (string Id, string Text)[] blocks)
    {
        var offset = 0;
        var sourceBlocks = blocks.Select(item =>
        {
            var block = ScientificSourceBlock.Create(
                item.Id,
                ScientificSourceBlockKind.Paragraph,
                ScientificSourceLocation.Create(
                    1,
                    item.Id,
                    boundingRegion: null,
                    ScientificCharacterRange.Create(offset, offset + item.Text.Length)),
                item.Text,
                isRequired: true,
                ScientificRecoveryStatus.NotRequired);
            offset += item.Text.Length;
            return block;
        }).ToArray();
        return ScientificDocumentExtraction.Create(
            Guid.NewGuid(),
            "sha256:a3f75487411ef13d0270c65fc801ceff2b28e6b339afed9b407fe477f7e8453e",
            ScientificExtractorIdentity.Create("fixture", "1.0"),
            ScientificExtractionQuality.Create(
                false,
                false,
                ScientificReadingOrderStatus.Reliable,
                ScientificRequiredContentStatus.Complete),
            sourceBlocks,
            []);
    }

    internal static ScientificFigureDraftRequest DraftRequest()
    {
        return new ScientificFigureDraftRequest(
            "Explain the source mechanism.",
            "The source claims define the mechanism.",
            "Scientific readers",
            IsSchematic: true,
            ScientificFigureRiskLevel.Medium);
    }
}
