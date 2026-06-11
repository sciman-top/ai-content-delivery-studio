using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using ImageSeriesStudio.Application.Delivery;
using ImageSeriesStudio.Application.Diagnostics;
using ImageSeriesStudio.Core.Generation;
using ImageSeriesStudio.Core.Projects;
using ImageSeriesStudio.Core.Providers;
using ImageSeriesStudio.Infrastructure.Diagnostics;
using ImageSeriesStudio.Infrastructure.Delivery;
using ImageSeriesStudio.Infrastructure.OpenAI;
using ImageSeriesStudio.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using SkiaSharp;

namespace ImageSeriesStudio.Tests;

public sealed class OpenAiLiveV1SampleRouteTests
{
    private const string RunOptInEnvironmentVariable = "IMAGE_SERIES_STUDIO_RUN_LIVE_V1_SAMPLE";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
    };

    [Fact]
    public async Task LiveV1SampleRoute_WhenExplicitlyOptedIn_RunsEndToEndAndWritesEvidence()
    {
        if (!ShouldRunLiveSample())
        {
            return;
        }

        var repositoryRoot = FindRepositoryRoot();
        var envPath = Path.Combine(repositoryRoot, ".env");
        if (!File.Exists(envPath))
        {
            throw new FileNotFoundException("Repository .env file was not found for the live sample run.", envPath);
        }

        var runStartedAt = DateTimeOffset.UtcNow;
        var runStamp = runStartedAt.ToString("yyyyMMdd-HHmmss");
        var artifactRoot = Path.Combine(repositoryRoot, "artifacts", "live-openai-v1-sample", runStamp);
        var generatedDirectory = Path.Combine(artifactRoot, "outputs", "generated");
        var deliveryDirectory = Path.Combine(artifactRoot, "outputs", "delivery");
        var diagnosticsDirectory = Path.Combine(artifactRoot, "diagnostics");
        var reviewPrepDirectory = Path.Combine(artifactRoot, "review-prep");
        var reviewAssetDirectory = Path.Combine(artifactRoot, "review-assets");
        var revisionDirectory = Path.Combine(artifactRoot, "prompt-revisions");
        var attemptDirectory = Path.Combine(artifactRoot, "attempts");

        Directory.CreateDirectory(generatedDirectory);
        Directory.CreateDirectory(deliveryDirectory);
        Directory.CreateDirectory(diagnosticsDirectory);
        Directory.CreateDirectory(reviewPrepDirectory);
        Directory.CreateDirectory(reviewAssetDirectory);
        Directory.CreateDirectory(revisionDirectory);
        Directory.CreateDirectory(attemptDirectory);

        var configuration = await ProviderEnvironmentConfiguration.FromDotEnvFileAsync(envPath, CancellationToken.None);
        var secretStore = BuildSecretStore(envPath);
        var environment = new SystemOpenAiEnvironment();
        var preflight = await OpenAiLaunchPreflight.EvaluateAsync(
            configuration,
            secretStore,
            environment,
            new OpenAiSmokeTestOptions(),
            CancellationToken.None);
        var preflightWriter = new OpenAiLaunchPreflightReportWriter();
        var preflightPaths = await preflightWriter.WriteAsync(diagnosticsDirectory, preflight, CancellationToken.None);

        Assert.True(
            preflight.CanRunLiveV1SampleSeries,
            "OpenAI launch preflight did not pass. Inspect the generated preflight report under artifacts/live-openai-v1-sample.");

        var telemetrySink = new CapturingProviderCallTelemetrySink();
        var textOptions = OpenAiProviderOptions.FromTextProviderEnvironment(configuration, realApiEnabled: true);
        var imageOptions = OpenAiProviderOptions.FromImageProviderEnvironment(configuration, realApiEnabled: true);

        using var textHttpClient = CreateProviderHttpClient(textOptions.BaseUri);

        var textProvider = new OpenAiTextPlanningProvider(textHttpClient, textOptions, secretStore, telemetrySink);
        var visionProvider = new OpenAiVisionReviewProvider(
            CreateProviderHttpClient(textOptions.BaseUri),
            textOptions,
            secretStore,
            telemetrySink);
        var imageProvider = new OpenAiOfficialSdkImageGenerationProvider(
            imageOptions,
            secretStore,
            new OpenAiSdkImageTransport(
                new OpenAiOfficialSdkImageBackend(
                    new OpenAiOfficialSdkFactory())),
            telemetrySink);

        var databasePath = Path.Combine(artifactRoot, "live-v1-sample.sqlite");
        var dbOptions = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite($"Data Source={databasePath};Pooling=False")
            .Options;

        await using (var setup = new AppDbContext(dbOptions))
        {
            await setup.Database.EnsureCreatedAsync();
        }

        await using var dbContext = new AppDbContext(dbOptions);
        var repository = new EfProjectRepository(dbContext);
        var project = ImageProject.Create("OpenAI live V1 sample", runStartedAt);
        var textProfile = project.AddProviderProfile("OpenAI text and review", ProviderKind.OpenAI, runStartedAt);
        var imageProfile = project.AddProviderProfile("OpenAI image generation", ProviderKind.OpenAI, runStartedAt);
        await repository.SaveAsync(project, CancellationToken.None);

        var planningRequest = new PlanningRequest(
            "Create a 2-item educational image series about buoyancy for middle-school teacher slides.",
            "middle-school science teachers",
            2,
            "clean minimal educational illustration, no embedded text, clear label space");
        var plan = await textProvider.CreatePlanAsync(planningRequest, CancellationToken.None);
        var planningTelemetry = telemetrySink.GetLatest("text-planning")
            ?? throw new InvalidOperationException("Planning telemetry was not captured for the live sample run.");

        var series = project.AddSeries(planningRequest.Goal, plan.Summary, runStartedAt.AddMinutes(1));
        var reviewRubric = ReviewRubricTemplateCatalog
            .GetById(ReviewRubricTemplateCatalog.GeneralImage)
            .CreateRubric(project.Id, runStartedAt.AddMinutes(2));
        var approvedItems = new List<DeliveryExportItem>();
        var itemSummaries = new List<object>();

        foreach (var plannedItem in plan.Items.Take(2).Select((item, index) => (Item: item, Index: index + 1)))
        {
            var itemTimestamp = runStartedAt.AddMinutes(2 + plannedItem.Index);
            var item = series.AddItem(plannedItem.Item.Title, plannedItem.Item.Brief, itemTimestamp);
            var attemptRecords = new List<object>();
            PromptVersion? winningPrompt = null;
            CandidateImage? winningCandidate = null;
            StructuredReviewOutput? winningReview = null;
            ImageGenerationResult? winningGeneration = null;

            var currentPrompt = BuildInitialPrompt(plannedItem.Item.PromptDraft, plannedItem.Item.Brief);
            for (var attempt = 1; attempt <= 3; attempt++)
            {
                var promptVersion = item.AddPromptVersion(
                    currentPrompt,
                    new GenerationSettings(1536, 1024, "medium", "png"),
                    imageProfile.Id,
                    itemTimestamp.AddSeconds(attempt));

                var generationResult = await imageProvider.GenerateImageAsync(
                    new ImageGenerationRequest(
                        item.Id,
                        promptVersion.Id,
                        promptVersion.PromptText,
                        promptVersion.Settings,
                        generatedDirectory,
                        $"{plannedItem.Index:000}-{attempt:00}-{SanitizeFileName(item.Title)}.png"),
                    CancellationToken.None);
                var generationTelemetry = telemetrySink.GetLatest("image-generation")
                    ?? throw new InvalidOperationException("Image-generation telemetry was not captured for the live sample run.");

                var generationTask = new GenerationTask(
                    Guid.NewGuid(),
                    item.Id,
                    promptVersion.Id,
                    imageProfile.Id,
                    GenerationTaskStatus.Succeeded,
                    attemptCount: 1,
                    maxRetries: 0,
                    generationResult.GeneratedAt,
                    generationResult.GeneratedAt);
                item.AddGenerationTask(generationTask, generationResult.GeneratedAt);

                var candidate = new CandidateImage(
                    generationResult.CandidateImageId,
                    item.Id,
                    promptVersion.Id,
                    generationTask.Id,
                    imageProfile.Id,
                    CandidateImageStatus.ReviewPending,
                    generationResult.AssetPath,
                    generationResult.MetadataPath,
                    generationResult.GeneratedAt);
                item.AddCandidateImage(candidate, generationResult.GeneratedAt);
                await repository.SaveAsync(project, CancellationToken.None);

                var reviewPrep = await BuildReviewPrepArtifactAsync(
                    reviewPrepDirectory,
                    item.Title,
                    generationResult.AssetPath,
                    generationResult.MetadataPath,
                    promptVersion.PromptText,
                    attempt,
                    CancellationToken.None);
                var reviewAssetPath = await CreateCompactReviewAssetAsync(
                    reviewAssetDirectory,
                    generationResult.AssetPath,
                    plannedItem.Index,
                    attempt,
                    item.Title,
                    CancellationToken.None);
                var compactReview = await RunCompactLiveVisionReviewAsync(
                    visionProvider,
                    telemetrySink,
                    candidate,
                    reviewAssetPath,
                    reviewRubric,
                    promptVersion.PromptText,
                    reviewPrep,
                    CancellationToken.None);
                var structuredReview = compactReview.Review;

                var reviewSnapshot = structuredReview.ToReviewResult(DateTimeOffset.UtcNow);
                await repository.SaveReviewResultAsync(project.Id, reviewSnapshot, CancellationToken.None);

                var attemptEvidencePath = Path.Combine(
                    attemptDirectory,
                    $"{plannedItem.Index:000}-{attempt:00}-{SanitizeFileName(item.Title)}.json");
                await File.WriteAllTextAsync(
                    attemptEvidencePath,
                    JsonSerializer.Serialize(
                        new
                        {
                            itemIndex = plannedItem.Index,
                            itemTitle = item.Title,
                            attempt,
                            promptVersionId = promptVersion.Id,
                            promptText = promptVersion.PromptText,
                            image = new
                            {
                                generationResult.AssetPath,
                                generationResult.MetadataPath,
                                generationResult.ProviderTraceId,
                                requestId = generationTelemetry.RequestId,
                                generationTelemetry.HttpStatusCode,
                                generationTelemetry.Succeeded,
                                generationTelemetry.RecordedAt,
                            },
                            reviewPrep,
                            reviewAssetPath,
                            review = new
                            {
                                decision = structuredReview.Decision.ToString(),
                                needsRepair = structuredReview.NeedsRepair,
                                comments = structuredReview.Comments,
                                suggestedFix = structuredReview.SuggestedFix,
                                hardFailures = structuredReview.HardFailures,
                                scores = structuredReview.Scores,
                                requestId = compactReview.RequestId,
                                providerTraceId = compactReview.ProviderTraceId,
                                compactReview.StatusCode,
                                succeeded = true,
                            },
                        },
                        JsonOptions),
                    CancellationToken.None);

                attemptRecords.Add(
                    new
                    {
                        attempt,
                        promptVersionId = promptVersion.Id,
                        assetPath = generationResult.AssetPath,
                        metadataPath = generationResult.MetadataPath,
                        decision = structuredReview.Decision.ToString(),
                        needsRepair = structuredReview.NeedsRepair,
                        suggestedFix = structuredReview.SuggestedFix,
                        requestId = compactReview.RequestId,
                        providerTraceId = compactReview.ProviderTraceId,
                        evidencePath = attemptEvidencePath,
                    });

                if (CanApprove(structuredReview))
                {
                    var approval = FinalApprovalWorkflow.Decide(
                        new FinalApprovalRequest(
                            structuredReview,
                            Approve: true,
                            Reviewer: "Codex live sample",
                            Notes: "Approved after live OpenAI generation and review."),
                        DateTimeOffset.UtcNow);
                    await repository.SaveReviewResultAsync(project.Id, approval.ReviewResult, CancellationToken.None);

                    winningPrompt = promptVersion;
                    winningCandidate = candidate;
                    winningReview = structuredReview;
                    winningGeneration = generationResult;
                    approvedItems.Add(
                        new DeliveryExportItem(
                            $"{plannedItem.Index:000}-{SanitizeFileName(item.Title)}",
                            item.Title,
                            generationResult.AssetPath,
                            generationResult.MetadataPath,
                            promptVersion.PromptText,
                            winningReview.Decision,
                            HumanApproved: true,
                            FinalReviewer: approval.Reviewer,
                            FinalApprovalNotes: approval.Notes,
                            FinalApprovalDecidedAt: approval.DecidedAt,
                            GenerationTaskId: generationTask.Id,
                            ArtifactRole: "live-openai-v1-sample"));
                    break;
                }

                if (attempt == 3)
                {
                    throw new InvalidOperationException(
                        $"Live OpenAI sample item '{item.Title}' did not reach an approvable review result after 3 attempts. See artifacts under {artifactRoot}.");
                }

                currentPrompt = await RefinePromptAsync(
                    textHttpClient,
                    textOptions,
                    secretStore,
                    promptVersion.PromptText,
                    plannedItem.Item.Brief,
                    structuredReview,
                    revisionDirectory,
                    plannedItem.Index,
                    attempt + 1,
                    CancellationToken.None);
            }

            if (winningPrompt is null || winningCandidate is null || winningReview is null || winningGeneration is null)
            {
                throw new InvalidOperationException($"Live OpenAI sample item '{item.Title}' did not produce a final approved candidate.");
            }

            itemSummaries.Add(
                new
                {
                    itemIndex = plannedItem.Index,
                    itemId = item.Id,
                    item.Title,
                    item.Brief,
                    finalPromptVersionId = winningPrompt.Id,
                    finalCandidateImageId = winningCandidate.Id,
                    winningGeneration.ProviderTraceId,
                    finalDecision = winningReview.Decision.ToString(),
                    attempts = attemptRecords,
                });
        }

        await repository.SaveAsync(project, CancellationToken.None);

        var deliveryWriter = new DeliveryPackageWriter();
        var delivery = await deliveryWriter.WriteAsync(
            new DeliveryExportRequest(
                project.Name,
                deliveryDirectory,
                approvedItems),
            CancellationToken.None);

        var secretSnapshots = await CreateSecretSnapshotsAsync(configuration, secretStore);
        var diagnosticsWriter = new DiagnosticsPackageWriter();
        var diagnostics = await diagnosticsWriter.WriteAsync(
            new DiagnosticsExportRequest(
                diagnosticsDirectory,
                new DiagnosticsApplicationSnapshot(
                    "AI Content Delivery Studio",
                    "0.1.0-live-sample",
                    "Debug",
                    runStartedAt),
                DiagnosticsPackageWriter.CaptureMachineSnapshot(),
                [DiagnosticsProjectSnapshot.FromProject(project)],
                [
                    new DiagnosticsProviderSnapshot(
                        textProvider.Capabilities.ProviderId,
                        textProvider.Capabilities.DisplayName,
                        ProviderKind.OpenAI.ToString(),
                        textProvider.Capabilities.ModelIds,
                        ["text-planning", "vision-review"],
                        RealApiEnabled: true,
                        DryRunOnly: false),
                    new DiagnosticsProviderSnapshot(
                        imageProvider.Capabilities.ProviderId,
                        imageProvider.Capabilities.DisplayName,
                        ProviderKind.OpenAI.ToString(),
                        imageProvider.Capabilities.ModelIds,
                        ["image-generation"],
                        RealApiEnabled: true,
                        DryRunOnly: false),
                ],
                secretSnapshots,
                OpenAiLaunchPreflight: OpenAiLaunchPreflightDiagnosticsSnapshotFactory.FromReport(preflight)),
            CancellationToken.None);

        var liveSummaryPath = Path.Combine(artifactRoot, "live-v1-sample-summary.json");
        await File.WriteAllTextAsync(
            liveSummaryPath,
            JsonSerializer.Serialize(
                new
                {
                    runStartedAt,
                    artifactRoot,
                    plan = new
                    {
                        planningRequest,
                        plan.ProviderTraceId,
                        requestId = planningTelemetry.RequestId,
                        plan.Summary,
                    },
                    preflight = new
                    {
                        preflight.CanRunLiveV1SampleSeries,
                        preflightPaths.JsonPath,
                        preflightPaths.MarkdownPath,
                    },
                    delivery,
                    diagnostics,
                    items = itemSummaries,
                },
                JsonOptions),
            CancellationToken.None);

        var secretValues = await ResolveSensitiveValuesAsync(configuration, secretStore);
        await AssertArtifactsAreRedactedAsync(artifactRoot, secretValues);

        Assert.Equal(2, approvedItems.Count);
        Assert.True(File.Exists(delivery.ManifestJsonPath));
        Assert.True(File.Exists(delivery.ReviewReportPath));
        Assert.True(File.Exists(diagnostics.JsonPath));
        Assert.True(File.Exists(liveSummaryPath));
    }

    private static bool ShouldRunLiveSample()
    {
        return string.Equals(Environment.GetEnvironmentVariable(RunOptInEnvironmentVariable), "1", StringComparison.Ordinal)
            && string.Equals(
                Environment.GetEnvironmentVariable(OpenAiSmokeTestGate.DefaultOptInEnvironmentVariable),
                "1",
                StringComparison.Ordinal);
    }

    private static HttpClient CreateProviderHttpClient(Uri baseUri)
    {
        return new HttpClient
        {
            BaseAddress = baseUri,
        };
    }

    private static IOpenAiSecretStore BuildSecretStore(string envPath)
    {
        if (OperatingSystem.IsWindows())
        {
            return new CompositeOpenAiSecretStore(
            [
                new DpapiOpenAiSecretStore(),
                new EnvironmentOpenAiSecretStore(),
                new DotEnvSecretStore(envPath),
            ]);
        }

        return new CompositeOpenAiSecretStore(
        [
            new EnvironmentOpenAiSecretStore(),
            new DotEnvSecretStore(envPath),
        ]);
    }

    private static string BuildInitialPrompt(string providerPromptDraft, string brief)
    {
        return string.Join(
            Environment.NewLine,
            [
                providerPromptDraft.Trim(),
                string.IsNullOrWhiteSpace(brief) ? string.Empty : $"Brief anchor: {brief.Trim()}",
                "Use a clean educational illustration style.",
                "Do not render any embedded text, letters, numbers, captions, or watermarks inside the image.",
                "Reserve uncluttered negative space for later deterministic labels.",
                "Keep a single clear focal concept and avoid visual clutter.",
            ]).Trim();
    }

    private static bool CanApprove(StructuredReviewOutput review)
    {
        return review.Decision is ReviewDecision.Pass && !review.NeedsRepair;
    }

    private static async Task<CompactVisionReviewResult> RunCompactLiveVisionReviewAsync(
        OpenAiVisionReviewProvider visionProvider,
        CapturingProviderCallTelemetrySink telemetrySink,
        CandidateImage candidate,
        string assetPath,
        ReviewRubric rubric,
        string promptText,
        ReviewPrepArtifactContract reviewPrep,
        CancellationToken cancellationToken)
    {
        var result = await visionProvider.ReviewAsync(
            new VisionReviewRequest(
                candidate.Id,
                assetPath,
                rubric,
                promptText,
                reviewPrep),
            cancellationToken);
        var telemetry = telemetrySink.GetLatest("vision-review")
            ?? throw new InvalidOperationException("Vision-review telemetry was not captured for the live sample run.");
        var structuredReview = StructuredReviewOutput.FromProviderResult(result, rubric);

        return new CompactVisionReviewResult(
            structuredReview,
            telemetry.RequestId,
            telemetry.ProviderTraceId,
            telemetry.HttpStatusCode);
    }

    private static async Task<string> CreateCompactReviewAssetAsync(
        string outputDirectory,
        string originalAssetPath,
        int itemIndex,
        int attempt,
        string itemTitle,
        CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(outputDirectory);

        using var originalBitmap = SKBitmap.Decode(originalAssetPath)
            ?? throw new InvalidOperationException($"Could not decode image for review compression: {originalAssetPath}");
        var maxDimension = Math.Max(originalBitmap.Width, originalBitmap.Height);
        if (maxDimension <= 384)
        {
            return originalAssetPath;
        }

        var scale = 384d / maxDimension;
        var resizedWidth = Math.Max(1, (int)Math.Round(originalBitmap.Width * scale));
        var resizedHeight = Math.Max(1, (int)Math.Round(originalBitmap.Height * scale));
        using var resizedBitmap = originalBitmap.Resize(
            new SKImageInfo(resizedWidth, resizedHeight),
            new SKSamplingOptions(SKFilterMode.Linear))
            ?? throw new InvalidOperationException("Could not resize image for compact review.");
        using var resizedImage = SKImage.FromBitmap(resizedBitmap);
        using var encodedImage = resizedImage.Encode(SKEncodedImageFormat.Jpeg, quality: 70);

        var reviewAssetPath = Path.Combine(
            outputDirectory,
            $"{itemIndex:000}-{attempt:00}-{SanitizeFileName(itemTitle)}-review.jpg");
        await File.WriteAllBytesAsync(reviewAssetPath, encodedImage.ToArray(), cancellationToken);
        return reviewAssetPath;
    }

    private static async Task<ReviewPrepArtifactContract> BuildReviewPrepArtifactAsync(
        string outputDirectory,
        string itemTitle,
        string assetPath,
        string metadataPath,
        string promptText,
        int attempt,
        CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(outputDirectory);
        var manifestPath = Path.Combine(
            outputDirectory,
            $"{attempt:00}-{SanitizeFileName(itemTitle)}-review-prep.json");
        var manifest = new ReviewPrepArtifactManifest(
            itemTitle,
            assetPath,
            metadataPath,
            promptText,
            [
                new ReviewPrepEvidenceSelection(
                    "candidate-image",
                    "generated-asset",
                    assetPath,
                    "Primary candidate image selected for live remote review."),
                new ReviewPrepEvidenceSelection(
                    "candidate-metadata",
                    "generation-metadata",
                    metadataPath,
                    "Generation metadata kept as provenance evidence."),
            ],
            DateTimeOffset.UtcNow);

        await File.WriteAllTextAsync(
            manifestPath,
            JsonSerializer.Serialize(manifest, JsonOptions),
            cancellationToken);

        return new ReviewPrepArtifactContract(
            Summary: $"Compact review prep for {itemTitle}",
            ManifestPath: manifestPath,
            ThumbnailGridPath: null,
            EvidenceSelections: manifest.EvidenceSelections);
    }

    private static async Task<string> RefinePromptAsync(
        HttpClient httpClient,
        OpenAiProviderOptions options,
        IOpenAiSecretStore secretStore,
        string currentPrompt,
        string brief,
        StructuredReviewOutput review,
        string revisionDirectory,
        int itemIndex,
        int nextAttempt,
        CancellationToken cancellationToken)
    {
        await OpenAiProviderGuard.EnsureCanCallRealApiAsync(
            options,
            secretStore,
            OpenAiProviderOperation.TextPlanning,
            cancellationToken);
        var apiKey = await secretStore.GetSecretAsync(options.ApiKeySecretName, cancellationToken)
            ?? throw new InvalidOperationException("OpenAI API key was not found while refining the prompt.");

        Directory.CreateDirectory(revisionDirectory);

        using var request = new HttpRequestMessage(HttpMethod.Post, new Uri(options.BaseUri, "responses"));
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        request.Content = JsonContent.Create(
            new Dictionary<string, object?>
            {
                ["model"] = options.TextPlanningModel,
                ["instructions"] = "Revise the image-generation prompt using the review feedback. Return only the revised prompt text. Do not use markdown, bullet points, or JSON.",
                ["input"] = string.Join(
                    Environment.NewLine,
                    [
                        $"Current prompt: {currentPrompt}",
                        $"Item brief: {brief}",
                        $"Review decision: {review.Decision}",
                        $"Review comments: {review.Comments}",
                        $"Suggested fix: {review.SuggestedFix ?? "None"}",
                        $"Hard failures: {string.Join("; ", review.HardFailures)}",
                        "Keep the revised prompt concise, specific, and visually clear.",
                        "Preserve the no-text-inside-image requirement and keep explicit negative space for deterministic labels.",
                    ]),
                ["store"] = false,
            },
            options: JsonOptions);

        using var response = await httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        await using var responseStream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var document = await JsonDocument.ParseAsync(responseStream, cancellationToken: cancellationToken);
        var revisedPrompt = ExtractOutputText(document.RootElement).Trim();
        if (string.IsNullOrWhiteSpace(revisedPrompt))
        {
            throw new InvalidOperationException("Prompt refinement returned an empty prompt.");
        }

        var revisionPath = Path.Combine(revisionDirectory, $"{itemIndex:000}-{nextAttempt:00}-prompt-revision.json");
        await File.WriteAllTextAsync(
            revisionPath,
            JsonSerializer.Serialize(
                new
                {
                    itemIndex,
                    nextAttempt,
                    currentPrompt,
                    review.Comments,
                    review.SuggestedFix,
                    revisedPrompt,
                    requestId = TryReadHeader(response, "x-request-id"),
                    providerTraceId = TryReadString(document.RootElement, "id"),
                },
                JsonOptions),
            cancellationToken);

        return revisedPrompt;
    }

    private static string ExtractOutputText(JsonElement root)
    {
        if (root.TryGetProperty("output_text", out var outputTextElement)
            && outputTextElement.ValueKind is JsonValueKind.String)
        {
            return outputTextElement.GetString()!;
        }

        if (root.TryGetProperty("output", out var outputElement)
            && outputElement.ValueKind is JsonValueKind.Array)
        {
            foreach (var outputItem in outputElement.EnumerateArray())
            {
                if (!outputItem.TryGetProperty("content", out var contentElement)
                    || contentElement.ValueKind is not JsonValueKind.Array)
                {
                    continue;
                }

                foreach (var contentItem in contentElement.EnumerateArray())
                {
                    if (contentItem.TryGetProperty("text", out var textElement)
                        && textElement.ValueKind is JsonValueKind.String)
                    {
                        return textElement.GetString()!;
                    }
                }
            }
        }

        throw new InvalidOperationException("OpenAI response did not include output text.");
    }

    private static string? TryReadHeader(HttpResponseMessage response, string headerName)
    {
        return response.Headers.TryGetValues(headerName, out var values)
            ? values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value))
            : null;
    }

    private static string? TryReadString(JsonElement root, string propertyName)
    {
        return root.TryGetProperty(propertyName, out var value) && value.ValueKind is JsonValueKind.String
            ? value.GetString()
            : null;
    }

    private static async Task<IReadOnlyList<DiagnosticsSecretSnapshot>> CreateSecretSnapshotsAsync(
        ProviderEnvironmentConfiguration configuration,
        IOpenAiSecretStore secretStore)
    {
        var secretNames = configuration.Text.ApiKeySecretNames
            .Concat(configuration.Image.ApiKeySecretNames)
            .Concat(
            new string?[]
            {
                configuration.Text.AppIdSecretName,
                configuration.Text.AppSecretSecretName,
                configuration.Image.AppIdSecretName,
                configuration.Image.AppSecretSecretName,
            }.OfType<string>())
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        var snapshots = new List<DiagnosticsSecretSnapshot>();
        foreach (var secretName in secretNames)
        {
            var value = await secretStore.GetSecretAsync(secretName, CancellationToken.None);
            snapshots.Add(new DiagnosticsSecretSnapshot(secretName, !string.IsNullOrWhiteSpace(value)));
        }

        return snapshots;
    }

    private static async Task<IReadOnlyList<string>> ResolveSensitiveValuesAsync(
        ProviderEnvironmentConfiguration configuration,
        IOpenAiSecretStore secretStore)
    {
        var secretNames = configuration.Text.ApiKeySecretNames
            .Concat(configuration.Image.ApiKeySecretNames)
            .Concat(
            new string?[]
            {
                configuration.Text.AppIdSecretName,
                configuration.Text.AppSecretSecretName,
                configuration.Image.AppIdSecretName,
                configuration.Image.AppSecretSecretName,
            }.OfType<string>())
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        var values = new List<string>();
        foreach (var secretName in secretNames)
        {
            var value = await secretStore.GetSecretAsync(secretName, CancellationToken.None);
            if (!string.IsNullOrWhiteSpace(value))
            {
                values.Add(value);
            }
        }

        return values;
    }

    private static async Task AssertArtifactsAreRedactedAsync(
        string artifactRoot,
        IReadOnlyList<string> sensitiveValues)
    {
        var textFileExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ".json",
            ".md",
            ".txt",
            ".csv",
        };

        foreach (var path in Directory.EnumerateFiles(artifactRoot, "*", SearchOption.AllDirectories))
        {
            if (!textFileExtensions.Contains(Path.GetExtension(path)))
            {
                continue;
            }

            var content = await File.ReadAllTextAsync(path, CancellationToken.None);
            foreach (var sensitiveValue in sensitiveValues)
            {
                if (!string.IsNullOrWhiteSpace(sensitiveValue)
                    && content.Contains(sensitiveValue, StringComparison.Ordinal))
                {
                    throw new InvalidOperationException(
                        $"Secret redaction check failed for live evidence artifact: {path}");
                }
            }
        }
    }

    private static string SanitizeFileName(string value)
    {
        var invalidChars = Path.GetInvalidFileNameChars();
        var sanitized = new string(value.Select(character => invalidChars.Contains(character) ? '-' : character).ToArray());
        return string.IsNullOrWhiteSpace(sanitized) ? "sample" : sanitized.Trim();
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "ImageSeriesStudio.sln")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not find ImageSeriesStudio.sln from the test output path.");
    }

    private sealed class CapturingProviderCallTelemetrySink : IProviderCallTelemetrySink
    {
        private readonly List<ProviderCallTelemetry> _entries = [];

        public void Record(ProviderCallTelemetry telemetry)
        {
            lock (_entries)
            {
                _entries.Add(telemetry);
            }
        }

        public ProviderCallTelemetry? GetLatest(string operation)
        {
            lock (_entries)
            {
                return _entries.LastOrDefault(entry => entry.Operation.Equals(operation, StringComparison.OrdinalIgnoreCase));
            }
        }
    }

    private sealed record CompactVisionReviewResult(
        StructuredReviewOutput Review,
        string? RequestId,
        string? ProviderTraceId,
        int StatusCode);
}
