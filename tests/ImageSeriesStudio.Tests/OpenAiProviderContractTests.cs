using System.Net;
using System.Text;
using System.Text.Json;
using ImageSeriesStudio.Core.Projects;
using ImageSeriesStudio.Core.Providers;
using ImageSeriesStudio.Infrastructure.OpenAI;

namespace ImageSeriesStudio.Tests;

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
    public async Task ImageGenerationProvider_PostsImageRequestAndWritesAssetAndMetadata()
    {
        var rootDirectory = Path.Combine(Path.GetTempPath(), "ImageSeriesStudio.Tests", Guid.NewGuid().ToString("N"));
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

            using var metadata = JsonDocument.Parse(await File.ReadAllTextAsync(result.MetadataPath, CancellationToken.None));
            Assert.Equal("openai-image", metadata.RootElement.GetProperty("providerId").GetString());
            Assert.Equal("gpt-image-2", metadata.RootElement.GetProperty("model").GetString());
            Assert.Equal("img_resp_123", metadata.RootElement.GetProperty("providerTraceId").GetString());
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
        var rootDirectory = Path.Combine(Path.GetTempPath(), "ImageSeriesStudio.Tests", Guid.NewGuid().ToString("N"));
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
        var rootDirectory = Path.Combine(Path.GetTempPath(), "ImageSeriesStudio.Tests", Guid.NewGuid().ToString("N"));
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
                    "Create a clean science poster."),
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
            Assert.Equal(
                "json_schema",
                payload.RootElement.GetProperty("text").GetProperty("format").GetProperty("type").GetString());

            var content = payload.RootElement
                .GetProperty("input")[0]
                .GetProperty("content");
            Assert.Equal("input_text", content[0].GetProperty("type").GetString());
            Assert.Contains("Create a clean science poster.", content[0].GetProperty("text").GetString());
            Assert.Equal("input_image", content[1].GetProperty("type").GetString());
            Assert.StartsWith("data:image/png;base64,", content[1].GetProperty("image_url").GetString());
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
        var rootDirectory = Path.Combine(Path.GetTempPath(), "ImageSeriesStudio.Tests", Guid.NewGuid().ToString("N"));
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
        var rootDirectory = Path.Combine(Path.GetTempPath(), "ImageSeriesStudio.Tests", Guid.NewGuid().ToString("N"));
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
            "prompt");
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
