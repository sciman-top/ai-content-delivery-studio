using System.Xml.Linq;
using ContentDeliveryStudio.Application.ScientificFigures;
using ContentDeliveryStudio.Core.ScientificFigures;
using ContentDeliveryStudio.Infrastructure.ScientificFigures;

namespace ContentDeliveryStudio.Tests;

public sealed class ArticleRequestedFigureProfileTests
{
    public static TheoryData<string, string[], string, ArticleScientificFigureCandidateKind[]> Profiles => new()
    {
        {
            "电表使用中，正确的“试触”",
            ["指针摆动有惯性。", "手不能立即离开开关，待指针基本稳定。", "安全依靠预估和保护装置。"],
            "article-meter-trial-v1",
            [ArticleScientificFigureCandidateKind.MeterTransientResponse, ArticleScientificFigureCandidateKind.MeterTrialDecision, ArticleScientificFigureCandidateKind.MeterProtectionLayers]
        },
        {
            "沸腾前、后，气泡大小变化的真正原因",
            ["沸腾前气泡变小甚至会消失。", "气泡表面的水不断汽化。", "底部水柱压强略大于水面。"],
            "article-boiling-bubbles-v1",
            [ArticleScientificFigureCandidateKind.BoilingPreBubbleCollapse, ArticleScientificFigureCandidateKind.BoilingBubbleGrowth, ArticleScientificFigureCandidateKind.BoilingPressureScale]
        },
        {
            "伽利略望远镜中目镜的成像原理",
            ["物镜和目镜组合后出射光线是基本平行的。", "讨论一倍焦距和二倍焦距。", "文章比较放大倍数并给出目镜为凹透镜。"],
            "article-galilean-eyepiece-v1",
            [ArticleScientificFigureCandidateKind.GalileanAfocalPath, ArticleScientificFigureCandidateKind.GalileanVirtualObjectRegimes, ArticleScientificFigureCandidateKind.GalileanAngularMagnification]
        },
    };

    [Theory]
    [MemberData(nameof(Profiles))]
    public void RequestedProfile_PlansRendersAndPassesDeterministicReview(
        string title,
        string[] paragraphs,
        string expectedPackage,
        ArticleScientificFigureCandidateKind[] expectedKinds)
    {
        var extraction = CreateExtraction(paragraphs);
        var candidates = new ArticleScientificFigurePlanningService().Plan(
            extraction,
            title,
            "初中物理教师与学生");

        Assert.Equal(expectedKinds, candidates.Where(item => item.Kind != ArticleScientificFigureCandidateKind.SourceEvidenceBoard).Select(item => item.Kind));
        Assert.Contains(candidates, item => item.Kind == ArticleScientificFigureCandidateKind.SourceEvidenceBoard);

        var reviewer = ArticleScientificFigureReviewerFactory.CreateFor(candidates);
        var renderer = new ArticleScientificFigureCandidateRenderer();
        var audit = new ArticleSourceFigureAudit(extraction.SourceSha256, paragraphs.Length, []);
        foreach (var candidate in candidates.Where(item => item.Kind != ArticleScientificFigureCandidateKind.SourceEvidenceBoard))
        {
            var artifact = renderer.Render(candidate, 1);
            var report = reviewer.Review(candidate, artifact, audit, null);
            Assert.True(report.Passed, string.Join("; ", report.Findings.Select(item => $"{item.Code}: {item.Evidence}")));
            Assert.Equal(expectedPackage, report.PackageId);
            Assert.Contains("data-article-role", artifact.Svg, StringComparison.Ordinal);
        }

        var boardCandidate = Assert.Single(candidates, item => item.Kind == ArticleScientificFigureCandidateKind.SourceEvidenceBoard);
        var boardReport = reviewer.Review(
            boardCandidate,
            artifact: null,
            audit,
            new ArticleSourceEvidenceBoard([1], "sha256:board", 1, 1, []));
        Assert.True(boardReport.Passed, string.Join("; ", boardReport.Findings.Select(item => item.Code)));
        Assert.Equal(expectedPackage, boardReport.PackageId);
    }

