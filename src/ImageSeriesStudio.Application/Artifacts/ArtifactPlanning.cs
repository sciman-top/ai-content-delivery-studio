using ImageSeriesStudio.Application.Projects;
using ImageSeriesStudio.Core.Artifacts;
using ImageSeriesStudio.Core.Projects;

namespace ImageSeriesStudio.Application.Artifacts;

public sealed class ArtifactPlanningApplicationService
{
    private readonly IProjectRepository _repository;
    private readonly IArtifactPlanningProvider _artifactPlanningProvider;

    public ArtifactPlanningApplicationService(
        IProjectRepository repository,
        IArtifactPlanningProvider artifactPlanningProvider)
    {
        _repository = repository;
        _artifactPlanningProvider = artifactPlanningProvider;
    }

    public async Task<ArtifactPlanningWorkflowResult> PlanArtifactsAsync(
        Guid projectId,
        ArtifactPlanningRequest request,
        DateTimeOffset timestamp,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (_artifactPlanningProvider.Capabilities.RequiresExplicitApproval)
        {
            throw new InvalidOperationException("Artifact planning provider requires explicit approval.");
        }

        var project = await _repository.LoadAsync(projectId, cancellationToken)
            ?? throw new InvalidOperationException($"Project not found: {projectId}");
        var sourceAssetIds = ResolveSourceAssetIds(project, request.EvidenceAnchorIds);
        var providerResult = await _artifactPlanningProvider.PlanAsync(
            new ArtifactPlanningProviderRequest(project.Id, request, sourceAssetIds, timestamp),
            cancellationToken);

        if (providerResult.Artifacts.Count == 0)
        {
            throw new InvalidOperationException("Artifact planning provider returned no artifacts.");
        }

        var outputArtifactIds = new List<Guid>();
        foreach (var draft in providerResult.Artifacts)
        {
            var artifact = OutputArtifact.Plan(
                project.Id,
                draft.Kind,
                draft.DisplayName,
                draft.RelativePath,
                draft.MimeType,
                draft.Role,
                draft.SourceAssetIds,
                draft.EvidenceAnchorIds,
                draft.Metadata,
                timestamp);
            project.AddOutputArtifact(artifact, timestamp);
            outputArtifactIds.Add(artifact.Id);
        }

        await _repository.SaveAsync(project, cancellationToken);
        return new ArtifactPlanningWorkflowResult(outputArtifactIds, providerResult.ProviderTraceId);
    }

    private static IReadOnlyList<Guid> ResolveSourceAssetIds(
        ImageProject project,
        IReadOnlyList<Guid> evidenceAnchorIds)
    {
        ArgumentNullException.ThrowIfNull(evidenceAnchorIds);

        var sourceAssetIds = new List<Guid>();
        foreach (var evidenceAnchorId in evidenceAnchorIds.Distinct())
        {
            if (evidenceAnchorId == Guid.Empty)
            {
                throw new ArgumentException("Evidence anchor ids cannot contain empty values.", nameof(evidenceAnchorIds));
            }

            var sourceAsset = project.SourceAssets.SingleOrDefault(asset =>
                asset.EvidenceAnchors.Any(anchor => anchor.Id == evidenceAnchorId))
                ?? throw new InvalidOperationException($"Evidence anchor not found: {evidenceAnchorId}");
            sourceAssetIds.Add(sourceAsset.Id);
        }

        return sourceAssetIds.Distinct().ToArray();
    }
}

public interface IArtifactPlanningProvider
{
    ArtifactPlanningProviderCapabilities Capabilities { get; }

    Task<ArtifactPlanningProviderResult> PlanAsync(
        ArtifactPlanningProviderRequest request,
        CancellationToken cancellationToken);
}

public sealed record ArtifactPlanningProviderCapabilities(
    string ProviderId,
    string DisplayName,
    bool RequiresExplicitApproval = false);

public sealed record ArtifactPlanningRequest(
    string BriefTitle,
    string BriefText,
    IReadOnlyList<Guid> EvidenceAnchorIds,
    IReadOnlyList<OutputArtifactKind> RequestedKinds,
    string OutputDirectory = "delivery");

public sealed record ArtifactPlanningProviderRequest(
    Guid ProjectId,
    ArtifactPlanningRequest Request,
    IReadOnlyList<Guid> SourceAssetIds,
    DateTimeOffset Timestamp);

public sealed record ArtifactPlanningProviderResult(
    IReadOnlyList<ArtifactPlanDraft> Artifacts,
    IReadOnlyList<string> Assumptions,
    string ProviderTraceId);

public sealed record ArtifactPlanDraft(
    OutputArtifactKind Kind,
    string DisplayName,
    string RelativePath,
    string MimeType,
    string Role,
    IReadOnlyList<Guid> SourceAssetIds,
    IReadOnlyList<Guid> EvidenceAnchorIds,
    IReadOnlyDictionary<string, string> Metadata);

public sealed record ArtifactPlanningWorkflowResult(
    IReadOnlyList<Guid> OutputArtifactIds,
    string ProviderTraceId);
