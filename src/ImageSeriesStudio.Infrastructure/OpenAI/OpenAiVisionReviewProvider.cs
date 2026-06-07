using System.Diagnostics;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using ImageSeriesStudio.Core.Projects;
using ImageSeriesStudio.Core.Providers;

namespace ImageSeriesStudio.Infrastructure.OpenAI;

public sealed class OpenAiVisionReviewProvider : IVisionReviewProvider
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

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
        await OpenAiProviderGuard.EnsureCanCallRealApiAsync(
            _options,
            _secretStore,
            OpenAiProviderOperation.VisionReview,
            cancellationToken);
        var apiKey = await _secretStore.GetSecretAsync(_options.ApiKeySecretName, cancellationToken)
            ?? throw new InvalidOperationException("OpenAI API key was not found in the configured secret store.");
        var imageDataUrl = await CreateImageDataUrlAsync(request.AssetPath, cancellationToken);

        var endpoint = new Uri(_options.BaseUri, OpenAiRoutingDefaults.VisionReviewEndpointPath);
        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, endpoint);
        httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        httpRequest.Content = JsonContent.Create(CreatePayload(request, imageDataUrl), options: JsonOptions);

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

        return new VisionReviewResult(
            request.CandidateImageId,
            ParseDecision(review.Decision),
            review.Scores ?? new Dictionary<string, int>(),
            review.HardFailures ?? [],
            review.Comments,
            review.SuggestedFix);
    }

    private Dictionary<string, object?> CreatePayload(VisionReviewRequest request, string imageDataUrl)
    {
        return new Dictionary<string, object?>
        {
            ["model"] = _options.VisionReviewModel,
            ["instructions"] = "Review the generated image against the prompt and rubric. Return only valid JSON.",
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
                            ["detail"] = "high",
                        },
                    },
                },
            },
            ["store"] = OpenAiRoutingDefaults.StoreRemoteStateByDefault,
            ["text"] = new Dictionary<string, object?>
            {
                ["format"] = new Dictionary<string, object?>
                {
                    ["type"] = "json_schema",
                    ["name"] = "image_review",
                    ["strict"] = true,
                    ["schema"] = CreateReviewSchema(),
                },
            },
        };
    }

    private static string BuildReviewText(VisionReviewRequest request)
    {
        var rubric = string.Join(
            Environment.NewLine,
            request.Rubric.Dimensions.Select(
                dimension => $"- {dimension.Name} (weight {dimension.Weight}): {dimension.Requirement}"));

        return string.Join(
            Environment.NewLine,
            [
                $"Prompt: {request.PromptText}",
                $"Rubric: {request.Rubric.Name}",
                rubric,
            ]);
    }

    private static Dictionary<string, object?> CreateReviewSchema()
    {
        return new Dictionary<string, object?>
        {
            ["type"] = "object",
            ["additionalProperties"] = false,
            ["required"] = new[] { "decision", "scores", "hardFailures", "comments", "suggestedFix" },
            ["properties"] = new Dictionary<string, object?>
            {
                ["decision"] = new Dictionary<string, object?>
                {
                    ["type"] = "string",
                    ["enum"] = new[] { "pass", "fail", "pending" },
                },
                ["scores"] = new Dictionary<string, object?>
                {
                    ["type"] = "object",
                    ["additionalProperties"] = new Dictionary<string, object?>
                    {
                        ["type"] = "integer",
                    },
                },
                ["hardFailures"] = new Dictionary<string, object?>
                {
                    ["type"] = "array",
                    ["items"] = new Dictionary<string, object?>
                    {
                        ["type"] = "string",
                    },
                },
                ["comments"] = new Dictionary<string, object?>
                {
                    ["type"] = "string",
                },
                ["suggestedFix"] = new Dictionary<string, object?>
                {
                    ["type"] = new[] { "string", "null" },
                },
            },
        };
    }

    private static async Task<string> CreateImageDataUrlAsync(
        string assetPath,
        CancellationToken cancellationToken)
    {
        var bytes = await File.ReadAllBytesAsync(assetPath, cancellationToken);
        return $"data:{GetMimeType(assetPath)};base64,{Convert.ToBase64String(bytes)}";
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
