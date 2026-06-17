using System.Diagnostics;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using ContentDeliveryStudio.Core.Projects;
using ContentDeliveryStudio.Core.Providers;
using SkiaSharp;

namespace ContentDeliveryStudio.Infrastructure.OpenAI;

public sealed class OpenAiVisionReviewProvider : IVisionReviewProvider
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private static readonly OpenAiRoutingDecision BaseRouting = OpenAiProviderRoutingPolicy.ForVisionReview();

    private readonly HttpClient _httpClient;
    private readonly OpenAiProviderOptions _options;
    private readonly IOpenAiSecretStore _secretStore;
    private readonly IProviderCallTelemetrySink _telemetrySink;
    private readonly OpenAiCostRateCard _rateCard;

    public OpenAiVisionReviewProvider(
        HttpClient httpClient,
        OpenAiProviderOptions options,
        IOpenAiSecretStore secretStore,
        IProviderCallTelemetrySink? telemetrySink = null,
        OpenAiCostRateCard? rateCard = null)
    {
        _httpClient = httpClient;
        _options = options;
        _secretStore = secretStore;
        _telemetrySink = telemetrySink ?? NullProviderCallTelemetrySink.Instance;
        _rateCard = rateCard ?? OpenAiCostRateCard.Unpriced;
        OpenAiProviderGuard.EnsureAllowsOperation(_options, OpenAiProviderOperation.VisionReview);

        Capabilities = new ProviderCapabilities(
            "openai-vision",
            "OpenAI Vision Review Provider",
            [_options.VisionReviewModel],
            SupportsTextPlanning: false,
            SupportsImageGeneration: false,
            SupportsVisionReview: true,
            SupportsImageEditing: false,
            SupportsStreaming: false);
    }

    public IProviderCapabilities Capabilities { get; }

    public async Task<VisionReviewResult> ReviewAsync(
        VisionReviewRequest request,
        CancellationToken cancellationToken)
    {
        ValidateStatefulReviewOptions(request);

        await OpenAiProviderGuard.EnsureCanCallRealApiAsync(
            _options,
            _secretStore,
            OpenAiProviderOperation.VisionReview,
            cancellationToken);
        var apiKey = await _secretStore.GetSecretAsync(_options.ApiKeySecretName, cancellationToken)
            ?? throw new InvalidOperationException("OpenAI API key was not found in the configured secret store.");
        var imageDataUrl = await CreateCompactImageDataUrlAsync(request.AssetPath, cancellationToken);
        var routing = BaseRouting with { Store = _options.VisionReviewUsesStoredResponses };

        var endpoint = new Uri(_options.BaseUri, routing.RelativePath);
        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, endpoint);
        httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        httpRequest.Content = JsonContent.Create(CreatePayload(request, imageDataUrl, routing), options: JsonOptions);

        var stopwatch = Stopwatch.StartNew();
        using var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
        stopwatch.Stop();
        if (!response.IsSuccessStatusCode)
        {
            RecordTelemetry(endpoint, response, body: null, providerTraceId: null, stopwatch.Elapsed);
            throw new HttpRequestException(
                $"OpenAI vision review request failed with status {(int)response.StatusCode} {response.ReasonPhrase}.");
        }

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
        var providerTraceId = ExtractTraceId(document.RootElement);
        RecordTelemetry(endpoint, response, document.RootElement, providerTraceId, stopwatch.Elapsed);
        var outputText = ExtractOutputText(document.RootElement);
        var review = JsonSerializer.Deserialize<OpenAiVisionReviewResponse>(outputText, JsonOptions)
            ?? throw new InvalidOperationException("OpenAI vision review response was empty.");

        return BuildResult(request, review);
    }

    private void ValidateStatefulReviewOptions(VisionReviewRequest request)
    {
        if (_options.VisionReviewAllowsPreviousResponseId && !_options.VisionReviewUsesStoredResponses)
        {
            throw new InvalidOperationException(
                "Vision review previous_response_id requires stored responses to be enabled explicitly.");
        }

        if (!_options.VisionReviewAllowsPreviousResponseId
            && !string.IsNullOrWhiteSpace(request.PreviousResponseId))
        {
            throw new InvalidOperationException(
                "Vision review previous_response_id is disabled by default. Enable stateful review options explicitly before dispatch.");
        }
    }

    private Dictionary<string, object?> CreatePayload(
        VisionReviewRequest request,
        string imageDataUrl,
        OpenAiRoutingDecision routing)
    {
        var payload = new Dictionary<string, object?>
        {
            ["model"] = _options.VisionReviewModel,
            ["instructions"] = "Review the generated image against the prompt and rubric. Return only valid JSON with the final decision and a short comment.",
            ["input"] = new object[]
            {
                new Dictionary<string, object?>
                {
                    ["role"] = "user",
                    ["content"] = new object[]
                    {
                        new Dictionary<string, object?>
                        {
                            ["type"] = "input_text",
                            ["text"] = BuildReviewText(request),
                        },
                        new Dictionary<string, object?>
                        {
                            ["type"] = "input_image",
                            ["image_url"] = imageDataUrl,
                            ["detail"] = "low",
                        },
                    },
                },
            },
            ["store"] = routing.Store,
            ["text"] = new Dictionary<string, object?>
            {
                ["format"] = new Dictionary<string, object?>
                {
                    ["type"] = "json_schema",
                    ["name"] = "compact_image_review",
                    ["strict"] = true,
                    ["schema"] = CreateReviewSchema(),
                },
            },
        };

        if (_options.VisionReviewAllowsPreviousResponseId
            && !string.IsNullOrWhiteSpace(request.PreviousResponseId))
        {
            payload["previous_response_id"] = request.PreviousResponseId;
        }

        return payload;
    }

    private static string BuildReviewText(VisionReviewRequest request)
    {
        return string.Join(
            Environment.NewLine,
            [
                $"Prompt: {request.PromptText}",
                $"Rubric dimensions: {string.Join(", ", request.Rubric.Dimensions.Select(dimension => dimension.Name))}",
                $"Compact review summary: {request.ReviewPrep?.Summary ?? "Not provided"}",
                "Pass only if the image clearly matches the prompt, has no embedded text, and keeps clean label space for later deterministic overlays.",
                "Fail if the image contains visible text, major clutter, or misses the requested concept.",
            ]);
    }

    private static Dictionary<string, object?> CreateReviewSchema()
    {
        return new Dictionary<string, object?>
        {
            ["type"] = "object",
            ["additionalProperties"] = false,
            ["required"] = new[] { "decision", "comments" },
            ["properties"] = new Dictionary<string, object?>
            {
                ["decision"] = new Dictionary<string, object?>
                {
                    ["type"] = "string",
                    ["enum"] = new[] { "pass", "fail", "pending" },
                },
                ["comments"] = new Dictionary<string, object?>
                {
                    ["type"] = "string",
                },
            },
        };
    }

    private static VisionReviewResult BuildResult(
        VisionReviewRequest request,
        OpenAiVisionReviewResponse review)
    {
        var decision = ParseDecision(review.Decision);
        var scores = review.Scores ?? CreateFallbackScores(request.Rubric, decision);
        var hardFailures = review.HardFailures ?? [];
        var suggestedFix = !string.IsNullOrWhiteSpace(review.SuggestedFix)
            ? review.SuggestedFix
            : decision is ReviewDecision.Pass
                ? null
                : review.Comments;

        return new VisionReviewResult(
            request.CandidateImageId,
            decision,
            scores,
            hardFailures,
            review.Comments,
            suggestedFix);
    }

    private static IReadOnlyDictionary<string, int> CreateFallbackScores(ReviewRubric rubric, ReviewDecision decision)
    {
        var scoreValue = decision switch
        {
            ReviewDecision.Pass => 5,
            ReviewDecision.Pending => 2,
            _ => 1,
        };

        return rubric.Dimensions.ToDictionary(dimension => dimension.Name, _ => scoreValue);
    }

    private static async Task<string> CreateCompactImageDataUrlAsync(
        string assetPath,
        CancellationToken cancellationToken)
    {
        var originalBytes = await File.ReadAllBytesAsync(assetPath, cancellationToken);
        SKBitmap? originalBitmap = null;
        try
        {
            originalBitmap = SKBitmap.Decode(originalBytes);
        }
        catch (ArgumentNullException)
        {
            originalBitmap = null;
        }

        if (originalBitmap is null)
        {
            return $"data:{GetMimeType(assetPath)};base64,{Convert.ToBase64String(originalBytes)}";
        }

        using (originalBitmap)
        {
            var maxDimension = Math.Max(originalBitmap.Width, originalBitmap.Height);
            if (maxDimension <= VisionReviewExecutionPolicy.DefaultCompactReviewImageMaxDimension)
            {
                return $"data:{GetMimeType(assetPath)};base64,{Convert.ToBase64String(originalBytes)}";
            }

            var scale = VisionReviewExecutionPolicy.DefaultCompactReviewImageMaxDimension / (double)maxDimension;
            var resizedWidth = Math.Max(1, (int)Math.Round(originalBitmap.Width * scale));
            var resizedHeight = Math.Max(1, (int)Math.Round(originalBitmap.Height * scale));
            using var resizedBitmap = originalBitmap.Resize(
                new SKImageInfo(resizedWidth, resizedHeight),
                new SKSamplingOptions(SKFilterMode.Linear))
                ?? throw new InvalidOperationException("Could not resize image for compact vision review.");
            using var resizedImage = SKImage.FromBitmap(resizedBitmap);
            using var encodedImage = resizedImage.Encode(
                SKEncodedImageFormat.Jpeg,
                VisionReviewExecutionPolicy.DefaultCompactReviewImageJpegQuality);
            return $"data:image/jpeg;base64,{Convert.ToBase64String(encodedImage.ToArray())}";
        }
    }

    private static string GetMimeType(string assetPath)
    {
        return Path.GetExtension(assetPath).ToLowerInvariant() switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".webp" => "image/webp",
            _ => "image/png",
        };
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

        throw new InvalidOperationException("OpenAI vision review response did not include output text.");
    }

    private static ReviewDecision ParseDecision(string decision)
    {
        return decision.Trim().ToLowerInvariant() switch
        {
            "pass" => ReviewDecision.Pass,
            "fail" => ReviewDecision.Fail,
            _ => ReviewDecision.Pending,
        };
    }

    private static string ExtractTraceId(JsonElement root)
    {
        return root.TryGetProperty("id", out var idElement) && idElement.ValueKind is JsonValueKind.String
            ? idElement.GetString()!
            : "openai-vision-review";
    }

    private void RecordTelemetry(
        Uri endpoint,
        HttpResponseMessage response,
        JsonElement? body,
        string? providerTraceId,
        TimeSpan latency)
    {
        _telemetrySink.Record(OpenAiProviderTelemetry.Create(
            Capabilities.ProviderId,
            "vision-review",
            _options.VisionReviewModel,
            endpoint,
            response,
            body,
            providerTraceId,
            latency,
            _rateCard.VisionReviewRequestUsd,
            _rateCard.Name));
    }

    private sealed record OpenAiVisionReviewResponse(
        string Decision,
        IReadOnlyDictionary<string, int>? Scores,
        IReadOnlyList<string>? HardFailures,
        string Comments,
        string? SuggestedFix);
}
