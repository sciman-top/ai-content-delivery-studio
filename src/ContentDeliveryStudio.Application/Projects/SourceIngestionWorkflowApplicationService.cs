using ContentDeliveryStudio.Application.Sources;

namespace ContentDeliveryStudio.Application.Projects;

public sealed class SourceIngestionWorkflowApplicationService
{
    private readonly SourceIngestionApplicationService? _sourceIngestionApplicationService;

    public SourceIngestionWorkflowApplicationService(SourceIngestionApplicationService? sourceIngestionApplicationService)
    {
        _sourceIngestionApplicationService = sourceIngestionApplicationService;
    }

    public async Task<SourceIngestionWorkflowResult> IngestSourceAsync(
        Guid projectId,
        SourceIngestionRequest request,
        DateTimeOffset timestamp,
        CancellationToken cancellationToken)
    {
        if (_sourceIngestionApplicationService is null)
        {
            throw new InvalidOperationException("Source ingestion provider is not registered.");
        }

        return await _sourceIngestionApplicationService.IngestSourceAsync(
            projectId,
            request,
            timestamp,
            cancellationToken);
    }
}
