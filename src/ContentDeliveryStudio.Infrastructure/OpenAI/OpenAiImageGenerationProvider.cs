using System.Diagnostics;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using ContentDeliveryStudio.Core.Providers;

namespace ContentDeliveryStudio.Infrastructure.OpenAI;

public sealed class OpenAiImageGenerationProvider : IImageGenerationProvider
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
    };
    private static readonly OpenAiRoutingDecision SingleShotRouting = OpenAiProviderRoutingPolicy.ForImageGeneration();
    private static readonly OpenAiRoutingDecision StatefulRouting = OpenAiProviderRoutingPolicy.ForStatefulImageGeneration();

    private readonly HttpClient _httpClient;
    private readonly OpenAiProviderOptions _options;
    private readonly IOpenAiSecretStore _secretStore;
    private readonly IProviderCallTelemetrySink _telemetrySink;
    private readonly OpenAiCostRateCard _rateCard;

    public OpenAiImageGenerationProvider(
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
        OpenAiProviderGuard.EnsureAllowsOperation(_options, OpenAiProviderOperation.ImageGeneration);

        Capabilities = new ProviderCapabilities(
            "openai-image",
            "OpenAI Image Generation Provider",
            GetCapabilityModelIds(_options),
            SupportsTextPlanning: false,
            SupportsImageGeneration: true,
            SupportsVisionReview: false,
            SupportsImageEditing: false,
            SupportsStreaming: _options.ImageGenerationAllowsResponsesState,
            supportedSizes:
            [
                new ImageOutputSize(1024, 1024),
                new ImageOutputSize(1024, 1536),
                new ImageOutputSize(1536, 1024),
            ],
            supportedQualities: ["auto", "low", "medium", "high"],
            supportedOutputFormats: ["png", "jpeg", "webp"],
            supportedBackgroundModes: ["auto", "opaque"],
            supportsReferenceImages: false,
            costHints: [new ProviderCostHint(_options.ImageGenerationModel, "provider-rate-card")]);
    }

    public IProviderCapabilities Capabilities { get; }

    public async Task<ImageGenerationResult> GenerateImageAsync(
        ImageGenerationRequest request,
        CancellationToken cancellationToken)
    {
        await OpenAiProviderGuard.EnsureCanCallRealApiAsync(
            _options,
            _secretStore,
            OpenAiProviderOperation.ImageGeneration,
            cancellationToken);
        var apiKey = await _secretStore.GetSecretAsync(_options.ApiKeySecretName, cancellationToken)
            ?? throw new InvalidOperationException("OpenAI API key was not found in the configured secret store.");
        var appId = await GetOptionalSecretAsync(_options.AppIdSecretName, cancellationToken);
        var appSecret = await GetOptionalSecretAsync(_options.AppSecretSecretName, cancellationToken);

        return request.UseResponsesApi
            ? await GenerateStatefulImageAsync(request, apiKey, appId, appSecret, cancellationToken)
            : await GenerateSingleShotImageAsync(request, apiKey, appId, appSecret, cancellationToken);
    }

    private async Task<ImageGenerationResult> GenerateSingleShotImageAsync(
        ImageGenerationRequest request,
        string apiKey,
        string? appId,
        string? appSecret,
        CancellationToken cancellationToken)
    {
        var endpoint = new Uri(_options.BaseUri, SingleShotRouting.RelativePath);
        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, endpoint);
        httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        AddOptionalAppHeaders(httpRequest, appId, appSecret);
        httpRequest.Content = JsonContent.Create(CreateSingleShotPayload(request), options: JsonOptions);

        var stopwatch = Stopwatch.StartNew();
        using var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
        stopwatch.Stop();
        if (!response.IsSuccessStatusCode)
        {
            RecordTelemetry(endpoint, response, body: null, providerTraceId: null, model: _options.ImageGenerationModel, latency: stopwatch.Elapsed);
            throw new HttpRequestException(
                $"OpenAI image generation request failed with status {(int)response.StatusCode} {response.ReasonPhrase}.");
        }

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
        var providerTraceId = ExtractTraceId(document.RootElement);
        var telemetry = CreateTelemetry(endpoint, response, document.RootElement, providerTraceId, _options.ImageGenerationModel, stopwatch.Elapsed);
        _telemetrySink.Record(telemetry);

        return await PersistImageResultAsync(
            request,
            document.RootElement,
            telemetry,
            SingleShotRouting,
            _options.ImageGenerationModel,
            cancellationToken);
    }

    private async Task<ImageGenerationResult> GenerateStatefulImageAsync(
        ImageGenerationRequest request,
        string apiKey,
        string? appId,
        string? appSecret,
        CancellationToken cancellationToken)
    {
        if (!_options.ImageGenerationAllowsResponsesState)
        {
            throw new InvalidOperationException(
                "Responses API image state is disabled by default. Configure a Responses image generation model explicitly before dispatch.");
        }

        var responsesModel = _options.ImageGenerationResponsesModel
            ?? throw new InvalidOperationException("Responses image generation model is required for stateful image generation.");
        var endpoint = new Uri(_options.BaseUri, StatefulRouting.RelativePath);
        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, endpoint);
        httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        AddOptionalAppHeaders(httpRequest, appId, appSecret);
        httpRequest.Content = JsonContent.Create(CreateStatefulPayload(request, responsesModel), options: JsonOptions);

        var stopwatch = Stopwatch.StartNew();
        using var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
        stopwatch.Stop();
        if (!response.IsSuccessStatusCode)
        {
            RecordTelemetry(endpoint, response, body: null, providerTraceId: null, model: responsesModel, latency: stopwatch.Elapsed);
            throw new HttpRequestException(
                $"OpenAI stateful image generation request failed with status {(int)response.StatusCode} {response.ReasonPhrase}.");
        }

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
        var providerTraceId = ExtractTraceId(document.RootElement);
        var telemetry = CreateTelemetry(endpoint, response, document.RootElement, providerTraceId, responsesModel, stopwatch.Elapsed);
        _telemetrySink.Record(telemetry);

        return await PersistImageResultAsync(
            request,
            document.RootElement,
            telemetry,
            StatefulRouting,
            responsesModel,
            cancellationToken);
    }

    private Dictionary<string, object?> CreateSingleShotPayload(ImageGenerationRequest request)
    {
        return new Dictionary<string, object?>
        {
            ["model"] = _options.ImageGenerationModel,
            ["prompt"] = request.PromptText,
            ["n"] = 1,
            ["store"] = SingleShotRouting.Store,
            ["size"] = BuildSize(request),
            ["quality"] = NormalizeQuality(request.Settings.Quality),
            ["output_format"] = NormalizeOutputFormat(request.Settings.OutputFormat),
        };
    }

    private Dictionary<string, object?> CreateStatefulPayload(ImageGenerationRequest request, string responsesModel)
    {
        var tool = new Dictionary<string, object?>
        {
            ["type"] = "image_generation",
            ["size"] = BuildSize(request),
            ["quality"] = NormalizeQuality(request.Settings.Quality),
            ["output_format"] = NormalizeOutputFormat(request.Settings.OutputFormat),
            ["partial_images"] = request.Settings.Seed.HasValue ? 0 : 1,
            ["action"] = request.PreviousResponseId is null ? "generate" : "auto",
        };

        var payload = new Dictionary<string, object?>
        {
            ["model"] = responsesModel,
            ["input"] = request.PromptText,
            ["tools"] = new object[] { tool },
            ["store"] = StatefulRouting.Store,
        };

        if (!string.IsNullOrWhiteSpace(request.PreviousResponseId))
        {
            payload["previous_response_id"] = request.PreviousResponseId;
        }

        return payload;
    }

    private async Task<ImageGenerationResult> PersistImageResultAsync(
        ImageGenerationRequest request,
        JsonElement root,
        ProviderCallTelemetry telemetry,
        OpenAiRoutingDecision routing,
        string model,
        CancellationToken cancellationToken)
    {
        var imageBase64 = ExtractImageBase64(root);
        var imageBytes = Convert.FromBase64String(imageBase64);
        var generatedAt = DateTimeOffset.UtcNow;
        var outputFormat = NormalizeOutputFormat(request.Settings.OutputFormat);
        var providerTraceId = ExtractTraceId(root);
        var revisedPrompt = ExtractRevisedPrompt(root);
        var toolCallId = ExtractToolCallId(root);

        Directory.CreateDirectory(request.OutputDirectory);

        var assetPath = Path.Combine(
            request.OutputDirectory,
            EnsureOutputFileName(request.OutputFileName, outputFormat, request.SeriesItemId, request.PromptVersionId));
        var metadataPath = Path.ChangeExtension(assetPath, ".json");

        await File.WriteAllBytesAsync(assetPath, imageBytes, cancellationToken);
        await File.WriteAllTextAsync(
            metadataPath,
            JsonSerializer.Serialize(
                new
                {
                    providerId = Capabilities.ProviderId,
                    endpointFamily = routing.EndpointFamily.ToString().ToLowerInvariant(),
                    store = routing.Store,
                    model,
                    providerTraceId,
                    toolCallId,
                    revisedPrompt,
                    previousResponseId = request.PreviousResponseId,
                    usedResponsesApi = request.UseResponsesApi,
                    seriesItemId = request.SeriesItemId,
                    promptVersionId = request.PromptVersionId,
                    promptText = request.PromptText,
                    settings = new
                    {
                        size = BuildSize(request),
                        quality = NormalizeQuality(request.Settings.Quality),
                        outputFormat,
                        seed = request.Settings.Seed,
                    },
                    telemetry = new
                    {
                        telemetry.RequestId,
                        telemetry.HttpStatusCode,
                        telemetry.Succeeded,
                        telemetry.Latency,
                        usage = telemetry.Usage,
                        telemetry.EstimatedCostUsd,
                        telemetry.RateCardName,
                    },
                    generatedAt,
                },
                JsonOptions),
            cancellationToken);

        return new ImageGenerationResult(
            Guid.NewGuid(),
            assetPath,
            metadataPath,
            providerTraceId,
            generatedAt,
            revisedPrompt,
            toolCallId);
    }

    private static string BuildSize(ImageGenerationRequest request)
    {
        return request.Settings.Width > 0 && request.Settings.Height > 0
            ? $"{request.Settings.Width}x{request.Settings.Height}"
            : "auto";
    }

    private static string NormalizeQuality(string quality)
    {
        return quality.Trim().ToLowerInvariant() switch
        {
            "low" => "low",
            "medium" => "medium",
            "high" or "hd" => "high",
            _ => "auto",
        };
    }

    private static string NormalizeOutputFormat(string outputFormat)
    {
        return outputFormat.Trim().ToLowerInvariant() switch
        {
            "jpeg" or "jpg" => "jpeg",
            "webp" => "webp",
            _ => "png",
        };
    }

    private static string EnsureOutputFileName(
        string outputFileName,
        string outputFormat,
        Guid seriesItemId,
        Guid promptVersionId)
    {
        var extension = outputFormat is "jpeg" ? ".jpg" : $".{outputFormat}";
        var fileName = string.IsNullOrWhiteSpace(outputFileName)
            ? $"{seriesItemId:N}-{promptVersionId:N}{extension}"
            : Path.GetFileName(outputFileName);

        return Path.GetExtension(fileName).Equals(extension, StringComparison.OrdinalIgnoreCase)
            ? fileName
            : Path.ChangeExtension(fileName, extension);
    }

    private static string ExtractImageBase64(JsonElement root)
    {
        if (root.TryGetProperty("output", out var outputElement)
            && outputElement.ValueKind is JsonValueKind.Array)
        {
            foreach (var item in outputElement.EnumerateArray())
            {
                if (item.TryGetProperty("type", out var typeElement)
                    && typeElement.ValueKind is JsonValueKind.String
                    && string.Equals(typeElement.GetString(), "image_generation_call", StringComparison.Ordinal)
                    && item.TryGetProperty("result", out var resultElement)
                    && resultElement.ValueKind is JsonValueKind.String)
                {
                    return resultElement.GetString()!;
                }
            }
        }

        if (root.TryGetProperty("data", out var dataElement)
            && dataElement.ValueKind is JsonValueKind.Array)
        {
            foreach (var image in dataElement.EnumerateArray())
            {
                if (image.TryGetProperty("b64_json", out var base64Element)
                    && base64Element.ValueKind is JsonValueKind.String)
                {
                    return base64Element.GetString()!;
                }
            }
        }

        throw new InvalidOperationException("OpenAI image generation response did not include base64 image data.");
    }

    private static string? ExtractRevisedPrompt(JsonElement root)
    {
        if (root.TryGetProperty("output", out var outputElement)
            && outputElement.ValueKind is JsonValueKind.Array)
        {
            foreach (var item in outputElement.EnumerateArray())
            {
                if (item.TryGetProperty("type", out var typeElement)
                    && typeElement.ValueKind is JsonValueKind.String
                    && string.Equals(typeElement.GetString(), "image_generation_call", StringComparison.Ordinal)
                    && item.TryGetProperty("revised_prompt", out var revisedPromptElement)
                    && revisedPromptElement.ValueKind is JsonValueKind.String)
                {
                    return revisedPromptElement.GetString();
                }
            }
        }

        return null;
    }

    private static string? ExtractToolCallId(JsonElement root)
    {
        if (root.TryGetProperty("output", out var outputElement)
            && outputElement.ValueKind is JsonValueKind.Array)
        {
            foreach (var item in outputElement.EnumerateArray())
            {
                if (item.TryGetProperty("type", out var typeElement)
                    && typeElement.ValueKind is JsonValueKind.String
                    && string.Equals(typeElement.GetString(), "image_generation_call", StringComparison.Ordinal)
                    && item.TryGetProperty("id", out var idElement)
                    && idElement.ValueKind is JsonValueKind.String)
                {
                    return idElement.GetString();
                }
            }
        }

        return null;
    }

    private static string ExtractTraceId(JsonElement root)
    {
        if (root.TryGetProperty("id", out var idElement) && idElement.ValueKind is JsonValueKind.String)
        {
            return idElement.GetString()!;
        }

        return root.TryGetProperty("created", out var createdElement)
            ? $"openai-image-{createdElement}"
            : "openai-image-generate";
    }

    private async Task<string?> GetOptionalSecretAsync(string? secretName, CancellationToken cancellationToken)
    {
        return string.IsNullOrWhiteSpace(secretName)
            ? null
            : await _secretStore.GetSecretAsync(secretName, cancellationToken);
    }

    private static void AddOptionalAppHeaders(
        HttpRequestMessage request,
        string? appId,
        string? appSecret)
    {
        if (!string.IsNullOrWhiteSpace(appId))
        {
            request.Headers.TryAddWithoutValidation("X-App-ID", appId);
        }

        if (!string.IsNullOrWhiteSpace(appSecret))
        {
            request.Headers.TryAddWithoutValidation("X-App-Secret", appSecret);
        }
    }

    private void RecordTelemetry(
        Uri endpoint,
        HttpResponseMessage response,
        JsonElement? body,
        string? providerTraceId,
        string model,
        TimeSpan latency)
    {
        _telemetrySink.Record(CreateTelemetry(endpoint, response, body, providerTraceId, model, latency));
    }

    private ProviderCallTelemetry CreateTelemetry(
        Uri endpoint,
        HttpResponseMessage response,
        JsonElement? body,
        string? providerTraceId,
        string model,
        TimeSpan latency)
    {
        return OpenAiProviderTelemetry.Create(
            Capabilities.ProviderId,
            "image-generation",
            model,
            endpoint,
            response,
            body,
            providerTraceId,
            latency,
            _rateCard.ImageGenerationRequestUsd,
            _rateCard.Name);
    }

    private static IReadOnlyList<string> GetCapabilityModelIds(OpenAiProviderOptions options)
    {
        var models = new List<string> { options.ImageGenerationModel };
        if (!string.IsNullOrWhiteSpace(options.ImageGenerationResponsesModel))
        {
            models.Add(options.ImageGenerationResponsesModel);
        }

        return models
            .Where(model => !string.IsNullOrWhiteSpace(model))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }
}
