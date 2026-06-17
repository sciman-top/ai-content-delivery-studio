using ContentDeliveryStudio.Application.Projects;
using ContentDeliveryStudio.Core.Sources;

namespace ContentDeliveryStudio.Application.Sources;

public sealed class SourceIngestionApplicationService
{
    private readonly IProjectRepository _repository;
    private readonly ISourceIngestionProvider _sourceIngestionProvider;

    public SourceIngestionApplicationService(
        IProjectRepository repository,
        ISourceIngestionProvider sourceIngestionProvider)
    {
        _repository = repository;
        _sourceIngestionProvider = sourceIngestionProvider;
    }

    public async Task<SourceIngestionWorkflowResult> IngestSourceAsync(
        Guid projectId,
        SourceIngestionRequest request,
        DateTimeOffset timestamp,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (_sourceIngestionProvider.Capabilities.RequiresExplicitApproval)
        {
            throw new InvalidOperationException("Source ingestion provider requires explicit approval.");
        }

        var project = await _repository.LoadAsync(projectId, cancellationToken)
            ?? throw new InvalidOperationException($"Project not found: {projectId}");
        var providerResult = await _sourceIngestionProvider.IngestAsync(
            new SourceIngestionProviderRequest(project.Id, request, timestamp),
            cancellationToken);

        if (providerResult.SourceAsset.ProjectId != project.Id)
        {
            throw new InvalidOperationException("Source asset must belong to the target project.");
        }

        project.AddSourceAsset(providerResult.SourceAsset, timestamp);
        await _repository.SaveAsync(project, cancellationToken);

        return new SourceIngestionWorkflowResult(
            providerResult.SourceAsset.Id,
            providerResult.SourceAsset.ExtractedContents.Count,
            providerResult.SourceAsset.EvidenceAnchors.Count,
            providerResult.ProviderTraceId);
    }
}

public interface ISourceIngestionProvider
{
    SourceIngestionProviderCapabilities Capabilities { get; }

    Task<SourceIngestionProviderResult> IngestAsync(
        SourceIngestionProviderRequest request,
        CancellationToken cancellationToken);
}

public sealed record SourceIngestionProviderCapabilities(
    string ProviderId,
    string DisplayName,
    bool RequiresExplicitApproval = false);

public sealed record SourceIngestionRequest(
    SourceAssetKind Kind,
    string DisplayName,
    string SourceText,
    string? OriginalPath = null,
    string? MimeType = null,
    long? SizeBytes = null,
    string? Sha256 = null);

public sealed record SourceIngestionProviderRequest(
    Guid ProjectId,
    SourceIngestionRequest Source,
    DateTimeOffset Timestamp);

public sealed record SourceIngestionProviderResult(
    SourceAsset SourceAsset,
    string ProviderTraceId);

public sealed record SourceIngestionWorkflowResult(
    Guid SourceAssetId,
    int ExtractedContentCount,
    int EvidenceAnchorCount,
    string ProviderTraceId);
