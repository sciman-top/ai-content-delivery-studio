using System.Diagnostics;
using ImageSeriesStudio.Core.Documents;
using ImageSeriesStudio.Core.Providers;

namespace ImageSeriesStudio.Infrastructure.OpenAI;

public sealed class OpenAiSdkTextPlanningProvider : ITextPlanningProvider
{
    private readonly OpenAiProviderOptions _options;
    private readonly IOpenAiSecretStore _secretStore;
    private readonly Func<CancellationToken, Task<IOpenAiResponsesClient>> _clientFactory;
    private readonly IProviderCallTelemetrySink _telemetrySink;
    private readonly OpenAiCostRateCard _rateCard;

    public OpenAiSdkTextPlanningProvider(
        OpenAiProviderOptions options,
        OpenAiSdkClientFactory clientFactory,
        IOpenAiSecretStore secretStore,
        IProviderCallTelemetrySink? telemetrySink = null,
        OpenAiCostRateCard? rateCard = null)
        : this(
            options,
            cancellationToken => CreateSdkResponsesClientAsync(options, clientFactory, cancellationToken),
            secretStore,
            telemetrySink,
            rateCard)
    {
    }

    internal OpenAiSdkTextPlanningProvider(
        OpenAiProviderOptions options,
        IOpenAiResponsesClient responsesClient,
        IOpenAiSecretStore secretStore,
        IProviderCallTelemetrySink? telemetrySink = null,
        OpenAiCostRateCard? rateCard = null)
        : this(
            options,
            _ => Task.FromResult(responsesClient),
            secretStore,
            telemetrySink,
            rateCard)
    {
    }

    private OpenAiSdkTextPlanningProvider(
        OpenAiProviderOptions options,
        Func<CancellationToken, Task<IOpenAiResponsesClient>> clientFactory,
        IOpenAiSecretStore secretStore,
        IProviderCallTelemetrySink? telemetrySink,
        OpenAiCostRateCard? rateCard)
    {
        _options = options;
        _clientFactory = clientFactory;
        _secretStore = secretStore;
        _telemetrySink = telemetrySink ?? NullProviderCallTelemetrySink.Instance;
        _rateCard = rateCard ?? OpenAiCostRateCard.Unpriced;
        OpenAiProviderGuard.EnsureAllowsOperation(_options, OpenAiProviderOperation.TextPlanning);

        Capabilities = new ProviderCapabilities(
            "openai-text-sdk",
            "OpenAI SDK Text Planning Provider",
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
        await OpenAiProviderGuard.EnsureCanCallRealApiAsync(
            _options,
            _secretStore,
            OpenAiProviderOperation.TextPlanning,
            cancellationToken);

        var client = await _clientFactory(cancellationToken);
        var sdkOptions = OpenAiSdkResponseOptionsFactory.CreateTextPlanningOptions(_options, request);
        var endpoint = new Uri(_options.BaseUri, OpenAiRoutingDefaults.PlanningEndpointPath);
        var stopwatch = Stopwatch.StartNew();

        try
        {
            var response = await client.CreateResponseAsync(sdkOptions, cancellationToken);
            stopwatch.Stop();
            var body = response.Body
                ?? throw new InvalidOperationException("OpenAI SDK text planning response did not include a JSON body.");
            var providerTraceId = OpenAiTextPlanningResponseMapper.ExtractTraceId(body);

            RecordTelemetry(endpoint, response, providerTraceId, stopwatch.Elapsed);

            return OpenAiTextPlanningResponseMapper.ParseSeriesPlan(body);
        }
        catch (OpenAiResponsesClientException exception)
        {
            stopwatch.Stop();
            RecordTelemetry(endpoint, exception.Result, providerTraceId: null, stopwatch.Elapsed);
            throw new HttpRequestException(
                $"OpenAI SDK text planning request failed with status {exception.Result.StatusCode} {exception.Result.ReasonPhrase}.",
                exception);
        }
    }

    public Task<BriefPlanningResult> CreatePromptDirectionsAsync(
        BriefPlanningRequest request,
        CancellationToken cancellationToken)
    {
        throw new NotSupportedException(
            "Brief direction planning is not implemented for OpenAI SDK in this slice. Use FakeTextPlanningProvider first.");
    }

    public Task<BlueprintPlanningResult> CreateDesignBlueprintsAsync(
        BlueprintPlanningRequest request,
        CancellationToken cancellationToken)
    {
        throw new NotSupportedException(
            "Blueprint planning is not implemented for OpenAI SDK in this slice. Use FakeTextPlanningProvider first.");
    }

    public Task<DocumentIllustrationPlanningResult> CreateDocumentIllustrationPlanAsync(
        DocumentIllustrationPlanningRequest request,
        CancellationToken cancellationToken)
    {
        throw new InvalidOperationException(
            "OpenAI SDK document illustration planning is not enabled in the first fake-provider implementation.");
    }

    private static async Task<IOpenAiResponsesClient> CreateSdkResponsesClientAsync(
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
        TimeSpan latency)
    {
        _telemetrySink.Record(OpenAiProviderTelemetry.Create(
            Capabilities.ProviderId,
            "text-planning",
            _options.TextPlanningModel,
            endpoint,
            response.StatusCode,
            response.IsSuccessStatusCode,
            response.RequestId,
            response.Body,
            providerTraceId,
            latency,
            _rateCard.TextPlanningRequestUsd,
            _rateCard.Name));
    }
}
