using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;
using ContentDeliveryStudio.Application.ScientificFigures;
using ContentDeliveryStudio.Infrastructure.ScientificFigures;

namespace ContentDeliveryStudio.Tests;

public sealed class ArticleScientificFigureDeliveryPromoterTests
{
    [Fact]
    public void Promote_CreatesImmutableHashBoundPackageAndSeparatesEvidence()
    {
        using var fixture = PromotionFixture.Create();

        var result = new ArticleScientificFigureDeliveryPromoter().Promote(fixture.Request);

        Assert.True(Directory.Exists(result.PackageDirectory));
        Assert.True(File.Exists(Path.Combine(result.PackageDirectory, "figures", "01-figure.svg")));
        Assert.True(File.Exists(Path.Combine(result.PackageDirectory, "figures", "01-figure.png")));
        Assert.True(File.Exists(Path.Combine(result.PackageDirectory, "figures", "01-figure.pdf")));
        Assert.False(File.Exists(Path.Combine(result.PackageDirectory, "figures", "02-source-evidence-board.png")));
        Assert.True(File.Exists(Path.Combine(result.PackageDirectory, "evidence", "02-source-evidence-board.png")));
        Assert.True(File.Exists(Path.Combine(result.PackageDirectory, "evidence", "source-assets", "page-1-image-1.png")));
        Assert.Equal(3, result.FigureAssetCount);
        Assert.Equal(5, result.ReviewCount);
        Assert.True(File.Exists(Path.Combine(
            result.PackageDirectory,
            "reviews",
            ArticleScientificFigureReviewAutomationService.ReceiptFileName)));

        using var manifest = JsonDocument.Parse(File.ReadAllText(result.ManifestPath));
        var entries = manifest.RootElement.GetProperty("Files").EnumerateArray().ToArray();
        Assert.NotEmpty(entries);
        Assert.All(entries, entry =>
        {
            var relativePath = entry.GetProperty("PackageRelativePath").GetString()!;
            Assert.False(Path.IsPathRooted(relativePath));
            var delivered = Path.Combine(result.PackageDirectory, relativePath.Replace('/', Path.DirectorySeparatorChar));
            Assert.Equal(entry.GetProperty("Sha256").GetString(), Hash(File.ReadAllBytes(delivered)));
        });
        Assert.DoesNotContain(
            fixture.Root,
            File.ReadAllText(result.ManifestPath),
            StringComparison.OrdinalIgnoreCase);
        Assert.Equal(
            File.ReadAllBytes(Path.Combine(fixture.ReviewReadyDirectory, "01-figure.pdf")),
            File.ReadAllBytes(Path.Combine(result.PackageDirectory, "figures", "01-figure.pdf")));
    }

    [Theory]
    [InlineData(false, true)]
    [InlineData(true, false)]
    public void Promote_RequiresBothExplicitApprovals(bool gateOneApproved, bool gateTwoApproved)
    {
        using var fixture = PromotionFixture.Create();
        var request = fixture.Request with
        {
            GateOneApproved = gateOneApproved,
            GateTwoApproved = gateTwoApproved,
        };

        Assert.Throws<InvalidOperationException>(() =>
            new ArticleScientificFigureDeliveryPromoter().Promote(request));
        Assert.False(Directory.Exists(fixture.ExpectedPackageDirectory));
    }

    [Fact]
    public void Promote_AuthorizedAgentRequiresAuthorizationReference()
    {
        using var fixture = PromotionFixture.Create();
        var request = fixture.Request with { AuthorizationReference = null };

        Assert.Throws<InvalidOperationException>(() =>
            new ArticleScientificFigureDeliveryPromoter().Promote(request));
    }

    [Fact]
    public void Promote_AuthorizedAgentRequiresHashBoundVisualReceipt()
    {
        using var fixture = PromotionFixture.Create();
        File.Delete(Path.Combine(
            fixture.ReviewReadyDirectory,
            ArticleScientificFigureReviewAutomationService.ReceiptFileName));
        File.Delete(Path.Combine(
            fixture.ReviewReadyDirectory,
            ArticleScientificFigureReviewAutomationService.AssessmentFileName));

        Assert.Throws<InvalidOperationException>(() =>
            new ArticleScientificFigureDeliveryPromoter().Promote(fixture.Request));
    }

