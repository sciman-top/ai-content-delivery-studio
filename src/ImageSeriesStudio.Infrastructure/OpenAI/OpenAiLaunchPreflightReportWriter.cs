using System.Text;
using System.Text.Json;

namespace ImageSeriesStudio.Infrastructure.OpenAI;

public sealed class OpenAiLaunchPreflightReportWriter
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
    };

    public async Task<OpenAiLaunchPreflightReportResult> WriteAsync(
        string outputDirectory,
        OpenAiLaunchPreflightReport report,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(outputDirectory))
        {
            throw new ArgumentException("Output directory cannot be empty.", nameof(outputDirectory));
        }

        ArgumentNullException.ThrowIfNull(report);

        Directory.CreateDirectory(outputDirectory);

        var jsonPath = Path.Combine(outputDirectory, "openai-launch-preflight.json");
        var markdownPath = Path.Combine(outputDirectory, "openai-launch-preflight.md");

        await File.WriteAllTextAsync(
            jsonPath,
            JsonSerializer.Serialize(report, JsonOptions),
            cancellationToken);

        await File.WriteAllTextAsync(
            markdownPath,
            WriteMarkdown(report),
            cancellationToken);

        return new OpenAiLaunchPreflightReportResult(outputDirectory, jsonPath, markdownPath);
    }

    private static string WriteMarkdown(OpenAiLaunchPreflightReport report)
    {
        var builder = new StringBuilder();
        builder.AppendLine("# OpenAI Launch Preflight");
        builder.AppendLine();
        builder.AppendLine($"- CanRunLiveV1SampleSeries: {report.CanRunLiveV1SampleSeries}");
        builder.AppendLine($"- ConfigurationErrors: {report.ConfigurationErrors.Count}");
        builder.AppendLine();
        builder.AppendLine("## Operations");
        AppendOperation(builder, report.TextPlanning);
        AppendOperation(builder, report.VisionReview);
        AppendOperation(builder, report.ImageGeneration);
        builder.AppendLine();
        builder.AppendLine("## Smoke Gates");
        AppendSmoke(builder, "Text smoke", report.TextSmoke);
        AppendSmoke(builder, "Image smoke", report.ImageSmoke);
        builder.AppendLine();
        builder.AppendLine("## Blocking Reasons");

        foreach (var reason in report.BlockingReasons)
        {
            builder.AppendLine($"- {reason}");
        }

        return builder.ToString();
    }

    private static void AppendOperation(StringBuilder builder, OpenAiOperationPreflight operation)
    {
        builder.AppendLine(
            $"- {operation.Operation}: provider={operation.ProviderPrefix}, canCallRealApi={operation.CanCallRealApi}");
        foreach (var error in operation.Errors)
        {
            builder.AppendLine($"  - {error}");
        }
    }

    private static void AppendSmoke(
        StringBuilder builder,
        string title,
        OpenAiSmokeTestDecision smoke)
    {
        builder.AppendLine(
            $"- {title}: canRunRealApiSmoke={smoke.CanRunRealApiSmoke}, isDryRun={smoke.IsDryRun}, optIn={smoke.OptInEnvironmentVariable}");
        foreach (var reason in smoke.Reasons)
        {
            builder.AppendLine($"  - {reason}");
        }
    }
}

public sealed record OpenAiLaunchPreflightReportResult(
    string OutputDirectory,
    string JsonPath,
    string MarkdownPath);
