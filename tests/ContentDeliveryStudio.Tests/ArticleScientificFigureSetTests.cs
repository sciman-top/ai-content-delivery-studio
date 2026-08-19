using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Xml.Linq;
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
            Assert.True(item.DeterministicScientificReview.Passed);
            Assert.NotEmpty(item.VisualReviewRequest.RegionCrops);
            Assert.All(item.VisualReviewRequest.RegionCrops, crop => Assert.NotNull(crop.ExpectedCheck));
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
    public async Task TextClipping_IsPresentationOnlyAndTriggersBoundedRepair()
    {
        var candidate = CreateCandidates().Single(item =>
            item.Kind == ArticleScientificFigureCandidateKind.LensEquationGraph);
        var visual = new SequenceVisualReviewProvider(
            FailedReview(ScientificProviderFindingKind.VisualDefect, "text-clipped"),
            FakeScientificReviewResults.Pass("text-clipping-repaired"));

        var run = await CreateService(visual).RunAsync(
            "article.pdf",
            [candidate],
            CancellationToken.None);

        var result = Assert.Single(run.Items);
        Assert.True(run.Complete);
        Assert.Equal(2, result.PresentationAttempts);
        Assert.Contains(result.Repairs, repair => repair.Reason.Contains("text-clipped", StringComparison.Ordinal));
        Assert.True(result.DeterministicScientificReview.Passed);
    }

    [Theory]
    [InlineData(ArticleScientificFigureCandidateKind.LensEquationGraph, "wrong-formula", "optics-virtual-object-branch-invalid")]
    [InlineData(ArticleScientificFigureCandidateKind.CorrectiveLensControl, "swap-lens", "optics-corrective-lens-type-invalid")]
    [InlineData(ArticleScientificFigureCandidateKind.CorrectiveLensControl, "same-focus", "optics-focus-shift-not-represented")]
    [InlineData(ArticleScientificFigureCandidateKind.Mechanism, "reverse-ray", "optics-ray-direction-reversed")]
    [InlineData(ArticleScientificFigureCandidateKind.Comparison, "swap-plane-order", "optics-plane-order-right-invalid")]
    public void DeterministicOpticsReview_BlocksScientificMutations(
        ArticleScientificFigureCandidateKind kind,
        string mutation,
        string expectedCode)
    {
        var candidate = CreateCandidates().Single(item => item.Kind == kind);
        var artifact = new ArticleScientificFigureCandidateRenderer().Render(candidate, 1);
        var mutated = Mutate(artifact.Svg, mutation);
        var review = new ArticleOpticalScientificReviewer().Review(
            candidate,
            artifact with
            {
                Svg = mutated,
                Sha256 = Hash(Encoding.UTF8.GetBytes(mutated)),
            },
            new FakeSourceFigureExtractor().Extract("article.pdf"),
            board: null);

        Assert.False(review.Passed);
        Assert.Contains(review.Findings, finding => finding.Code == expectedCode);
    }

    [Fact]
    public void DeterministicOpticsReview_BlocksMissingSourcePhoto()
    {
        var candidate = CreateCandidates().Single(item =>
            item.Kind == ArticleScientificFigureCandidateKind.SourceEvidenceBoard);
        var audit = new FakeSourceFigureExtractor().Extract("article.pdf");
        var board = new FakeEvidenceBoardRenderer().Render(audit) with
        {
            SourceAssetIds = audit.Assets.Take(5).Select(item => item.AssetId).ToArray(),
        };

        var review = new ArticleOpticalScientificReviewer().Review(
            candidate,
            artifact: null,
            audit,
            board);

        Assert.False(review.Passed);
        Assert.Contains(review.Findings, finding => finding.Code == "optics-source-photo-coverage-invalid");
    }

    [Theory]
    [InlineData(ArticleScientificFigureCandidateKind.ThermalTransferModes, "人体红外散热", "人体红外：可忽略", "thermal-radiation-negligible-overclaim")]
    [InlineData(ArticleScientificFigureCandidateKind.ThermalHumidityClothing, "高相对湿度使衣物保温性下降", "融雪时的湿度上升使衣物保温性下降", "thermal-snowmelt-humidity-overclaim")]
    public void DeterministicThermalReview_BlocksScientificOverclaims(
        ArticleScientificFigureCandidateKind kind,
        string currentText,
        string overclaim,
        string expectedCode)
    {
        var candidate = CreateThermalCandidate(kind);
        var artifact = new ArticleScientificFigureCandidateRenderer().Render(candidate, 1);
        var mutatedSvg = artifact.Svg.Replace(currentText, overclaim, StringComparison.Ordinal);
        var review = new ArticleThermalScientificReviewer().Review(
            candidate,
            artifact with
            {
                Svg = mutatedSvg,
                Sha256 = Hash(Encoding.UTF8.GetBytes(mutatedSvg)),
            },
            new FakeSourceFigureExtractor().Extract("article.pdf"),
            board: null);

        Assert.False(review.Passed);
        Assert.Contains(review.Findings, finding => finding.Code == expectedCode);
    }

    [Fact]
    public void EvidenceBoardRenderer_UsesContentHeightForSixPhotographs()
    {
        var assets = Enumerable.Range(1, 6).Select(index =>
        {
            var png = CreatePhotographicPng(index);
            return new ArticleSourceFigureAsset(
                $"source-photo-{index}",
                PageNumber: index,
                PageImageIndex: 1,
                PixelWidth: 800,
                PixelHeight: 600,
                PageLeft: 10,
                PageBottom: 10,
                PageWidth: 400,
                PageHeight: 300,
                Hash(png),
                png);
        }).ToArray();
        var audit = new ArticleSourceFigureAudit(
            $"sha256:{new string('a', 64)}",
            PageCount: 6,
            assets);

        var board = new SkiaArticleSourceEvidenceBoardRenderer().Render(audit);

        Assert.Equal(1600, board.PixelWidth);
        Assert.Equal(636, board.PixelHeight);
        Assert.Equal(assets.Select(asset => asset.AssetId), board.SourceAssetIds);
        using var bitmap = SKBitmap.Decode(board.PngBytes);
        Assert.NotNull(bitmap);
        Assert.Equal(board.PixelWidth, bitmap.Width);
        Assert.Equal(board.PixelHeight, bitmap.Height);
    }

    [Fact]
    public void CandidateRenderer_OmitsWorkflowAnnotationsFromVisibleSvg()
    {
        foreach (var candidate in CreateCandidates().Where(item =>
                     item.Kind != ArticleScientificFigureCandidateKind.SourceEvidenceBoard))
        {
            var svg = new ArticleScientificFigureCandidateRenderer().Render(candidate, 1).Svg;

            Assert.Contains("gate1=pending", svg, StringComparison.Ordinal);
            Assert.DoesNotContain(candidate.ReplacementRationale, svg, StringComparison.Ordinal);
            Assert.DoesNotContain("替代/解释来源", svg, StringComparison.Ordinal);
            Assert.DoesNotContain("候选图 | 非按比例", svg, StringComparison.Ordinal);
            Assert.DoesNotContain("不声明清晰度、正倒或生理结论", svg, StringComparison.Ordinal);
            Assert.DoesNotContain("两种装置不是同一个观察条件", svg, StringComparison.Ordinal);
            Assert.DoesNotContain("文章中的 f≈2 cm 示例不自动作为人眼常数", svg, StringComparison.Ordinal);
            Assert.DoesNotContain("能否看见、清晰度与视觉正倒属于待核验主张", svg, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void VisualContract_RejectsWorkflowAnnotationsInPublicationArtwork()
    {
        var candidate = CreateCandidates().Single(item =>
            item.Kind == ArticleScientificFigureCandidateKind.Mechanism);
        var renderer = new ArticleScientificFigureCandidateRenderer();

        foreach (var annotation in new[]
                 {
                     candidate.ReplacementRationale,
                     "替代/解释来源：图1、图2",
                     "候选图 | 非按比例",
                     "仅供 Gate 1 科学核验",
                 })
        {
            var artifact = renderer.Render(candidate, 1);
            var document = XDocument.Parse(artifact.Svg);
            XNamespace svg = "http://www.w3.org/2000/svg";
            document.Descendants(svg + "g").Single(element =>
                element.Attribute("data-element-kind") is not null).Add(new XElement(
                svg + "text",
                new XAttribute("x", 100),
                new XAttribute("y", 700),
                new XAttribute("data-content-kind", "annotation"),
                annotation));
            var mutatedSvg = document.ToString(SaveOptions.DisableFormatting);
            var mutatedArtifact = artifact with
            {
                Svg = mutatedSvg,
                Sha256 = Hash(Encoding.UTF8.GetBytes(mutatedSvg)),
            };
            var exports = new ScientificFigureExporter().Export(
                new ScientificFigureExportRequest(
                    mutatedArtifact,
                    mutatedArtifact.Sha256,
                    1200,
                    800));

            var report = new ArticleCandidateVisualContractReviewer().Review(
                candidate,
                mutatedArtifact,
                exports);

            Assert.Contains(report.Findings, finding =>
                finding.Code == "candidate-workflow-annotation-visible");
        }
    }

    [Fact]
    public async Task ThermalCjkPdf_UsesBoundedFontRepresentation()
    {
        var evidenceText = "导热系数 λ W/(m·K)，空气0.02，水蒸气0.02，水0.6，棉毛0.05。";
        var evidence = ArticleScientificFigureEvidence.Create(
            ScientificSourceBlock.Create(
                "thermal-conductivity-source",
                ScientificSourceBlockKind.Table,
                ScientificSourceLocation.Create(
                    2,
                    "conductivity table",
                    boundingRegion: null,
                    ScientificCharacterRange.Create(0, evidenceText.Length)),
                evidenceText,
                isRequired: true,
                ScientificRecoveryStatus.Recovered),
            excerptLength: 240);
        var candidate = new ArticleScientificFigureCandidate(
            "thermal-conductivity-test",
            "下雪不冷，融雪冷",
            ArticleScientificFigureCandidateKind.ThermalConductivityComparison,
            "空气、水蒸气、液态水与棉毛的导热系数比较",
            "比较导热系数",
            "保留原文数据和单位。",
            "教师与学生",
            ScientificFigureRiskLevel.High,
            [evidence],
            ["表1"],
            ArticleScientificFigureDisposition.ReplaceExisting,
            "重绘原文小表格。",
            RequiresGateOneApproval: true,
            GateOneStatus: ArticleScientificFigureGateStatus.PendingHumanApproval,
            DeliveryStatus: ArticleScientificFigureDeliveryStatus.NotCreated);
        var service = new ArticleScientificFigureSetService(
            new FakeSourceFigureExtractor(),
            new FakeEvidenceBoardRenderer(),
            new ArticleScientificFigureCandidateRenderer(),
            new ScientificFigureExporter(),
            new FakeScientificVisualReviewProvider(),
            new SkiaScientificReviewImageCropper(),
            scientificReviewer: new ArticleThermalScientificReviewer());

        var run = await service.RunAsync("article.pdf", [candidate], CancellationToken.None);

        var result = Assert.Single(run.Items);
        Assert.True(result.PassedVisualReview);
        var pdf = Assert.Single(result.Exports!.Artifacts, item => item.Format == "pdf");
        Assert.InRange(pdf.Bytes.Length, 1, 1_000_000);
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
        var scientificReviewer = candidates.Any(item =>
                item.Kind is ArticleScientificFigureCandidateKind.ThermalFrontMechanism
                    or ArticleScientificFigureCandidateKind.ThermalBasinException
                    or ArticleScientificFigureCandidateKind.ThermalConductivityComparison
                    or ArticleScientificFigureCandidateKind.ThermalTransferModes
                    or ArticleScientificFigureCandidateKind.ThermalHumidityClothing
                    or ArticleScientificFigureCandidateKind.ThermalDryWetHeat)
            ? new ArticleThermalScientificReviewer() as IArticleScientificFigureReviewer
            : new ArticleOpticalScientificReviewer();
        var run = await new ArticleScientificFigureSetService(
            new PdfPigArticleSourceFigureExtractor(),
            new SkiaArticleSourceEvidenceBoardRenderer(),
            new ArticleScientificFigureCandidateRenderer(),
            new ScientificFigureExporter(),
            new FakeScientificVisualReviewProvider(),
            new SkiaScientificReviewImageCropper(),
            scientificReviewer: scientificReviewer).RunAsync(
                sourcePath,
                candidates,
                CancellationToken.None);

        Assert.True(run.Complete, string.Join(Environment.NewLine, run.Items
            .Where(item => !item.PassedVisualReview)
            .Select(item => $"{item.Candidate.Kind}: contract=[{string.Join(',', item.ContractReview.Findings.Select(f => f.Code))}] "
                + $"science=[{string.Join(',', item.DeterministicScientificReview.Findings.Select(f => f.Code))}] "
                + $"visual=[{string.Join(',', item.VisualReview.Findings.Select(f => f.Code))}]")));
        Assert.Equal(extraction.Blocks.Select(block => block.Location.PageNumber).Distinct().Count(), run.SourceAudit.PageCount);
        Assert.NotEmpty(run.SourceAudit.Assets);
        Assert.Equal(candidates.Count, run.Items.Count);
        Assert.NotEmpty(run.Items.Single(item =>
            item.Candidate.Kind == ArticleScientificFigureCandidateKind.SourceEvidenceBoard)
            .EvidenceBoard!.SourceAssetIds);
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
            var prefix = Prefix(item.Candidate);
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
                deterministicScientificPassed = item.DeterministicScientificReview.Passed,
                deterministicScientificPackage = item.DeterministicScientificReview.PackageId,
                deterministicScientificAuthority = item.DeterministicScientificReview.AuthorityBoundary,
                deterministicScientificFindings = item.DeterministicScientificReview.Findings,
                expectedVisualChecks = item.DeterministicScientificReview.ExpectedVisualChecks,
                typedCrops = item.VisualReviewRequest.RegionCrops.Select(crop => new
                {
                    crop.CropId,
                    crop.Kind,
                    crop.ResponsibleItemId,
                    crop.X,
                    crop.Y,
                    crop.Width,
                    crop.Height,
                    crop.ExpectedCheck,
                }),
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
            deterministicReview = run.Items.Select(item => item.DeterministicScientificReview.PackageId).Distinct().Single(),
            deterministicReviewBoundary = "machine-checkable domain invariants; not human Gate 1",
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
            visual,
            new SkiaScientificReviewImageCropper());

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

    private static string Prefix(ArticleScientificFigureCandidate candidate) => candidate.Kind switch
    {
        ArticleScientificFigureCandidateKind.Mechanism => "01-secondary-imaging",
        ArticleScientificFigureCandidateKind.LensEquationGraph => "02-lens-equation",
        ArticleScientificFigureCandidateKind.ExperimentalComparison => "03-screen-retina",
        ArticleScientificFigureCandidateKind.Comparison => "04-observation-position",
        ArticleScientificFigureCandidateKind.CorrectiveLensControl => "05-corrective-lens",
        ArticleScientificFigureCandidateKind.SourceEvidenceBoard => candidate.ArticleTitle.Contains("下雪", StringComparison.Ordinal)
            ? "07-source-evidence-board"
            : "06-source-evidence-board",
        ArticleScientificFigureCandidateKind.ThermalFrontMechanism => "01-thermal-snow-front",
        ArticleScientificFigureCandidateKind.ThermalBasinException => "02-thermal-basin-exception",
        ArticleScientificFigureCandidateKind.ThermalConductivityComparison => "03-thermal-conductivity",
        ArticleScientificFigureCandidateKind.ThermalTransferModes => "04-thermal-transfer-modes",
        ArticleScientificFigureCandidateKind.ThermalHumidityClothing => "05-thermal-humidity-clothing",
        ArticleScientificFigureCandidateKind.ThermalDryWetHeat => "06-thermal-dry-wet-heat",
        _ => throw new ArgumentOutOfRangeException(nameof(candidate), candidate.Kind, null),
    };

    private static ArticleScientificFigureCandidate CreateThermalCandidate(
        ArticleScientificFigureCandidateKind kind)
    {
        const string evidenceText = "相对湿度、传热方式与人体散热的文章证据。";
        var evidence = ArticleScientificFigureEvidence.Create(
            ScientificSourceBlock.Create(
                "thermal-source",
                ScientificSourceBlockKind.Paragraph,
                ScientificSourceLocation.Create(
                    3,
                    "thermal paragraph",
                    boundingRegion: null,
                    ScientificCharacterRange.Create(0, evidenceText.Length)),
                evidenceText,
                isRequired: true,
                ScientificRecoveryStatus.NotRequired),
            excerptLength: 240);
        return new ArticleScientificFigureCandidate(
            $"thermal-{kind}",
            "下雪不冷，融雪冷",
            kind,
            "热学图解",
            "呈现文章中的热学关系。",
            "保留条件，拒绝过强因果。",
            "教师与学生",
            ScientificFigureRiskLevel.High,
            [evidence],
            ["第三页"],
            ArticleScientificFigureDisposition.AddExplanatoryReplacement,
            "重绘文章关系。",
            RequiresGateOneApproval: true,
            GateOneStatus: ArticleScientificFigureGateStatus.PendingHumanApproval,
            DeliveryStatus: ArticleScientificFigureDeliveryStatus.NotCreated);
    }

    private static Task WriteJsonAsync(string path, object value) =>
        File.WriteAllTextAsync(path, JsonSerializer.Serialize(value, new JsonSerializerOptions
        {
            WriteIndented = true,
            Converters = { new JsonStringEnumConverter() },
        }));

    private static string Hash(byte[] bytes) =>
        $"sha256:{Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant()}";

    private static byte[] CreatePhotographicPng(int index)
    {
        using var bitmap = new SKBitmap(800, 600);
        bitmap.Erase(new SKColor(
            (byte)(40 + (index * 20)),
            (byte)(20 + (index * 10)),
            (byte)(180 - (index * 10))));
        using var image = SKImage.FromBitmap(bitmap);
        using var encoded = image.Encode(SKEncodedImageFormat.Png, quality: 100);
        return encoded.ToArray();
    }

    private static string Mutate(string svg, string mutation)
    {
        return mutation switch
        {
            "wrong-formula" => svg.Replace("x / (x + 1)", "x / (x - 1)", StringComparison.Ordinal),
            "swap-lens" => svg.Replace("附加凹透镜", "附加凸透镜", StringComparison.Ordinal),
            "same-focus" => svg.Replace("x=\"1038\"", "x=\"990\"", StringComparison.Ordinal),
            "swap-plane-order" => svg.Replace("L2 位于 S 右侧", "L2 位于 S 左侧", StringComparison.Ordinal),
            "reverse-ray" => ReverseFirstRay(svg),
            _ => throw new ArgumentOutOfRangeException(nameof(mutation), mutation, null),
        };
    }

    private static string ReverseFirstRay(string svg)
    {
        XNamespace ns = "http://www.w3.org/2000/svg";
        var document = XDocument.Parse(svg, LoadOptions.PreserveWhitespace);
        var ray = document.Descendants(ns + "path").First(item =>
            item.Attribute("marker-end") is not null
            && ((string?)item.Attribute("d"))?.StartsWith("M 135 255 L 330", StringComparison.Ordinal) == true);
        ray.SetAttributeValue("d", "M 330 300 L 135 255");
        return document.ToString(SaveOptions.DisableFormatting);
    }

    private sealed class FakeSourceFigureExtractor : IArticleSourceFigureExtractor
    {
        public ArticleSourceFigureAudit Extract(string sourcePdfPath) => new(
            new string('b', 64).Insert(0, "sha256:"),
            8,
            Enumerable.Range(1, 6).Select(index => new ArticleSourceFigureAsset(
                $"page-6-image-{index}",
                6,
                index,
                800,
                600,
                10,
                10,
                400,
                300,
                $"sha256:{new string((char)('a' + index), 64)}",
                [1, 2, 3])).ToArray());
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
                audit.Assets.Select(item => item.AssetId).ToArray());
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
