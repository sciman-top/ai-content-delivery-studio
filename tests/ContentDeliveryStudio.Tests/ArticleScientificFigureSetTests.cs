using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;
using ContentDeliveryStudio.Application.ScientificFigures;
using ContentDeliveryStudio.Core.ScientificFigures;
using ContentDeliveryStudio.Core.Sources;
using ContentDeliveryStudio.Infrastructure.ScientificFigures;
using SkiaSharp;

namespace ContentDeliveryStudio.Tests;

public sealed class ArticleScientificFigureSetTests
{
    [Fact]
    public async Task FigureSet_RequiresOneReviewedResultPerPlannedCandidate()
    {
        var candidates = CreateCandidates();
        var visual = new SequenceVisualReviewProvider();
        var run = await CreateService(visual).RunAsync(
            "article.pdf",
            candidates,
            CancellationToken.None);

        Assert.True(run.Complete);
        Assert.Equal(candidates.Select(item => item.CandidateId), run.RequestedCandidateIds);
        Assert.Equal(6, run.Items.Count);
        Assert.Equal(6, visual.InvocationCount);
        Assert.Equal(5, run.Items.Count(item => item.Svg is not null && item.Exports is not null));
        Assert.Single(run.Items, item => item.EvidenceBoard is not null);
        Assert.All(run.Items, item =>
        {
            Assert.True(item.PassedVisualReview);
            Assert.Equal(ArticleScientificFigureGateStatus.PendingHumanApproval, item.Candidate.GateOneStatus);
            Assert.False(string.IsNullOrWhiteSpace(item.VisualReview.ProviderTraceId));
        });
    }

    [Fact]
    public async Task PresentationDefect_TriggersOneBoundedRepairThenPasses()
    {
        var candidate = CreateCandidates().Single(item =>
            item.Kind == ArticleScientificFigureCandidateKind.Mechanism);
        var visual = new SequenceVisualReviewProvider(
            FailedReview(ScientificProviderFindingKind.VisualDefect, "label-overlap"),
            FakeScientificReviewResults.Pass("second-attempt-pass"));
        var renderer = new RecordingCandidateRenderer();
        var run = await CreateService(visual, renderer).RunAsync(
            "article.pdf",
            [candidate],
            CancellationToken.None);

        var result = Assert.Single(run.Items);
        Assert.True(run.Complete);
        Assert.Equal(2, result.PresentationAttempts);
        var repair = Assert.Single(result.Repairs);
        Assert.Contains("label-overlap", repair.Reason, StringComparison.Ordinal);
        Assert.Equal([1, 2], renderer.Attempts);
    }

    [Fact]
    public async Task ScientificMismatch_DoesNotTriggerPresentationRepair()
    {
        var candidate = CreateCandidates().Single(item =>
            item.Kind == ArticleScientificFigureCandidateKind.Mechanism);
        var visual = new SequenceVisualReviewProvider(
            FailedReview(ScientificProviderFindingKind.ScientificMismatch, "ray-direction-mismatch"));
        var renderer = new RecordingCandidateRenderer();
        var run = await CreateService(visual, renderer).RunAsync(
            "article.pdf",
            [candidate],
            CancellationToken.None);

        var result = Assert.Single(run.Items);
        Assert.False(run.Complete);
        Assert.Equal(1, result.PresentationAttempts);
        Assert.Empty(result.Repairs);
        Assert.Equal([1], renderer.Attempts);
    }

    [Fact]
    public async Task PresentationRepair_StopsAfterThreeAttempts()
    {
        var candidate = CreateCandidates().Single(item =>
            item.Kind == ArticleScientificFigureCandidateKind.Mechanism);
        var visual = new SequenceVisualReviewProvider(
            FailedReview(ScientificProviderFindingKind.VisualDefect, "persistent-overlap"),
            FailedReview(ScientificProviderFindingKind.VisualDefect, "persistent-overlap"),
            FailedReview(ScientificProviderFindingKind.VisualDefect, "persistent-overlap"));
        var renderer = new RecordingCandidateRenderer();
        var run = await CreateService(visual, renderer).RunAsync(
            "article.pdf",
            [candidate],
            CancellationToken.None);

        var result = Assert.Single(run.Items);
        Assert.False(run.Complete);
        Assert.Equal(3, result.PresentationAttempts);
        Assert.Equal(2, result.Repairs.Count);
        Assert.Equal([1, 2, 3], renderer.Attempts);
    }

