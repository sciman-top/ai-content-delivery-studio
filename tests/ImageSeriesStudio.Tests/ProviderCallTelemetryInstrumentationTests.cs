using System.Diagnostics;
using System.Diagnostics.Metrics;
using ImageSeriesStudio.Infrastructure.OpenAI;

namespace ImageSeriesStudio.Tests;

public sealed class ProviderCallTelemetryInstrumentationTests
{
    [Fact]
    public void DiagnosticSink_EmitsActivityWithSafeProviderTags()
    {
        var stoppedActivities = new List<Activity>();
        using var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == DiagnosticProviderCallTelemetrySink.ActivitySourceName,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStopped = stoppedActivities.Add,
        };
        ActivitySource.AddActivityListener(listener);
        var sink = new DiagnosticProviderCallTelemetrySink();

        sink.Record(CreateTelemetry());

        var activity = Assert.Single(stoppedActivities);
        Assert.Equal("provider.call", activity.OperationName);
        Assert.Equal(ActivityKind.Client, activity.Kind);
        Assert.Equal("openai-text", activity.GetTagItem("provider.id"));
        Assert.Equal("text-planning", activity.GetTagItem("provider.operation"));
        Assert.Equal("gpt-5", activity.GetTagItem("provider.model"));
        Assert.Equal(200, activity.GetTagItem("http.response.status_code"));
        Assert.Equal(true, activity.GetTagItem("provider.succeeded"));
        Assert.Equal("req_123", activity.GetTagItem("provider.request_id"));
        Assert.Equal("resp_123", activity.GetTagItem("provider.trace_id"));
        Assert.Equal("api.openai.com", activity.GetTagItem("server.address"));
        Assert.Equal("/v1/responses", activity.GetTagItem("url.path"));
        Assert.Equal(20, activity.GetTagItem("provider.tokens.total"));
        Assert.Null(activity.GetTagItem("promptText"));
        Assert.Null(activity.GetTagItem("apiKey"));
    }

    [Fact]
    public void DiagnosticSink_EmitsMetricsWithoutHighCardinalityRequestTags()
    {
        var measurements = new List<MetricMeasurement>();
        using var listener = new MeterListener();
        listener.InstrumentPublished = (instrument, meterListener) =>
        {
            if (instrument.Meter.Name == DiagnosticProviderCallTelemetrySink.MeterName)
            {
                meterListener.EnableMeasurementEvents(instrument);
            }
        };
        listener.SetMeasurementEventCallback<long>((instrument, measurement, tags, _) =>
            measurements.Add(new MetricMeasurement(instrument.Name, measurement, ToDictionary(tags))));
        listener.SetMeasurementEventCallback<double>((instrument, measurement, tags, _) =>
            measurements.Add(new MetricMeasurement(instrument.Name, measurement, ToDictionary(tags))));
        listener.Start();
        var sink = new DiagnosticProviderCallTelemetrySink();

        sink.Record(CreateTelemetry());

        var call = Assert.Single(measurements, measurement =>
            measurement.InstrumentName == "image_series_studio.provider.calls");
        Assert.Equal(1L, call.Value);
        Assert.Equal("openai-text", call.Tags["provider.id"]);
        Assert.Equal("text-planning", call.Tags["provider.operation"]);
        Assert.Equal("gpt-5", call.Tags["provider.model"]);
        Assert.Equal(200, call.Tags["http.response.status_code"]);
        Assert.False(call.Tags.ContainsKey("provider.request_id"));
        Assert.False(call.Tags.ContainsKey("provider.trace_id"));

        Assert.Contains(measurements, measurement =>
            measurement.InstrumentName == "image_series_studio.provider.tokens"
            && Equals(20L, measurement.Value));
        Assert.Contains(measurements, measurement =>
            measurement.InstrumentName == "image_series_studio.provider.call.duration"
            && Convert.ToDouble(measurement.Value) >= 123d);
        Assert.Contains(measurements, measurement =>
            measurement.InstrumentName == "image_series_studio.provider.cost.estimate"
            && Convert.ToDouble(measurement.Value) == 0.03d);
    }

    private static ProviderCallTelemetry CreateTelemetry()
    {
        return new ProviderCallTelemetry(
            "openai-text",
            "text-planning",
            "gpt-5",
            "https://api.openai.com/v1/responses",
            HttpStatusCode: 200,
            Succeeded: true,
            RequestId: "req_123",
            ProviderTraceId: "resp_123",
            Usage: new ProviderTokenUsage(
                InputTokens: 12,
                OutputTokens: 8,
                TotalTokens: 20,
                CachedInputTokens: 3,
                ReasoningOutputTokens: 2),
            Latency: TimeSpan.FromMilliseconds(123.4),
            EstimatedCostUsd: 0.03m,
            RateCardName: "test-card",
            RecordedAt: DateTimeOffset.Parse("2026-06-04T10:00:00Z"));
    }

    private static Dictionary<string, object?> ToDictionary(ReadOnlySpan<KeyValuePair<string, object?>> tags)
    {
        var values = new Dictionary<string, object?>(StringComparer.Ordinal);
        foreach (var tag in tags)
        {
            values[tag.Key] = tag.Value;
        }

        return values;
    }

    private sealed record MetricMeasurement(
        string InstrumentName,
        object Value,
        IReadOnlyDictionary<string, object?> Tags);
}
