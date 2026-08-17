using System.Diagnostics;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text.Json;
using ContentDeliveryStudio.Core.Providers;
using ContentDeliveryStudio.Core.References;
using ContentDeliveryStudio.Infrastructure.IO;
using SkiaSharp;

namespace ContentDeliveryStudio.Infrastructure.OpenAI;

public sealed class OpenAiImageEditProvider : IImageEditProvider
{
    private const long MaximumInputBytes = 50L * 1024 * 1024;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
    };

    private readonly HttpClient _httpClient;
    private readonly OpenAiProviderOptions _options;
    private readonly IOpenAiSecretStore _secretStore;
    private readonly IProviderCallTelemetrySink _telemetrySink;
    private readonly OpenAiCostRateCard _rateCard;

    public OpenAiImageEditProvider(
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
            "openai-image-edit",
            "OpenAI Image Edit Provider",
            [_options.ImageGenerationModel],
            SupportsTextPlanning: false,
            SupportsImageGeneration: false,
            SupportsVisionReview: false,
            SupportsImageEditing: true,
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
            supportsReferenceImages: true,
            costHints: [new ProviderCostHint(_options.ImageGenerationModel, "provider-rate-card-plus-image-input")],
            supportedReferenceImageRoles: [ReferenceImageRole.Subject],
            supportsMaskEditing: true,
            maxReferenceImageCount: 1,
            maxReferenceImageBytes: MaximumInputBytes);
    }

    public IProviderCapabilities Capabilities { get; }

    public async Task<ImageGenerationResult> EditImageAsync(
        ImageEditRequest request,
        CancellationToken cancellationToken)
    {
        var validatedInputs = await ValidateRequestAsync(request, cancellationToken);
        await OpenAiProviderGuard.EnsureCanCallRealApiAsync(
            _options,
            _secretStore,
            OpenAiProviderOperation.ImageGeneration,
            cancellationToken);
        var credentials = await ProviderRequestAuthentication.ResolveAsync(
            _secretStore,
            _options.ApiKeySecretName,
            _options.AppIdSecretName,
            _options.AppSecretSecretName,
            cancellationToken);
        var endpoint = new Uri(_options.BaseUri, "images/edits");

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, endpoint);
        ProviderRequestAuthentication.Apply(httpRequest, credentials);
        using var content = CreateMultipartContent(request, _options.ImageGenerationModel);
        httpRequest.Content = content;

        var stopwatch = Stopwatch.StartNew();
        using var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
        stopwatch.Stop();
        var rawBody = await response.Content.ReadAsByteArrayAsync(cancellationToken);
        using var document = ParseJsonOrThrow(rawBody);
        var providerTraceId = ExtractTraceId(document.RootElement);
        var telemetry = OpenAiProviderTelemetry.Create(
            Capabilities.ProviderId,
            "image-edit",
            _options.ImageGenerationModel,
            endpoint,
            response,
            document.RootElement,
            providerTraceId,
            stopwatch.Elapsed,
            _rateCard.ImageGenerationRequestUsd,
            _rateCard.Name);
        _telemetrySink.Record(telemetry);
        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException(
                $"OpenAI image edit request failed with status {(int)response.StatusCode} {response.ReasonPhrase}.");
        }

        var imageBytes = GeneratedImageAssetInspector.DecodeBase64(ExtractImageBase64(document.RootElement));
        var generatedAt = DateTimeOffset.UtcNow;
        var outputFormat = NormalizeOutputFormat(request.Settings.OutputFormat);
        var inspectedAsset = GeneratedImageAssetInspector.Inspect(imageBytes, request.Settings, outputFormat);
        Directory.CreateDirectory(request.OutputDirectory);
        var assetPath = Path.GetFullPath(Path.Combine(
            request.OutputDirectory,
            EnsureOutputFileName(request.OutputFileName, outputFormat, request.SourceCandidateImageId)));
        EnsureNonDestructiveOutput(assetPath, request);
        var metadataPath = Path.ChangeExtension(assetPath, ".json");

        await AtomicFileWriter.WriteAllBytesAsync(assetPath, imageBytes, cancellationToken);
        await AtomicFileWriter.WriteAllTextAsync(
            metadataPath,
            JsonSerializer.Serialize(new
            {
                providerId = Capabilities.ProviderId,
                endpointFamily = "images",
                operation = "edit",
                model = _options.ImageGenerationModel,
                providerTraceId,
                request.SourceCandidateImageId,
                sourceSha256 = validatedInputs.SourceSha256,
                maskSha256 = validatedInputs.MaskSha256,
                referenceRoles = request.References.Select(reference => reference.Role.ToString()).ToArray(),
                instructionSha256 = ComputeSha256ForText(request.PromptText),
                requestedSize = BuildSize(request.Settings),
                originalSize = inspectedAsset.Size,
                deliveredSize = inspectedAsset.Size,
                deliveredFormat = inspectedAsset.Format,
                settings = new
                {
                    size = BuildSize(request.Settings),
                    quality = NormalizeQuality(request.Settings.Quality),
                    outputFormat,
                },
                generatedAt,
            }, JsonOptions),
            cancellationToken);

        return new ImageGenerationResult(
            Guid.NewGuid(),
            assetPath,
            metadataPath,
            providerTraceId,
            generatedAt);
    }

    private async Task<ValidatedImageEditInputs> ValidateRequestAsync(
        ImageEditRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (string.IsNullOrWhiteSpace(request.PromptText))
        {
            throw new ArgumentException("Image edit prompt is required.", nameof(request));
        }

        if (!Capabilities.SupportedSizes.Any(size =>
                size.Width == request.Settings.Width && size.Height == request.Settings.Height)
            || !Capabilities.SupportedQualities.Contains(request.Settings.Quality, StringComparer.OrdinalIgnoreCase)
            || !Capabilities.SupportedOutputFormats.Contains(
                NormalizeOutputFormat(request.Settings.OutputFormat),
                StringComparer.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Image edit settings are not supported by this provider profile.");
        }
        if (!File.Exists(request.SourceImagePath))
        {
            throw new FileNotFoundException("Source candidate image was not found.", request.SourceImagePath);
        }

        var source = new FileInfo(request.SourceImagePath);
        if (source.Length > MaximumInputBytes)
        {
            throw new InvalidOperationException("Source candidate exceeds the bounded 50 MB edit limit.");
        }

        if (request.References.Count != 1
            || request.References[0].ReferenceId != request.SourceCandidateImageId
            || request.References[0].Role is not ReferenceImageRole.Subject
            || !Path.GetFullPath(request.References[0].AssetPath)
                .Equals(Path.GetFullPath(request.SourceImagePath), StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("This provider slice supports exactly one Subject source-candidate reference.");
        }

        var actualSourceHash = await ComputeSha256Async(request.SourceImagePath, cancellationToken);
        if (!actualSourceHash.Equals(request.References[0].Sha256, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Source candidate hash does not match the edit reference record.");
        }

        var outputFormat = NormalizeOutputFormat(request.Settings.OutputFormat);
        var outputPath = Path.GetFullPath(Path.Combine(
            request.OutputDirectory,
            EnsureOutputFileName(request.OutputFileName, outputFormat, request.SourceCandidateImageId)));
        EnsureNonDestructiveOutput(outputPath, request);

        if (string.IsNullOrWhiteSpace(request.MaskImagePath))
        {
            return new ValidatedImageEditInputs(actualSourceHash, MaskSha256: null);
        }

        if (!File.Exists(request.MaskImagePath))
        {
            throw new FileNotFoundException("Mask image was not found.", request.MaskImagePath);
        }

        var mask = new FileInfo(request.MaskImagePath);
        if (mask.Length > MaximumInputBytes
            || !source.Extension.Equals(mask.Extension, StringComparison.OrdinalIgnoreCase)
            || !mask.Extension.Equals(".png", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Mask edits require bounded PNG source and mask files of the same format.");
        }

        if (ReadImageDimensions(request.SourceImagePath) != ReadImageDimensions(request.MaskImagePath))
        {
            throw new InvalidOperationException("Mask edits require source and mask images with identical dimensions.");
        }

        var maskSha256 = await ComputeSha256Async(request.MaskImagePath, cancellationToken);
        return new ValidatedImageEditInputs(actualSourceHash, maskSha256);
    }

    private static MultipartFormDataContent CreateMultipartContent(
        ImageEditRequest request,
        string model)
    {
        var content = new MultipartFormDataContent();
        try
        {
            content.Add(new StringContent(model), "model");
            content.Add(new StringContent(request.PromptText), "prompt");
            content.Add(new StringContent("1"), "n");
            content.Add(new StringContent(BuildSize(request.Settings)), "size");
            content.Add(new StringContent(NormalizeQuality(request.Settings.Quality)), "quality");
            content.Add(new StringContent(NormalizeOutputFormat(request.Settings.OutputFormat)), "output_format");
            content.Add(CreateFileContent(request.SourceImagePath), "image[]", Path.GetFileName(request.SourceImagePath));

            if (!string.IsNullOrWhiteSpace(request.MaskImagePath))
            {
                content.Add(CreateFileContent(request.MaskImagePath), "mask", Path.GetFileName(request.MaskImagePath));
            }

            return content;
        }
        catch
        {
            content.Dispose();
            throw;
        }
    }

    private static StreamContent CreateFileContent(string path)
    {
        var stream = new FileStream(
            path,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            bufferSize: 81920,
            FileOptions.Asynchronous | FileOptions.SequentialScan);
        var content = new StreamContent(stream);
        content.Headers.ContentType = new MediaTypeHeaderValue(GetMediaType(path));
        return content;
    }

    private static void EnsureNonDestructiveOutput(string assetPath, ImageEditRequest request)
    {
        if (assetPath.Equals(Path.GetFullPath(request.SourceImagePath), StringComparison.OrdinalIgnoreCase)
            || (!string.IsNullOrWhiteSpace(request.MaskImagePath)
                && assetPath.Equals(Path.GetFullPath(request.MaskImagePath), StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException("Image edit output cannot overwrite a source or mask asset.");
        }
    }

    private static string ExtractImageBase64(JsonElement root)
    {
        if (root.TryGetProperty("data", out var data) && data.ValueKind is JsonValueKind.Array)
        {
            foreach (var item in data.EnumerateArray())
            {
                if (item.TryGetProperty("b64_json", out var value) && value.ValueKind is JsonValueKind.String)
                {
                    return value.GetString()!;
                }
            }
        }

        throw new InvalidOperationException("OpenAI image edit response did not include base64 image data.");
    }

    private static string ExtractTraceId(JsonElement root)
    {
        return root.TryGetProperty("id", out var id) && id.ValueKind is JsonValueKind.String
            ? id.GetString()!
            : "openai-image-edit";
    }

    private static JsonDocument ParseJsonOrThrow(byte[] value)
    {
        try
        {
            return JsonDocument.Parse(value);
        }
        catch (JsonException exception)
        {
            throw new InvalidOperationException("OpenAI image edit response contained invalid JSON.", exception);
        }
    }

    private static string BuildSize(ContentDeliveryStudio.Core.Projects.GenerationSettings settings) =>
        settings.Width > 0 && settings.Height > 0 ? $"{settings.Width}x{settings.Height}" : "auto";

    private static string NormalizeQuality(string value) => value.Trim().ToLowerInvariant() switch
    {
        "low" => "low",
        "medium" => "medium",
        "high" or "hd" => "high",
        _ => "auto",
    };

    private static string NormalizeOutputFormat(string value) => value.Trim().ToLowerInvariant() switch
    {
        "jpeg" or "jpg" => "jpeg",
        "webp" => "webp",
        _ => "png",
    };

    private static string EnsureOutputFileName(string value, string format, Guid sourceCandidateId)
    {
        var extension = format == "jpeg" ? ".jpg" : $".{format}";
        var fileName = string.IsNullOrWhiteSpace(value)
            ? $"{sourceCandidateId:N}-edited{extension}"
            : Path.GetFileName(value);
        return Path.GetExtension(fileName).Equals(extension, StringComparison.OrdinalIgnoreCase)
            ? fileName
            : Path.ChangeExtension(fileName, extension);
    }

    private static string GetMediaType(string path) => Path.GetExtension(path).ToLowerInvariant() switch
    {
        ".jpg" or ".jpeg" => "image/jpeg",
        ".webp" => "image/webp",
        _ => "image/png",
    };

    private static (int Width, int Height) ReadImageDimensions(string path)
    {
        using var stream = File.OpenRead(path);
        using var codec = SKCodec.Create(stream)
            ?? throw new InvalidOperationException($"Image dimensions could not be read for '{Path.GetFileName(path)}'.");
        return (codec.Info.Width, codec.Info.Height);
    }

    internal static string ComputeSha256(string value)
    {
        using var stream = File.OpenRead(value);
        return Convert.ToHexStringLower(SHA256.HashData(stream));
    }

    internal static string ComputeSha256ForText(string value) =>
        Convert.ToHexStringLower(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(value)));

    private static async Task<string> ComputeSha256Async(string value, CancellationToken cancellationToken)
    {
        await using var stream = new FileStream(
            value,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            bufferSize: 81920,
            FileOptions.Asynchronous | FileOptions.SequentialScan);
        return Convert.ToHexStringLower(await SHA256.HashDataAsync(stream, cancellationToken));
    }

    private sealed record ValidatedImageEditInputs(string SourceSha256, string? MaskSha256);
}