    [Fact]
    public void CandidateRenderer_UsesTheArticleConjugateFunctionsAndExplicitOpticalStates()
    {
        var candidates = CreateCandidates();
        var renderer = new ArticleScientificFigureCandidateRenderer();
        var graph = renderer.Render(candidates.Single(item =>
            item.Kind == ArticleScientificFigureCandidateKind.LensEquationGraph), 1).Svg;
        var positions = renderer.Render(candidates.Single(item =>
            item.Kind == ArticleScientificFigureCandidateKind.Comparison), 1).Svg;
        var corrective = renderer.Render(candidates.Single(item =>
            item.Kind == ArticleScientificFigureCandidateKind.CorrectiveLensControl), 1).Svg;

        Assert.Contains("y = x / (x + 1)", graph, StringComparison.Ordinal);
        Assert.Contains("y = x / (1 - x)", graph, StringComparison.Ordinal);
        Assert.DoesNotContain("y = x / (x - 1)", graph, StringComparison.Ordinal);
        Assert.Contains("L2 位于 S 右侧", positions, StringComparison.Ordinal);
        Assert.Contains("L2 与 S 平面重合", positions, StringComparison.Ordinal);
        Assert.Contains("L2 位于 S 左侧", positions, StringComparison.Ordinal);
        Assert.Contains("附加凹透镜", corrective, StringComparison.Ordinal);
    }

    [Fact]
    public void Exporter_RendersEndAnchoredCandidateWatermarkInsideTheCanvas()
    {
        var candidate = CreateCandidates().Single(item =>
            item.Kind == ArticleScientificFigureCandidateKind.Mechanism);
        var svg = new ArticleScientificFigureCandidateRenderer().Render(candidate, 1);
        var exports = new ScientificFigureExporter().Export(
            new ScientificFigureExportRequest(svg, svg.Sha256, 1200, 800));
        var png = exports.Artifacts.Single(item => item.Format == "png");
        using var bitmap = SKBitmap.Decode(png.Bytes);
        Assert.NotNull(bitmap);
        var amberPixels = new List<int>();
        for (var y = 730; y < 770; y++)
        {
            for (var x = 0; x < bitmap.Width; x++)
            {
                var color = bitmap.GetPixel(x, y);
                if (color.Red > 120 && color.Green is > 45 and < 130 && color.Blue < 70)
                {
                    amberPixels.Add(x);
                }
            }
        }

        Assert.NotEmpty(amberPixels);
        Assert.True(amberPixels.Min() < 1000);
        Assert.True(amberPixels.Max() <= 1140);
    }

    [Fact]
    public async Task SamplePdf_ProducesCompleteAuditedFigureSetWhenExplicitlyRequested()
    {
        var sourcePath = Environment.GetEnvironmentVariable(
            "ARTICLE_SCIENTIFIC_FIGURE_SET_SOURCE_PATH");
        var outputDirectory = Environment.GetEnvironmentVariable(
            "ARTICLE_SCIENTIFIC_FIGURE_SET_OUTPUT_DIRECTORY");
        if (string.IsNullOrWhiteSpace(sourcePath) || string.IsNullOrWhiteSpace(outputDirectory))
        {
            return;
        }

        var sourceBytes = await File.ReadAllBytesAsync(sourcePath);
        var extraction = await new PdfPigScientificDocumentExtractor().ExtractAsync(
            new ScientificDocumentExtractionRequest(
                Guid.NewGuid(),
                Hash(sourceBytes),
                SourceAssetKind.Pdf,
                Path.GetFileNameWithoutExtension(sourcePath),
                string.Empty,
                sourcePath,
                IsScanned: false,
                UseOcr: false,
                ReadingOrder: ScientificReadingOrderStatus.Reliable,
                RequiredContent: []),
            CancellationToken.None);
        Assert.Equal(ScientificExtractionStatus.Ready, extraction.Status);
        var candidates = new ArticleScientificFigurePlanningService().Plan(
            extraction,
            Path.GetFileNameWithoutExtension(sourcePath),
            "初中物理教师与学生");
        var run = await new ArticleScientificFigureSetService(
            new PdfPigArticleSourceFigureExtractor(),
            new SkiaArticleSourceEvidenceBoardRenderer(),
            new ArticleScientificFigureCandidateRenderer(),
            new ScientificFigureExporter(),
            new FakeScientificVisualReviewProvider()).RunAsync(
                sourcePath,
                candidates,
                CancellationToken.None);

        Assert.True(run.Complete);
        Assert.Equal(8, run.SourceAudit.PageCount);
        Assert.True(run.SourceAudit.Assets.Count >= 6);
        Assert.Equal(6, run.Items.Count);
        Assert.Equal(6, run.Items.Single(item =>
            item.Candidate.Kind == ArticleScientificFigureCandidateKind.SourceEvidenceBoard)
            .EvidenceBoard!.SourceAssetIds.Count);
        await PersistRunAsync(sourcePath, extraction, run, outputDirectory);
    }

