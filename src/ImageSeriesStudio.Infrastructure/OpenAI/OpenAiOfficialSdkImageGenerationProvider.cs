using System.ClientModel;
using System.Diagnostics;
using System.Net;
using System.Text.Json;
using ImageSeriesStudio.Core.Providers;

namespace ImageSeriesStudio.Infrastructure.OpenAI;

public sealed class OpenAiOfficialSdkImageGenerationProvider : IImageGenerationProvider
{
    private const int MaxTransientUpstreamRetryCount = 1;
    private static readonly TimeSpan TransientUpstreamRetryDelay = TimeSpan.FromSeconds(2);
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
    };
    private static readonly OpenAiRoutingDecision Routing = OpenAiProviderRoutingPolicy.ForImageGeneration();

    private readonly OpenAiProviderOptions _options;
    private readonly IOpenAiSecretStore _secretStore;
    private readonly IOpenAiSdkImageTransport _transport;
    private readonly IProviderCallTelemetrySink _telemetrySink;
    private readonly OpenAiCostRateCard _rateCard;

    public OpenAiOfficialSdkImageGenerationProvider(
        OpenAiProviderOptions options,
        IOpenAiSecretStore secretStore,
        IOpenAiSdkImageTransport transport,
        IProviderCallTelemetrySink? telemetrySink = null,
        OpenAiCostRateCard? rateCard = null)
    {
        _options = options;
        _secretStore = secretStore;
        _transport = transport;
        _telemetrySink = telemetrySink ?? NullProviderCallTelemetrySink.Instance;
        _rateCard = rateCard ?? OpenAiCostRateCard.Unpriced;
        OpenAiProviderGuard.EnsureAllowsOperation(_options, OpenAiProviderOperation.ImageGeneration);

        Capabilities = new ProviderCapabilities(
            "openai-image-sdk",
            "OpenAI Image Generation Provider (Official SDK)",
            [_options.ImageGenerationModel],
            SupportsTextPlanning: false,
            SupportsImageGeneration: true,
            SupportsVisionReview: false,
            SupportsImageEditing: false,
            SupportsStreaming: false,
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

        var endpoint = new Uri(_options.BaseUri, Routing.RelativePath);
        var stopwatch = Stopwatch.StartNew();

        OpenAiSdkImageTransportResult transportResult;
        try
        {
            transportResult = await GenerateWithTransientRetryAsync(
                apiKey,
                appId,
                appSecret,
                request,
                cancellationToken);
        }
        catch (ClientResultException exception)
        {
            stopwatch.Stop();
            _telemetrySink.Record(
                OpenAiProviderTelemetry.Create(
                    Capabilities.ProviderId,
                    "image-generation",
                    _options.ImageGenerationModel,
                    endpoint,
                    exception.Status,
                    succeeded: false,
                    requestId: null,
                    body: null,
                    providerTraceId: null,
                    stopwatch.Elapsed,
                    _rateCard.ImageGenerationRequestUsd,
                    _rateCard.Name));
            throw new HttpRequestException(
                $"OpenAI official SDK image generation request failed with status {exception.Status}.",
                exception,
                (HttpStatusCode)exception.Status);
        }

        stopwatch.Stop();
        using var document = JsonDocument.Parse(transportResult.RawBody);
        var generatedAt = DateTimeOffset.UtcNow;
        var outputFormat = NormalizeOutputFormat(request.Settings.OutputFormat);
        var telemetry = OpenAiProviderTelemetry.Create(
            Capabilities.ProviderId,
            "image-generation",
            _options.ImageGenerationModel,
            endpoint,
            transportResult.StatusCode,
            succeeded: transportResult.StatusCode is >= 200 and < 300,
            transportResult.RequestId,
            document.RootElement,
            transportResult.ProviderTraceId,
            stopwatch.Elapsed,
            _rateCard.ImageGenerationRequestUsd,
            _rateCard.Name);
        _telemetrySink.Record(telemetry);

        Directory.CreateDirectory(request.OutputDirectory);

        var assetPath = Path.Combine(
            request.OutputDirectory,
            EnsureOutputFileName(request.OutputFileName, outputFormat, request.SeriesItemId, request.PromptVersionId));
        var metadataPath = Path.ChangeExtension(assetPath, ".json");

        await File.WriteAllBytesAsync(assetPath, transportResult.ImageBytes, cancellationToken);
        await File.WriteAllTextAsync(
            metadataPath,
            JsonSerializer.Serialize(
                new
                {
                    providerId = Capabilities.ProviderId,
                    endpointFamily = Routing.EndpointFamily.ToString().ToLowerInvariant(),
                    store = Routing.Store,
                    model = _options.ImageGenerationModel,
                    providerTraceId = transportResult.ProviderTraceId,
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
            transportResult.ProviderTraceId,
            generatedAt);
    }

    private async Task<OpenAiSdkImageTransportResult> GenerateWithTransientRetryAsync(
        string apiKey,
        string? appId,
        string? appSecret,
        ImageGenerationRequest request,
        CancellationToken cancellationToken)
    {
        for (var attempt = 0; ; attempt++)
        {
            try
            {
                return await _transport.GenerateAsync(_options, apiKey, appId, appSecret, request, cancellationToken);
            }
            catch (ClientResultException exception) when (ShouldRetryTransientUpstream(exception) && attempt < MaxTransientUpstreamRetryCount)
            {
                await Task.Delay(TransientUpstreamRetryDelay, cancellationToken);
            }
        }
    }

    private static bool ShouldRetryTransientUpstream(ClientResultException exception)
    {
        return exception.Status == 502
            && exception.Message.Contains("upstream_error", StringComparison.OrdinalIgnoreCase);
    }

    private async Task<string?> GetOptionalSecretAsync(string? secretName, CancellationToken cancellationToken)
    {
        return string.IsNullOrWhiteSpace(secretName)
            ? null
            : await _secretStore.GetSecretAsync(secretName, cancellationToken);
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
}
