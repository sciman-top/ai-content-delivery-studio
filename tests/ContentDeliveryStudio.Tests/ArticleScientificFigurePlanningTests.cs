using System.Text.Json;
using System.Text.Json.Serialization;
using System.Security.Cryptography;
using System.Globalization;
using System.Xml.Linq;
using ContentDeliveryStudio.Application.ScientificFigures;
using ContentDeliveryStudio.Core.ScientificFigures;
using ContentDeliveryStudio.Core.Sources;
using ContentDeliveryStudio.Infrastructure.ScientificFigures;

namespace ContentDeliveryStudio.Tests;

public sealed class ArticleScientificFigurePlanningTests
{
    [Fact]
    public void Plan_ProducesSixDistinctEvidenceBoundCandidates()
    {
        var extraction = CreateExtraction();

        var candidates = new ArticleScientificFigurePlanningService().Plan(
            extraction,
            "眼睛直接观察凸透镜成像时的各种问题",
            "初中物理教师与学生");

        Assert.Equal(6, candidates.Count);
        Assert.Equal(6, candidates.Select(candidate => candidate.Kind).Distinct().Count());
        Assert.All(candidates, candidate =>
        {
            Assert.NotEmpty(candidate.Evidence);
            Assert.NotEmpty(candidate.SourceFigureReferences);
            Assert.True(Enum.IsDefined(candidate.Disposition));
            Assert.False(string.IsNullOrWhiteSpace(candidate.ReplacementRationale));
            Assert.True(candidate.RequiresGateOneApproval);
            Assert.Equal(ArticleScientificFigureGateStatus.PendingHumanApproval, candidate.GateOneStatus);
            Assert.Equal(ArticleScientificFigureDeliveryStatus.NotCreated, candidate.DeliveryStatus);
        });
        Assert.All(candidates.SelectMany(candidate => candidate.Evidence), evidence =>
            Assert.InRange(evidence.PageNumber, 1, 8));
    }

    [Fact]
    public void Plan_RoutesSnowArticleToThermalProfileInsteadOfOpticalProfile()
    {
        var candidates = new ArticleScientificFigurePlanningService().Plan(
            CreateThermalExtraction(),
            "“下雪不冷，融雪冷”的正确解释",
            "初中物理教师与学生");

        Assert.True(candidates.Count == 7, string.Join(", ", candidates.Select(item => item.Kind)));
        Assert.DoesNotContain(candidates, item =>
            item.Kind is ArticleScientificFigureCandidateKind.Mechanism
                or ArticleScientificFigureCandidateKind.LensEquationGraph
                or ArticleScientificFigureCandidateKind.ExperimentalComparison
                or ArticleScientificFigureCandidateKind.Comparison
                or ArticleScientificFigureCandidateKind.CorrectiveLensControl);
        Assert.Contains(candidates, item => item.Kind == ArticleScientificFigureCandidateKind.ThermalConductivityComparison);
        Assert.Contains(candidates, item => item.Kind == ArticleScientificFigureCandidateKind.SourceEvidenceBoard);
        Assert.All(candidates, item => Assert.Equal(ArticleScientificFigureGateStatus.PendingHumanApproval, item.GateOneStatus));
    }