    private static async Task PersistRunAsync(
        string sourcePath,
        ScientificDocumentExtraction extraction,
        ArticleScientificFigureSetRun run,
        string outputDirectory)
    {
        Directory.CreateDirectory(outputDirectory);
        var sourceAssetsDirectory = Path.Combine(outputDirectory, "source-assets");
        Directory.CreateDirectory(sourceAssetsDirectory);
        foreach (var asset in run.SourceAudit.Assets)
        {
            await File.WriteAllBytesAsync(
                Path.Combine(sourceAssetsDirectory, $"{asset.AssetId}.png"),
                asset.PngBytes);
        }

        await WriteJsonAsync(Path.Combine(outputDirectory, "article-figure-set-plan.json"),
            run.Items.Select(item => item.Candidate));
        await WriteJsonAsync(Path.Combine(outputDirectory, "source-figure-audit.json"), new
        {
            run.SourceAudit.SourceSha256,
            run.SourceAudit.PageCount,
            assets = run.SourceAudit.Assets.Select(asset => new
            {
                asset.AssetId,
                asset.PageNumber,
                asset.PageImageIndex,
                asset.PixelWidth,
                asset.PixelHeight,
                asset.PageLeft,
                asset.PageBottom,
                asset.PageWidth,
                asset.PageHeight,
                asset.Sha256,
                fileName = $"source-assets/{asset.AssetId}.png",
            }),
        });

        var itemReports = new List<object>();
        foreach (var item in run.Items)
        {
            var prefix = Prefix(item.Candidate.Kind);
            var files = new List<string>();
            if (item.Svg is not null)
            {
                var svgName = $"{prefix}.svg";
                await File.WriteAllTextAsync(Path.Combine(outputDirectory, svgName), item.Svg.Svg);
                files.Add(svgName);
            }

            if (item.Exports is not null)
            {
                foreach (var artifact in item.Exports.Artifacts)
                {
                    var name = $"{prefix}.{artifact.Format}";
                    await File.WriteAllBytesAsync(Path.Combine(outputDirectory, name), artifact.Bytes);
                    files.Add(name);
                }
            }

            if (item.EvidenceBoard is not null)
            {
                var name = $"{prefix}.png";
                await File.WriteAllBytesAsync(
                    Path.Combine(outputDirectory, name),
                    item.EvidenceBoard.PngBytes);
                files.Add(name);
            }

            var reviewName = $"{prefix}.visual-review.json";
            await WriteJsonAsync(Path.Combine(outputDirectory, reviewName), new
            {
                item.Candidate.CandidateId,
                item.Candidate.Kind,
                item.Candidate.SourceFigureReferences,
                item.Candidate.Disposition,
                gateOneStatus = item.Candidate.GateOneStatus,
                authorityBoundary = "visual review only; no scientific Gate 1 or human acceptance",
                contractPassed = item.ContractReview.Passed,
                contractFindings = item.ContractReview.Findings,
                item.VisualReview.Verdict,
                item.VisualReview.Findings,
                item.VisualReview.ProviderTraceId,
                item.PresentationAttempts,
                item.Repairs,
            });
            files.Add(reviewName);
            itemReports.Add(new
            {
                item.Candidate.CandidateId,
                item.Candidate.Kind,
                files,
                item.PassedVisualReview,
                item.PresentationAttempts,
            });
        }

        await WriteJsonAsync(Path.Combine(outputDirectory, "article-figure-set-report.json"), new
        {
            schemaVersion = 1,
            source = new
            {
                fileName = Path.GetFileName(sourcePath),
                extraction.Status,
                extraction.SourceSha256,
                run.SourceAudit.PageCount,
                sourceAssetCount = run.SourceAudit.Assets.Count,
            },
            requestedCandidateCount = run.RequestedCandidateIds.Count,
            resultCount = run.Items.Count,
            run.Complete,
            visualReviewProvider = "fake-scientific-visual",
            visualReviewBoundary = "fake-first contract path; not a live multimodal-model or scientific-expert verdict",
            gateOneStatus = "pending for every candidate",
            gateTwoStatus = "not-run",
            deliveryStatus = "not-created",
            items = itemReports,
        });
    }

    private static ArticleScientificFigureSetService CreateService(
        IScientificVisualReviewProvider visual,
        IArticleScientificFigureCandidateRenderer? renderer = null) =>
        new(
            new FakeSourceFigureExtractor(),
            new FakeEvidenceBoardRenderer(),
            renderer ?? new ArticleScientificFigureCandidateRenderer(),
            new ScientificFigureExporter(),
            visual);

