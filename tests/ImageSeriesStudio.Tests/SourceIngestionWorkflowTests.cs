using ImageSeriesStudio.Application.Projects;
using ImageSeriesStudio.Application.Sources;
using ImageSeriesStudio.Core.Projects;
using ImageSeriesStudio.Core.Sources;
using ImageSeriesStudio.Infrastructure.Sources;

namespace ImageSeriesStudio.Tests;

public sealed class SourceIngestionWorkflowTests
{
    [Fact]
    public async Task SourceIngestionApplicationService_AddsFakeIngestedSourceAssetToProject()
    {
        var repository = new InMemoryProjectRepository();
        var project = ImageProject.Create(
            "Ingestion demo",
            DateTimeOffset.Parse("2026-06-03T09:00:00Z"));
        await repository.SaveAsync(project, CancellationToken.None);

        var service = new SourceIngestionApplicationService(repository, new FakeSourceIngestionProvider());
        var result = await service.IngestSourceAsync(
            project.Id,
            new SourceIngestionRequest(
                SourceAssetKind.Pdf,
                "uploaded-paper.pdf",
                "The paper claims that visual review should stay traceable to source text.",
                OriginalPath: @"C:\uploads\uploaded-paper.pdf",
                MimeType: "application/pdf",
                SizeBytes: 1024,
                Sha256: "fake-sha256"),
            DateTimeOffset.Parse("2026-06-03T09:01:00Z"),
            CancellationToken.None);

        var loaded = await repository.LoadAsync(project.Id, CancellationToken.None);
        var asset = Assert.Single(loaded!.SourceAssets);
        var content = Assert.Single(asset.ExtractedContents);
        var anchor = Assert.Single(asset.EvidenceAnchors);

        Assert.Equal(asset.Id, result.SourceAssetId);
        Assert.Equal(1, result.ExtractedContentCount);
        Assert.Equal(1, result.EvidenceAnchorCount);
        Assert.Equal("fake-source-ingestion", result.ProviderTraceId);
        Assert.Equal(SourceAssetKind.Pdf, asset.Kind);
        Assert.Equal("uploaded-paper.pdf", asset.DisplayName);
        Assert.Contains("visual review", content.Text);
        Assert.Equal(content.Id, anchor.ExtractedContentId);
        Assert.Contains("visual review", anchor.Quote);
    }

    [Fact]
    public async Task SourceIngestionApplicationService_BlocksProvidersThatRequireExplicitApproval()
    {
        var repository = new InMemoryProjectRepository();
        var project = ImageProject.Create(
            "Approval demo",
            DateTimeOffset.Parse("2026-06-03T09:00:00Z"));
        await repository.SaveAsync(project, CancellationToken.None);

        var provider = new ApprovalRequiredSourceIngestionProvider();
        var service = new SourceIngestionApplicationService(repository, provider);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.IngestSourceAsync(
                project.Id,
                new SourceIngestionRequest(
                    SourceAssetKind.Pdf,
                    "real-document.pdf",
                    "Real extraction should require approval."),
                DateTimeOffset.Parse("2026-06-03T09:01:00Z"),
                CancellationToken.None));

        var loaded = await repository.LoadAsync(project.Id, CancellationToken.None);

        Assert.Contains("explicit approval", exception.Message);
        Assert.Equal(0, provider.CallCount);
        Assert.Empty(loaded!.SourceAssets);
    }

    private sealed class ApprovalRequiredSourceIngestionProvider : ISourceIngestionProvider
    {
        public int CallCount { get; private set; }

        public SourceIngestionProviderCapabilities Capabilities { get; } = new(
            "real-source",
            "Real Source Provider",
            RequiresExplicitApproval: true);

        public Task<SourceIngestionProviderResult> IngestAsync(
            SourceIngestionProviderRequest request,
            CancellationToken cancellationToken)
        {
            CallCount++;
            var asset = SourceAsset.Create(
                request.ProjectId,
                request.Source.Kind,
                request.Source.DisplayName,
                request.Source.OriginalPath,
                request.Source.MimeType,
                request.Source.SizeBytes,
                request.Source.Sha256,
                request.Timestamp);

            return Task.FromResult(new SourceIngestionProviderResult(asset, "real-source"));
        }
    }

    private sealed class InMemoryProjectRepository : IProjectRepository
    {
        private readonly Dictionary<Guid, ImageProject> _projects = [];

        public Task SaveAsync(ImageProject project, CancellationToken cancellationToken)
        {
            _projects[project.Id] = project;
            return Task.CompletedTask;
        }

        public Task<ImageProject?> LoadAsync(Guid projectId, CancellationToken cancellationToken)
        {
            _projects.TryGetValue(projectId, out var project);
            return Task.FromResult(project);
        }

        public Task<IReadOnlyList<ProjectSummary>> ListAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult<IReadOnlyList<ProjectSummary>>(
                _projects.Values
                    .OrderByDescending(project => project.UpdatedAt)
                    .Select(project => new ProjectSummary(
                        project.Id,
                        project.Name,
                        project.CreatedAt,
                        project.UpdatedAt))
                    .ToArray());
        }
    }
}
