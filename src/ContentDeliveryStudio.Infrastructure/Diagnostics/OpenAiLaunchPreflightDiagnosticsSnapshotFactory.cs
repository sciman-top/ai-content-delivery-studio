using ContentDeliveryStudio.Application.Diagnostics;
using ContentDeliveryStudio.Infrastructure.OpenAI;

namespace ContentDeliveryStudio.Infrastructure.Diagnostics;

public static class OpenAiLaunchPreflightDiagnosticsSnapshotFactory
{
    public static OpenAiLaunchPreflightDiagnosticsSnapshot FromReport(OpenAiLaunchPreflightReport report)
    {
        ArgumentNullException.ThrowIfNull(report);

        return new OpenAiLaunchPreflightDiagnosticsSnapshot(
            report.CanRunLiveV1SampleSeries,
            report.ConfigurationErrors.Count,
            report.BlockingReasons,
            MapOperation(report.TextPlanning),
            MapOperation(report.VisionReview),
            MapOperation(report.ImageGeneration),
            MapSmoke(report.TextSmoke),
            MapSmoke(report.ImageSmoke));
    }

    private static OpenAiOperationDiagnosticsSnapshot MapOperation(OpenAiOperationPreflight operation)
    {
        return new OpenAiOperationDiagnosticsSnapshot(
            operation.Operation.ToString(),
            operation.ProviderPrefix,
            operation.CanCallRealApi,
            operation.Errors);
    }

    private static OpenAiSmokeDiagnosticsSnapshot MapSmoke(OpenAiSmokeTestDecision smoke)
    {
        return new OpenAiSmokeDiagnosticsSnapshot(
            smoke.CanRunRealApiSmoke,
            smoke.IsDryRun,
            smoke.OptInEnvironmentVariable,
            smoke.Reasons);
    }
}