    [Fact]
    public void Promote_HumanApprovalDoesNotRequireAuthorizedAgentReceipt()
    {
        using var fixture = PromotionFixture.Create();
        File.Delete(Path.Combine(
            fixture.ReviewReadyDirectory,
            ArticleScientificFigureReviewAutomationService.ReceiptFileName));
        File.Delete(Path.Combine(
            fixture.ReviewReadyDirectory,
            ArticleScientificFigureReviewAutomationService.AssessmentFileName));
        var request = fixture.Request with
        {
            Actor = ArticleScientificFigureApprovalActor.Human,
            AuthorizationReference = null,
            Reviewer = "human-reviewer",
        };

        var result = new ArticleScientificFigureDeliveryPromoter().Promote(request);

        Assert.True(Directory.Exists(result.PackageDirectory));
        Assert.Equal(3, result.ReviewCount);
    }

    [Fact]
    public void Promote_IncompleteReportIsBlocked()
    {
        using var fixture = PromotionFixture.Create();
        fixture.MutateReport(root => root["complete"] = false);

        Assert.Throws<InvalidOperationException>(() =>
            new ArticleScientificFigureDeliveryPromoter().Promote(fixture.Request));
    }

    [Fact]
    public void Promote_FailedDeterministicOrVisualSidecarIsBlocked()
    {
        using var fixture = PromotionFixture.Create();
        fixture.MutateJson("01-figure.visual-review.json", root =>
            root["deterministicScientificPassed"] = false);

        Assert.Throws<InvalidOperationException>(() =>
            new ArticleScientificFigureDeliveryPromoter().Promote(fixture.Request));
    }

    [Fact]
    public void Promote_MissingOrHashDriftedSourceAssetIsBlocked()
    {
        using var fixture = PromotionFixture.Create();
        File.WriteAllBytes(
            Path.Combine(fixture.ReviewReadyDirectory, "source-assets", "page-1-image-1.png"),
            [9, 9, 9]);

        Assert.Throws<InvalidOperationException>(() =>
            new ArticleScientificFigureDeliveryPromoter().Promote(fixture.Request));
    }

    [Fact]
    public void Promote_DoesNotMergeIntoExistingPackage()
    {
        using var fixture = PromotionFixture.Create();
        var promoter = new ArticleScientificFigureDeliveryPromoter();
        promoter.Promote(fixture.Request);

        Assert.Throws<InvalidOperationException>(() => promoter.Promote(fixture.Request));
    }

    private static string Hash(byte[] bytes) =>
        $"sha256:{Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant()}";

