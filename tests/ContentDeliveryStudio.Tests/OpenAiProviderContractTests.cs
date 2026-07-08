using System.Net;
using System.Text;
using System.Text.Json;
using ContentDeliveryStudio.Core.Documents;
using ContentDeliveryStudio.Core.Projects;
using ContentDeliveryStudio.Core.Providers;
using ContentDeliveryStudio.Core.Styles;
using ContentDeliveryStudio.Infrastructure.OpenAI;
using OpenAI.Responses;

namespace ContentDeliveryStudio.Tests;

#pragma warning disable OPENAI001 // Tests intentionally verify ADR 0009 SDK text-planning migration details.
public sealed class OpenAiProviderContractTests
{
    [Fact]
    public async Task TextPlanningProvider_PostsResponsesRequestAndParsesOutputText()
    {
        using var handler = new CaptureHandler(_ => JsonResponse(
            """
            {
              "id": "resp_plan_123",
              "output_text": "{\"summary\":\"Two poster plan\",\"items\":[{\"title\":\"Opening\",\"brief\":\"Introduce the topic\",\"promptDraft\":\"Create an opening poster\"},{\"title\":\"Detail\",\"brief\":\"Show the key detail\",\"promptDraft\":\"Create a detail poster\"}]}"
            }
            """));
        using var httpClient = new HttpClient(handler);
        var provider = CreateTextProvider(httpClient);

        var result = await provider.CreatePlanAsync(
            new PlanningRequest("gravity poster series", "middle school", 2, "clean diagrams"),
            CancellationToken.None);

        Assert.Equal("resp_plan_123", result.ProviderTraceId);
        Assert.Equal("Two poster plan", result.Summary);
        Assert.Equal(2, result.Items.Count);
        Assert.Equal("Opening", result.Items[0].Title);
        Assert.Equal("Create a detail poster", result.Items[1].PromptDraft);

        Assert.Equal(HttpMethod.Post, handler.LastRequest!.Method);
        Assert.Equal("https://api.openai.com/v1/responses", handler.LastRequest.RequestUri!.ToString());
        Assert.Equal("Bearer", handler.LastRequest.Headers.Authorization!.Scheme);
        Assert.Equal("test-openai-key", handler.LastRequest.Headers.Authorization.Parameter);

        using var payload = JsonDocument.Parse(handler.LastRequestBody!);
        Assert.Equal("gpt-5", payload.RootElement.GetProperty("model").GetString());
        Assert.False(payload.RootElement.GetProperty("store").GetBoolean());
        Assert.Equal(
            "json_schema",
            payload.RootElement.GetProperty("text").GetProperty("format").GetProperty("type").GetString());

        var input = payload.RootElement.GetProperty("input").GetString();
        Assert.Contains("gravity poster series", input);
        Assert.Contains("middle school", input);
        Assert.Contains("Item count: 2", input);
    }

    [Fact]
    public async Task TextPlanningProvider_RecordsRequestUsageLatencyAndCostTelemetry()
    {
        var telemetrySink = new RecordingTelemetrySink();
        using var handler = new CaptureHandler(_ => JsonResponse(
            """
            {
              "id": "resp_plan_telemetry",
              "output_text": "{\"summary\":\"Telemetry plan\",\"items\":[{\"title\":\"Opening\",\"brief\":\"A brief\",\"promptDraft\":\"A prompt\"}]}",
              "usage": {
                "input_tokens": 12,
                "input_tokens_details": { "cached_tokens": 3 },
                "output_tokens": 34,
                "output_tokens_details": { "reasoning_tokens": 5 },
                "total_tokens": 46
              }
            }
            """,
            requestId: "req_text_123"));
        using var httpClient = new HttpClient(handler);
        var provider = new OpenAiTextPlanningProvider(
            httpClient,
            new OpenAiProviderOptions { RealApiEnabled = true },
            new StaticSecretStore("test-openai-key"),
            telemetrySink,
            new OpenAiCostRateCard("test-card", 0.03m, 0.20m, 0.01m));

        await provider.CreatePlanAsync(new PlanningRequest("topic", "audience", 1), CancellationToken.None);

        var telemetry = Assert.Single(telemetrySink.Events);
        Assert.Equal("openai-text", telemetry.ProviderId);
        Assert.Equal("text-planning", telemetry.Operation);
        Assert.Equal("gpt-5", telemetry.Model);
        Assert.Equal("https://api.openai.com/v1/responses", telemetry.Endpoint);
        Assert.Equal(200, telemetry.HttpStatusCode);
        Assert.True(telemetry.Succeeded);
        Assert.Equal("req_text_123", telemetry.RequestId);
        Assert.Equal("resp_plan_telemetry", telemetry.ProviderTraceId);
        Assert.Equal(12, telemetry.Usage!.InputTokens);
        Assert.Equal(34, telemetry.Usage.OutputTokens);
        Assert.Equal(46, telemetry.Usage.TotalTokens);
        Assert.Equal(3, telemetry.Usage.CachedInputTokens);
        Assert.Equal(5, telemetry.Usage.ReasoningOutputTokens);
        Assert.True(telemetry.Latency >= TimeSpan.Zero);
        Assert.Equal(0.03m, telemetry.EstimatedCostUsd);
        Assert.Equal("test-card", telemetry.RateCardName);
    }

    [Fact]
    public async Task TextPlanningProvider_ParsesMessageContentFallback()
    {
        using var handler = new CaptureHandler(_ => JsonResponse(
            """
            {
              "id": "resp_plan_456",
              "output": [
                {
                  "type": "message",
                  "content": [
                    {
                      "type": "output_text",
                      "text": "{\"summary\":\"Fallback plan\",\"items\":[{\"title\":\"Frame 1\",\"brief\":\"A brief\",\"promptDraft\":\"A prompt\"}]}"
                    }
                  ]
                }
              ]
            }
            """));
        using var httpClient = new HttpClient(handler);
        var provider = CreateTextProvider(httpClient);

        var result = await provider.CreatePlanAsync(
            new PlanningRequest("topic", "audience", 1),
            CancellationToken.None);

        Assert.Equal("resp_plan_456", result.ProviderTraceId);
        Assert.Equal("Fallback plan", result.Summary);
        Assert.Single(result.Items);
    }

    [Fact]
    public async Task TextPlanningProvider_RejectsInvalidOutputJsonWithExplicitMessage()
    {
        using var handler = new CaptureHandler(_ => JsonResponse(
            """
            {
              "id": "resp_plan_invalid_json",
              "output_text": "{not valid json"
            }
            """));
        using var httpClient = new HttpClient(handler);
        var provider = CreateTextProvider(httpClient);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            provider.CreatePlanAsync(
                new PlanningRequest("topic", "audience", 1),
                CancellationToken.None));

