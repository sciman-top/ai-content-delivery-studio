using ImageSeriesStudio.Core.Projects;

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
    IReadOnlyList<DiagnosticsSecretSnapshot> Secrets);

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

public sealed record DiagnosticsExportResult(
    string PackageDirectory,
    string JsonPath,
    string MarkdownPath);
