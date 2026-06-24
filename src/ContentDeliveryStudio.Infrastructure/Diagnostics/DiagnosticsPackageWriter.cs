using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using ContentDeliveryStudio.Application.Diagnostics;
using ContentDeliveryStudio.Infrastructure.IO;

namespace ContentDeliveryStudio.Infrastructure.Diagnostics;

public sealed class DiagnosticsPackageWriter : IDiagnosticsPackageWriter
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
    };

    public async Task<DiagnosticsExportResult> WriteAsync(
        DiagnosticsExportRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.OutputDirectory))
        {
            throw new ArgumentException("Output directory cannot be empty.", nameof(request));
        }

        Directory.CreateDirectory(request.OutputDirectory);

        var package = new DiagnosticsPackage(
            request.Application,
            request.Machine,
            request.Projects,
            request.Providers,
            request.Secrets,
            request.RepairPatches ?? [],
            request.OperatorRuns ?? [],
            request.OpenAiLaunchPreflight);

        var jsonPath = Path.Combine(request.OutputDirectory, "diagnostics.json");
        var markdownPath = Path.Combine(request.OutputDirectory, "diagnostics.md");

        await AtomicFileWriter.WriteAllTextAsync(
            jsonPath,
            JsonSerializer.Serialize(package, JsonOptions),
            cancellationToken);

        await AtomicFileWriter.WriteAllTextAsync(
            markdownPath,
            WriteMarkdown(package),
            cancellationToken);

        return new DiagnosticsExportResult(request.OutputDirectory, jsonPath, markdownPath);
    }

    public static DiagnosticsMachineSnapshot CaptureMachineSnapshot()
    {
        return new DiagnosticsMachineSnapshot(
            RuntimeInformation.OSDescription,
            RuntimeInformation.FrameworkDescription,
            RuntimeInformation.ProcessArchitecture.ToString(),
            CultureInfo.CurrentCulture.Name,
            CultureInfo.CurrentUICulture.Name);
    }

    private static string WriteMarkdown(DiagnosticsPackage package)
    {
        var builder = new StringBuilder();
        builder.AppendLine($"# Diagnostics: {package.Application.AppName}");
        builder.AppendLine();
        builder.AppendLine($"- Version: {package.Application.AppVersion}");
        builder.AppendLine($"- Build: {package.Application.BuildConfiguration}");
        builder.AppendLine($"- CreatedAt: {package.Application.CreatedAt:O}");
        builder.AppendLine($"- OS: {package.Machine.OsDescription}");
        builder.AppendLine($"- Framework: {package.Machine.FrameworkDescription}");
        builder.AppendLine();
        builder.AppendLine("## Projects");

        foreach (var project in package.Projects)
        {
            builder.AppendLine(
                $"- {project.Name}: series={project.SeriesCount}, items={project.ItemCount}, prompts={project.PromptVersionCount}, candidates={project.CandidateImageCount}");
        }

        builder.AppendLine();
        builder.AppendLine("## Providers");

        foreach (var provider in package.Providers)
        {
            builder.AppendLine(
                $"- {provider.DisplayName}: kind={provider.Kind}, realApiEnabled={provider.RealApiEnabled}, dryRunOnly={provider.DryRunOnly}");
        }

        builder.AppendLine();
        builder.AppendLine("## Secrets");

        foreach (var secret in package.Secrets)
        {
            builder.AppendLine($"- {secret.Name}: configured={secret.IsConfigured}");
        }

        if (package.OpenAiLaunchPreflight is not null)
        {
            builder.AppendLine();
            builder.AppendLine("## OpenAI Launch Preflight");
            builder.AppendLine(
                $"- canRunLiveV1SampleSeries={package.OpenAiLaunchPreflight.CanRunLiveV1SampleSeries}, configurationErrors={package.OpenAiLaunchPreflight.ConfigurationErrorCount}");
            AppendPreflightOperation(builder, package.OpenAiLaunchPreflight.TextPlanning);
            AppendPreflightOperation(builder, package.OpenAiLaunchPreflight.VisionReview);
            AppendPreflightOperation(builder, package.OpenAiLaunchPreflight.ImageGeneration);
            AppendSmoke(builder, "Text smoke", package.OpenAiLaunchPreflight.TextSmoke);
            AppendSmoke(builder, "Image smoke", package.OpenAiLaunchPreflight.ImageSmoke);

            foreach (var reason in package.OpenAiLaunchPreflight.BlockingReasons)
            {
                builder.AppendLine($"- blockingReason: {reason}");
            }
        }

        builder.AppendLine();
        builder.AppendLine("## Repair Patches");

        foreach (var patch in package.RepairPatches)
        {
            builder.AppendLine(
                $"- {patch.PatchId}: project={patch.ProjectId}, candidate={patch.CandidateImageId}, items={patch.Items.Count}");
            foreach (var item in patch.Items)
            {
                builder.AppendLine(
                    $"  - {item.TargetLayer}: severity={item.Severity}, humanApproval={item.RequiresHumanApproval}, changes={string.Join("; ", item.ProposedChanges)}");
            }
        }

        builder.AppendLine();
        builder.AppendLine("## Operator Runs");

        foreach (var run in package.OperatorRuns)
        {
            builder.AppendLine(
                $"- {run.ToolAdapterId}: actionStatus={run.ActionStatus}, runStatus={run.RunStatus}, dryRun={run.DryRun}, summary={run.OutputSummary ?? string.Empty}");
        }

        return builder.ToString();
    }

    private static void AppendPreflightOperation(
        StringBuilder builder,
        OpenAiOperationDiagnosticsSnapshot operation)
    {
        builder.AppendLine(
            $"- operation={operation.Operation}, provider={operation.ProviderPrefix}, canCallRealApi={operation.CanCallRealApi}");

        foreach (var error in operation.Errors)
        {
            builder.AppendLine($"  - {error}");
        }
    }

    private static void AppendSmoke(
        StringBuilder builder,
        string title,
        OpenAiSmokeDiagnosticsSnapshot smoke)
    {
        builder.AppendLine(
            $"- {title}: canRunRealApiSmoke={smoke.CanRunRealApiSmoke}, isDryRun={smoke.IsDryRun}, optIn={smoke.OptInEnvironmentVariable}");

        foreach (var reason in smoke.Reasons)
        {
            builder.AppendLine($"  - {reason}");
        }
    }
}

internal sealed record DiagnosticsPackage(
    DiagnosticsApplicationSnapshot Application,
    DiagnosticsMachineSnapshot Machine,
    IReadOnlyList<DiagnosticsProjectSnapshot> Projects,
    IReadOnlyList<DiagnosticsProviderSnapshot> Providers,
    IReadOnlyList<DiagnosticsSecretSnapshot> Secrets,
    IReadOnlyList<RepairPatchDiagnosticsSnapshot> RepairPatches,
    IReadOnlyList<OperatorAuditSnapshot> OperatorRuns,
    OpenAiLaunchPreflightDiagnosticsSnapshot? OpenAiLaunchPreflight);
