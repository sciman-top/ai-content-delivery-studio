using ImageSeriesStudio.Infrastructure.OpenAI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

namespace ImageSeriesStudio.App.Telemetry;

internal static class AppOpenTelemetry
{
    public const string AspireDashboardProfileName = "AspireDashboard";
    public const string DefaultOtlpEndpoint = "http://localhost:4317";
    public const string DefaultServiceName = "ImageSeriesStudio.App";
    public const string OtlpEndpointSetting = "OTEL_EXPORTER_OTLP_ENDPOINT";
    public const string ServiceNameSetting = "OTEL_SERVICE_NAME";

    public static void AddImageSeriesStudioOpenTelemetry(this IHostApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        var openTelemetry = builder.Services.AddOpenTelemetry();

        openTelemetry.WithTracing(tracing =>
        {
            tracing.AddSource(DiagnosticProviderCallTelemetrySink.ActivitySourceName);
        });

        openTelemetry.WithMetrics(metrics =>
        {
            metrics.AddMeter(DiagnosticProviderCallTelemetrySink.MeterName);
        });

        if (ShouldUseOtlpExporter(builder.Configuration))
        {
            openTelemetry.UseOtlpExporter();
        }
    }

    public static bool ShouldUseOtlpExporter(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        return !string.IsNullOrWhiteSpace(configuration[OtlpEndpointSetting]);
    }
}