        Assert.Contains("invalid", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("json", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task TextPlanningProvider_RejectsInvalidTopLevelJsonWithExplicitMessage()
    {
        using var handler = new CaptureHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{not valid json", Encoding.UTF8, "application/json"),
        });
        using var httpClient = new HttpClient(handler);
        var provider = CreateTextProvider(httpClient);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            provider.CreatePlanAsync(
                new PlanningRequest("topic", "audience", 1),
                CancellationToken.None));

        Assert.Contains("invalid", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("json", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task TextPlanningProvider_RejectsOversizedRequestBeforeSendingHttp()
    {
        using var handler = new CaptureHandler(_ => JsonResponse("{}"));
        using var httpClient = new HttpClient(handler);
        var provider = CreateTextProvider(httpClient);
        var oversizedGoal = new string('A', TextPlanningExecutionPolicy.DefaultMaxInputCharacters + 100);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            provider.CreatePlanAsync(
                new PlanningRequest(oversizedGoal, "audience", 1, "style"),
                CancellationToken.None));

        Assert.Contains("bounded", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("split", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(0, handler.CallCount);
    }

    [Fact]
    public async Task TextPlanningProvider_DoesNotSendHttpWhenRealApiIsDisabled()
    {
        using var handler = new CaptureHandler(_ => JsonResponse("{}"));
        using var httpClient = new HttpClient(handler);
        var provider = new OpenAiTextPlanningProvider(
            httpClient,
            new OpenAiProviderOptions(),
            new StaticSecretStore("test-openai-key"));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            provider.CreatePlanAsync(new PlanningRequest("topic", "audience", 1), CancellationToken.None));

        Assert.Equal(0, handler.CallCount);
    }

    [Fact]
    public async Task TextPlanningProvider_MapsNonSuccessResponsesToHttpRequestException()
    {
        using var handler = new CaptureHandler(_ => new HttpResponseMessage(HttpStatusCode.TooManyRequests)
        {
            ReasonPhrase = "Too Many Requests",
            Content = new StringContent("""{"error":{"message":"rate limited"}}""", Encoding.UTF8, "application/json"),
        });
        using var httpClient = new HttpClient(handler);
        var provider = CreateTextProvider(httpClient);

        var exception = await Assert.ThrowsAsync<HttpRequestException>(() =>
            provider.CreatePlanAsync(new PlanningRequest("topic", "audience", 1), CancellationToken.None));

        Assert.Contains("429", exception.Message, StringComparison.Ordinal);
        Assert.Equal(1, handler.CallCount);
    }

    [Fact]
    public async Task TextPlanningProvider_RecordsFailedHttpTelemetryBeforeThrowing()
    {
        var telemetrySink = new RecordingTelemetrySink();
        using var handler = new CaptureHandler(_ =>
        {
            var response = new HttpResponseMessage(HttpStatusCode.TooManyRequests)
            {
                ReasonPhrase = "Too Many Requests",
                Content = new StringContent("""{"error":{"message":"rate limited"}}""", Encoding.UTF8, "application/json"),
            };
            response.Headers.TryAddWithoutValidation("x-request-id", "req_text_failed");
            return response;
        });
        using var httpClient = new HttpClient(handler);
        var provider = new OpenAiTextPlanningProvider(
            httpClient,
            new OpenAiProviderOptions { RealApiEnabled = true },
            new StaticSecretStore("test-openai-key"),
            telemetrySink,
            new OpenAiCostRateCard("test-card", 0.03m, 0.20m, 0.01m));

        await Assert.ThrowsAsync<HttpRequestException>(() =>
            provider.CreatePlanAsync(new PlanningRequest("topic", "audience", 1), CancellationToken.None));

        var telemetry = Assert.Single(telemetrySink.Events);
        Assert.Equal("req_text_failed", telemetry.RequestId);
        Assert.Equal(429, telemetry.HttpStatusCode);
        Assert.False(telemetry.Succeeded);
        Assert.Null(telemetry.Usage);
        Assert.Equal(0m, telemetry.EstimatedCostUsd);
    }

    [Fact]
    public async Task TextPlanningProvider_BlocksPromptDirectionsUntilRealImplementationExists()
    {
        using var handler = new CaptureHandler(_ => JsonResponse("{}"));
        using var httpClient = new HttpClient(handler);
        var provider = CreateTextProvider(httpClient);

        var exception = await Assert.ThrowsAsync<NotSupportedException>(() =>
            provider.CreatePromptDirectionsAsync(
                new BriefPlanningRequest(
                    "poster",
                    "teachers",
                    "clean style",
                    ["accurate diagram"],
                    ["tiny text"],
                    DirectionCount: 2),
                CancellationToken.None));

        Assert.Contains("Brief direction planning is not implemented for OpenAI", exception.Message);
        Assert.Equal(0, handler.CallCount);
    }

    [Fact]
    public async Task TextPlanningProvider_ParsesDocumentIllustrationPlanFromStructuredResponse()
    {
        using var handler = new CaptureHandler(_ => JsonResponse(
            """
            {
              "id": "resp_doc_123",
              "output_text": "{\"brief\":{\"sourceKind\":\"Paste\",\"sourceDisplayName\":\"Quantum teaching note.txt\",\"title\":\"Quantum teaching note\",\"documentFamily\":\"Educational\",\"audience\":\"teachers\",\"sections\":[\"Introduction\",\"Analogy\"],\"keyClaims\":[\"Superposition needs a visual analogy.\"],\"visualOpportunities\":[\"Illustrate the contrast between single-state intuition and multi-state possibility.\"],\"knownConstraints\":[\"avoid fake lab data\"],\"strictnessLevel\":\"Educational\"},\"plan\":{\"summary\":\"Create one educational concept diagram grounded in the supplied teaching note.\",\"coverageNotes\":[\"Cover the central classroom analogy before expanding to deeper detail.\"],\"riskNotes\":[\"Do not imply real laboratory evidence or measured data.\"],\"targets\":[{\"title\":\"Concept diagram for superposition\",\"documentLocation\":\"Introduction\",\"purpose\":\"ConceptDiagram\",\"mustShow\":[\"A clear comparison between a single certain state and overlapping possible states\"],\"mustNotShow\":[\"fake lab apparatus readings\"],\"sourceEvidence\":[\"Superposition needs a visual analogy.\"],\"suggestedImageTypePresetId\":\"concept-diagram\",\"suggestedReviewRubricTemplateId\":\"educational-accuracy\",\"textPolicy\":\"DeterministicPostRender\",\"strictnessNotes\":[\"Use deterministic labels for any explanatory text.\"]}]}}"
            }
            """));
        using var httpClient = new HttpClient(handler);
        var provider = CreateTextProvider(httpClient);

        var result = await provider.CreateDocumentIllustrationPlanAsync(
            new DocumentIllustrationPlanningRequest(
                "Quantum teaching note",
                "Teachers need an intuitive explanation of superposition.",
                "teachers",
                DocumentFamily.Educational,
                IllustrationStrictnessLevel.Educational,
                ["Introduction", "Analogy"],
                ["Superposition needs a visual analogy."],
                ["avoid fake lab data"]),
            CancellationToken.None);

        Assert.Equal("resp_doc_123", result.ProviderTraceId);
        Assert.Equal("Quantum teaching note", result.Brief.Title);
        Assert.Equal(DocumentFamily.Educational, result.Brief.DocumentFamily);
        Assert.Equal(IllustrationStrictnessLevel.Educational, result.Brief.StrictnessLevel);
        var target = Assert.Single(result.Plan.Targets);
        Assert.Equal(IllustrationPurpose.ConceptDiagram, target.Purpose);
        Assert.Equal(ImageTextPolicy.DeterministicPostRender, target.TextPolicy);
        Assert.Equal("concept-diagram", target.SuggestedImageTypePresetId);
        Assert.Equal("educational-accuracy", target.SuggestedReviewRubricTemplateId);
        Assert.Contains("visual analogy", target.SourceEvidence[0], StringComparison.OrdinalIgnoreCase);

        using var payload = JsonDocument.Parse(handler.LastRequestBody!);
        Assert.Equal("gpt-5", payload.RootElement.GetProperty("model").GetString());
        Assert.False(payload.RootElement.GetProperty("store").GetBoolean());
        Assert.Contains("Quantum teaching note", payload.RootElement.GetProperty("input").GetString());
        Assert.Contains("teachers", payload.RootElement.GetProperty("input").GetString());
        Assert.Equal(
            "document_illustration_plan",
            payload.RootElement.GetProperty("text").GetProperty("format").GetProperty("name").GetString());
    }

    [Fact]
    public async Task SdkTextPlanningProvider_ParsesDocumentIllustrationPlanFromStructuredResponse()
    {
        var client = new FakeResponsesClient(
            SdkJsonResponse(
                """
                {
                  "id": "resp_sdk_doc_123",
                  "output_text": "{\"brief\":{\"sourceKind\":\"Paste\",\"sourceDisplayName\":\"Quantum teaching note.txt\",\"title\":\"Quantum teaching note\",\"documentFamily\":\"Educational\",\"audience\":\"teachers\",\"sections\":[\"Introduction\"],\"keyClaims\":[\"Superposition needs a visual analogy.\"],\"visualOpportunities\":[\"Focus on the central comparison.\"],\"knownConstraints\":[\"avoid fake lab data\"],\"strictnessLevel\":\"Educational\"},\"plan\":{\"summary\":\"Create one educational concept diagram grounded in the supplied teaching note.\",\"coverageNotes\":[\"Cover the central classroom analogy first.\"],\"riskNotes\":[\"Do not imply real laboratory evidence or measured data.\"],\"targets\":[{\"title\":\"Concept diagram for superposition\",\"documentLocation\":\"Introduction\",\"purpose\":\"ConceptDiagram\",\"mustShow\":[\"A clear comparison between a single certain state and overlapping possible states\"],\"mustNotShow\":[\"fake lab apparatus readings\"],\"sourceEvidence\":[\"Superposition needs a visual analogy.\"],\"suggestedImageTypePresetId\":\"concept-diagram\",\"suggestedReviewRubricTemplateId\":\"educational-accuracy\",\"textPolicy\":\"DeterministicPostRender\",\"strictnessNotes\":[\"Use deterministic labels for any explanatory text.\"]}]}}"
                }
                """,
                requestId: "req_sdk_doc_123"));
        var provider = CreateSdkTextProvider(client);

        var result = await provider.CreateDocumentIllustrationPlanAsync(
            new DocumentIllustrationPlanningRequest(
                "Quantum teaching note",
                "Teachers need an intuitive explanation of superposition.",
                "teachers",
                DocumentFamily.Educational,
                IllustrationStrictnessLevel.Educational,
                ["Introduction"],
                ["Superposition needs a visual analogy."],
                ["avoid fake lab data"]),
            CancellationToken.None);

        Assert.Equal("resp_sdk_doc_123", result.ProviderTraceId);
        Assert.Equal("Quantum teaching note", result.Brief.Title);
        Assert.Single(result.Plan.Targets);
        Assert.Equal(1, client.CallCount);
        Assert.NotNull(client.LastOptions);
        Assert.Equal("gpt-5", client.LastOptions!.Model);
        Assert.False(client.LastOptions.StoredOutputEnabled);
        Assert.NotNull(client.LastOptions.TextOptions);
    }

    [Fact]
    public async Task SdkTextPlanningProvider_RetriesSingleTransientUpstream502AndThenSucceeds()
    {
        var telemetrySink = new RecordingTelemetrySink();
        var transientFailure = new OpenAiResponsesClientException(
            SdkJsonResponse(
                """{"error":{"type":"server_error","code":"upstream_error","message":"temporary upstream failure"}}""",
                statusCode: 502,
                reasonPhrase: "Bad Gateway",
                requestId: "req_sdk_text_failed"),
            new InvalidOperationException("HTTP 502 (server_error: upstream_error)"));
        var client = new SequencedFakeResponsesClient(
        [
            transientFailure,
            SdkJsonResponse(
                """
                {
                  "id": "resp_sdk_text_retry",
                  "output_text": "{\"summary\":\"Retry plan\",\"items\":[{\"title\":\"Opening\",\"brief\":\"A brief\",\"promptDraft\":\"A prompt\"}]}",
                  "usage": {
                    "input_tokens": 10,
                    "output_tokens": 12,
                    "total_tokens": 22
                  }
                }
                """,
                requestId: "req_sdk_text_retry"),
        ]);
        var provider = new OpenAiSdkTextPlanningProvider(
            new OpenAiProviderOptions { RealApiEnabled = true },
            client,
            new StaticSecretStore("test-openai-key"),
            telemetrySink,
            new OpenAiCostRateCard("test-card", 0.03m, 0.20m, 0.01m));

        var result = await provider.CreatePlanAsync(
            new PlanningRequest("topic", "audience", 1),
            CancellationToken.None);

        Assert.Equal("resp_sdk_text_retry", result.ProviderTraceId);
        Assert.Equal("Retry plan", result.Summary);
        Assert.Equal(2, client.CallCount);

        var telemetry = Assert.Single(telemetrySink.Events);
        Assert.Equal("openai-text-sdk", telemetry.ProviderId);
        Assert.True(telemetry.Succeeded);
        Assert.Equal(200, telemetry.HttpStatusCode);
        Assert.Equal("req_sdk_text_retry", telemetry.RequestId);
    }

    [Fact]
    public async Task SdkTextPlanningProvider_RejectsInvalidStructuredJsonWithExplicitMessage()
    {
        var client = new FakeResponsesClient(
            SdkJsonResponse(
                """
                {
                  "id": "resp_sdk_invalid_json",
                  "output_text": "{ definitely not json"
                }
                """,
                requestId: "req_sdk_invalid_json"));
        var provider = CreateSdkTextProvider(client);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            provider.CreatePlanAsync(
                new PlanningRequest("topic", "audience", 1),
                CancellationToken.None));

        Assert.Contains("invalid", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("json", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SdkTextPlanningProvider_DoesNotRetryOrdinarySdkFailure()
    {
        var telemetrySink = new RecordingTelemetrySink();
        var sdkFailure = new OpenAiResponsesClientException(
            SdkJsonResponse(
                """{"error":{"type":"rate_limit_error","message":"rate limited"}}""",
                statusCode: 429,
                reasonPhrase: "Too Many Requests",
                requestId: "req_sdk_text_429"),
            new InvalidOperationException("SDK request failed."));
        var client = new SequencedFakeResponsesClient([sdkFailure]);
        var provider = new OpenAiSdkTextPlanningProvider(
            new OpenAiProviderOptions { RealApiEnabled = true },
            client,
            new StaticSecretStore("test-openai-key"),
            telemetrySink,
            new OpenAiCostRateCard("test-card", 0.03m, 0.20m, 0.01m));

        var exception = await Assert.ThrowsAsync<HttpRequestException>(() =>
            provider.CreatePlanAsync(new PlanningRequest("topic", "audience", 1), CancellationToken.None));

        Assert.Contains("429", exception.Message, StringComparison.Ordinal);
        Assert.Equal(1, client.CallCount);

        var telemetry = Assert.Single(telemetrySink.Events);
        Assert.Equal("openai-text-sdk", telemetry.ProviderId);
        Assert.Equal(429, telemetry.HttpStatusCode);
        Assert.False(telemetry.Succeeded);
        Assert.Equal("req_sdk_text_429", telemetry.RequestId);
    }

    [Fact]
    public async Task ImageGenerationProvider_PostsImageRequestAndWritesAssetAndMetadata()
    {
        var rootDirectory = Path.Combine(Path.GetTempPath(), "ContentDeliveryStudio.Tests", Guid.NewGuid().ToString("N"));
        var imageBytes = new byte[] { 137, 80, 78, 71 };
        using var handler = new CaptureHandler(_ => JsonResponse(
            $$"""
            {
              "id": "img_resp_123",
              "created": 1790000000,
              "data": [
                {
                  "b64_json": "{{Convert.ToBase64String(imageBytes)}}"
                }
              ]
            }
            """));
        using var httpClient = new HttpClient(handler);
        var provider = CreateImageProvider(httpClient);

        try
        {
            var result = await provider.GenerateImageAsync(
                new ImageGenerationRequest(
                    Guid.Parse("11111111-1111-1111-1111-111111111111"),
                    Guid.Parse("22222222-2222-2222-2222-222222222222"),
                    "Create a clean science poster.",
                    new GenerationSettings(1024, 1024, "standard", "png"),
                    rootDirectory,
                    "science-poster.png"),
                CancellationToken.None);

            Assert.Equal("img_resp_123", result.ProviderTraceId);
            Assert.Equal(imageBytes, await File.ReadAllBytesAsync(result.AssetPath, CancellationToken.None));
            Assert.True(File.Exists(result.MetadataPath));

            Assert.Equal(HttpMethod.Post, handler.LastRequest!.Method);
            Assert.Equal("https://api.openai.com/v1/images/generations", handler.LastRequest.RequestUri!.ToString());
            Assert.Equal("Bearer", handler.LastRequest.Headers.Authorization!.Scheme);
            Assert.Equal("test-openai-key", handler.LastRequest.Headers.Authorization.Parameter);

            using var payload = JsonDocument.Parse(handler.LastRequestBody!);
            Assert.Equal("gpt-image-2", payload.RootElement.GetProperty("model").GetString());
            Assert.Equal("Create a clean science poster.", payload.RootElement.GetProperty("prompt").GetString());
            Assert.Equal("1024x1024", payload.RootElement.GetProperty("size").GetString());
            Assert.Equal("auto", payload.RootElement.GetProperty("quality").GetString());
            Assert.Equal("png", payload.RootElement.GetProperty("output_format").GetString());
            Assert.Equal(1, payload.RootElement.GetProperty("n").GetInt32());
            Assert.False(payload.RootElement.GetProperty("store").GetBoolean());

            using var metadata = JsonDocument.Parse(await File.ReadAllTextAsync(result.MetadataPath, CancellationToken.None));
            Assert.Equal("openai-image", metadata.RootElement.GetProperty("providerId").GetString());
            Assert.Equal("gpt-image-2", metadata.RootElement.GetProperty("model").GetString());
            Assert.Equal("img_resp_123", metadata.RootElement.GetProperty("providerTraceId").GetString());
            Assert.Equal("images", metadata.RootElement.GetProperty("endpointFamily").GetString());
            Assert.False(metadata.RootElement.GetProperty("store").GetBoolean());
        }
        finally
        {
            if (Directory.Exists(rootDirectory))
            {
                Directory.Delete(rootDirectory, recursive: true);
            }
        }
    }

    [Fact]
    public async Task ImageGenerationProvider_RecordsTelemetryAndWritesSafeMetadataSummary()
    {
        var rootDirectory = Path.Combine(Path.GetTempPath(), "ContentDeliveryStudio.Tests", Guid.NewGuid().ToString("N"));
        var imageBytes = new byte[] { 137, 80, 78, 71 };
        var telemetrySink = new RecordingTelemetrySink();
        using var handler = new CaptureHandler(_ => JsonResponse(
            $$"""
            {
              "id": "img_resp_telemetry",
              "created": 1790000000,
              "data": [
                {
                  "b64_json": "{{Convert.ToBase64String(imageBytes)}}"
                }
              ]
            }
            """,
            requestId: "req_image_123"));
        using var httpClient = new HttpClient(handler);
        var provider = new OpenAiImageGenerationProvider(
            httpClient,
            new OpenAiProviderOptions { RealApiEnabled = true },
            new StaticSecretStore("test-openai-key"),
            telemetrySink,
            new OpenAiCostRateCard("test-card", 0.03m, 0.20m, 0.01m));

        try
        {
            var result = await provider.GenerateImageAsync(
                new ImageGenerationRequest(
                    Guid.NewGuid(),
                    Guid.NewGuid(),
                    "Create a clean science poster.",
                    new GenerationSettings(1024, 1024, "standard", "png"),
                    rootDirectory,
                    "science-poster.png"),
                CancellationToken.None);

            var telemetry = Assert.Single(telemetrySink.Events);
            Assert.Equal("openai-image", telemetry.ProviderId);
            Assert.Equal("image-generation", telemetry.Operation);
            Assert.Equal("gpt-image-2", telemetry.Model);
            Assert.Equal("req_image_123", telemetry.RequestId);
            Assert.Equal("img_resp_telemetry", telemetry.ProviderTraceId);
            Assert.Equal(0.20m, telemetry.EstimatedCostUsd);

            using var metadata = JsonDocument.Parse(await File.ReadAllTextAsync(result.MetadataPath, CancellationToken.None));
            var metadataTelemetry = metadata.RootElement.GetProperty("telemetry");
            Assert.Equal("req_image_123", metadataTelemetry.GetProperty("requestId").GetString());
            Assert.Equal(200, metadataTelemetry.GetProperty("httpStatusCode").GetInt32());
            Assert.True(metadataTelemetry.GetProperty("succeeded").GetBoolean());
            Assert.Equal(0.20m, metadataTelemetry.GetProperty("estimatedCostUsd").GetDecimal());
            Assert.Equal("test-card", metadataTelemetry.GetProperty("rateCardName").GetString());
        }
        finally
        {
            if (Directory.Exists(rootDirectory))
            {
                Directory.Delete(rootDirectory, recursive: true);
            }
        }
    }

    [Fact]
    public async Task ImageGenerationProvider_DoesNotSendHttpWhenRealApiIsDisabled()
    {
        using var handler = new CaptureHandler(_ => JsonResponse("{}"));
        using var httpClient = new HttpClient(handler);
        var provider = new OpenAiImageGenerationProvider(
            httpClient,
            new OpenAiProviderOptions(),
            new StaticSecretStore("test-openai-key"));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            provider.GenerateImageAsync(
                new ImageGenerationRequest(
                    Guid.NewGuid(),
                    Guid.NewGuid(),
                    "prompt",
                    new GenerationSettings(1024, 1024, "standard", "png"),
                    Path.GetTempPath()),
                CancellationToken.None));

        Assert.Equal(0, handler.CallCount);
    }

    [Fact]
    public async Task ImageGenerationProvider_MapsNonSuccessResponsesToHttpRequestException()
    {
        using var handler = new CaptureHandler(_ => new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            ReasonPhrase = "Bad Request",
            Content = new StringContent("""{"error":{"message":"invalid image request"}}""", Encoding.UTF8, "application/json"),
        });
        using var httpClient = new HttpClient(handler);
        var provider = CreateImageProvider(httpClient);

        var exception = await Assert.ThrowsAsync<HttpRequestException>(() =>
            provider.GenerateImageAsync(
                new ImageGenerationRequest(
                    Guid.NewGuid(),
                    Guid.NewGuid(),
                    "prompt",
                    new GenerationSettings(1024, 1024, "standard", "png"),
                    Path.GetTempPath()),
                CancellationToken.None));

        Assert.Contains("400", exception.Message, StringComparison.Ordinal);
        Assert.Equal(1, handler.CallCount);
    }

    [Fact]
    public async Task VisionReviewProvider_PostsImageInputAndParsesStructuredReview()
    {
        var rootDirectory = Path.Combine(Path.GetTempPath(), "ContentDeliveryStudio.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(rootDirectory);
        var imagePath = Path.Combine(rootDirectory, "candidate.png");
        await File.WriteAllBytesAsync(imagePath, [1, 2, 3, 4], CancellationToken.None);

        using var handler = new CaptureHandler(_ => JsonResponse(
            """
            {
              "id": "resp_review_123",
              "output_text": "{\"decision\":\"pass\",\"scores\":{\"match\":5},\"hardFailures\":[],\"comments\":\"Looks aligned.\",\"suggestedFix\":null}"
            }
            """));
        using var httpClient = new HttpClient(handler);
        var provider = CreateVisionProvider(httpClient);
        var candidateId = Guid.Parse("33333333-3333-3333-3333-333333333333");

        try
        {
            var result = await provider.ReviewAsync(
                new VisionReviewRequest(
                    candidateId,
                    imagePath,
                    new ReviewRubric(
                        Guid.NewGuid(),
                        Guid.NewGuid(),
                        "Default review",
                        [new ReviewRubricDimension("match", "Image should match the prompt.", 1)],
                        DateTimeOffset.UtcNow),
                    "Create a clean science poster.",
                    new ReviewPrepArtifactContract(
                        "Compact local review prep.",
                        EvidenceSelections:
                        [
                            new ReviewPrepEvidenceSelection(
                                "candidate-image",
                                "generated-asset",
                                imagePath,
                                "Primary local candidate image selected for bounded remote review."),
                            new ReviewPrepEvidenceSelection(
                                "candidate-metadata",
                                "generation-metadata",
                                Path.ChangeExtension(imagePath, ".json"),
                                "Local generation sidecar metadata kept as provenance evidence."),
                        ])),
                CancellationToken.None);

            Assert.Equal(candidateId, result.CandidateImageId);
            Assert.Equal(ReviewDecision.Pass, result.Decision);
            Assert.Equal(5, result.Scores["match"]);
            Assert.Empty(result.HardFailures);
            Assert.Equal("Looks aligned.", result.Comments);
            Assert.Null(result.SuggestedFix);

            Assert.Equal(HttpMethod.Post, handler.LastRequest!.Method);
            Assert.Equal("https://api.openai.com/v1/responses", handler.LastRequest.RequestUri!.ToString());
            Assert.Equal("Bearer", handler.LastRequest.Headers.Authorization!.Scheme);

            using var payload = JsonDocument.Parse(handler.LastRequestBody!);
            Assert.Equal("gpt-5", payload.RootElement.GetProperty("model").GetString());
            Assert.False(payload.RootElement.GetProperty("store").GetBoolean());
            Assert.False(payload.RootElement.TryGetProperty("previous_response_id", out _));
            Assert.Equal(
                "json_schema",
                payload.RootElement.GetProperty("text").GetProperty("format").GetProperty("type").GetString());

            var content = payload.RootElement
                .GetProperty("input")[0]
                .GetProperty("content");
            Assert.Equal("input_text", content[0].GetProperty("type").GetString());
            Assert.Contains("Create a clean science poster.", content[0].GetProperty("text").GetString());
            Assert.Contains("Rubric dimensions:", content[0].GetProperty("text").GetString());
            Assert.Contains("Compact review summary:", content[0].GetProperty("text").GetString());
            Assert.Equal("input_image", content[1].GetProperty("type").GetString());
            Assert.StartsWith("data:image/png;base64,", content[1].GetProperty("image_url").GetString());
            Assert.Equal("low", content[1].GetProperty("detail").GetString());

            var schema = payload.RootElement.GetProperty("text").GetProperty("format").GetProperty("schema");
            var required = schema.GetProperty("required").EnumerateArray().Select(element => element.GetString() ?? string.Empty).ToArray();
            Assert.Equal(["decision", "comments"], required);
        }
        finally
        {
            if (Directory.Exists(rootDirectory))
            {
                Directory.Delete(rootDirectory, recursive: true);
            }
        }
    }

    [Fact]
    public async Task VisionReviewProvider_RecordsRequestUsageLatencyAndCostTelemetry()
    {
        var rootDirectory = Path.Combine(Path.GetTempPath(), "ContentDeliveryStudio.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(rootDirectory);
        var imagePath = Path.Combine(rootDirectory, "candidate.png");
        await File.WriteAllBytesAsync(imagePath, [1, 2, 3, 4], CancellationToken.None);
        var telemetrySink = new RecordingTelemetrySink();
        using var handler = new CaptureHandler(_ => JsonResponse(
            """
            {
              "id": "resp_review_telemetry",
              "output_text": "{\"decision\":\"pass\",\"scores\":{\"match\":5},\"hardFailures\":[],\"comments\":\"Looks aligned.\",\"suggestedFix\":null}",
              "usage": {
                "input_tokens": 22,
                "output_tokens": 8,
                "total_tokens": 30
              }
            }
            """,
            requestId: "req_vision_123"));
        using var httpClient = new HttpClient(handler);
        var provider = new OpenAiVisionReviewProvider(
            httpClient,
            new OpenAiProviderOptions { RealApiEnabled = true },
            new StaticSecretStore("test-openai-key"),
            telemetrySink,
            new OpenAiCostRateCard("test-card", 0.03m, 0.20m, 0.01m));

        try
        {
            await provider.ReviewAsync(CreateVisionReviewRequest(imagePath), CancellationToken.None);

            var telemetry = Assert.Single(telemetrySink.Events);
            Assert.Equal("openai-vision", telemetry.ProviderId);
            Assert.Equal("vision-review", telemetry.Operation);
            Assert.Equal("gpt-5", telemetry.Model);
            Assert.Equal("req_vision_123", telemetry.RequestId);
            Assert.Equal("resp_review_telemetry", telemetry.ProviderTraceId);
            Assert.Equal(22, telemetry.Usage!.InputTokens);
            Assert.Equal(8, telemetry.Usage.OutputTokens);
            Assert.Equal(30, telemetry.Usage.TotalTokens);
            Assert.Equal(0.01m, telemetry.EstimatedCostUsd);
        }
        finally
        {
            if (Directory.Exists(rootDirectory))
            {
                Directory.Delete(rootDirectory, recursive: true);
            }
        }
    }

    [Fact]
    public async Task VisionReviewProvider_BackfillsRubricScoresAndSuggestedFixWhenCompactResponseOmitsRichFields()
    {
        var rootDirectory = Path.Combine(Path.GetTempPath(), "ContentDeliveryStudio.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(rootDirectory);
        var imagePath = Path.Combine(rootDirectory, "candidate.png");
        await File.WriteAllBytesAsync(imagePath, [1, 2, 3, 4], CancellationToken.None);
        using var handler = new CaptureHandler(_ => JsonResponse(
            """
            {
              "id": "resp_review_compact_fail",
              "output_text": "{\"decision\":\"fail\",\"comments\":\"Visible text and crowded layout.\"}"
            }
            """));
        using var httpClient = new HttpClient(handler);
        var provider = CreateVisionProvider(httpClient);
        var rubric = ReviewRubricTemplateCatalog
            .GetById(ReviewRubricTemplateCatalog.GeneralImage)
            .CreateRubric(Guid.NewGuid(), DateTimeOffset.UtcNow);

        try
        {
            var result = await provider.ReviewAsync(
                new VisionReviewRequest(
                    Guid.NewGuid(),
                    imagePath,
                    rubric,
                    "Create a clean science poster with empty label space.",
                    new ReviewPrepArtifactContract(
                        "Compact local review prep.",
                        EvidenceSelections:
                        [
                            new ReviewPrepEvidenceSelection(
                                "candidate-image",
                                "generated-asset",
                                imagePath,
                                "Primary local candidate image selected for bounded remote review."),
                        ])),
                CancellationToken.None);

            Assert.Equal(ReviewDecision.Fail, result.Decision);
            Assert.Equal("Visible text and crowded layout.", result.Comments);
            Assert.Equal("Visible text and crowded layout.", result.SuggestedFix);
            Assert.Empty(result.HardFailures);
            Assert.Equal(rubric.Dimensions.Count, result.Scores.Count);
            Assert.All(
                rubric.Dimensions,
                dimension => Assert.Equal(1, result.Scores[dimension.Name]));
        }
        finally
        {
            if (Directory.Exists(rootDirectory))
            {
                Directory.Delete(rootDirectory, recursive: true);
            }
        }
    }

    [Fact]
    public async Task ImageGenerationProvider_AddsAppModeHeadersWhenConfigured()
    {
        using var handler = new CaptureHandler(_ => JsonResponse(
            """
            {
              "id": "img_resp_app_mode",
              "created": 1790000000,
              "data": [
                {
                  "b64_json": "iVBORw=="
                }
              ]
            }
            """));
        using var httpClient = new HttpClient(handler);
        var provider = new OpenAiImageGenerationProvider(
            httpClient,
            new OpenAiProviderOptions
            {
                RealApiEnabled = true,
                AppIdSecretName = "IMAGE_PROVIDER_APP_ID",
                AppSecretSecretName = "IMAGE_PROVIDER_APP_SECRET",
            },
            new StaticSecretMapStore(
                new Dictionary<string, string?>
                {
                    ["OPENAI_API_KEY"] = "test-openai-key",
                    ["IMAGE_PROVIDER_APP_ID"] = "app-id",
                    ["IMAGE_PROVIDER_APP_SECRET"] = "app-secret",
                }));

        await provider.GenerateImageAsync(
            new ImageGenerationRequest(
                Guid.NewGuid(),
                Guid.NewGuid(),
                "Create a clean science poster.",
                new GenerationSettings(1024, 1024, "standard", "png"),
                Path.GetTempPath()),
            CancellationToken.None);

        Assert.True(handler.LastRequest!.Headers.TryGetValues("X-App-ID", out var appIdValues));
        Assert.Equal("app-id", Assert.Single(appIdValues));
        Assert.True(handler.LastRequest.Headers.TryGetValues("X-App-Secret", out var appSecretValues));
        Assert.Equal("app-secret", Assert.Single(appSecretValues));
    }

    [Fact]
    public async Task ImageGenerationProvider_UsesResponsesStatefulPathWhenExplicitlyEnabled()
    {
        var rootDirectory = Path.Combine(Path.GetTempPath(), "ContentDeliveryStudio.Tests", Guid.NewGuid().ToString("N"));
        using var handler = new CaptureHandler(_ => JsonResponse(
            """
            {
              "id": "resp_image_stateful_123",
              "output": [
                {
                  "id": "ig_call_123",
                  "type": "image_generation_call",
                  "revised_prompt": "A cleaner revised prompt for the final render.",
                  "result": "iVBORw=="
                }
              ],
              "usage": {
                "input_tokens": 20,
                "output_tokens": 10,
                "total_tokens": 30
              }
            }
            """,
            requestId: "req_image_stateful_123"));
        using var httpClient = new HttpClient(handler);
        var provider = new OpenAiImageGenerationProvider(
            httpClient,
            new OpenAiProviderOptions
            {
                RealApiEnabled = true,
                ImageGenerationResponsesModel = "gpt-5.5",
                ImageGenerationAllowsResponsesState = true,
            },
            new StaticSecretStore("test-openai-key"));

        try
        {
            var result = await provider.GenerateImageAsync(
                new ImageGenerationRequest(
                    Guid.NewGuid(),
                    Guid.NewGuid(),
                    "Make the poster look more realistic.",
                    new GenerationSettings(1024, 1024, "medium", "png"),
                    rootDirectory,
                    "stateful.png",
                    UseResponsesApi: true,
                    PreviousResponseId: "resp_prev_image_123"),
                CancellationToken.None);

            Assert.Equal("resp_image_stateful_123", result.ProviderTraceId);
            Assert.Equal("A cleaner revised prompt for the final render.", result.RevisedPrompt);
            Assert.Equal("ig_call_123", result.ToolCallId);

            Assert.Equal("https://api.openai.com/v1/responses", handler.LastRequest!.RequestUri!.ToString());
            using var payload = JsonDocument.Parse(handler.LastRequestBody!);
            Assert.Equal("gpt-5.5", payload.RootElement.GetProperty("model").GetString());
            Assert.True(payload.RootElement.GetProperty("store").GetBoolean());
            Assert.Equal("resp_prev_image_123", payload.RootElement.GetProperty("previous_response_id").GetString());
            Assert.Equal("Make the poster look more realistic.", payload.RootElement.GetProperty("input").GetString());

            var tool = payload.RootElement.GetProperty("tools")[0];
            Assert.Equal("image_generation", tool.GetProperty("type").GetString());
            Assert.Equal("auto", tool.GetProperty("action").GetString());
            Assert.Equal(1, tool.GetProperty("partial_images").GetInt32());

            using var metadata = JsonDocument.Parse(await File.ReadAllTextAsync(result.MetadataPath, CancellationToken.None));
            Assert.Equal("responses", metadata.RootElement.GetProperty("endpointFamily").GetString());
            Assert.True(metadata.RootElement.GetProperty("store").GetBoolean());
            Assert.Equal("gpt-5.5", metadata.RootElement.GetProperty("model").GetString());
            Assert.Equal("ig_call_123", metadata.RootElement.GetProperty("toolCallId").GetString());
            Assert.Equal("A cleaner revised prompt for the final render.", metadata.RootElement.GetProperty("revisedPrompt").GetString());
            Assert.Equal("resp_prev_image_123", metadata.RootElement.GetProperty("previousResponseId").GetString());
            Assert.True(metadata.RootElement.GetProperty("usedResponsesApi").GetBoolean());
        }
        finally
        {
            if (Directory.Exists(rootDirectory))
            {
                Directory.Delete(rootDirectory, recursive: true);
            }
        }
    }

    [Fact]
    public async Task ImageGenerationProvider_UsesResponsesPathByDefaultWhenProviderIsConfiguredForResponses()
    {
        var rootDirectory = Path.Combine(Path.GetTempPath(), "ContentDeliveryStudio.Tests", Guid.NewGuid().ToString("N"));
        using var handler = new CaptureHandler(_ => JsonResponse(
            """
            {
              "id": "resp_image_default_123",
              "output": [
                {
                  "id": "ig_call_default_123",
                  "type": "image_generation_call",
                  "revised_prompt": "A revised default response prompt.",
                  "result": "iVBORw=="
                }
              ]
            }
            """));
        using var httpClient = new HttpClient(handler);
        var provider = new OpenAiImageGenerationProvider(
            httpClient,
            new OpenAiProviderOptions
            {
                RealApiEnabled = true,
                ImageGenerationResponsesModel = "gpt-5.5",
                ImageGenerationAllowsResponsesState = true,
                ImageGenerationUsesResponsesByDefault = true,
            },
            new StaticSecretStore("test-openai-key"));

        try
        {
            var result = await provider.GenerateImageAsync(
                new ImageGenerationRequest(
                    Guid.NewGuid(),
                    Guid.NewGuid(),
                    "Generate through the provider default route.",
                    new GenerationSettings(1024, 1024, "medium", "png"),
                    rootDirectory,
                    "default-responses.png"),
                CancellationToken.None);

            Assert.Equal("resp_image_default_123", result.ProviderTraceId);
            Assert.Equal("https://api.openai.com/v1/responses", handler.LastRequest!.RequestUri!.ToString());
            using var payload = JsonDocument.Parse(handler.LastRequestBody!);
            Assert.Equal("gpt-5.5", payload.RootElement.GetProperty("model").GetString());
            Assert.False(payload.RootElement.TryGetProperty("previous_response_id", out _));

            using var metadata = JsonDocument.Parse(await File.ReadAllTextAsync(result.MetadataPath, CancellationToken.None));
            Assert.Equal("responses", metadata.RootElement.GetProperty("endpointFamily").GetString());
            Assert.True(metadata.RootElement.GetProperty("usedResponsesApi").GetBoolean());
        }
        finally
        {
            if (Directory.Exists(rootDirectory))
            {
                Directory.Delete(rootDirectory, recursive: true);
            }
        }
    }

    [Fact]
    public async Task ImageGenerationProvider_RejectsResponsesStateWhenProviderOptionIsNotEnabled()
    {
        using var handler = new CaptureHandler(_ => JsonResponse("{}"));
        using var httpClient = new HttpClient(handler);
        var provider = CreateImageProvider(httpClient);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            provider.GenerateImageAsync(
                new ImageGenerationRequest(
                    Guid.NewGuid(),
                    Guid.NewGuid(),
                    "prompt",
                    new GenerationSettings(1024, 1024, "standard", "png"),
                    Path.GetTempPath(),
                    UseResponsesApi: true),
                CancellationToken.None));

        Assert.Contains("disabled by default", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(0, handler.CallCount);
    }

    [Fact]
    public async Task VisionReviewProvider_UsesExplicitStatefulReviewOptionsWhenEnabled()
    {
        var rootDirectory = Path.Combine(Path.GetTempPath(), "ContentDeliveryStudio.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(rootDirectory);
        var imagePath = Path.Combine(rootDirectory, "candidate.png");
        await File.WriteAllBytesAsync(imagePath, [1, 2, 3, 4], CancellationToken.None);

        using var handler = new CaptureHandler(_ => JsonResponse(
            """
            {
              "id": "resp_review_stateful",
              "output_text": "{\"decision\":\"pass\",\"scores\":{\"match\":5},\"hardFailures\":[],\"comments\":\"Looks aligned.\",\"suggestedFix\":null}"
            }
            """));
        using var httpClient = new HttpClient(handler);
        var provider = new OpenAiVisionReviewProvider(
            httpClient,
            new OpenAiProviderOptions
            {
                RealApiEnabled = true,
                VisionReviewUsesStoredResponses = true,
                VisionReviewAllowsPreviousResponseId = true,
            },
            new StaticSecretStore("test-openai-key"));

        try
        {
            await provider.ReviewAsync(
                CreateVisionReviewRequest(imagePath) with { PreviousResponseId = "resp_prev_123" },
                CancellationToken.None);

            using var payload = JsonDocument.Parse(handler.LastRequestBody!);
            Assert.True(payload.RootElement.GetProperty("store").GetBoolean());
            Assert.Equal("resp_prev_123", payload.RootElement.GetProperty("previous_response_id").GetString());
        }
        finally
        {
            if (Directory.Exists(rootDirectory))
            {
                Directory.Delete(rootDirectory, recursive: true);
            }
        }
    }

    [Fact]
    public async Task VisionReviewProvider_RejectsPreviousResponseIdWhenStatefulReviewIsDisabled()
    {
        var rootDirectory = Path.Combine(Path.GetTempPath(), "ContentDeliveryStudio.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(rootDirectory);
        var imagePath = Path.Combine(rootDirectory, "candidate.png");
        await File.WriteAllBytesAsync(imagePath, [1, 2, 3, 4], CancellationToken.None);

        using var handler = new CaptureHandler(_ => JsonResponse("{}"));
        using var httpClient = new HttpClient(handler);
        var provider = CreateVisionProvider(httpClient);

        try
        {
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                provider.ReviewAsync(
                    CreateVisionReviewRequest(imagePath) with { PreviousResponseId = "resp_prev_123" },
                    CancellationToken.None));

            Assert.Contains("previous_response_id", exception.Message, StringComparison.OrdinalIgnoreCase);
            Assert.Equal(0, handler.CallCount);
        }
        finally
        {
            if (Directory.Exists(rootDirectory))
            {
                Directory.Delete(rootDirectory, recursive: true);
            }
        }
    }

    [Fact]
    public async Task VisionReviewProvider_DoesNotSendHttpWhenRealApiIsDisabled()
    {
        using var handler = new CaptureHandler(_ => JsonResponse("{}"));
        using var httpClient = new HttpClient(handler);
        var provider = new OpenAiVisionReviewProvider(
            httpClient,
            new OpenAiProviderOptions(),
            new StaticSecretStore("test-openai-key"));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            provider.ReviewAsync(
                CreateVisionReviewRequest(Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.png")),
                CancellationToken.None));

        Assert.Equal(0, handler.CallCount);
    }

    [Fact]
    public async Task VisionReviewProvider_MapsNonSuccessResponsesToHttpRequestException()
    {
        var rootDirectory = Path.Combine(Path.GetTempPath(), "ContentDeliveryStudio.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(rootDirectory);
        var imagePath = Path.Combine(rootDirectory, "candidate.png");
        await File.WriteAllBytesAsync(imagePath, [1, 2, 3, 4], CancellationToken.None);
        using var handler = new CaptureHandler(_ => new HttpResponseMessage(HttpStatusCode.BadGateway)
        {
            ReasonPhrase = "Bad Gateway",
            Content = new StringContent("""{"error":{"message":"upstream failed"}}""", Encoding.UTF8, "application/json"),
        });
        using var httpClient = new HttpClient(handler);
        var provider = CreateVisionProvider(httpClient);

        try
        {
            var exception = await Assert.ThrowsAsync<HttpRequestException>(() =>
                provider.ReviewAsync(CreateVisionReviewRequest(imagePath), CancellationToken.None));

            Assert.Contains("502", exception.Message, StringComparison.Ordinal);
            Assert.Equal(1, handler.CallCount);
        }
        finally
        {
            if (Directory.Exists(rootDirectory))
            {
                Directory.Delete(rootDirectory, recursive: true);
            }
        }
    }

    private static OpenAiTextPlanningProvider CreateTextProvider(HttpClient httpClient)
    {
        return new OpenAiTextPlanningProvider(
            httpClient,
            new OpenAiProviderOptions { RealApiEnabled = true },
            new StaticSecretStore("test-openai-key"));
    }

    private static OpenAiSdkTextPlanningProvider CreateSdkTextProvider(FakeResponsesClient sdkClient)
    {
        return new OpenAiSdkTextPlanningProvider(
            new OpenAiProviderOptions { RealApiEnabled = true },
            sdkClient,
            new StaticSecretStore("test-openai-key"));
    }

    private static OpenAiImageGenerationProvider CreateImageProvider(HttpClient httpClient)
    {
        return new OpenAiImageGenerationProvider(
            httpClient,
            new OpenAiProviderOptions { RealApiEnabled = true },
            new StaticSecretStore("test-openai-key"));
    }

    private static OpenAiVisionReviewProvider CreateVisionProvider(HttpClient httpClient)
    {
        return new OpenAiVisionReviewProvider(
            httpClient,
            new OpenAiProviderOptions { RealApiEnabled = true },
            new StaticSecretStore("test-openai-key"));
    }

    private static VisionReviewRequest CreateVisionReviewRequest(string assetPath)
    {
        return new VisionReviewRequest(
            Guid.NewGuid(),
            assetPath,
            new ReviewRubric(
                Guid.NewGuid(),
                Guid.NewGuid(),
                "Default review",
                [new ReviewRubricDimension("match", "Image should match the prompt.", 1)],
                DateTimeOffset.UtcNow),
            "prompt",
            new ReviewPrepArtifactContract(
                "Compact local review prep.",
                EvidenceSelections:
                [
                    new ReviewPrepEvidenceSelection(
                        "candidate-image",
                        "generated-asset",
                        assetPath,
                        "Primary local candidate image selected for bounded remote review."),
                ]));
    }

    private static HttpResponseMessage JsonResponse(string content, string? requestId = null)
    {
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(content, Encoding.UTF8, "application/json"),
        };

        if (!string.IsNullOrWhiteSpace(requestId))
        {
            response.Headers.TryAddWithoutValidation("x-request-id", requestId);
        }

        return response;
    }

    private static OpenAiResponsesClientResult SdkJsonResponse(
        string content,
        int statusCode = 200,
        string? reasonPhrase = "OK",
        string? requestId = null)
    {
        using var document = JsonDocument.Parse(content);
        return new OpenAiResponsesClientResult(
            document.RootElement.TryGetProperty("output_text", out var outputText)
                ? outputText.GetString()
                : null,
            document.RootElement.Clone(),
            statusCode,
            reasonPhrase,
            requestId);
    }

    private sealed class CaptureHandler(Func<HttpRequestMessage, HttpResponseMessage> responseFactory) : HttpMessageHandler, IDisposable
    {
        public HttpRequestMessage? LastRequest { get; private set; }

        public string? LastRequestBody { get; private set; }

        public int CallCount { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            CallCount++;
            LastRequest = request;
            LastRequestBody = request.Content is null
                ? null
                : await request.Content.ReadAsStringAsync(cancellationToken);

            return responseFactory(request);
        }
    }

    private sealed class StaticSecretStore(string? value) : IOpenAiSecretStore
    {
        public Task<string?> GetSecretAsync(string secretName, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            return Task.FromResult(value);
        }
    }

    private sealed class StaticSecretMapStore(IReadOnlyDictionary<string, string?> values) : IOpenAiSecretStore
    {
        public Task<string?> GetSecretAsync(string secretName, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            return Task.FromResult(values.TryGetValue(secretName, out var value) ? value : null);
        }
    }

    private sealed class FakeResponsesClient(OpenAiResponsesClientResult response) : IOpenAiResponsesClient
    {
        public CreateResponseOptions? LastOptions { get; private set; }

        public int CallCount { get; private set; }

        public bool ThrowOnCreate { get; set; }

        public Task<OpenAiResponsesClientResult> CreateResponseAsync(
            CreateResponseOptions options,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            CallCount++;
            LastOptions = options;

            if (ThrowOnCreate)
            {
                throw new OpenAiResponsesClientException(
                    response,
                    new InvalidOperationException("SDK request failed."));
            }

            return Task.FromResult(response);
        }
    }

    private sealed class SequencedFakeResponsesClient(IReadOnlyList<object> results) : IOpenAiResponsesClient
    {
        public CreateResponseOptions? LastOptions { get; private set; }

        public int CallCount { get; private set; }

        public Task<OpenAiResponsesClientResult> CreateResponseAsync(
            CreateResponseOptions options,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            LastOptions = options;
            var index = Math.Min(CallCount, results.Count - 1);
            CallCount++;

            return results[index] switch
            {
                OpenAiResponsesClientResult response => Task.FromResult(response),
                OpenAiResponsesClientException exception => throw exception,
                Exception exception => throw exception,
                var result => throw new InvalidOperationException(
                    $"Unsupported fake SDK response result type: {result.GetType().FullName}."),
            };
        }
    }

    private sealed class RecordingTelemetrySink : IProviderCallTelemetrySink
    {
        private readonly List<ProviderCallTelemetry> _events = [];

        public IReadOnlyList<ProviderCallTelemetry> Events => _events;

        public void Record(ProviderCallTelemetry telemetry)
        {
            _events.Add(telemetry);
        }
    }
}
#pragma warning restore OPENAI001
