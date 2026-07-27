using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using ContentDeliveryStudio.Application.ScientificFigures;
using ContentDeliveryStudio.Core.ScientificFigures;
using ContentDeliveryStudio.Infrastructure.OpenAI;
using ContentDeliveryStudio.Infrastructure.ScientificFigures;

namespace ContentDeliveryStudio.Tests;

public sealed class OpenAiScientificFigureLiveAcceptanceTests
{
    private const string RunOptInEnvironmentVariable =
        "CONTENT_DELIVERY_STUDIO_RUN_SCIENTIFIC_LIVE_ACCEPTANCE";
    private const string ReportPathEnvironmentVariable =
        "SCIENTIFIC_FIGURE_LIVE_ACCEPTANCE_REPORT_PATH";
    private static readonly string[] SelectedItemIds =
    [
        "electromagnetism-rotating-coil-generator",
        "thermal-three-mode-heat-transfer-comparison",
        "quantum-photoelectric-threshold-summary",
    ];
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
    };

    [Fact]
    [Trait("Category", "LiveProvider")]
    public async Task LiveScientificAcceptance_WhenExplicitlyOptedIn_WritesThreeHumanReviewBundles()
    {
        if (!string.Equals(
                Environment.GetEnvironmentVariable(RunOptInEnvironmentVariable),
                "1",
                StringComparison.Ordinal))
        {
            return;
        }

        var repositoryRoot = FindRepositoryRoot();
        var envPath = Path.Combine(repositoryRoot, ".env");
        var configuration = await ProviderEnvironmentConfiguration.FromDotEnvFileAsync(
            envPath,
            CancellationToken.None);
        var errors = configuration.Validate();
        Assert.True(errors.Count == 0, string.Join(" ", errors));

        var reportPath = ResolveReportPath(repositoryRoot);
        var artifactRoot = Path.GetDirectoryName(reportPath)!;
        Directory.CreateDirectory(artifactRoot);
        var corpus = await ScientificFigureCorpusBaselineLoader.LoadAsync(
            Path.Combine(repositoryRoot, "eval", "scientific-figures", "corpus.json"),
            CancellationToken.None);
        var items = SelectedItemIds.Select(itemId =>
            corpus.Items.Single(item => string.Equals(item.ItemId, itemId, StringComparison.Ordinal)))
            .ToArray();
        Assert.Equal(3, items.Select(item => item.Category).Distinct(StringComparer.Ordinal).Count());

        var secretStore = new DotEnvSecretStore(envPath);
        var options = OpenAiProviderOptions.FromTextProviderEnvironment(
            configuration,
            realApiEnabled: true);
        var telemetry = new CapturingTelemetrySink();
        var understandingProvider = new OpenAiScientificUnderstandingProvider(
            options,
            new OpenAiSdkClientFactory(secretStore),
            secretStore,
            telemetry);
        using var reviewHttpClient = new HttpClient { BaseAddress = options.BaseUri };
        var reviewProvider = new OpenAiScientificReviewProvider(
            reviewHttpClient,
            options,
            secretStore,
            telemetry);
        var renderer = new DeterministicSvgRenderer();
        var exporter = new ScientificFigureExporter();
        var cropper = new SkiaScientificReviewImageCropper();
        var startedAt = DateTimeOffset.UtcNow;
        var sampleReports = new List<object>();

        foreach (var item in items)
        {
            var sampleDirectory = Path.Combine(artifactRoot, item.ItemId);
            Directory.CreateDirectory(sampleDirectory);
            if (TryLoadPassedSampleReport(sampleDirectory, item) is { } passedSample)
            {
                sampleReports.Add(passedSample);
                continue;
            }

            var model = ScientificFigureCorpusRunner.BuildModel(item);
            var telemetryStart = telemetry.Count;
            var understandingRequest = new ScientificUnderstandingChunkRequest(
                model.Extraction,
                item.Baseline.FigureObjective,
                ChunkIndex: 0,
                ChunkCount: 1,
                model.Extraction.Blocks);
            var liveUnderstanding = await understandingProvider.AnalyzeChunkAsync(
                understandingRequest,
                CancellationToken.None);
            var understandingProviderCalls = telemetry.SnapshotFrom(telemetryStart)
                .Select(ToTelemetryEvidence)
                .ToArray();
            await WriteJsonAsync(
                Path.Combine(sampleDirectory, "understanding-checkpoint.json"),
                new
                {
                    item.ItemId,
                    item.Category,
                    stage = "understanding",
                    liveUnderstanding.ProviderTraceId,
                    liveUnderstanding.IsBlocked,
                    liveUnderstanding.BlockingCodes,
                    claimCount = liveUnderstanding.Claims.Count,
                    providerCalls = understandingProviderCalls,
                    humanGate1Status = "accepted-existing-baseline-authority",
                    humanGate2Status = "not-reached",
                });
            Assert.Single(understandingProviderCalls);
            Assert.False(liveUnderstanding.IsBlocked);

            var reviewedAt = ParseReviewTime(item.Baseline.HumanReview.ReviewedAt);
            var workflow = ScientificFigureWorkflow.Create(model.Specification)
                .ApproveGate1(
                    item.Baseline.HumanReview.Reviewer,
                    "Checkpoint 0 accepted baseline authority reused without mutation.",
                    reviewedAt);
            var plan = new ScientificFigureSpecCompiler().Compile(workflow);
            var svg = renderer.Render(plan);
            var exports = exporter.Export(new ScientificFigureExportRequest(
                svg,
                svg.Sha256,
                Width: 1200,
                Height: 800));
            var contractReport = new ScientificContractReviewer().Review(
                new ScientificContractReviewRequest(
                    model.Specification,
                    plan,
                    svg,
                    exports));
            var prep = new ScientificReviewPrepBuilder(cropper).Build(
                model.Understanding,
                model.Specification,
                plan,
                svg,
                exports);
            await WriteArtifactsAsync(sampleDirectory, svg, exports, prep.VisualRequest);
            var semanticResult = await reviewProvider.ReviewAsync(
                prep.SemanticRequest,
                CancellationToken.None);
            var visualResult = await reviewProvider.ReviewAsync(
                prep.VisualRequest,
                CancellationToken.None);
            var machineDecision = await new ScientificReviewExecutionService(
                    new FixedSemanticProvider(semanticResult),
                    new FixedVisualProvider(visualResult))
                .ReviewAsync(
                    prep.SemanticRequest,
                    prep.VisualRequest,
                    CancellationToken.None);

            var semanticPayloadHash = Hash(JsonSerializer.SerializeToUtf8Bytes(
                OpenAiScientificReviewMapper.CreateSemanticPayload(
                    prep.SemanticRequest,
                    options.VisionReviewModel),
                JsonOptions));
            var visualPayloadHash = Hash(JsonSerializer.SerializeToUtf8Bytes(
                OpenAiScientificReviewMapper.CreateVisualPayload(
                    prep.VisualRequest,
                    options.VisionReviewModel),
                JsonOptions));
            var providerCalls = telemetry.SnapshotFrom(telemetryStart)
                .Select(ToTelemetryEvidence)
                .ToArray();
            var sampleReport = new
            {
                item.ItemId,
                item.Category,
                sourceHash = item.Baseline.SourceHash,
                figureObjective = item.Baseline.FigureObjective,
                model = options.VisionReviewModel,
                understanding = new
                {
                    liveUnderstanding.ProviderTraceId,
                    isBlocked = liveUnderstanding.IsBlocked,
                    blockingCodes = liveUnderstanding.BlockingCodes,
                    claims = liveUnderstanding.Claims.Select(claim => new
                    {
                        claim.MergeKey,
                        category = claim.Category.ToString(),
                        claim.NormalizedStatement,
                        claim.Confidence,
                        status = claim.Status.ToString(),
                        claim.SourceBlockId,
                        claim.QuotedText,
                    }),
                },
                render = new
                {
                    svg.Sha256,
                    exports.SemanticSha256,
                    artifacts = exports.Artifacts.Select(artifact => new
                    {
                        artifact.Format,
                        artifact.Sha256,
                        artifact.MimeType,
                    }),
                    contractReviewPassed = contractReport.Passed,
                },
                requests = new
                {
                    semanticPayloadHash,
                    visualPayloadHash,
                    store = false,
                    visualInput = "original PNG bytes plus typed critical-item crops",
                    redaction = "secrets, authorization headers, base64 image bodies, and absolute local paths omitted",
                },
                reviews = new
                {
                    semantic = ToReviewEvidence(semanticResult),
                    visual = ToReviewEvidence(visualResult),
                    machineReviewPassed = machineDecision.CanProceedToGate2,
                    blockers = machineDecision.Blockers,
                },
                providerCalls,
                humanGates = new
                {
                    gate1Status = "accepted-existing-baseline-authority",
                    reviewer = item.Baseline.HumanReview.Reviewer,
                    reviewedAt = item.Baseline.HumanReview.ReviewedAt,
                    corrections = Array.Empty<string>(),
                    gate2Status = "pending-human-review",
                    gate2Reviewer = (string?)null,
                    gate2ReviewedAt = (string?)null,
                },
                artifactDirectory = item.ItemId,
            };
            var sampleReportPath = Path.Combine(sampleDirectory, "review-bundle.json");
            await File.WriteAllTextAsync(
                sampleReportPath,
                JsonSerializer.Serialize(sampleReport, JsonOptions),
                CancellationToken.None);
            sampleReports.Add(sampleReport);

            Assert.True(contractReport.Passed);
            Assert.True(machineDecision.CanProceedToGate2);
            Assert.Equal(3, providerCalls.Length);
        }

        var report = new
        {
            schemaVersion = 1,
            runId = Path.GetFileName(artifactRoot),
            startedAt,
            completedAt = DateTimeOffset.UtcNow,
            provider = "OpenAI-compatible Responses",
            model = options.VisionReviewModel,
            paidCallsAuthorized = true,
            passedMachinePath = true,
            readyForHumanReview = true,
            accepted = false,
            acceptanceBlocker = "Three fresh Gate 2 human decisions are required.",
            cost = new
            {
                status = "unpriced-no-local-rate-card",
                currency = "USD",
                estimatedAmount = (decimal?)null,
                note = "Token usage is preserved per call; billing amount is not invented without a configured rate card.",
            },
            samples = sampleReports,
        };
        await File.WriteAllTextAsync(
            reportPath,
            JsonSerializer.Serialize(report, JsonOptions),
            CancellationToken.None);
    }

    private static object ToReviewEvidence(ScientificProviderReviewResult result) => new
    {
        verdict = result.Verdict.ToString(),
        result.ProviderTraceId,
        findings = result.Findings.Select(finding => new
        {
            finding.Code,
            kind = finding.Kind.ToString(),
            finding.ResponsibleItemId,
            finding.Evidence,
        }),
    };

    private static object ToTelemetryEvidence(ProviderCallTelemetry entry) => new
    {
        entry.Operation,
        entry.Model,
        entry.HttpStatusCode,
        entry.Succeeded,
        entry.RequestId,
        entry.ProviderTraceId,
        entry.Usage,
        entry.Latency,
        entry.EstimatedCostUsd,
        entry.RateCardName,
        entry.RecordedAt,
    };

    private static Task WriteJsonAsync(string path, object value) =>
        File.WriteAllTextAsync(
            path,
            JsonSerializer.Serialize(value, JsonOptions),
            CancellationToken.None);

    private static JsonElement? TryLoadPassedSampleReport(
        string directory,
        ScientificFigureCorpusDefinitionItem item)
    {
        var bundlePath = Path.Combine(directory, "review-bundle.json");
        if (!File.Exists(bundlePath))
        {
            return null;
        }

        using var document = JsonDocument.Parse(File.ReadAllText(bundlePath));
        var root = document.RootElement;
        if (root.GetProperty("itemId").GetString() != item.ItemId
            || root.GetProperty("category").GetString() != item.Category
            || root.GetProperty("understanding").GetProperty("isBlocked").GetBoolean()
            || !root.GetProperty("render").GetProperty("contractReviewPassed").GetBoolean()
            || !root.GetProperty("reviews").GetProperty("machineReviewPassed").GetBoolean()
            || root.GetProperty("humanGates").GetProperty("gate2Status").GetString()
                != "pending-human-review")
        {
            return null;
        }

        var providerCalls = root.GetProperty("providerCalls").EnumerateArray().ToArray();
        if (providerCalls.Length != 3
            || providerCalls.Any(call => !call.GetProperty("succeeded").GetBoolean()))
        {
            return null;
        }

        var render = root.GetProperty("render");
        var model = ScientificFigureCorpusRunner.BuildModel(item);
        var expectedWorkflow = ScientificFigureWorkflow.Create(model.Specification)
            .ApproveGate1(
                item.Baseline.HumanReview.Reviewer,
                "Checkpoint 0 accepted baseline authority reused without mutation.",
                ParseReviewTime(item.Baseline.HumanReview.ReviewedAt));
        var expectedSvgHash = new DeterministicSvgRenderer()
            .Render(new ScientificFigureSpecCompiler().Compile(expectedWorkflow))
            .Sha256;
        if (!string.Equals(
                render.GetProperty("sha256").GetString(),
                expectedSvgHash,
                StringComparison.OrdinalIgnoreCase)
            || !HashMatches(
                Path.Combine(directory, "figure.svg"),
                render.GetProperty("sha256").GetString()))
        {
            return null;
        }

        foreach (var artifact in render.GetProperty("artifacts").EnumerateArray())
        {
            var format = artifact.GetProperty("format").GetString();
            if (string.IsNullOrWhiteSpace(format)
                || !HashMatches(
                    Path.Combine(directory, $"figure.{format}"),
                    artifact.GetProperty("sha256").GetString()))
            {
                return null;
            }
        }

        return root.Clone();
    }

    private static bool HashMatches(string path, string? expected) =>
        File.Exists(path)
        && string.Equals(Hash(File.ReadAllBytes(path)), expected, StringComparison.OrdinalIgnoreCase);

    private static async Task WriteArtifactsAsync(
        string directory,
        ScientificSvgArtifact svg,
        ScientificFigureExportBundle exports,
        ScientificVisualReviewRequest visualRequest)
    {
        await File.WriteAllTextAsync(
            Path.Combine(directory, "figure.svg"),
            svg.Svg,
            CancellationToken.None);
        foreach (var artifact in exports.Artifacts)
        {
            await File.WriteAllBytesAsync(
                Path.Combine(directory, $"figure.{artifact.Format}"),
                artifact.Bytes,
                CancellationToken.None);
        }

        var cropDirectory = Path.Combine(directory, "crops");
        Directory.CreateDirectory(cropDirectory);
        foreach (var crop in visualRequest.RegionCrops)
        {
            await File.WriteAllBytesAsync(
                Path.Combine(cropDirectory, $"{crop.CropId}.png"),
                crop.Bytes,
                CancellationToken.None);
        }
    }

    private static string ResolveReportPath(string repositoryRoot)
    {
        var configured = Environment.GetEnvironmentVariable(ReportPathEnvironmentVariable);
        if (!string.IsNullOrWhiteSpace(configured))
        {
            return Path.GetFullPath(configured);
        }

        var stamp = DateTimeOffset.UtcNow.ToString("yyyyMMdd-HHmmss");
        return Path.Combine(
            repositoryRoot,
            "artifacts",
            "scientific-figure-live-acceptance",
            stamp,
            "report.json");
    }

    private static string Hash(byte[] bytes) =>
        $"sha256:{Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant()}";

    private static DateTimeOffset ParseReviewTime(string reviewedAt) =>
        DateTimeOffset.ParseExact(
            reviewedAt,
            "yyyy-MM-dd",
            System.Globalization.CultureInfo.InvariantCulture,
            System.Globalization.DateTimeStyles.AssumeUniversal);

    private static string FindRepositoryRoot()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory);
             directory is not null;
             directory = directory.Parent)
        {
            if (File.Exists(Path.Combine(directory.FullName, "ContentDeliveryStudio.sln")))
            {
                return directory.FullName;
            }
        }

        throw new DirectoryNotFoundException("Could not locate ContentDeliveryStudio.sln.");
    }

    private sealed class CapturingTelemetrySink : IProviderCallTelemetrySink
    {
        private readonly List<ProviderCallTelemetry> _entries = [];

        public int Count => _entries.Count;

        public void Record(ProviderCallTelemetry telemetry) => _entries.Add(telemetry);

        public IReadOnlyList<ProviderCallTelemetry> SnapshotFrom(int index) =>
            _entries.Skip(index).ToArray();
    }

    private sealed class FixedSemanticProvider(ScientificProviderReviewResult result)
        : IScientificSemanticReviewProvider
    {
        public Task<ScientificProviderReviewResult> ReviewAsync(
            ScientificSemanticReviewRequest request,
            CancellationToken cancellationToken) => Task.FromResult(result);
    }

    private sealed class FixedVisualProvider(ScientificProviderReviewResult result)
        : IScientificVisualReviewProvider
    {
        public Task<ScientificProviderReviewResult> ReviewAsync(
            ScientificVisualReviewRequest request,
            CancellationToken cancellationToken) => Task.FromResult(result);
    }
}
