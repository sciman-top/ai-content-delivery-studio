using ImageSeriesStudio.Core.Projects;
using ImageSeriesStudio.Core.Operators;

namespace ImageSeriesStudio.Application.Diagnostics;

public interface IDiagnosticsPackageWriter
{
    Task<DiagnosticsExportResult> WriteAsync(DiagnosticsExportRequest request, CancellationToken cancellationToken);
}

public sealed record DiagnosticsExportRequest(
    string OutputDirectory,
    DiagnosticsApplicationSnapshot Application,
    DiagnosticsMachineSnapshot Machine,
    IReadOnlyList<DiagnosticsProjectSnapshot> Projects,
    IReadOnlyList<DiagnosticsProviderSnapshot> Providers,
    IReadOnlyList<DiagnosticsSecretSnapshot> Secrets,
    IReadOnlyList<RepairPatchDiagnosticsSnapshot>? RepairPatches = null,
    IReadOnlyList<OperatorAuditSnapshot>? OperatorRuns = null);

public sealed record DiagnosticsApplicationSnapshot(
    string AppName,
    string AppVersion,
    string BuildConfiguration,
    DateTimeOffset CreatedAt);

public sealed record DiagnosticsMachineSnapshot(
    string OsDescription,
    string FrameworkDescription,
    string ProcessArchitecture,
    string CurrentCulture,
    string CurrentUICulture);

public sealed record DiagnosticsProjectSnapshot(
    Guid ProjectId,
    string Name,
    int SeriesCount,
    int ItemCount,
    int PromptVersionCount,
    int CandidateImageCount,
    int ProviderProfileCount,
    DateTimeOffset UpdatedAt)
{
    public static DiagnosticsProjectSnapshot FromProject(ImageProject project)
    {
        var items = project.Series.SelectMany(series => series.Items).ToArray();
        var updatedAt = new[]
            {
                project.UpdatedAt,
            }
            .Concat(project.Series.Select(series => series.UpdatedAt))
            .Concat(items.Select(item => item.UpdatedAt))
            .Concat(items.SelectMany(item => item.PromptVersions).Select(prompt => prompt.CreatedAt))
            .Concat(items.SelectMany(item => item.CandidateImages).Select(candidate => candidate.CreatedAt))
            .Max();

        return new DiagnosticsProjectSnapshot(
            project.Id,
            project.Name,
            project.Series.Count,
            items.Length,
            items.Sum(item => item.PromptVersions.Count),
            items.Sum(item => item.CandidateImages.Count),
            project.ProviderProfiles.Count,
            updatedAt);
    }
}

public sealed record DiagnosticsProviderSnapshot(
    string ProviderId,
    string DisplayName,
    string Kind,
    IReadOnlyList<string> ModelIds,
    IReadOnlyList<string> Capabilities,
    bool RealApiEnabled,
    bool DryRunOnly);

public sealed record DiagnosticsSecretSnapshot(
    string Name,
    bool IsConfigured);

public sealed record RepairPatchDiagnosticsSnapshot(
    Guid PatchId,
    Guid ProjectId,
    Guid RepairPlanId,
    Guid CandidateImageId,
    DateTimeOffset CreatedAt,
    IReadOnlyList<RepairPatchItemDiagnosticsSnapshot> Items)
{
    public static RepairPatchDiagnosticsSnapshot FromPatch(RoutedRepairPatch patch)
    {
        ArgumentNullException.ThrowIfNull(patch);

        return new RepairPatchDiagnosticsSnapshot(
            patch.Id,
            patch.ProjectId,
            patch.RepairPlanId,
            patch.CandidateImageId,
            patch.CreatedAt,
            patch.Items.Select(RepairPatchItemDiagnosticsSnapshot.FromItem).ToArray());
    }
}

public sealed record RepairPatchItemDiagnosticsSnapshot(
    int Order,
    string TargetLayer,
    string Severity,
    IReadOnlyList<string> Evidence,
    IReadOnlyList<string> ProposedChanges,
    bool RequiresHumanApproval)
{
    public static RepairPatchItemDiagnosticsSnapshot FromItem(RoutedRepairPatchItem item)
    {
        ArgumentNullException.ThrowIfNull(item);

        return new RepairPatchItemDiagnosticsSnapshot(
            item.Order,
            item.TargetLayer.ToString(),
            item.Severity.ToString(),
            item.Evidence,
            item.ProposedChanges,
            item.RequiresHumanApproval);
    }
}

public sealed record OperatorAuditSnapshot(
    Guid OperatorActionId,
    Guid OperatorRunId,
    string ToolAdapterId,
    string RiskLevel,
    string ActionStatus,
    string RunStatus,
    bool DryRun,
    DateTimeOffset StartedAt,
    DateTimeOffset? CompletedAt,
    string? OutputSummary,
    string? ErrorMessage,
    string? ApprovedBy,
    DateTimeOffset? ApprovedAt)
{
    public static OperatorAuditSnapshot FromRun(
        OperatorAction action,
        OperatorRun run)
    {
        ArgumentNullException.ThrowIfNull(action);
        ArgumentNullException.ThrowIfNull(run);

        if (action.Id != run.OperatorActionId)
        {
            throw new ArgumentException("Operator run does not belong to the supplied operator action.", nameof(run));
        }

        return new OperatorAuditSnapshot(
            action.Id,
            run.Id,
            run.ToolAdapterId,
            action.RiskLevel.ToString(),
            action.Status.ToString(),
            run.Status.ToString(),
            run.DryRun,
            run.StartedAt,
            run.CompletedAt,
            run.OutputSummary,
            run.ErrorMessage,
            action.ApprovedBy,
            action.ApprovedAt);
    }
}

public sealed record DiagnosticsExportResult(
    string PackageDirectory,
    string JsonPath,
    string MarkdownPath);
