using System.Security.Cryptography;
using System.Text;
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
    public void PlanningRoutesGravityArticleToCompleteGravityProfile()
    {
        var candidates = CreateGravityCandidates();

        Assert.Equal(7, candidates.Count);
        Assert.Equal(6, candidates.Count(candidate =>
            candidate.Kind != ArticleScientificFigureCandidateKind.SourceEvidenceBoard));
        Assert.All(candidates, candidate =>
        {
            Assert.Equal(["NASA", "NIST"], candidate.ExternalScientificReferences
                .Select(reference => reference.Publisher)
                .OrderBy(value => value, StringComparer.Ordinal));
        });
        Assert.IsType<ArticleGravityScientificReviewer>(
            ArticleScientificFigureReviewerFactory.CreateFor(candidates));
    }

    [Fact]
    public void ReviewerFactory_SourceEvidenceWithoutDomainCandidateFailsClosed()
    {
        var sourceEvidence = CreateCandidates().Single(candidate =>
            candidate.Kind == ArticleScientificFigureCandidateKind.SourceEvidenceBoard);

        var exception = Assert.Throws<InvalidOperationException>(() =>
            ArticleScientificFigureReviewerFactory.CreateFor([sourceEvidence]));

        Assert.Contains("No supported article scientific review profile", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ReviewerFactory_RoutesThermistorAndArchimedesProfiles()
    {
        var thermistor = CreateProfileCandidate(
            ArticleScientificFigureCandidateKind.ThermistorCircuitDivider,
            "2022新疆物理中考的第11题");
        var archimedes = CreateProfileCandidate(
            ArticleScientificFigureCandidateKind.ArchimedesWaterModel,
            "阿基米德原理的本质、适用条件、修正");

        Assert.IsType<ArticleThermistorScientificReviewer>(
            ArticleScientificFigureReviewerFactory.CreateFor([thermistor]));
        Assert.IsType<ArticleArchimedesScientificReviewer>(
            ArticleScientificFigureReviewerFactory.CreateFor([archimedes]));
    }

    [Fact]
    public void ThermistorReviewer_BlocksVoltmeterConnectedAcrossWrongNodes()
    {
        var candidate = CreateProfileCandidate(
            ArticleScientificFigureCandidateKind.ThermistorCircuitDivider,
            "2022新疆物理中考的第11题");
        var renderer = new ArticleScientificFigureCandidateRenderer();
        var artifact = renderer.Render(candidate, 1);
        var mutatedSvg = artifact.Svg.Replace(
            "M 330 360 L 360 320",
            "M 330 360 L 250 320",
            StringComparison.Ordinal);
        var review = new ArticleThermistorScientificReviewer().Review(
            candidate,
            artifact with
            {
                Svg = mutatedSvg,
                Sha256 = Hash(Encoding.UTF8.GetBytes(mutatedSvg)),
            },
            new FakeSourceFigureExtractor().Extract("article.pdf"),
            board: null);

        Assert.Contains(review.Findings, finding =>
            finding.Code == "thermistor-voltmeter-connection-invalid");
    }

    [Fact]
    public void DeterministicGravityReview_BlocksZeroGravityInOrbit()
    {
        var candidate = CreateGravityCandidates().Single(item =>
            item.Kind == ArticleScientificFigureCandidateKind.GravityOrbitFreeFall);
        var artifact = new ArticleScientificFigureCandidateRenderer().Render(candidate, 1);
        var mutatedSvg = artifact.Svg.Replace(
            @"g(r)=\frac{GM}{r^2}\ne0",
            "重力为零",
            StringComparison.Ordinal);
        var review = new ArticleGravityScientificReviewer().Review(
            candidate,
            artifact with
            {
                Svg = mutatedSvg,
                Sha256 = Hash(Encoding.UTF8.GetBytes(mutatedSvg)),
            },
            new FakeSourceFigureExtractor().Extract("article.pdf"),
            board: null);

        Assert.False(review.Passed);
        Assert.Contains(review.Findings, finding => finding.Code == "gravity-orbit-zero-overclaim");
    }

    [Fact]
    public void GravityRenderer_UsesTypesetMathAndSeparatesVectorSumFromForceBalance()
    {
        var candidates = CreateGravityCandidates();
        var renderer = new ArticleScientificFigureCandidateRenderer();
        var terminology = renderer.Render(candidates.Single(item =>
            item.Kind == ArticleScientificFigureCandidateKind.GravityTerminology), 1).Svg;
        var orbit = renderer.Render(candidates.Single(item =>
            item.Kind == ArticleScientificFigureCandidateKind.GravityOrbitFreeFall), 1).Svg;
        var surface = renderer.Render(candidates.Single(item =>
            item.Kind == ArticleScientificFigureCandidateKind.GravitySurfaceRotation), 1).Svg;
        var frames = renderer.Render(candidates.Single(item =>
            item.Kind == ArticleScientificFigureCandidateKind.GravityReferenceFrames), 1).Svg;

        Assert.DoesNotContain("先声明参考系与术语约定，再写公式和结论", terminology, StringComparison.Ordinal);
        Assert.Contains(@"\mathbf{g}_{\mathrm{eff}}", surface, StringComparison.Ordinal);
        Assert.Contains(@"\mathbf{R}+m\mathbf{g}_{\mathrm{eff}}=0", surface, StringComparison.Ordinal);
        Assert.Contains("surface-gravity-translation", surface, StringComparison.Ordinal);
        Assert.Contains("surface-centrifugal-translation", surface, StringComparison.Ordinal);
        Assert.Contains(@"\sum\mathbf{F}_{\mathrm{real}}=m\mathbf{a}", frames, StringComparison.Ordinal);
        Assert.Contains("data-math-tex", frames, StringComparison.Ordinal);

        var orbitDocument = XDocument.Parse(orbit, LoadOptions.PreserveWhitespace);
        var orbitLabel = orbitDocument.Descendants((XNamespace)"http://www.w3.org/2000/svg" + "text")
            .Single(text => text.Value == "共同自由落体：秤读数");
        Assert.Equal("700", (string?)orbitLabel.Attribute("x"));
        Assert.Equal("start", (string?)orbitLabel.Attribute("text-anchor"));
        var orbitFormula = orbitDocument.Descendants((XNamespace)"http://www.w3.org/2000/svg" + "g")
            .Single(group => (string?)group.Attribute("data-math-tex") == @"\mathbf{N}\approx0");
        Assert.Equal("900", (string?)orbitFormula.Element((XNamespace)"http://www.w3.org/2000/svg" + "text")?.Attribute("x"));
    }

    [Fact]
    public void DeterministicGravityReview_BlocksUnclosedSurfaceVectorSum()
    {
        var candidate = CreateGravityCandidates().Single(item =>
            item.Kind == ArticleScientificFigureCandidateKind.GravitySurfaceRotation);
        var artifact = new ArticleScientificFigureCandidateRenderer().Render(candidate, 1);
        var document = XDocument.Parse(artifact.Svg, LoadOptions.PreserveWhitespace);
        var svg = (XNamespace)"http://www.w3.org/2000/svg";
        var effective = document.Descendants(svg + "path").Single(path =>
            (string?)path.Attribute("data-gravity-role") == "surface-effective-gravity");
        effective.SetAttributeValue("d", "M 485 278 L 435 420");
        var mutatedSvg = document.ToString(SaveOptions.DisableFormatting);

        var review = new ArticleGravityScientificReviewer().Review(
            candidate,
            artifact with
            {
                Svg = mutatedSvg,
                Sha256 = Hash(Encoding.UTF8.GetBytes(mutatedSvg)),
            },
            new FakeSourceFigureExtractor().Extract("article.pdf"),
            board: null);

        Assert.False(review.Passed);
        Assert.Contains(review.Findings, finding => finding.Code == "gravity-surface-vector-sum-invalid");
    }

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
        Assert.Equal(
            ArticleHumanReviewMode.ScientificApprovalAndPerCandidateVisualSpotCheck,
            run.HumanReviewRecommendation.Mode);
        Assert.False(run.HumanReviewRecommendation.IndependentVisualReviewPassed);
        Assert.True(run.HumanReviewRecommendation.RequiresEveryCandidateVisualSpotCheck);
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
    public async Task ProviderVisualPass_AllowsSampledDeliveryReviewWithoutPerCandidateSpotCheck()
    {
        var visual = new SequenceVisualReviewProvider(new ScientificProviderReviewResult(
            ScientificReviewVerdict.Pass,
            [],
            "provider-pass",
            ScientificProviderReviewOrigin.ProviderResponse));

        var run = await CreateService(visual).RunAsync(
            "article.pdf",
            CreateCandidates(),
            CancellationToken.None);

        Assert.True(run.Complete);
        Assert.Equal(
            ArticleHumanReviewMode.ScientificApprovalAndSampledDeliveryReview,
            run.HumanReviewRecommendation.Mode);
        Assert.True(run.HumanReviewRecommendation.IndependentVisualReviewPassed);
        Assert.False(run.HumanReviewRecommendation.RequiresEveryCandidateVisualSpotCheck);
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

        Assert.Contains(@"y=\frac{x}{x+1},\ x&gt;0", graph, StringComparison.Ordinal);
        Assert.Contains(@"y=\frac{x}{1-x},\ 0&lt;x&lt;1", graph, StringComparison.Ordinal);
        Assert.DoesNotContain(@"\frac{x}{x-1}", graph, StringComparison.Ordinal);
        Assert.Contains("data-math-tex", graph, StringComparison.Ordinal);
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
    public void DeterministicThermalReview_BlocksBasinAirThatDescendsToGround()
    {
        var candidate = CreateThermalCandidate(ArticleScientificFigureCandidateKind.ThermalBasinException);
        var artifact = new ArticleScientificFigureCandidateRenderer().Render(candidate, 1);
        var mutatedSvg = artifact.Svg.Replace(
            "M 650 350 L 790 405",
            "M 650 350 L 790 590",
            StringComparison.Ordinal);

        var review = ReviewThermalMutation(candidate, artifact, mutatedSvg);

        Assert.False(review.Passed);
        Assert.Contains(review.Findings, finding =>
            finding.Code == "thermal-basin-cold-air-altitude-invalid");
    }

    [Fact]
    public void DeterministicThermalReview_BlocksEqualDryAndHumidEvaporationArrows()
    {
        var candidate = CreateThermalCandidate(ArticleScientificFigureCandidateKind.ThermalDryWetHeat);
        var artifact = new ArticleScientificFigureCandidateRenderer().Render(candidate, 1);
        var mutatedSvg = artifact.Svg.Replace(
            "M 180 430 L 480 430",
            "M 180 430 L 280 430",
            StringComparison.Ordinal);

        var review = ReviewThermalMutation(candidate, artifact, mutatedSvg);

        Assert.False(review.Passed);
        Assert.Contains(review.Findings, finding =>
            finding.Code == "thermal-evaporation-rate-contrast-invalid");
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
    public void EvidenceBoardRenderer_RetainsNonPhotographicScientificFigures()
    {
        var assets = new[]
        {
            CreateSourceFigureAsset("photo", CreatePhotographicPng(1)),
            CreateSourceFigureAsset("line-art", CreateLineArtPng()),
        };
        var audit = new ArticleSourceFigureAudit(
            $"sha256:{new string('c', 64)}",
            PageCount: 2,
            assets);

        var board = new SkiaArticleSourceEvidenceBoardRenderer().Render(audit);

        Assert.Equal(assets.Select(asset => asset.AssetId), board.SourceAssetIds);
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
    public void VisualContract_RejectsTextThatEscapesAnExplicitLayoutPanel()
    {
        var candidate = CreateGravityCandidates().Single(item =>
            item.Kind == ArticleScientificFigureCandidateKind.GravityOrbitFreeFall);
        var renderer = new ArticleScientificFigureCandidateRenderer();
        var artifact = renderer.Render(candidate, 1);
        var document = XDocument.Parse(artifact.Svg);
        XNamespace svg = "http://www.w3.org/2000/svg";
        var label = document.Descendants(svg + "text").Single(text =>
            text.Value == "共同自由落体：秤读数");
        label.SetAttributeValue("x", 850);
        label.SetAttributeValue("text-anchor", "end");
        label.SetAttributeValue("data-text-bounds", "660,625,190,22");
        var mutatedSvg = document.ToString(SaveOptions.DisableFormatting);
        var mutatedArtifact = artifact with
        {
            Svg = mutatedSvg,
            Sha256 = Hash(Encoding.UTF8.GetBytes(mutatedSvg)),
        };
        var exports = new ScientificFigureExporter().Export(
            new ScientificFigureExportRequest(mutatedArtifact, mutatedArtifact.Sha256, 1200, 800));

        var report = new ArticleCandidateVisualContractReviewer().Review(
            candidate,
            mutatedArtifact,
            exports);

        Assert.Contains(report.Findings, finding =>
            finding.Code == "candidate-panel-content-outside");
    }

    [Fact]
    public void VisualContract_RejectsOutOfCanvasMathBounds()
    {
        var candidate = CreateGravityCandidates().Single(item =>
            item.Kind == ArticleScientificFigureCandidateKind.GravityOrbitFreeFall);
        var renderer = new ArticleScientificFigureCandidateRenderer();
        var artifact = renderer.Render(candidate, 1);
        var document = XDocument.Parse(artifact.Svg);
        XNamespace svg = "http://www.w3.org/2000/svg";
        document.Descendants(svg + "g").Single(group =>
            (string?)group.Attribute("data-math-tex") == @"\mathbf{N}\approx0")
            .SetAttributeValue("data-math-bounds", "1190,790,30,30");
        var mutatedSvg = document.ToString(SaveOptions.DisableFormatting);
        var mutatedArtifact = artifact with
        {
            Svg = mutatedSvg,
            Sha256 = Hash(Encoding.UTF8.GetBytes(mutatedSvg)),
        };
        var exports = new ScientificFigureExporter().Export(
            new ScientificFigureExportRequest(mutatedArtifact, mutatedArtifact.Sha256, 1200, 800));

        var report = new ArticleCandidateVisualContractReviewer().Review(
            candidate,
            mutatedArtifact,
            exports);

        Assert.Contains(report.Findings, finding =>
            finding.Code == "candidate-math-bounds-outside-canvas");
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

    [Theory]
    [InlineData(ArticleScientificFigureCandidateKind.BernoulliFanEnergy, "伯努利定律不适用于电吹风")]
    [InlineData(ArticleScientificFigureCandidateKind.BernoulliFanZones, "伯努利定律不适用于电吹风")]
    [InlineData(ArticleScientificFigureCandidateKind.BernoulliStreamlineBoundary, "伯努利定律不适用于电吹风")]
    [InlineData(ArticleScientificFigureCandidateKind.PinholeGeometry, "不用光屏，相机手动对焦直接观察小孔成像")]
    [InlineData(ArticleScientificFigureCandidateKind.PinholeFocusPlane, "不用光屏，相机手动对焦直接观察小孔成像")]
    [InlineData(ArticleScientificFigureCandidateKind.PinholeObservation, "不用光屏，相机手动对焦直接观察小孔成像")]
    [InlineData(ArticleScientificFigureCandidateKind.SuperconductingEnergy, "超导磁体")]
    [InlineData(ArticleScientificFigureCandidateKind.SuperconductingPersistentCurrent, "超导磁体")]
    [InlineData(ArticleScientificFigureCandidateKind.SuperconductingExcitation, "超导磁体")]
    public void ExtendedProfileRenderer_ProvidesRequiredApparatusAndTopology(
        ArticleScientificFigureCandidateKind kind,
        string title)
    {
        var candidate = CreateProfileCandidate(kind, title);
        var artifact = new ArticleScientificFigureCandidateRenderer().Render(candidate, 1);

        var report = new ArticleMechanicsScientificReviewer().Review(
            candidate,
            artifact,
            new FakeSourceFigureExtractor().Extract("article.pdf"),
            board: null);

        Assert.True(report.Passed, string.Join(" | ", report.Findings.Select(finding => $"{finding.Code}: {finding.Evidence}")));

        // Regression guard for the two failures that previously looked like
        // plausible diagrams but had a broken physical topology.
        var svg = XElement.Parse(artifact.Svg);
        if (kind == ArticleScientificFigureCandidateKind.BernoulliStreamlineBoundary)
            Assert.True(svg.Descendants().Count(node => (string?)node.Attribute("data-article-role") == "duct-wall") >= 4);
        if (kind == ArticleScientificFigureCandidateKind.SuperconductingPersistentCurrent)
            Assert.True(svg.Descendants().Count(node => (string?)node.Attribute("data-article-role") == "charging-loop") >= 6);
    }

    [Fact]
    public void ExtendedProfileReviewer_RejectsLabelOnlyArtworkWithoutRequiredGraphicRoles()
    {
        var candidate = CreateProfileCandidate(
            ArticleScientificFigureCandidateKind.PinholeGeometry,
            "不用光屏，相机手动对焦直接观察小孔成像");
        var artifact = new ArticleScientificFigureCandidateRenderer().Render(candidate, 1);
        var stripped = artifact with
        {
            Svg = artifact.Svg.Replace("data-article-role", "data-unrelated-role", StringComparison.Ordinal),
        };

        var report = new ArticleMechanicsScientificReviewer().Review(
            candidate,
            stripped,
            new FakeSourceFigureExtractor().Extract("article.pdf"),
            board: null);

        Assert.False(report.Passed);
        Assert.Contains(report.Findings, finding => finding.Code == "article-required-graphic-role-missing");
    }

    [Fact]
    public void ExtendedProfileReviewer_RejectsDisconnectedPinholeFocusTopology()
    {
        var candidate = CreateProfileCandidate(
            ArticleScientificFigureCandidateKind.PinholeFocusPlane,
            "不用光屏，相机手动对焦直接观察小孔成像");
        var artifact = new ArticleScientificFigureCandidateRenderer().Render(candidate, 1);
        var document = XElement.Parse(artifact.Svg);
        var brokenRay = document
            .Descendants()
            .First(element => (string?)element.Attribute("data-article-role") == "camera-focused-ray");
        brokenRay.SetAttributeValue("d", "M 900 300 L 1040 350");

        var report = new ArticleMechanicsScientificReviewer().Review(
            candidate,
            artifact with { Svg = document.ToString(SaveOptions.DisableFormatting) },
            new FakeSourceFigureExtractor().Extract("article.pdf"),
            board: null);

        Assert.False(report.Passed);
        Assert.Contains(report.Findings, finding => finding.Code == "article-focus-ray-topology-invalid");
    }

    [Fact]
    public void ExtendedProfileReviewer_RejectsHiddenGraphicRoles()
    {
        var candidate = CreateProfileCandidate(
            ArticleScientificFigureCandidateKind.PinholeGeometry,
            "不用光屏，相机手动对焦直接观察小孔成像");
        var artifact = new ArticleScientificFigureCandidateRenderer().Render(candidate, 1);
        var document = XElement.Parse(artifact.Svg);
        document
            .Descendants()
            .First(element => (string?)element.Attribute("data-article-role") == "image-plane")
            .SetAttributeValue("style", "display:none");

        var report = new ArticleMechanicsScientificReviewer().Review(
            candidate,
            artifact with { Svg = document.ToString(SaveOptions.DisableFormatting) },
            new FakeSourceFigureExtractor().Extract("article.pdf"),
            board: null);

        Assert.False(report.Passed);
        Assert.Contains(report.Findings, finding => finding.Code == "article-required-graphic-role-missing");
    }

    [Fact]
    public void ExtendedProfileReviewer_RejectsPinholeImageTargetTopology()
    {
        var candidate = CreateProfileCandidate(
            ArticleScientificFigureCandidateKind.PinholeGeometry,
            "不用光屏，相机手动对焦直接观察小孔成像");
        var artifact = new ArticleScientificFigureCandidateRenderer().Render(candidate, 1);
        var document = XElement.Parse(artifact.Svg);
        var brokenRay = document
            .Descendants()
            .First(element => (string?)element.Attribute("data-article-role") == "principal-ray" &&
                ((string?)element.Attribute("d"))?.Contains("1030 610", StringComparison.Ordinal) == true);
        brokenRay.SetAttributeValue("d", "M 520 360 L 700 500");

        var report = new ArticleMechanicsScientificReviewer().Review(
            candidate,
            artifact with { Svg = document.ToString(SaveOptions.DisableFormatting) },
            new FakeSourceFigureExtractor().Extract("article.pdf"),
            board: null);

        Assert.False(report.Passed);
        Assert.Contains(report.Findings, finding => finding.Code == "article-pinhole-ray-topology-invalid");
    }

    [Fact]
    public void ExtendedProfileReviewer_RejectsDisconnectedSuperconductingThermalCoupling()
    {
        var candidate = CreateProfileCandidate(
            ArticleScientificFigureCandidateKind.SuperconductingExcitation,
            "超导磁体");
        var artifact = new ArticleScientificFigureCandidateRenderer().Render(candidate, 1);
        var document = XElement.Parse(artifact.Svg);
        var coupling = document
            .Descendants()
            .First(element => (string?)element.Attribute("data-article-role") == "thermal-coupling");
        coupling.SetAttributeValue("d", "M 635 225 L 800 320");

        var report = new ArticleMechanicsScientificReviewer().Review(
            candidate,
            artifact with { Svg = document.ToString(SaveOptions.DisableFormatting) },
            new FakeSourceFigureExtractor().Extract("article.pdf"),
            board: null);

        Assert.False(report.Passed);
        Assert.Contains(report.Findings, finding => finding.Code == "article-superconducting-heater-topology-invalid");
    }

    [Fact]
    public void ExtendedProfileReviewer_RejectsDisconnectedSuperconductingEnergyCircuit()
    {
        var candidate = CreateProfileCandidate(
            ArticleScientificFigureCandidateKind.SuperconductingEnergy,
            "超导磁体");
        var artifact = new ArticleScientificFigureCandidateRenderer().Render(candidate, 1);
        var document = XElement.Parse(artifact.Svg);
        var brokenCircuit = document
            .Descendants()
            .First(element => (string?)element.Attribute("data-article-role") == "circuit" &&
                ((string?)element.Attribute("d"))?.Contains("832 400", StringComparison.Ordinal) == true);
        brokenCircuit.SetAttributeValue("d", "M 700 400 L 760 350");

        var report = new ArticleMechanicsScientificReviewer().Review(
            candidate,
            artifact with { Svg = document.ToString(SaveOptions.DisableFormatting) },
            new FakeSourceFigureExtractor().Extract("article.pdf"),
            board: null);

        Assert.False(report.Passed);
        Assert.Contains(report.Findings, finding => finding.Code == "article-superconducting-energy-topology-invalid");
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

    private static IReadOnlyList<ArticleScientificFigureCandidate> CreateGravityCandidates()
    {
        const string text = "Gravitation Gravity Weight 空间站 自由下落的电梯 地球表面的物体 重量为零 非惯性系";
        var block = ScientificSourceBlock.Create(
            "gravity-source",
            ScientificSourceBlockKind.Paragraph,
            ScientificSourceLocation.Create(
                1,
                "gravity article",
                boundingRegion: null,
                ScientificCharacterRange.Create(0, text.Length)),
            text,
            isRequired: true,
            ScientificRecoveryStatus.NotRequired);
        var extraction = ScientificDocumentExtraction.Create(
            Guid.NewGuid(),
            new string('d', 64).Insert(0, "sha256:"),
            ScientificExtractorIdentity.Create("test", "1"),
            ScientificExtractionQuality.Create(
                isScanned: false,
                ocrApplied: false,
                ScientificReadingOrderStatus.Reliable,
                ScientificRequiredContentStatus.Complete),
            [block],
            []);
        return new ArticleScientificFigurePlanningService().Plan(
            extraction,
            "“重力”定义的混乱",
            "初中物理教师与学生");
    }

    private static ArticleScientificFigureCandidate CreateProfileCandidate(
        ArticleScientificFigureCandidateKind kind,
        string title)
    {
        const string evidenceText = "来源证据：文章中的物理图示和公式。";
        var block = ScientificSourceBlock.Create(
            $"profile-{kind}",
            ScientificSourceBlockKind.Paragraph,
            ScientificSourceLocation.Create(
                1,
                "profile test",
                boundingRegion: null,
                ScientificCharacterRange.Create(0, evidenceText.Length)),
            evidenceText,
            isRequired: true,
            ScientificRecoveryStatus.NotRequired);
        return new ArticleScientificFigureCandidate(
            $"profile-{kind}",
            title,
            kind,
            "profile test figure",
            "profile test objective",
            "profile test message",
            "教师与学生",
            ScientificFigureRiskLevel.High,
            [ArticleScientificFigureEvidence.Create(block, excerptLength: 240)],
            ["图1"],
            ArticleScientificFigureDisposition.AddExplanatoryReplacement,
            "profile test rationale",
            RequiresGateOneApproval: true,
            GateOneStatus: ArticleScientificFigureGateStatus.PendingHumanApproval,
            DeliveryStatus: ArticleScientificFigureDeliveryStatus.NotCreated);
    }

    private static ArticleOpticalScientificReviewReport ReviewThermalMutation(
        ArticleScientificFigureCandidate candidate,
        ScientificSvgArtifact artifact,
        string mutatedSvg)
    {
        var mutatedArtifact = artifact with
        {
            Svg = mutatedSvg,
            Sha256 = Hash(Encoding.UTF8.GetBytes(mutatedSvg)),
        };
        return new ArticleThermalScientificReviewer().Review(
            candidate,
            mutatedArtifact,
            new FakeSourceFigureExtractor().Extract("article.pdf"),
            board: null);
    }

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

    private static byte[] CreateLineArtPng()
    {
        using var bitmap = new SKBitmap(420, 320);
        bitmap.Erase(SKColors.White);
        using var canvas = new SKCanvas(bitmap);
        using var paint = new SKPaint { Color = SKColors.Black, StrokeWidth = 3 };
        canvas.DrawLine(20, 160, 400, 160, paint);
        canvas.DrawLine(210, 20, 210, 300, paint);
        using var image = SKImage.FromBitmap(bitmap);
        using var encoded = image.Encode(SKEncodedImageFormat.Png, quality: 100);
        return encoded.ToArray();
    }

    private static ArticleSourceFigureAsset CreateSourceFigureAsset(string id, byte[] png)
    {
        using var bitmap = SKBitmap.Decode(png);
        return new ArticleSourceFigureAsset(
            id,
            PageNumber: 1,
            PageImageIndex: 1,
            bitmap.Width,
            bitmap.Height,
            PageLeft: 10,
            PageBottom: 10,
            PageWidth: 400,
            PageHeight: 300,
            Hash(png),
            png);
    }

    private static string Mutate(string svg, string mutation)
    {
        return mutation switch
        {
            "wrong-formula" => svg
                .Replace(@"\frac{x}{x+1}", @"\frac{x}{x-1}", StringComparison.Ordinal)
                .Replace("x + 1", "x − 1", StringComparison.Ordinal),
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
