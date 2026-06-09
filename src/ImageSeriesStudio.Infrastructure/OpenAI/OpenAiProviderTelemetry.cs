using System.Diagnostics;
using System.Diagnostics.Metrics;
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

public sealed class DiagnosticProviderCallTelemetrySink : IProviderCallTelemetrySink
{
    public const string ActivitySourceName = "ImageSeriesStudio.OpenAI.ProviderCalls";
    public const string MeterName = "ImageSeriesStudio.OpenAI.ProviderCalls";
    public const string InstrumentationVersion = "1.0.0";

    public static ActivitySource ActivitySource { get; } = new(ActivitySourceName, InstrumentationVersion);

    public static Meter Meter { get; } = new(MeterName, InstrumentationVersion);

    private static readonly Counter<long> CallCounter = Meter.CreateCounter<long>(
        "image_series_studio.provider.calls",
        unit: "call",
        description: "Number of provider calls recorded by Image Series Studio.");

    private static readonly Histogram<double> LatencyHistogram = Meter.CreateHistogram<double>(
        "image_series_studio.provider.call.duration",
        unit: "ms",
        description: "Provider call latency in milliseconds.");

    private static readonly Counter<long> TokenCounter = Meter.CreateCounter<long>(
        "image_series_studio.provider.tokens",
        unit: "token",
        description: "Provider token usage reported by the upstream response.");

    private static readonly Histogram<double> CostEstimateHistogram = Meter.CreateHistogram<double>(
        "image_series_studio.provider.cost.estimate",
        unit: "USD",
        description: "Configured provider cost estimate for successful calls.");

    public void Record(ProviderCallTelemetry telemetry)
    {
        ArgumentNullException.ThrowIfNull(telemetry);

        using var activity = ActivitySource.StartActivity("provider.call", ActivityKind.Client);
        if (activity is not null)
        {
            PopulateActivityTags(activity, telemetry);
        }

        var metricTags = CreateMetricTags(telemetry);
        CallCounter.Add(1, metricTags);
        LatencyHistogram.Record(telemetry.Latency.TotalMilliseconds, metricTags);

        if (telemetry.Usage?.TotalTokens is > 0)
        {
            TokenCounter.Add(telemetry.Usage.TotalTokens.Value, metricTags);
        }

        if (telemetry.EstimatedCostUsd > 0m)
        {
            CostEstimateHistogram.Record((double)telemetry.EstimatedCostUsd, metricTags);
        }
    }

    private static void PopulateActivityTags(Activity activity, ProviderCallTelemetry telemetry)
    {
        activity.SetTag("provider.id", telemetry.ProviderId);
        activity.SetTag("provider.operation", telemetry.Operation);
        activity.SetTag("provider.model", telemetry.Model);
        activity.SetTag("http.response.status_code", telemetry.HttpStatusCode);
        activity.SetTag("provider.succeeded", telemetry.Succeeded);
        activity.SetTag("provider.latency_ms", telemetry.Latency.TotalMilliseconds);
        activity.SetTag("provider.rate_card", telemetry.RateCardName);
        activity.SetTag("provider.estimated_cost_usd", (double)telemetry.EstimatedCostUsd);
        activity.SetTag("otel.status_code", telemetry.Succeeded ? "OK" : "ERROR");

        if (Uri.TryCreate(telemetry.Endpoint, UriKind.Absolute, out var endpoint))
        {
            activity.SetTag("server.address", endpoint.Host);
            activity.SetTag("url.path", endpoint.AbsolutePath);
        }

        if (!string.IsNullOrWhiteSpace(telemetry.RequestId))
        {
            activity.SetTag("provider.request_id", telemetry.RequestId);
        }

        if (!string.IsNullOrWhiteSpace(telemetry.ProviderTraceId))
        {
            activity.SetTag("provider.trace_id", telemetry.ProviderTraceId);
        }

        if (telemetry.Usage is { } usage)
        {
            activity.SetTag("provider.tokens.input", usage.InputTokens);
            activity.SetTag("provider.tokens.output", usage.OutputTokens);
            activity.SetTag("provider.tokens.total", usage.TotalTokens);
            activity.SetTag("provider.tokens.cached_input", usage.CachedInputTokens);
            activity.SetTag("provider.tokens.reasoning_output", usage.ReasoningOutputTokens);
        }
    }

    private static TagList CreateMetricTags(ProviderCallTelemetry telemetry)
    {
        var tags = new TagList
        {
            { "provider.id", telemetry.ProviderId },
            { "provider.operation", telemetry.Operation },
            { "provider.model", telemetry.Model },
            { "http.response.status_code", telemetry.HttpStatusCode },
            { "provider.succeeded", telemetry.Succeeded },
            { "provider.rate_card", telemetry.RateCardName },
        };

        if (Uri.TryCreate(telemetry.Endpoint, UriKind.Absolute, out var endpoint))
        {
            tags.Add("server.address", endpoint.Host);
            tags.Add("url.path", endpoint.AbsolutePath);
        }

        return tags;
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
    internal static readonly string[] RequestIdHeaderNames =
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

        return Create(
            providerId,
            operation,
            model,
            endpoint,
            (int)response.StatusCode,
            response.IsSuccessStatusCode,
            ExtractRequestId(response),
            body,
            providerTraceId,
            latency,
            estimatedCostUsd,
            rateCardName);
    }

    public static ProviderCallTelemetry Create(
        string providerId,
        string operation,
        string model,
        Uri endpoint,
        int statusCode,
        bool succeeded,
        string? requestId,
        JsonElement? body,
        string? providerTraceId,
        TimeSpan latency,
        decimal estimatedCostUsd,
        string rateCardName)
    {
        return new ProviderCallTelemetry(
            providerId,
            operation,
            model,
            endpoint.ToString(),
            statusCode,
            succeeded,
            requestId,
            providerTraceId,
            body is { } root ? ExtractUsage(root) : null,
            latency,
            succeeded ? estimatedCostUsd : 0m,
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
