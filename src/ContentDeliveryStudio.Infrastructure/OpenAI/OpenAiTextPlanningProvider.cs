using System.Diagnostics;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using ContentDeliveryStudio.Core.Documents;
using ContentDeliveryStudio.Core.Providers;

namespace ContentDeliveryStudio.Infrastructure.OpenAI;

public sealed class OpenAiTextPlanningProvider : ITextPlanningProvider
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private static readonly OpenAiRoutingDecision Routing = OpenAiProviderRoutingPolicy.ForTextPlanning();

    private readonly HttpClient _httpClient;
    private readonly OpenAiProviderOptions _options;
    private readonly IOpenAiSecretStore _secretStore;
    private readonly IProviderCallTelemetrySink _telemetrySink;
    private readonly OpenAiCostRateCard _rateCard;

    public OpenAiTextPlanningProvider(
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
        OpenAiProviderGuard.EnsureAllowsOperation(_options, OpenAiProviderOperation.TextPlanning);

        Capabilities = new ProviderCapabilities(
            "openai-text",
            "OpenAI Text Planning Provider",
            [_options.TextPlanningModel],
            SupportsTextPlanning: true,
            SupportsImageGeneration: false,
            SupportsVisionReview: false,
            SupportsImageEditing: false,
            SupportsStreaming: false);
    }

    public IProviderCapabilities Capabilities { get; }

    public async Task<SeriesPlanResult> CreatePlanAsync(
        PlanningRequest request,
        CancellationToken cancellationToken)
    {
        ValidateRequest(request);

        await OpenAiProviderGuard.EnsureCanCallRealApiAsync(
            _options,
            _secretStore,
            OpenAiProviderOperation.TextPlanning,
            cancellationToken);
        var apiKey = await _secretStore.GetSecretAsync(_options.ApiKeySecretName, cancellationToken)
            ?? throw new InvalidOperationException("OpenAI API key was not found in the configured secret store.");

        var endpoint = new Uri(_options.BaseUri, Routing.RelativePath);
        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, endpoint);
        httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        httpRequest.Content = JsonContent.Create(CreatePayload(request), options: JsonOptions);

        var stopwatch = Stopwatch.StartNew();
        using var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
        stopwatch.Stop();
        if (!response.IsSuccessStatusCode)
        {
            RecordTelemetry(endpoint, response, body: null, providerTraceId: null, stopwatch.Elapsed);
            throw new HttpRequestException(
                $"OpenAI text planning request failed with status {(int)response.StatusCode} {response.ReasonPhrase}.");
        }

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var document = await ParseJsonOrThrowAsync(
            stream,
            cancellationToken,
            "OpenAI text planning response contained invalid JSON.");
        var providerTraceId = OpenAiTextPlanningResponseMapper.ExtractTraceId(document.RootElement);
        RecordTelemetry(endpoint, response, document.RootElement, providerTraceId, stopwatch.Elapsed);
        return OpenAiTextPlanningResponseMapper.ParseSeriesPlan(document.RootElement);
    }

    private static void ValidateRequest(PlanningRequest request)
    {
        var estimatedCharacters = TextPlanningExecutionPolicy.EstimateInputCharacters(request);
        if (estimatedCharacters > TextPlanningExecutionPolicy.DefaultMaxInputCharacters)
        {
            throw new InvalidOperationException(
                $"Text planning request exceeds the bounded local-direct default of {TextPlanningExecutionPolicy.DefaultMaxInputCharacters} characters. Split or summarize the request locally before dispatch.");
        }
    }

    public Task<BriefPlanningResult> CreatePromptDirectionsAsync(
        BriefPlanningRequest request,
        CancellationToken cancellationToken)
    {
        throw new NotSupportedException(
            "Brief direction planning is not implemented for OpenAI in this slice. Use FakeTextPlanningProvider first.");
    }

    public Task<BlueprintPlanningResult> CreateDesignBlueprintsAsync(
        BlueprintPlanningRequest request,
        CancellationToken cancellationToken)
    {
        throw new NotSupportedException(
            "Blueprint planning is not implemented for OpenAI in this slice. Use FakeTextPlanningProvider first.");
    }

    public Task<DocumentIllustrationPlanningResult> CreateDocumentIllustrationPlanAsync(
        DocumentIllustrationPlanningRequest request,
        CancellationToken cancellationToken)
        => CreateDocumentIllustrationPlanInternalAsync(request, cancellationToken);

    private Dictionary<string, object?> CreatePayload(PlanningRequest request)
    {
        return new Dictionary<string, object?>
        {
            ["model"] = _options.TextPlanningModel,
            ["instructions"] = "You plan coherent image series. Return only valid JSON that matches the requested schema.",
            ["input"] = BuildInput(request),
            ["store"] = Routing.Store,
            ["text"] = new Dictionary<string, object?>
            {
                ["format"] = new Dictionary<string, object?>
                {
                    ["type"] = "json_schema",
                    ["name"] = "image_series_plan",
                    ["strict"] = true,
                    ["schema"] = CreatePlanSchema(),
                },
            },
        };
    }

    private async Task<DocumentIllustrationPlanningResult> CreateDocumentIllustrationPlanInternalAsync(
        DocumentIllustrationPlanningRequest request,
        CancellationToken cancellationToken)
    {
        await OpenAiProviderGuard.EnsureCanCallRealApiAsync(
            _options,
            _secretStore,
            OpenAiProviderOperation.TextPlanning,
            cancellationToken);
        var apiKey = await _secretStore.GetSecretAsync(_options.ApiKeySecretName, cancellationToken)
            ?? throw new InvalidOperationException("OpenAI API key was not found in the configured secret store.");

        var endpoint = new Uri(_options.BaseUri, Routing.RelativePath);
        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, endpoint);
        httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        httpRequest.Content = JsonContent.Create(
            OpenAiTextPlanningRequestMapper.CreateDocumentIllustrationResponsesPayload(_options, request),
            options: JsonOptions);

        var stopwatch = Stopwatch.StartNew();
        using var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
        stopwatch.Stop();
        if (!response.IsSuccessStatusCode)
        {
            RecordTelemetry(endpoint, response, body: null, providerTraceId: null, stopwatch.Elapsed);
            throw new HttpRequestException(
                $"OpenAI text planning request failed with status {(int)response.StatusCode} {response.ReasonPhrase}.");
        }

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var document = await ParseJsonOrThrowAsync(
            stream,
            cancellationToken,
            "OpenAI document illustration planning response contained invalid JSON.");
        var providerTraceId = OpenAiTextPlanningResponseMapper.ExtractTraceId(document.RootElement);
        RecordTelemetry(endpoint, response, document.RootElement, providerTraceId, stopwatch.Elapsed);
        return OpenAiTextPlanningResponseMapper.ParseDocumentIllustrationPlan(document.RootElement);
    }

    private static string BuildInput(PlanningRequest request)
    {
        return string.Join(
            Environment.NewLine,
            [
                $"Goal: {request.Goal}",
                $"Audience: {request.Audience}",
                $"Item count: {Math.Max(1, request.ItemCount)}",
                $"Style brief: {request.StyleBrief}",
            ]);
    }

    private static Dictionary<string, object?> CreatePlanSchema()
    {
        return new Dictionary<string, object?>
        {
            ["type"] = "object",
            ["additionalProperties"] = false,
            ["required"] = new[] { "summary", "items" },
            ["properties"] = new Dictionary<string, object?>
            {
                ["summary"] = new Dictionary<string, object?>
                {
                    ["type"] = "string",
                    ["minLength"] = 1,
                },
                ["items"] = new Dictionary<string, object?>
                {
                    ["type"] = "array",
                    ["minItems"] = 1,
                    ["items"] = new Dictionary<string, object?>
                    {
                        ["type"] = "object",
                        ["additionalProperties"] = false,
                        ["required"] = new[] { "title", "brief", "promptDraft" },
                        ["properties"] = new Dictionary<string, object?>
                        {
                            ["title"] = new Dictionary<string, object?>
                            {
                                ["type"] = "string",
                                ["minLength"] = 1,
                            },
                            ["brief"] = new Dictionary<string, object?>
                            {
                                ["type"] = "string",
                                ["minLength"] = 1,
                            },
                            ["promptDraft"] = new Dictionary<string, object?>
                            {
                                ["type"] = "string",
                                ["minLength"] = 1,
                            },
                        },
                    },
                },
            },
        };
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
            "text-planning",
            _options.TextPlanningModel,
            endpoint,
            response,
            body,
            providerTraceId,
            latency,
            _rateCard.TextPlanningRequestUsd,
            _rateCard.Name));
    }

    private static async Task<JsonDocument> ParseJsonOrThrowAsync(
        Stream stream,
        CancellationToken cancellationToken,
        string message)
    {
        try
        {
            return await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
        }
        catch (JsonException exception)
        {
            throw new InvalidOperationException(message, exception);
        }
    }

}
