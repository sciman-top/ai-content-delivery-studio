using System.Text.Json;

namespace ImageSeriesStudio.Infrastructure.OpenAI;

public interface IProviderCallTelemetrySink
{
    void Record(ProviderCallTelemetry telemetry);
}

public sealed class NullProviderCallTelemetrySink : IProviderCallTelemetrySink
{
    public static NullProviderCallTelemetrySink Instance { get; } = new();

    private NullProviderCallTelemetrySink()
    {
    }

    public void Record(ProviderCallTelemetry telemetry)
    {
    }
}

public sealed record ProviderCallTelemetry(
    string ProviderId,
    string Operation,
    string Model,
    string Endpoint,
    int HttpStatusCode,
    bool Succeeded,
    string? RequestId,
    string? ProviderTraceId,
    ProviderTokenUsage? Usage,
    TimeSpan Latency,
    decimal EstimatedCostUsd,
    string RateCardName,
    DateTimeOffset RecordedAt);

public sealed record ProviderTokenUsage(
    int? InputTokens,
    int? OutputTokens,
    int? TotalTokens,
    int? CachedInputTokens,
    int? ReasoningOutputTokens);

internal static class OpenAiProviderTelemetry
{
    private static readonly string[] RequestIdHeaderNames =
    [
        "x-request-id",
        "openai-request-id",
        "request-id",
    ];

    public static ProviderCallTelemetry Create(
        string providerId,
        string operation,
        string model,
        Uri endpoint,
        HttpResponseMessage response,
        JsonElement? body,
        string? providerTraceId,
        TimeSpan latency,
        decimal estimatedCostUsd,
        string rateCardName)
    {
        ArgumentNullException.ThrowIfNull(response);

        return new ProviderCallTelemetry(
            providerId,
            operation,
            model,
            endpoint.ToString(),
            (int)response.StatusCode,
            response.IsSuccessStatusCode,
            ExtractRequestId(response),
            providerTraceId,
            body is { } root ? ExtractUsage(root) : null,
            latency,
            response.IsSuccessStatusCode ? estimatedCostUsd : 0m,
            rateCardName,
            DateTimeOffset.UtcNow);
    }

    public static ProviderTokenUsage? ExtractUsage(JsonElement root)
    {
        if (!root.TryGetProperty("usage", out var usageElement)
            || usageElement.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
        {
            return null;
        }

        var cachedInputTokens = ReadNestedInt32(
            usageElement,
            "input_tokens_details",
            "cached_tokens");
        var reasoningOutputTokens = ReadNestedInt32(
            usageElement,
            "output_tokens_details",
            "reasoning_tokens");

        return new ProviderTokenUsage(
            ReadInt32(usageElement, "input_tokens"),
            ReadInt32(usageElement, "output_tokens"),
            ReadInt32(usageElement, "total_tokens"),
            cachedInputTokens,
            reasoningOutputTokens);
    }

    private static string? ExtractRequestId(HttpResponseMessage response)
    {
        foreach (var headerName in RequestIdHeaderNames)
        {
            if (response.Headers.TryGetValues(headerName, out var headerValues))
            {
                return headerValues.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));
            }

            if (response.Content.Headers.TryGetValues(headerName, out var contentHeaderValues))
            {
                return contentHeaderValues.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));
            }
        }

        return null;
    }

    private static int? ReadNestedInt32(JsonElement root, string objectName, string propertyName)
    {
        return root.TryGetProperty(objectName, out var nested)
            ? ReadInt32(nested, propertyName)
            : null;
    }

    private static int? ReadInt32(JsonElement root, string propertyName)
    {
        return root.TryGetProperty(propertyName, out var property)
            && property.ValueKind is JsonValueKind.Number
            && property.TryGetInt32(out var value)
                ? value
                : null;
    }
}
