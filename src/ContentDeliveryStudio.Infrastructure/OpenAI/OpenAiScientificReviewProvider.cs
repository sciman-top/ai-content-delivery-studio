using System.Diagnostics;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using ContentDeliveryStudio.Application.ScientificFigures;

namespace ContentDeliveryStudio.Infrastructure.OpenAI;

public sealed class OpenAiScientificReviewProvider
    : IScientificSemanticReviewProvider, IScientificVisualReviewProvider
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly HttpClient _httpClient;
    private readonly OpenAiProviderOptions _options;
    private readonly IOpenAiSecretStore _secretStore;
    private readonly IProviderCallTelemetrySink _telemetrySink;

    public OpenAiScientificReviewProvider(
        HttpClient httpClient,
        OpenAiProviderOptions options,
        IOpenAiSecretStore secretStore,
        IProviderCallTelemetrySink? telemetrySink = null)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _secretStore = secretStore ?? throw new ArgumentNullException(nameof(secretStore));
        _telemetrySink = telemetrySink ?? NullProviderCallTelemetrySink.Instance;
        OpenAiProviderGuard.EnsureAllowsOperation(options, OpenAiProviderOperation.VisionReview);
    }

    public Task<ScientificProviderReviewResult> ReviewAsync(
        ScientificSemanticReviewRequest request,
        CancellationToken cancellationToken)
    {
        var allowedIds = request.Specification.Elements.Select(item => item.ElementId)
            .Concat(request.Specification.Relations.Select(item => item.RelationId))
            .ToHashSet(StringComparer.Ordinal);
        return ReviewAsync(
            "scientific-semantic-review",
            OpenAiScientificReviewMapper.CreateSemanticPayload(request, _options.VisionReviewModel),
            allowedIds,
            ScientificReviewLayer.Semantic.ToString(),
            cancellationToken);
    }

    public Task<ScientificProviderReviewResult> ReviewAsync(
        ScientificVisualReviewRequest request,
        CancellationToken cancellationToken)
    {
        var allowedIds = request.RegionCrops.Select(item => item.ResponsibleItemId)
            .ToHashSet(StringComparer.Ordinal);
        return ReviewAsync(
            "scientific-visual-review",
            OpenAiScientificReviewMapper.CreateVisualPayload(request, _options.VisionReviewModel),
            allowedIds,
            ScientificReviewLayer.Visual.ToString(),
            cancellationToken);
    }

    private async Task<ScientificProviderReviewResult> ReviewAsync(
        string operation,
        Dictionary<string, object?> payload,
        IReadOnlySet<string> allowedIds,
        string layerResponsibleItemId,
        CancellationToken cancellationToken)
    {
        await OpenAiProviderGuard.EnsureCanCallRealApiAsync(
            _options,
            _secretStore,
            OpenAiProviderOperation.VisionReview,
            cancellationToken);
        var apiKey = await _secretStore.GetSecretAsync(_options.ApiKeySecretName, cancellationToken)
            ?? throw new InvalidOperationException("OpenAI API key was not found in the configured secret store.");
        var endpoint = new Uri(_options.BaseUri, OpenAiRoutingDefaults.VisionReviewEndpointPath);
        using var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        request.Content = JsonContent.Create(payload, options: JsonOptions);

        var stopwatch = Stopwatch.StartNew();
        using var response = await _httpClient.SendAsync(request, cancellationToken);
        stopwatch.Stop();
        if (!response.IsSuccessStatusCode)
        {
            RecordTelemetry(operation, endpoint, response, null, null, stopwatch.Elapsed);
            throw new HttpRequestException(
                $"OpenAI scientific review request failed with status {(int)response.StatusCode} {response.ReasonPhrase}.");
        }

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
        var traceId = OpenAiTextPlanningResponseMapper.ExtractTraceId(document.RootElement);
        RecordTelemetry(operation, endpoint, response, document.RootElement, traceId, stopwatch.Elapsed);
        return OpenAiScientificReviewMapper.Parse(
            document.RootElement,
            allowedIds,
            layerResponsibleItemId);
    }

    private void RecordTelemetry(
        string operation,
        Uri endpoint,
        HttpResponseMessage response,
        JsonElement? body,
        string? traceId,
        TimeSpan latency)
    {
        _telemetrySink.Record(OpenAiProviderTelemetry.Create(
            "openai-scientific-review",
            operation,
            _options.VisionReviewModel,
            endpoint,
            response,
            body,
            traceId,
            latency,
            estimatedCostUsd: 0m,
            rateCardName: "unpriced"));
    }
}
