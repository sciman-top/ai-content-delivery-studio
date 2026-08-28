using System.Diagnostics;
using ContentDeliveryStudio.Application.ScientificFigures;

namespace ContentDeliveryStudio.Infrastructure.OpenAI;

public sealed class OpenAiScientificUnderstandingProvider : IScientificUnderstandingProvider
{
    private readonly OpenAiProviderOptions _options;
    private readonly IOpenAiSecretStore _secretStore;
    private readonly Func<CancellationToken, Task<IOpenAiResponsesClient>> _clientFactory;
    private readonly IProviderCallTelemetrySink _telemetrySink;
    private readonly IOpenAiModelAvailabilityProbe? _modelAvailabilityProbe;
    private readonly IOpenAiExecutionSlotScheduler? _executionSlotScheduler;
    private readonly IOpenAiActivePresetSetState? _activePresetSetState;

    public OpenAiScientificUnderstandingProvider(
        OpenAiProviderOptions options,
        OpenAiSdkClientFactory clientFactory,
        IOpenAiSecretStore secretStore,
        IProviderCallTelemetrySink? telemetrySink = null,
        IOpenAiModelAvailabilityProbe? modelAvailabilityProbe = null,
        IOpenAiExecutionSlotScheduler? executionSlotScheduler = null,
        IOpenAiActivePresetSetState? activePresetSetState = null)
        : this(
            options,
            token => CreateClientAsync(options, clientFactory, token),
            secretStore,
            telemetrySink,
            modelAvailabilityProbe,
            executionSlotScheduler,
            activePresetSetState)
    {
    }

    internal OpenAiScientificUnderstandingProvider(
        OpenAiProviderOptions options,
        IOpenAiResponsesClient responsesClient,
        IOpenAiSecretStore secretStore,
        IProviderCallTelemetrySink? telemetrySink = null,
        IOpenAiModelAvailabilityProbe? modelAvailabilityProbe = null,
        IOpenAiExecutionSlotScheduler? executionSlotScheduler = null,
        IOpenAiActivePresetSetState? activePresetSetState = null)
        : this(options, _ => Task.FromResult(responsesClient), secretStore, telemetrySink, modelAvailabilityProbe, executionSlotScheduler, activePresetSetState)
    {
    }

    private OpenAiScientificUnderstandingProvider(
        OpenAiProviderOptions options,
        Func<CancellationToken, Task<IOpenAiResponsesClient>> clientFactory,
        IOpenAiSecretStore secretStore,
        IProviderCallTelemetrySink? telemetrySink,
        IOpenAiModelAvailabilityProbe? modelAvailabilityProbe,
        IOpenAiExecutionSlotScheduler? executionSlotScheduler,
        IOpenAiActivePresetSetState? activePresetSetState)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _clientFactory = clientFactory ?? throw new ArgumentNullException(nameof(clientFactory));
        _secretStore = secretStore ?? throw new ArgumentNullException(nameof(secretStore));
        _telemetrySink = telemetrySink ?? NullProviderCallTelemetrySink.Instance;
        _modelAvailabilityProbe = modelAvailabilityProbe;
        _executionSlotScheduler = executionSlotScheduler;
        _activePresetSetState = activePresetSetState;
        OpenAiProviderGuard.EnsureAllowsOperation(options, OpenAiProviderOperation.TextPlanning);
    }

    public async Task<ScientificUnderstandingChunkResult> AnalyzeChunkAsync(
        ScientificUnderstandingChunkRequest request,
        CancellationToken cancellationToken)
    {
        var route = OpenAiTaskModelRouter.ForScientificUnderstanding(_options, request);
        await OpenAiProviderGuard.EnsureCanCallRealApiAsync(
            _options,
            _secretStore,
            OpenAiProviderOperation.TextPlanning,
            cancellationToken);
        var endpoint = new Uri(_options.BaseUri, OpenAiRoutingDefaults.PlanningEndpointPath);
        return await OpenAiModelFailoverPolicy.ExecuteAsync(
            _options,
            route,
            _modelAvailabilityProbe,
            candidateRoute => ExecuteAsync(request, candidateRoute, endpoint, cancellationToken),
            cancellationToken,
            _executionSlotScheduler,
            _activePresetSetState);
    }

    private async Task<ScientificUnderstandingChunkResult> ExecuteAsync(
        ScientificUnderstandingChunkRequest request,
        OpenAiTaskModelRoute route,
        Uri endpoint,
        CancellationToken cancellationToken)
    {
        var sdkOptions = OpenAiSdkResponseOptionsFactory.CreateScientificUnderstandingOptions(
            _options,
            request,
            route);
        var client = await _clientFactory(cancellationToken);
        var stopwatch = Stopwatch.StartNew();
        try
        {
            var response = await client.CreateResponseAsync(sdkOptions, cancellationToken);
            stopwatch.Stop();
            var body = response.Body
                ?? throw new InvalidOperationException(
                    "OpenAI scientific understanding response did not include a JSON body.");
            var result = OpenAiScientificUnderstandingMapper.Parse(body, request);
            RecordTelemetry(endpoint, response, result.ProviderTraceId, stopwatch.Elapsed, route);
            return result;
        }
        catch (OpenAiResponsesClientException exception)
        {
            stopwatch.Stop();
            RecordTelemetry(endpoint, exception.Result, null, stopwatch.Elapsed, route);
            throw new HttpRequestException(
                $"OpenAI scientific understanding request failed with status {exception.Result.StatusCode} {exception.Result.ReasonPhrase}.",
                exception);
        }
    }

    private static async Task<IOpenAiResponsesClient> CreateClientAsync(
        OpenAiProviderOptions options,
        OpenAiSdkClientFactory clientFactory,
        CancellationToken cancellationToken)
    {
        var client = await clientFactory.CreateResponsesClientAsync(
            options,
            OpenAiProviderOperation.TextPlanning,
            cancellationToken);
        return new OpenAiSdkResponsesClient(client);
    }

    private void RecordTelemetry(
        Uri endpoint,
        OpenAiResponsesClientResult response,
        string? providerTraceId,
        TimeSpan latency,
        OpenAiTaskModelRoute route)
    {
        _telemetrySink.Record(OpenAiProviderTelemetry.Create(
            "openai-scientific-understanding",
            "scientific-understanding",
            route.Model,
            endpoint,
            response.StatusCode,
            response.IsSuccessStatusCode,
            response.RequestId,
            response.Body,
            providerTraceId,
            latency,
            estimatedCostUsd: 0m,
            rateCardName: "unpriced",
            route));
    }
}