    [Fact]
    public void GalileanAfocalPath_RejectsSolidObjectiveRaysThatPassThroughTheEyepiece()
    {
        var candidates = new ArticleScientificFigurePlanningService().Plan(
            CreateExtraction(["物镜和目镜组合后出射光线是基本平行的。", "讨论一倍焦距和二倍焦距。", "文章比较放大倍数并给出目镜为凹透镜。"]),
            "伽利略望远镜中目镜的成像原理",
            "初中物理教师与学生");
        var candidate = Assert.Single(candidates, item => item.Kind == ArticleScientificFigureCandidateKind.GalileanAfocalPath);
        var artifact = new ArticleScientificFigureCandidateRenderer().Render(candidate, 1);
        var incorrect = artifact with
        {
            Svg = artifact.Svg
                .Replace("M 300 200 L 810 350", "M 300 200 L 980 400", StringComparison.Ordinal)
                .Replace("M 300 400 L 810 400", "M 300 400 L 980 400", StringComparison.Ordinal)
                .Replace("M 300 600 L 810 450", "M 300 600 L 980 400", StringComparison.Ordinal)
        };

        var report = ArticleScientificFigureReviewerFactory.CreateFor(candidates).Review(
            candidate,
            incorrect,
            new ArticleSourceFigureAudit("sha256:source", 3, []),
            null);

        Assert.Contains(report.Findings, finding => finding.Code == "article-galilean-afocal-topology-invalid");
    }

    [Fact]
    public void BoilingPressureScale_RejectsAQuantityCardWithoutVisibleBubbleRise()
    {
        var candidates = new ArticleScientificFigurePlanningService().Plan(
            CreateExtraction(["沸腾前气泡变小甚至会消失。", "气泡表面的水不断汽化。", "底部水柱压强略大于水面。"]),
            "沸腾前、后，气泡大小变化的真正原因",
            "初中物理教师与学生");
        var candidate = Assert.Single(candidates, item => item.Kind == ArticleScientificFigureCandidateKind.BoilingPressureScale);
        var artifact = new ArticleScientificFigureCandidateRenderer().Render(candidate, 1);
        var incorrect = artifact with { Svg = artifact.Svg.Replace("data-article-role=\"bubble-rise\"", "data-article-role=\"removed-bubble-rise\"", StringComparison.Ordinal) };

        var report = ArticleScientificFigureReviewerFactory.CreateFor(candidates).Review(
            candidate,
            incorrect,
            new ArticleSourceFigureAudit("sha256:source", 3, []),
            null);

        Assert.Contains(report.Findings, finding => finding.Code == "article-required-graphic-role-missing" && finding.Evidence == "bubble-rise");
    }

    [Fact]
    public void BoilingBubbleGrowth_PlacesTheWaterTemperatureLabelOutsideTheLargestBubble()
    {
        var candidates = new ArticleScientificFigurePlanningService().Plan(
            CreateExtraction(["沸腾前气泡变小甚至会消失。", "气泡表面的水不断汽化。", "底部水柱压强略大于水面。"]),
            "沸腾前、后，气泡大小变化的真正原因",
            "初中物理教师与学生");
        var candidate = Assert.Single(candidates, item => item.Kind == ArticleScientificFigureCandidateKind.BoilingBubbleGrowth);
        var document = XDocument.Parse(new ArticleScientificFigureCandidateRenderer().Render(candidate, 1).Svg);
        var svg = (XNamespace)"http://www.w3.org/2000/svg";
        var label = Assert.Single(document.Descendants(svg + "text"), element => element.Value == "接近均匀沸腾温度的水");

        Assert.Equal("645", (string?)label.Attribute("x"));
        Assert.Equal("205", (string?)label.Attribute("y"));
    }

    [Fact]
    public void MeterFigure_RejectsAProcessCardWhenTheConcreteMeterIsRemoved()
    {
        var candidates = PlanRequested("电表使用中，正确的“试触”", ["指针摆动有惯性。", "手不能立即离开开关，待指针基本稳定。", "安全依靠预估和保护装置。"]);
        var candidate = Assert.Single(candidates, item => item.Kind == ArticleScientificFigureCandidateKind.MeterTrialDecision);
        var artifact = new ArticleScientificFigureCandidateRenderer().Render(candidate, 1);
        var incorrect = artifact with { Svg = artifact.Svg.Replace("data-article-role=\"meter-body\"", "data-article-role=\"removed-meter-body\"", StringComparison.Ordinal) };

        var report = ArticleScientificFigureReviewerFactory.CreateFor(candidates).Review(
            candidate, incorrect, new ArticleSourceFigureAudit("sha256:source", 3, []), null);

        Assert.IsType<ArticleMeterScientificReviewer>(ArticleScientificFigureReviewerFactory.CreateFor(candidates));
        Assert.Contains(report.Findings, finding => finding.Code == "article-required-concrete-object-missing" && finding.Evidence == "meter-body");
    }

