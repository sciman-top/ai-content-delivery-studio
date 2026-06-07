using System.ClientModel;
using System.ClientModel.Primitives;
using System.Text.Json;
using ImageSeriesStudio.Core.Providers;

namespace ImageSeriesStudio.Infrastructure.OpenAI;

public interface IOpenAiSdkImageBackend
{
    Task<OpenAiSdkImageBackendResponse> SendAsync(
        OpenAiProviderOptions options,
        string apiKey,
        BinaryContent payload,
        CancellationToken cancellationToken);
}

public sealed record OpenAiSdkImageBackendResponse(
    int StatusCode,
    string? RequestId,
    BinaryData RawBody);

public sealed class OpenAiOfficialSdkImageBackend : IOpenAiSdkImageBackend
{
    private readonly IOpenAiOfficialSdkFactory _factory;

    public OpenAiOfficialSdkImageBackend(IOpenAiOfficialSdkFactory factory)
    {
        _factory = factory;
    }

    public async Task<OpenAiSdkImageBackendResponse> SendAsync(
        OpenAiProviderOptions options,
        string apiKey,
        BinaryContent payload,
        CancellationToken cancellationToken)
    {
        var client = _factory.CreateImageClient(apiKey, options);
        var request = new RequestOptions
        {
            BufferResponse = true,
            CancellationToken = cancellationToken,
        };
        var response = await client.GenerateImagesAsync(payload, request);
        var rawResponse = response.GetRawResponse();
        var responseBody = rawResponse.Content ?? await rawResponse.BufferContentAsync(cancellationToken);

        return new OpenAiSdkImageBackendResponse(
            rawResponse.Status,
            rawResponse.Headers.TryGetValue("x-request-id", out var requestId) ? requestId : null,
            responseBody);
    }
}

public interface IOpenAiSdkImageTransport
{
    Task<OpenAiSdkImageTransportResult> GenerateAsync(
        OpenAiProviderOptions options,
        string apiKey,
        ImageGenerationRequest request,
        CancellationToken cancellationToken);
}

public sealed class OpenAiSdkImageTransport : IOpenAiSdkImageTransport
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly IOpenAiSdkImageBackend _backend;

    public OpenAiSdkImageTransport(IOpenAiSdkImageBackend backend)
    {
        _backend = backend;
    }

    public async Task<OpenAiSdkImageTransportResult> GenerateAsync(
        OpenAiProviderOptions options,
        string apiKey,
        ImageGenerationRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentException.ThrowIfNullOrWhiteSpace(apiKey);
        ArgumentNullException.ThrowIfNull(request);

        var response = await _backend.SendAsync(
            options,
            apiKey,
            BinaryContent.CreateJson(CreatePayload(options, request), JsonOptions),
            cancellationToken);

        using var document = JsonDocument.Parse(response.RawBody);
        var imageBase64 = ExtractImageBase64(document.RootElement);
        return new OpenAiSdkImageTransportResult(
            Convert.FromBase64String(imageBase64),
            ExtractTraceId(document.RootElement),
            response.StatusCode,
            response.RequestId,
            response.RawBody);
    }

    private static Dictionary<string, object?> CreatePayload(
        OpenAiProviderOptions options,
        ImageGenerationRequest request)
    {
        var routing = OpenAiProviderRoutingPolicy.ForImageGeneration();

        return new Dictionary<string, object?>
        {
            ["model"] = options.ImageGenerationModel,
            ["prompt"] = request.PromptText,
            ["n"] = 1,
            ["store"] = routing.Store,
            ["size"] = request.Settings.Width > 0 && request.Settings.Height > 0
                ? $"{request.Settings.Width}x{request.Settings.Height}"
                : "auto",
            ["quality"] = request.Settings.Quality.Trim().ToLowerInvariant() switch
            {
                "low" => "low",
                "medium" => "medium",
                "high" or "hd" => "high",
                _ => "auto",
            },
            ["output_format"] = request.Settings.OutputFormat.Trim().ToLowerInvariant() switch
            {
                "jpeg" or "jpg" => "jpeg",
                "webp" => "webp",
                _ => "png",
            },
        };
    }

    private static string ExtractImageBase64(JsonElement root)
    {
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

        throw new InvalidOperationException("OpenAI SDK image transport response did not include base64 image data.");
    }

    private static string ExtractTraceId(JsonElement root)
    {
        return root.TryGetProperty("id", out var idElement) && idElement.ValueKind is JsonValueKind.String
            ? idElement.GetString()!
            : "openai-image-generate";
    }
}

public sealed record OpenAiSdkImageTransportResult(
    byte[] ImageBytes,
    string ProviderTraceId,
    int StatusCode,
    string? RequestId,
    BinaryData RawBody);
