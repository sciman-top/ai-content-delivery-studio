using ContentDeliveryStudio.Application.Artifacts;
using ContentDeliveryStudio.Application.Projects;
using ContentDeliveryStudio.Core.Artifacts;
using ContentDeliveryStudio.Core.Projects;
using ContentDeliveryStudio.Core.Sources;
using ContentDeliveryStudio.Infrastructure.Artifacts;

namespace ContentDeliveryStudio.Tests;

public sealed class ArtifactPlanningWorkflowTests
{
    [Fact]
    public async Task ArtifactPlanningApplicationService_PlansMixedArtifactsWithFakeProvider()
    {
        var repository = new InMemoryProjectRepository();
        var timestamp = DateTimeOffset.Parse("2026-06-03T11:00:00Z");
        var project = ImageProject.Create("Artifact planning demo", timestamp);
        var source = SourceAsset.Create(
            project.Id,
            SourceAssetKind.Markdown,
            "lesson.md",
            "lesson.md",
            "text/markdown",
            256,
            "sha256-lesson",
            timestamp.AddMinutes(1));
        var content = source.AddExtractedContent(
            ExtractedContentKind.Markdown,
            "Superposition needs a visual analogy for teachers.",
            "section 1",
            pageNumber: null,
            startOffset: 0,
            endOffset: 50,
            timestamp.AddMinutes(2));
        var anchor = source.AddEvidenceAnchor(
            content.Id,
            "teaching claim",
            "visual analogy",
            "section 1",
            timestamp.AddMinutes(3));
        project.AddSourceAsset(source, timestamp.AddMinutes(4));
        await repository.SaveAsync(project, CancellationToken.None);

        var service = new ArtifactPlanningApplicationService(repository, new FakeArtifactPlanningProvider());
        var result = await service.PlanArtifactsAsync(
            project.Id,
            new ArtifactPlanningRequest(
                "Quantum teacher packet",
                "Create a mixed delivery package for teachers.",
                [anchor.Id],
                [OutputArtifactKind.Image, OutputArtifactKind.Pdf, OutputArtifactKind.Markdown, OutputArtifactKind.ReviewReport],
                OutputDirectory: "delivery"),
            timestamp.AddMinutes(5),
            CancellationToken.None);

        var loaded = await repository.LoadAsync(project.Id, CancellationToken.None);
        var artifacts = loaded!.OutputArtifacts.OrderBy(artifact => artifact.Kind).ToArray();

        Assert.Equal("fake-artifact-planning", result.ProviderTraceId);
        Assert.Equal(4, result.OutputArtifactIds.Count);
        Assert.Equal(4, artifacts.Length);
        Assert.All(artifacts, artifact =>
        {
            Assert.Equal(OutputArtifactStatus.Planned, artifact.Status);
            Assert.Equal([source.Id], artifact.SourceAssetIds);
            Assert.Equal([anchor.Id], artifact.EvidenceAnchorIds);
            Assert.Equal("Quantum teacher packet", artifact.Metadata["briefTitle"]);
        });
        Assert.Contains(artifacts, artifact => artifact.Kind == OutputArtifactKind.Image && artifact.RelativePath.EndsWith(".png", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(artifacts, artifact => artifact.Kind == OutputArtifactKind.Pdf && artifact.MimeType == "application/pdf");
        Assert.Contains(artifacts, artifact => artifact.Kind == OutputArtifactKind.Markdown && artifact.RelativePath.EndsWith(".md", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(artifacts, artifact => artifact.Kind == OutputArtifactKind.ReviewReport && artifact.Role == "review-evidence");
    }

    [Fact]
    public async Task ArtifactPlanningApplicationService_BlocksProvidersThatRequireExplicitApproval()
    {
        var repository = new InMemoryProjectRepository();
        var timestamp = DateTimeOffset.Parse("2026-06-03T11:00:00Z");
        var project = ImageProject.Create("Approval demo", timestamp);
        await repository.SaveAsync(project, CancellationToken.None);

        var provider = new ApprovalRequiredArtifactPlanningProvider();
        var service = new ArtifactPlanningApplicationService(repository, provider);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.PlanArtifactsAsync(
                project.Id,
                new ArtifactPlanningRequest(
                    "Real provider packet",
                    "Do not call real planning automatically.",
                    [],
                    [OutputArtifactKind.Pdf],
                    OutputDirectory: "delivery"),
                timestamp.AddMinutes(1),
                CancellationToken.None));

        Assert.Contains("explicit approval", exception.Message);
        Assert.Equal(0, provider.CallCount);
    }

    private sealed class ApprovalRequiredArtifactPlanningProvider : IArtifactPlanningProvider
    {
        public int CallCount { get; private set; }

        public ArtifactPlanningProviderCapabilities Capabilities { get; } = new(
            "real-artifact-planning",
            "Real Artifact Planning Provider",
            RequiresExplicitApproval: true);

        public Task<ArtifactPlanningProviderResult> PlanAsync(
            ArtifactPlanningProviderRequest request,
            CancellationToken cancellationToken)
        {
            CallCount++;
            return Task.FromResult(new ArtifactPlanningProviderResult([], [], "real-artifact-planning"));
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

        public Task SaveReviewResultAsync(Guid projectId, ReviewResult reviewResult, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public Task<ReviewResult?> LoadLatestReviewResultAsync(Guid candidateImageId, CancellationToken cancellationToken)
        {
            return Task.FromResult<ReviewResult?>(null);
        }
    }
}