    [Fact]
    public void BoilingFigure_RejectsAnApparatusWithoutHeatTransferConnection()
    {
        var candidates = PlanRequested("沸腾前、后，气泡大小变化的真正原因", ["沸腾前气泡变小甚至会消失。", "气泡表面的水不断汽化。", "底部水柱压强略大于水面。"]);
        var candidate = Assert.Single(candidates, item => item.Kind == ArticleScientificFigureCandidateKind.BoilingBubbleGrowth);
        var artifact = new ArticleScientificFigureCandidateRenderer().Render(candidate, 1);
        var incorrect = artifact with { Svg = artifact.Svg.Replace("data-article-connection=\"heat-to-water\"", "data-article-connection=\"removed-heat-to-water\"", StringComparison.Ordinal) };

        var report = ArticleScientificFigureReviewerFactory.CreateFor(candidates).Review(
            candidate, incorrect, new ArticleSourceFigureAudit("sha256:source", 3, []), null);

        Assert.IsType<ArticleBoilingScientificReviewer>(ArticleScientificFigureReviewerFactory.CreateFor(candidates));
        Assert.Contains(report.Findings, finding => finding.Code == "article-required-causal-connection-missing" && finding.Evidence == "heat-to-water");
    }

    [Fact]
    public void GalileanFigure_RejectsAFreeFloatingRayDiagramWithoutTelescopeContext()
    {
        var candidates = PlanRequested("伽利略望远镜中目镜的成像原理", ["物镜和目镜组合后出射光线是基本平行的。", "讨论一倍焦距和二倍焦距。", "文章比较放大倍数并给出目镜为凹透镜。"]);
        var candidate = Assert.Single(candidates, item => item.Kind == ArticleScientificFigureCandidateKind.GalileanAngularMagnification);
        var artifact = new ArticleScientificFigureCandidateRenderer().Render(candidate, 1);
        var incorrect = artifact with { Svg = artifact.Svg.Replace("data-article-role=\"telescope-tube\"", "data-article-role=\"removed-telescope-tube\"", StringComparison.Ordinal) };

        var report = ArticleScientificFigureReviewerFactory.CreateFor(candidates).Review(
            candidate, incorrect, new ArticleSourceFigureAudit("sha256:source", 3, []), null);

        Assert.IsType<ArticleGalileanScientificReviewer>(ArticleScientificFigureReviewerFactory.CreateFor(candidates));
        Assert.Contains(report.Findings, finding => finding.Code == "article-required-concrete-object-missing" && finding.Evidence == "telescope-tube");
    }

    private static IReadOnlyList<ArticleScientificFigureCandidate> PlanRequested(string title, string[] paragraphs) =>
        new ArticleScientificFigurePlanningService().Plan(CreateExtraction(paragraphs), title, "初中物理教师与学生");

    private static ScientificDocumentExtraction CreateExtraction(IReadOnlyList<string> paragraphs)
    {
        var blocks = paragraphs.Select((text, index) => ScientificSourceBlock.Create(
            $"block-{index + 1}",
            ScientificSourceBlockKind.Paragraph,
            ScientificSourceLocation.Create(
                index + 1,
                $"page {index + 1}",
                boundingRegion: null,
                ScientificCharacterRange.Create(0, text.Length)),
            text,
            isRequired: true,
            ScientificRecoveryStatus.NotRequired)).ToArray();
        return ScientificDocumentExtraction.Create(
            Guid.NewGuid(),
            $"sha256:{new string('a', 64)}",
            ScientificExtractorIdentity.Create("test-extractor", "1.0"),
            ScientificExtractionQuality.Create(
                isScanned: false,
                ocrApplied: false,
                ScientificReadingOrderStatus.Reliable,
                ScientificRequiredContentStatus.Complete),
            blocks,
            []);
    }
}
