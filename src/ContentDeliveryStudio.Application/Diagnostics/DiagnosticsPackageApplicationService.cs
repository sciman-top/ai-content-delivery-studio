using ContentDeliveryStudio.Application.Projects;

namespace ContentDeliveryStudio.Application.Diagnostics;

public sealed class DiagnosticsPackageApplicationService
{
    private readonly IProjectRepository _projectRepository;
    private readonly IDiagnosticsPackageWriter _writer;
    private readonly TimeProvider _timeProvider;

    public DiagnosticsPackageApplicationService(
        IProjectRepository projectRepository,
        IDiagnosticsPackageWriter writer,
        TimeProvider? timeProvider = null)
    {
        _projectRepository = projectRepository;
        _writer = writer;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    public async Task<DiagnosticsExportResult> ExportAsync(
        DiagnosticsPackageApplicationRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrWhiteSpace(request.OutputParentDirectory))
        {
            throw new ArgumentException("Output parent directory cannot be empty.", nameof(request));
        }

        var projectSummaries = await _projectRepository.ListAsync(cancellationToken);
        var projects = new List<DiagnosticsProjectSnapshot>(projectSummaries.Count);
        foreach (var summary in projectSummaries.OrderBy(item => item.Id))
        {
            var project = await _projectRepository.LoadAsync(summary.Id, cancellationToken);
            if (project is not null)
            {
                projects.Add(DiagnosticsProjectSnapshot.FromProject(project));
            }
        }

        var createdAt = _timeProvider.GetUtcNow();
        var directoryName = $"content-delivery-studio-diagnostics-{createdAt:yyyyMMdd-HHmmss-fff}-{Guid.NewGuid():N}";
        var outputDirectory = Path.Combine(Path.GetFullPath(request.OutputParentDirectory), directoryName);

        return await _writer.WriteAsync(
            new DiagnosticsExportRequest(
                outputDirectory,
                request.Application with { CreatedAt = createdAt },
                request.Machine,
                projects,
                request.Providers,
                request.Secrets),
            cancellationToken);
    }
}

public sealed record DiagnosticsPackageApplicationRequest(
    string OutputParentDirectory,
    DiagnosticsApplicationSnapshot Application,
    DiagnosticsMachineSnapshot Machine,
    IReadOnlyList<DiagnosticsProviderSnapshot> Providers,
    IReadOnlyList<DiagnosticsSecretSnapshot> Secrets);
