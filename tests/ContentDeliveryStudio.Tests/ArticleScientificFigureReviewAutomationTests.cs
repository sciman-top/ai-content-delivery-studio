using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;
using ContentDeliveryStudio.Application.ScientificFigures;
using ContentDeliveryStudio.Infrastructure.ScientificFigures;

namespace ContentDeliveryStudio.Tests;

public sealed class ArticleScientificFigureReviewAutomationTests
{
    [Fact]
    public void Assess_AuthorizedAgentReceiptRemovesOnsiteAndPerCandidateUserReview()
    {
        using var fixture = ReviewFixture.Create();

        var result = new ArticleScientificFigureReviewAutomationService().Assess(
            fixture.Request);

        Assert.Equal(ArticleScientificFigureReviewRoute.AuthorizedAgentAccept, result.Assessment.Route);
        Assert.True(result.Assessment.EligibleForPromotion);
        Assert.False(result.Assessment.RequiresHumanOnsiteReview);
        Assert.False(result.Assessment.RequiresPerCandidateUserReview);
        Assert.False(result.Assessment.RequiresIndependentHumanExpert);
        Assert.Equal(2, result.Assessment.CandidateCount);
        Assert.Equal(2, result.Receipt.Candidates.Count);
        Assert.Equal(3, result.Receipt.AuthorityFiles.Count);
        Assert.All(result.Receipt.Candidates, item => Assert.NotEmpty(item.Files));
        Assert.All(
            result.Receipt.Candidates.SelectMany(item => item.Files),
            file => Assert.Equal(71, file.Sha256.Length));
        Assert.True(File.Exists(result.ReceiptPath));
        Assert.True(File.Exists(result.AssessmentPath));
        Assert.DoesNotContain(fixture.Root, File.ReadAllText(result.ReceiptPath), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Assess_RequiresExplicitPerCandidateVisualConfirmation()
    {
        using var fixture = ReviewFixture.Create();

        Assert.Throws<InvalidOperationException>(() =>
            new ArticleScientificFigureReviewAutomationService().Assess(
                fixture.Request with { ConfirmEveryCandidateVisuallyInspected = false }));
    }

    [Fact]
    public void Assess_FailedSidecarCannotCreateAuthorizedAgentReceipt()
    {
        using var fixture = ReviewFixture.Create();
        fixture.MutateReview(root => root["Verdict"] = "Fail");

        Assert.Throws<InvalidOperationException>(() =>
            new ArticleScientificFigureReviewAutomationService().Assess(fixture.Request));
        Assert.False(File.Exists(Path.Combine(
            fixture.ReviewReadyDirectory,
            ArticleScientificFigureReviewAutomationService.ReceiptFileName)));
    }

    [Fact]
    public void Assess_ExistingReceiptCannotBeSilentlyOverwritten()
    {
        using var fixture = ReviewFixture.Create();
        var service = new ArticleScientificFigureReviewAutomationService();
        service.Assess(fixture.Request);

        Assert.Throws<InvalidOperationException>(() => service.Assess(fixture.Request));
    }

    [Fact]
    public void Assess_LowRiskIndependentVisualRunIsEligibleForFutureStandingAutomation()
    {
        using var fixture = ReviewFixture.Create();
        fixture.ReplaceText("article-figure-set-plan.json", "\"High\"", "\"Low\"");
        fixture.ReplaceText(
            "article-figure-set-report.json",
            "fake-scientific-visual",
            "independent-scientific-visual");

        var result = new ArticleScientificFigureReviewAutomationService().Assess(fixture.Request);

        Assert.True(result.Assessment.EligibleForFutureStandingAutomation);
        Assert.False(result.Assessment.RequiresHumanOnsiteReview);
    }

    [Fact]
    public void Assess_ExplicitExpertCertificationRequestCannotBeAutoPromoted()
    {
        using var fixture = ReviewFixture.Create();

        var result = new ArticleScientificFigureReviewAutomationService().Assess(
            fixture.Request with { RequireIndependentHumanExpertCertification = true });

        Assert.Equal(
            ArticleScientificFigureReviewRoute.IndependentHumanExpertRequired,
            result.Assessment.Route);
        Assert.False(result.Assessment.EligibleForPromotion);
        Assert.True(result.Assessment.RequiresHumanOnsiteReview);
        Assert.True(result.Assessment.RequiresIndependentHumanExpert);
    }

    [Fact]
    public void ValidateReceipt_FileDriftInvalidatesPriorAuthorizedAgentReview()
    {
        using var fixture = ReviewFixture.Create();
        var service = new ArticleScientificFigureReviewAutomationService();
        service.Assess(fixture.Request);
        File.WriteAllBytes(Path.Combine(fixture.ReviewReadyDirectory, "01-figure.png"), [9, 9, 9]);

        Assert.Throws<InvalidOperationException>(() =>
            service.ValidateReceipt(
                fixture.ReviewReadyDirectory,
                fixture.Request.Reviewer,
                fixture.Request.AuthorizationReference));
    }

    [Fact]
    public void ValidateReceipt_AuthorityInputDriftInvalidatesPriorReview()
    {
        using var fixture = ReviewFixture.Create();
        var service = new ArticleScientificFigureReviewAutomationService();
        service.Assess(fixture.Request);
        File.AppendAllText(
            Path.Combine(fixture.ReviewReadyDirectory, "article-figure-set-plan.json"),
            " ");

        Assert.Throws<InvalidOperationException>(() =>
            service.ValidateReceipt(
                fixture.ReviewReadyDirectory,
                fixture.Request.Reviewer,
                fixture.Request.AuthorizationReference));
    }

    private sealed class ReviewFixture : IDisposable
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = true,
            Converters = { new JsonStringEnumConverter() },
        };

        private ReviewFixture(string root)
        {
            Root = root;
            ReviewReadyDirectory = Path.Combine(root, "review-ready");
            Directory.CreateDirectory(ReviewReadyDirectory);
            Request = new ArticleScientificFigureReviewAutomationRequest(
                ReviewReadyDirectory,
                "codex-agent-authorized-by-sciman",
                "user-authorized-agent-review-2026-08-20",
                "Inspected every full-resolution PNG and PDF rerender; no visible or scientific conflicts.",
                ConfirmEveryCandidateVisuallyInspected: true,
                RequireIndependentHumanExpertCertification: false,
                DateTimeOffset.Parse("2026-08-20T19:00:00+08:00"));
        }

        public string Root { get; }
        public string ReviewReadyDirectory { get; }
        public ArticleScientificFigureReviewAutomationRequest Request { get; }

        public static ReviewFixture Create()
        {
            var fixture = new ReviewFixture(Path.Combine(
                Path.GetTempPath(),
                $"article-review-automation-{Guid.NewGuid():N}"));
            fixture.WriteFiles();
            return fixture;
        }

        public void MutateReview(Action<Dictionary<string, object?>> mutation)
        {
            var path = Path.Combine(ReviewReadyDirectory, "01-figure.visual-review.json");
            var root = JsonSerializer.Deserialize<Dictionary<string, object?>>(
                File.ReadAllText(path),
                JsonOptions)!;
            mutation(root);
            File.WriteAllText(path, JsonSerializer.Serialize(root, JsonOptions));
        }

        public void ReplaceText(string relativePath, string oldValue, string newValue)
        {
            var path = Path.Combine(ReviewReadyDirectory, relativePath);
            File.WriteAllText(
                path,
                File.ReadAllText(path).Replace(oldValue, newValue, StringComparison.Ordinal));
        }

        public void Dispose()
        {
            if (Directory.Exists(Root))
            {
                Directory.Delete(Root, recursive: true);
            }
        }

        private void WriteFiles()
        {
            Write("01-figure.svg", "<svg xmlns=\"http://www.w3.org/2000/svg\"></svg>"u8.ToArray());
            Write("01-figure.png", [1, 2, 3]);
            Write("01-figure.pdf", [4, 5, 6]);
            Write("02-source-evidence-board.png", [7, 8, 9]);
            WriteReview("01-figure", "candidate-figure", "Mechanism");
            WriteReview("02-source-evidence-board", "candidate-evidence", "SourceEvidenceBoard");
            WriteJson("source-figure-audit.json", new
            {
                SourceSha256 = $"sha256:{new string('a', 64)}",
                PageCount = 1,
                assets = Array.Empty<object>(),
            });
            WriteJson("article-figure-set-plan.json", new object[]
            {
                new
                {
                    CandidateId = "candidate-figure",
                    Kind = "Mechanism",
                    RiskLevel = "High",
                    Evidence = new[] { new { SourceBlockId = "block-1", Excerpt = "source" } },
                },
                new
                {
                    CandidateId = "candidate-evidence",
                    Kind = "SourceEvidenceBoard",
                    RiskLevel = "High",
                    Evidence = new[] { new { SourceBlockId = "block-1", Excerpt = "source" } },
                },
            });
            WriteJson("article-figure-set-report.json", new
            {
                schemaVersion = 1,
                requestedCandidateCount = 2,
                resultCount = 2,
                complete = true,
                visualReviewProvider = "fake-scientific-visual",
                deterministicReview = "article-optics-v1",
                gateOneStatus = "pending for every candidate",
                gateTwoStatus = "not-run",
                deliveryStatus = "not-created",
                items = new object[]
                {
                    new
                    {
                        CandidateId = "candidate-figure",
                        Kind = "Mechanism",
                        files = new[] { "01-figure.svg", "01-figure.png", "01-figure.pdf", "01-figure.visual-review.json" },
                        passedVisualReview = true,
                    },
                    new
                    {
                        CandidateId = "candidate-evidence",
                        Kind = "SourceEvidenceBoard",
                        files = new[] { "02-source-evidence-board.png", "02-source-evidence-board.visual-review.json" },
                        passedVisualReview = true,
                    },
                },
            });
        }

        private void WriteReview(string prefix, string candidateId, string kind) =>
            WriteJson($"{prefix}.visual-review.json", new
            {
                CandidateId = candidateId,
                Kind = kind,
                contractPassed = true,
                contractFindings = Array.Empty<object>(),
                deterministicScientificPassed = true,
                deterministicScientificPackage = "article-optics-v1",
                deterministicScientificFindings = Array.Empty<object>(),
                expectedVisualChecks = new[] { new { CheckId = "check-1" } },
                typedCrops = new[] { new { CropId = "crop-1" } },
                Verdict = "Pass",
                Findings = Array.Empty<object>(),
                ProviderTraceId = "fake-visual-review",
            });

        private void WriteJson(string relativePath, object value) =>
            File.WriteAllText(
                Path.Combine(ReviewReadyDirectory, relativePath),
                JsonSerializer.Serialize(value, JsonOptions));

        private void Write(string relativePath, byte[] bytes) =>
            File.WriteAllBytes(Path.Combine(ReviewReadyDirectory, relativePath), bytes);
    }
}