    [Fact]
    public void RenderOpticalPathPreview_PreservesPendingGateOneBoundary()
    {
        var candidate = new ArticleScientificFigurePlanningService().Plan(
                CreateExtraction(),
                "眼睛直接观察凸透镜成像时的各种问题",
                "初中物理教师与学生")
            .Single(item => item.Kind == ArticleScientificFigureCandidateKind.Mechanism);

        var preview = new ArticleScientificFigurePreviewRenderer().RenderOpticalPathPreview(candidate);

        Assert.Equal(candidate.CandidateId, preview.CandidateId);
        Assert.Equal(ArticleScientificFigureGateStatus.PendingHumanApproval, preview.GateOneStatus);
        Assert.Contains("仅供 Gate 1 科学核验", preview.Svg, StringComparison.Ordinal);
        Assert.Contains("中间像 S", preview.Svg, StringComparison.Ordinal);
        Assert.StartsWith("sha256:", preview.Sha256, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ApprovedMechanism_UsesPersistedGateOneAndSeparateMachineReviewLayers()
    {
        var extraction = CreateExtraction();
        var candidate = new ArticleScientificFigurePlanningService().Plan(
                extraction,
                "眼睛直接观察凸透镜成像时的各种问题",
                "初中物理教师与学生")
            .Single(item => item.Kind == ArticleScientificFigureCandidateKind.Mechanism);
        var repository = new InMemoryScientificFigureWorkflowRepository();
        var service = CreateWorkflowService(repository);
        var gateOneTime = DateTimeOffset.UtcNow;

        var approved = await service.CreateApprovedMechanismAsync(
            Guid.NewGuid(),
            extraction,
            candidate,
            new ScientificGateOneDecision(
                Approved: true,
                "sciman",
                "Approved only as a non-proportional optical schematic; no quantitative, clarity, orientation, or medical claim is approved.",
                gateOneTime),
            CancellationToken.None);
        var result = await service.RunMachineReviewAsync(
            approved,
            gateOneTime.AddMinutes(1),
            CancellationToken.None);

        Assert.True(result.ContractReview.Passed);
        Assert.True(result.MachineReview.CanProceedToGate2);
        Assert.NotEmpty(result.ReviewPreparation.VisualRequest.RegionCrops);
        Assert.Equal(ScientificFigureWorkflowState.ReviewPassed, result.Aggregate.Workflow.State);
        Assert.Equal(
            result.Aggregate.Id,
            (await repository.LoadAsync(result.Aggregate.Id, CancellationToken.None))!.Id);
    }

    [Fact]
    public async Task ApprovedMechanism_RendersEveryCriticalRelationLabelInANonOverlappingBand()
    {
        var extraction = CreateExtraction();
        var candidate = new ArticleScientificFigurePlanningService().Plan(
                extraction,
                "眼睛直接观察凸透镜成像时的各种问题",
                "初中物理教师与学生")
            .Single(item => item.Kind == ArticleScientificFigureCandidateKind.Mechanism);
        var service = CreateWorkflowService(new InMemoryScientificFigureWorkflowRepository());
        var approved = await service.CreateApprovedMechanismAsync(
            Guid.NewGuid(),
            extraction,
            candidate,
            new ScientificGateOneDecision(
                Approved: true,
                "label-layout-test",
                "Approve only the bounded non-proportional optical relationships.",
                DateTimeOffset.UtcNow),
            CancellationToken.None);
        var review = await service.RunMachineReviewAsync(
            approved,
            DateTimeOffset.UtcNow.AddMinutes(1),
            CancellationToken.None);
        var document = XDocument.Parse(review.Svg.Svg);
        var svg = (XNamespace)"http://www.w3.org/2000/svg";
        var labelBounds = document.Descendants(svg + "rect")
            .Where(item => (string?)item.Attribute("data-relation-label-background") == "true")
            .Select(item => new
            {
                Left = Number(item, "x"),
                Top = Number(item, "y"),
                Width = Number(item, "width"),
                Height = Number(item, "height"),
            })
            .ToArray();

        Assert.Equal(3, labelBounds.Length);
        Assert.All(
            labelBounds.SelectMany((first, index) => labelBounds.Skip(index + 1)
                .Select(second => (first, second))),
            pair => Assert.False(Overlaps(pair.first, pair.second)));
    }

    [Fact]
    public async Task SamplePdf_ProducesAnEvidenceBoundCandidateRunWhenExplicitlyRequested()
    {
        var sourcePath = Environment.GetEnvironmentVariable("ARTICLE_SCIENTIFIC_FIGURE_SOURCE_PATH");
        var outputDirectory = Environment.GetEnvironmentVariable("ARTICLE_SCIENTIFIC_FIGURE_OUTPUT_DIRECTORY");
        if (string.IsNullOrWhiteSpace(sourcePath) || string.IsNullOrWhiteSpace(outputDirectory))
        {
            return;
        }

        var extraction = await new PdfPigScientificDocumentExtractor().ExtractAsync(
            new ScientificDocumentExtractionRequest(
                Guid.NewGuid(),
                $"sha256:{Convert.ToHexString(SHA256.HashData(await File.ReadAllBytesAsync(sourcePath))).ToLowerInvariant()}",
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
        var opticalCandidate = candidates.Single(item =>
            item.Kind == ArticleScientificFigureCandidateKind.Mechanism);
        var preview = new ArticleScientificFigurePreviewRenderer().RenderOpticalPathPreview(opticalCandidate);
        var repository = new InMemoryScientificFigureWorkflowRepository();
        var workflowService = CreateWorkflowService(repository);
        var gateOneTime = DateTimeOffset.UtcNow;
        var approved = await workflowService.CreateApprovedMechanismAsync(
            Guid.NewGuid(),
            extraction,
            opticalCandidate,
            new ScientificGateOneDecision(
                Approved: true,
                "sciman",
                "Interactive Gate 1 approval: non-proportional optical geometry only; no quantitative, clarity, orientation, or medical claim is approved.",
                gateOneTime),
            CancellationToken.None);
        var review = await workflowService.RunMachineReviewAsync(
            approved,
            gateOneTime.AddMinutes(1),
            CancellationToken.None);
        Assert.True(review.ContractReview.Passed);
        Assert.True(review.MachineReview.CanProceedToGate2);

        Directory.CreateDirectory(outputDirectory);
        var reportPath = Path.Combine(outputDirectory, "article-scientific-figure-report.json");
        var previewPath = Path.Combine(outputDirectory, "candidate-01-secondary-lens-imaging-path.svg");
        var approvedSvgPath = Path.Combine(outputDirectory, "approved-mechanism.svg");
        var workflowPath = Path.Combine(outputDirectory, "approved-scientific-workflow.json");
        var reviewsPath = Path.Combine(outputDirectory, "machine-review.json");
        await File.WriteAllTextAsync(previewPath, preview.Svg);
        await File.WriteAllTextAsync(approvedSvgPath, review.Svg.Svg);
        await File.WriteAllTextAsync(workflowPath, JsonSerializer.Serialize(
            review.Aggregate,
            new JsonSerializerOptions { WriteIndented = true }));
        foreach (var artifact in review.Exports.Artifacts)
        {
            await File.WriteAllBytesAsync(
                Path.Combine(outputDirectory, $"approved-mechanism.{artifact.Format}"),
                artifact.Bytes);
        }
        await File.WriteAllTextAsync(reviewsPath, JsonSerializer.Serialize(new
        {
            contractPassed = review.ContractReview.Passed,
            contractHardFailures = review.ContractReview.HardFailures,
            machineReviewPassed = review.MachineReview.CanProceedToGate2,
            machineReviewBlockers = review.MachineReview.Blockers,
            visualReview = new
            {
                provider = "fake-scientific-visual",
                fullResolutionSha256 = review.ReviewPreparation.Manifest.FullResolutionSha256,
                cropCount = review.ReviewPreparation.VisualRequest.RegionCrops.Count,
                cropIds = review.ReviewPreparation.Manifest.CropIds,
            },
            semanticReview = new
            {
                provider = "fake-scientific-semantic",
                approvedClaimCount = review.ReviewPreparation.SemanticRequest.ApprovedClaims.Count,
            },
            gateTwoStatus = "not-run: a separate explicit human Gate 2 decision is required",
        }, new JsonSerializerOptions
        {
            WriteIndented = true,
            Converters = { new JsonStringEnumConverter() },
        }));
        await File.WriteAllTextAsync(
            reportPath,
            JsonSerializer.Serialize(new
            {
                schemaVersion = 1,
                source = new
                {
                    fileName = Path.GetFileName(sourcePath),
                    extraction.Status,
                    extraction.SourceSha256,
                    pageCount = extraction.Blocks.Select(block => block.Location.PageNumber).Distinct().Count(),
                    blockCount = extraction.Blocks.Count,
                },
                candidates,
                preview = new
                {
                    fileName = Path.GetFileName(previewPath),
                    preview.PreviewKind,
                    preview.Sha256,
                    preview.GateOneStatus,
                },
                formalWorkflow = new
                {
                    workflowFileName = Path.GetFileName(workflowPath),
                    reviewFileName = Path.GetFileName(reviewsPath),
                    approvedSvgFileName = Path.GetFileName(approvedSvgPath),
                    review.Aggregate.Workflow.State,
                    gateOneReviewer = review.Aggregate.Workflow.Gate1Approval!.Reviewer,
                    review.ContractReview.Passed,
                    machineReviewPassed = review.MachineReview.CanProceedToGate2,
                },
                gateTwoStatus = "not-run: a separate explicit human Gate 2 decision is required",
                deliveryStatus = ArticleScientificFigureDeliveryStatus.NotCreated,
            }, new JsonSerializerOptions
            {
                WriteIndented = true,
                Converters = { new JsonStringEnumConverter() },
            }));

        Assert.True(File.Exists(reportPath));
        Assert.True(File.Exists(previewPath));
        Assert.True(File.Exists(approvedSvgPath));
        Assert.True(File.Exists(workflowPath));
        Assert.True(File.Exists(reviewsPath));
        Assert.Equal(ScientificFigureWorkflowState.ReviewPassed, review.Aggregate.Workflow.State);
    }

    private static ScientificDocumentExtraction CreateExtraction()
    {
        var sourceAssetId = Guid.NewGuid();
        var blocks = new[]
        {
            Block("block-page-1", 1, "用眼睛直接观察像的时候，眼睛本身也是一个凸透镜，构成二次凸透镜成像。像 S 对于人眼来说，是一个物体，它通过眼睛后能形成二次成像。"),
            Block("block-page-2", 2, "当眼睛在像 S 右方 10cm 以内，看不清；当眼睛在 S 左方时，文章讨论正立的视觉。"),
            Block("block-page-3", 3, "文章用函数图象讨论负物距情形与一倍焦距以内虚像之间的共轭关系。"),
            Block("block-page-4", 4, "实验使用光屏和代表眼睛的第二凸透镜，并比较视网膜上的画面和清晰度。"),
            Block("block-page-6", 6, "验证实验照片包括图6至图9；近视眼镜属于附加透镜控制变量。"),
        };
        return ScientificDocumentExtraction.Create(
            sourceAssetId,
            "sha256:1111111111111111111111111111111111111111111111111111111111111111",
            ScientificExtractorIdentity.Create("test-extractor", "1.0"),
            ScientificExtractionQuality.Create(
                isScanned: false,
                ocrApplied: false,
                ScientificReadingOrderStatus.Reliable,
                ScientificRequiredContentStatus.Complete),
            blocks,
            []);
    }

    private static ScientificDocumentExtraction CreateThermalExtraction()
    {
        var blocks = new[]
        {
            Block("thermal-page-1", 1, "寒冷空气与暖湿空气相遇，锋面抬升，水蒸气凝华成雪。"),
            Block("thermal-page-2", 2, "盆地和高山地形会阻滞南下寒冷空气，地面仍较暖。"),
            Block("thermal-page-3", 3, "导热系数 λ W/(m·K)，空气0.02，水蒸气0.02，水0.6，棉毛0.05。"),
            Block("thermal-page-4", 4, "传热方式包括热传导、热对流、热辐射、相变潜热。冬季和夏季作用不同。"),
            Block("thermal-page-5", 5, "相对湿度较高，衣物潮湿，导热系数增大，热量快速散去。"),
            Block("thermal-page-6", 6, "干热时汗液的蒸发快，湿热时汗液蒸发受阻。"),
        };
        return ScientificDocumentExtraction.Create(
            Guid.NewGuid(),
            "sha256:2222222222222222222222222222222222222222222222222222222222222222",
            ScientificExtractorIdentity.Create("test-extractor", "1.0"),
            ScientificExtractionQuality.Create(
                isScanned: false,
                ocrApplied: false,
                ScientificReadingOrderStatus.Reliable,
                ScientificRequiredContentStatus.Complete),
            blocks,
            []);
    }

    private static ScientificSourceBlock Block(string id, int page, string text) =>
        ScientificSourceBlock.Create(
            id,
            ScientificSourceBlockKind.Paragraph,
            ScientificSourceLocation.Create(
                page,
                $"page {page}",
                boundingRegion: null,
                ScientificCharacterRange.Create(0, text.Length)),
            text,
            isRequired: true,
            ScientificRecoveryStatus.NotRequired);

    private static ArticleScientificFigureWorkflowService CreateWorkflowService(
        IScientificFigureWorkflowRepository repository) =>
        new(
            repository,
            new DeterministicSvgRenderer(),
            new ScientificFigureExporter(),
            new SkiaScientificReviewImageCropper(),
            new FakeScientificSemanticReviewProvider(),
            new FakeScientificVisualReviewProvider());

    private static double Number(XElement element, string attribute) =>
        double.Parse((string)element.Attribute(attribute)!, CultureInfo.InvariantCulture);

    private static bool Overlaps(
        dynamic first,
        dynamic second) =>
        first.Left < second.Left + second.Width
        && first.Left + first.Width > second.Left
        && first.Top < second.Top + second.Height
        && first.Top + first.Height > second.Top;
}