    private static IReadOnlyList<ArticleScientificFigureCandidate> CreateCandidates()
    {
        var textByPage = new[]
        {
            (1, "二次凸透镜成像把中间像作为眼睛的物体。"),
            (2, "眼睛在像 S 的右方、重合或左方时，入眼光束状态不同。"),
            (3, "函数图象用于讨论负物距与共轭关系。"),
            (4, "实验比较光屏和代表视网膜的固定接收面。"),
            (6, "验证实验照片包括图6至图9，近视眼镜作为附加透镜。"),
        };
        var blocks = textByPage.Select((item, index) => ScientificSourceBlock.Create(
            $"block-{index + 1}",
            ScientificSourceBlockKind.Paragraph,
            ScientificSourceLocation.Create(
                item.Item1,
                $"page {item.Item1}",
                boundingRegion: null,
                ScientificCharacterRange.Create(0, item.Item2.Length)),
            item.Item2,
            isRequired: true,
            ScientificRecoveryStatus.NotRequired)).ToArray();
        var extraction = ScientificDocumentExtraction.Create(
            Guid.NewGuid(),
            new string('a', 64).Insert(0, "sha256:"),
            ScientificExtractorIdentity.Create("test", "1"),
            ScientificExtractionQuality.Create(
                isScanned: false,
                ocrApplied: false,
                ScientificReadingOrderStatus.Reliable,
                ScientificRequiredContentStatus.Complete),
            blocks,
            []);
        return new ArticleScientificFigurePlanningService().Plan(
            extraction,
            "眼睛直接观察凸透镜成像时的各种问题",
            "初中物理教师与学生");
    }

    private static ScientificProviderReviewResult FailedReview(
        ScientificProviderFindingKind kind,
        string code) =>
        new(
            ScientificReviewVerdict.Fail,
            [new ScientificProviderFinding(code, kind, "candidate-full-frame", code)],
            $"trace-{code}");

    private static string Prefix(ArticleScientificFigureCandidateKind kind) => kind switch
    {
        ArticleScientificFigureCandidateKind.Mechanism => "01-secondary-imaging",
        ArticleScientificFigureCandidateKind.LensEquationGraph => "02-lens-equation",
        ArticleScientificFigureCandidateKind.ExperimentalComparison => "03-screen-retina",
        ArticleScientificFigureCandidateKind.Comparison => "04-observation-position",
        ArticleScientificFigureCandidateKind.CorrectiveLensControl => "05-corrective-lens",
        ArticleScientificFigureCandidateKind.SourceEvidenceBoard => "06-source-evidence-board",
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null),
    };

    private static Task WriteJsonAsync(string path, object value) =>
        File.WriteAllTextAsync(path, JsonSerializer.Serialize(value, new JsonSerializerOptions
        {
            WriteIndented = true,
            Converters = { new JsonStringEnumConverter() },
        }));

    private static string Hash(byte[] bytes) =>
        $"sha256:{Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant()}";

    private sealed class FakeSourceFigureExtractor : IArticleSourceFigureExtractor
    {
        public ArticleSourceFigureAudit Extract(string sourcePdfPath) => new(
            new string('b', 64).Insert(0, "sha256:"),
            8,
            [new ArticleSourceFigureAsset(
                "page-6-image-1",
                6,
                1,
                800,
                600,
                10,
                10,
                400,
                300,
                new string('c', 64).Insert(0, "sha256:"),
                [1, 2, 3])]);
    }

    private sealed class FakeEvidenceBoardRenderer : IArticleSourceEvidenceBoardRenderer
    {
        public ArticleSourceEvidenceBoard Render(ArticleSourceFigureAudit audit)
        {
            byte[] bytes = [137, 80, 78, 71];
            return new ArticleSourceEvidenceBoard(
                bytes,
                Hash(bytes),
                1600,
                1200,
                [audit.Assets[0].AssetId]);
        }
    }

    private sealed class RecordingCandidateRenderer : IArticleScientificFigureCandidateRenderer
    {
        private readonly ArticleScientificFigureCandidateRenderer _inner = new();

        public List<int> Attempts { get; } = [];

        public ScientificSvgArtifact Render(
            ArticleScientificFigureCandidate candidate,
            int presentationAttempt)
        {
            Attempts.Add(presentationAttempt);
            return _inner.Render(candidate, presentationAttempt);
        }
    }

    private sealed class SequenceVisualReviewProvider : IScientificVisualReviewProvider
    {
        private readonly Queue<ScientificProviderReviewResult> _results;
        private ScientificProviderReviewResult _last;

        public SequenceVisualReviewProvider(params ScientificProviderReviewResult[] results)
        {
            _results = new Queue<ScientificProviderReviewResult>(results);
            _last = results.LastOrDefault()
                ?? FakeScientificReviewResults.Pass("fake-figure-set-pass");
        }

        public int InvocationCount { get; private set; }

        public Task<ScientificProviderReviewResult> ReviewAsync(
            ScientificVisualReviewRequest request,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            InvocationCount++;
            if (_results.Count > 0)
            {
                _last = _results.Dequeue();
            }

            return Task.FromResult(_last);
        }
    }
}