    private sealed class PromotionFixture : IDisposable
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = true,
            Converters = { new JsonStringEnumConverter() },
        };

        private PromotionFixture(string root)
        {
            Root = root;
            ReviewReadyDirectory = Path.Combine(root, "review-ready");
            DeliveryRoot = Path.Combine(root, "deliveries");
            Directory.CreateDirectory(ReviewReadyDirectory);
            Directory.CreateDirectory(Path.Combine(ReviewReadyDirectory, "source-assets"));
            Request = new ArticleScientificFigureDeliveryPromotionRequest(
                ReviewReadyDirectory,
                DeliveryRoot,
                "sample-article",
                "20260820-v1",
                "codex-agent-authorized-by-sciman",
                ArticleScientificFigureApprovalActor.AuthorizedAgent,
                "Codex task authorization: user message on 2026-08-20",
                GateOneApproved: true,
                "Scientific meaning, conditions, directions, values, and exceptions approved.",
                GateTwoApproved: true,
                "Every candidate visually spot-checked; package approved.",
                DateTimeOffset.Parse("2026-08-20T10:00:00+08:00"));
        }

        public string Root { get; }
        public string ReviewReadyDirectory { get; }
        public string DeliveryRoot { get; }
        public ArticleScientificFigureDeliveryPromotionRequest Request { get; }
        public string ExpectedPackageDirectory =>
            Path.Combine(DeliveryRoot, "sample-article", "20260820-v1");

        public static PromotionFixture Create()
        {
            var fixture = new PromotionFixture(Path.Combine(
                Path.GetTempPath(),
                $"article-delivery-{Guid.NewGuid():N}"));
            fixture.WriteCandidateFiles();
            return fixture;
        }

        public void MutateReport(Action<Dictionary<string, object?>> mutation) =>
            MutateJson("article-figure-set-report.json", mutation);

        public void MutateJson(string relativePath, Action<Dictionary<string, object?>> mutation)
        {
            var path = Path.Combine(ReviewReadyDirectory, relativePath);
            var root = JsonSerializer.Deserialize<Dictionary<string, object?>>(File.ReadAllText(path), JsonOptions)!;
            mutation(root);
            File.WriteAllText(path, JsonSerializer.Serialize(root, JsonOptions));
        }

        public void Dispose()
        {
            if (Directory.Exists(Root))
            {
                Directory.Delete(Root, recursive: true);
            }
        }

        private void WriteCandidateFiles()
        {
            Write("01-figure.svg", "<svg xmlns=\"http://www.w3.org/2000/svg\"></svg>"u8.ToArray());
            Write("01-figure.png", [1, 2, 3]);
            Write("01-figure.pdf", [4, 5, 6]);
            Write("02-source-evidence-board.png", [7, 8, 9]);
            var sourceAsset = new byte[] { 10, 11, 12 };
            Write(Path.Combine("source-assets", "page-1-image-1.png"), sourceAsset);

            WriteReview("01-figure", "candidate-figure", "Mechanism");
            WriteReview("02-source-evidence-board", "candidate-evidence", "SourceEvidenceBoard");
            WriteJson("article-figure-set-plan.json", new object[]
            {
                new
                {
                    CandidateId = "candidate-figure",
                    Kind = "Mechanism",
                    ArticleTitle = "Sample article",
                    RiskLevel = "High",
                    Evidence = new[] { new { SourceBlockId = "block-1", Excerpt = "source" } },
                },
                new
                {
                    CandidateId = "candidate-evidence",
                    Kind = "SourceEvidenceBoard",
                    ArticleTitle = "Sample article",
                    RiskLevel = "High",
                    Evidence = new[] { new { SourceBlockId = "block-1", Excerpt = "source" } },
                },
            });
            WriteJson("source-figure-audit.json", new
            {
                SourceSha256 = $"sha256:{new string('a', 64)}",
                PageCount = 1,
                assets = new[]
                {
                    new
                    {
                        AssetId = "page-1-image-1",
                        Sha256 = Hash(sourceAsset),
                        fileName = "source-assets/page-1-image-1.png",
                    },
                },
            });
            WriteJson("article-figure-set-report.json", new
            {
                schemaVersion = 1,
                source = new
                {
                    fileName = "sample.pdf",
                    SourceSha256 = $"sha256:{new string('a', 64)}",
                },
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
            new ArticleScientificFigureReviewAutomationService().Assess(
                new ArticleScientificFigureReviewAutomationRequest(
                    ReviewReadyDirectory,
                    Request.Reviewer,
                    Request.AuthorizationReference!,
                    "Inspected every candidate and approved the exact bytes for delivery.",
                    ConfirmEveryCandidateVisuallyInspected: true,
                    RequireIndependentHumanExpertCertification: false,
                    DateTimeOffset.Parse("2026-08-20T09:55:00+08:00")));
        }

        private void WriteReview(string prefix, string candidateId, string kind) =>
            WriteJson($"{prefix}.visual-review.json", new
            {
                CandidateId = candidateId,
                Kind = kind,
                gateOneStatus = "PendingHumanApproval",
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

        private void Write(string relativePath, byte[] bytes)
        {
            var path = Path.Combine(ReviewReadyDirectory, relativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllBytes(path, bytes);
        }
    }
}
