using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text.Json;
using ContentDeliveryStudio.Application.ScientificFigures;

namespace ContentDeliveryStudio.Infrastructure.OpenAI;

public sealed class OpenAiScientificReviewProvider
    : IScientificSemanticReviewProvider, IScientificVisualReviewProvider
{
    private const int MaximumTransientAttempts = 3;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly HttpClient _httpClient;
    private readonly OpenAiProviderOptions _options;
    private readonly IOpenAiSecretStore _secretStore;
    private readonly IProviderCallTelemetrySink _telemetrySink;
    private readonly IOpenAiScientificReviewCheckpointStore _checkpointStore;

    public OpenAiScientificReviewProvider(
        HttpClient httpClient,
        OpenAiProviderOptions options,
        IOpenAiSecretStore secretStore,
        IProviderCallTelemetrySink? telemetrySink = null,
        IOpenAiScientificReviewCheckpointStore? checkpointStore = null)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _secretStore = secretStore ?? throw new ArgumentNullException(nameof(secretStore));
        _telemetrySink = telemetrySink ?? NullProviderCallTelemetrySink.Instance;
        _checkpointStore = checkpointStore ?? NullOpenAiScientificReviewCheckpointStore.Instance;
        OpenAiProviderGuard.EnsureAllowsOperation(options, OpenAiProviderOperation.VisionReview);
    }

    public Task<ScientificProviderReviewResult> ReviewAsync(
        ScientificSemanticReviewRequest request,
        CancellationToken cancellationToken)
    {
        var allowedIds = request.Specification.Elements.Select(item => item.ElementId)
            .Concat(request.Specification.Relations.Select(item => item.RelationId))
            .ToHashSet(StringComparer.Ordinal);
        var route = OpenAiTaskModelRouter.ForScientificSemanticReview(_options, request);
        return ReviewAsync(
            "scientific-semantic-review",
            OpenAiScientificReviewMapper.CreateSemanticPayload(
                request,
                route.Model,
                route.ReasoningEffort),
            allowedIds,
            ScientificReviewLayer.Semantic.ToString(),
            route,
            cancellationToken);
    }

    public Task<ScientificProviderReviewResult> ReviewAsync(
        ScientificVisualReviewRequest request,
        CancellationToken cancellationToken)
    {
        var allowedIds = request.RegionCrops.Select(item => item.ResponsibleItemId)
            .ToHashSet(StringComparer.Ordinal);
        var route = OpenAiTaskModelRouter.ForScientificVisualReview(_options, request);
        return ReviewAsync(
            "scientific-visual-review",
            OpenAiScientificReviewMapper.CreateVisualPayload(
                request,
                route.Model,
                route.ReasoningEffort),
            allowedIds,
            ScientificReviewLayer.Visual.ToString(),
            route,
            cancellationToken);
    }

    private async Task<ScientificProviderReviewResult> ReviewAsync(
        string operation,
        Dictionary<string, object?> payload,
        IReadOnlySet<string> allowedIds,
        string layerResponsibleItemId,
        OpenAiTaskModelRoute route,
        CancellationToken cancellationToken)
    {
        if (!_options.RealApiEnabled)
        {
            throw new InvalidOperationException("Real OpenAI API calls are disabled.");
        }

        var endpoint = new Uri(_options.BaseUri, OpenAiRoutingDefaults.VisionReviewEndpointPath);
        var payloadBytes = JsonSerializer.SerializeToUtf8Bytes(payload, JsonOptions);
        var checkpointIdentity = OpenAiScientificReviewCheckpointIdentity.Create(
            operation,
            endpoint,
            route.Model,
            route.ReasoningEffort,
            payloadBytes);
        var resumed = await _checkpointStore.TryLoadAsync(checkpointIdentity, cancellationToken);
        if (resumed is not null)
        {
            ValidateResumedResult(resumed, allowedIds);
            return resumed with { Origin = ScientificProviderReviewOrigin.PersistedCheckpoint };
        }

        await OpenAiProviderGuard.EnsureCanCallRealApiAsync(
            _options,
            _secretStore,
            OpenAiProviderOperation.VisionReview,
            cancellationToken);
        var apiKey = await _secretStore.GetSecretAsync(_options.ApiKeySecretName, cancellationToken)
            ?? throw new InvalidOperationException("OpenAI API key was not found in the configured secret store.");
        for (var attempt = 1; attempt <= MaximumTransientAttempts; attempt++)
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
            request.Content = new ByteArrayContent(payloadBytes);
            request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

            var stopwatch = Stopwatch.StartNew();
            HttpResponseMessage response;
            try
            {
                response = await _httpClient.SendAsync(request, cancellationToken);
            }
            catch (HttpRequestException) when (attempt < MaximumTransientAttempts)
            {
                stopwatch.Stop();
                await DelayForRetryAsync(attempt, cancellationToken);
                continue;
            }

            using (response)
            {
                stopwatch.Stop();
                if (!response.IsSuccessStatusCode)
                {
                    RecordTelemetry(operation, endpoint, response, null, null, stopwatch.Elapsed, route);
                    if (IsTransient(response.StatusCode) && attempt < MaximumTransientAttempts)
                    {
                        await DelayForRetryAsync(attempt, cancellationToken);
                        continue;
                    }

                    throw new HttpRequestException(
                        $"OpenAI scientific review request failed with status {(int)response.StatusCode} {response.ReasonPhrase}.");
                }

                await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
                using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
                var traceId = OpenAiTextPlanningResponseMapper.ExtractTraceId(document.RootElement);
                RecordTelemetry(operation, endpoint, response, document.RootElement, traceId, stopwatch.Elapsed, route);
                var result = OpenAiScientificReviewMapper.Parse(
                    document.RootElement,
                    allowedIds,
                    layerResponsibleItemId);
                await _checkpointStore.SaveAsync(checkpointIdentity, result, cancellationToken);
                return result;
            }
        }

        throw new HttpRequestException("OpenAI scientific review exhausted transient retries.");
    }

    private static bool IsTransient(System.Net.HttpStatusCode statusCode) =>
        statusCode is System.Net.HttpStatusCode.RequestTimeout
            or System.Net.HttpStatusCode.TooManyRequests
        || (int)statusCode >= 500;

    private static Task DelayForRetryAsync(int attempt, CancellationToken cancellationToken) =>
        Task.Delay(TimeSpan.FromMilliseconds(100 * attempt), cancellationToken);

    private static void ValidateResumedResult(
        ScientificProviderReviewResult result,
        IReadOnlySet<string> allowedIds)
    {
        JsonOpenAiScientificReviewCheckpointStore.ValidateResult(result);
        if (result.Findings.Any(item => !allowedIds.Contains(item.ResponsibleItemId)))
        {
            throw new InvalidDataException(
                "Scientific review checkpoint contains an unknown responsible item.");
        }
    }

    private void RecordTelemetry(
        string operation,
        Uri endpoint,
        HttpResponseMessage response,
        JsonElement? body,
        string? traceId,
        TimeSpan latency,
        OpenAiTaskModelRoute route)
    {
        _telemetrySink.Record(OpenAiProviderTelemetry.Create(
            "openai-scientific-review",
            operation,
            route.Model,
            endpoint,
            response,
            body,
            traceId,
            latency,
            estimatedCostUsd: 0m,
            rateCardName: "unpriced",
            route));
    }
}
