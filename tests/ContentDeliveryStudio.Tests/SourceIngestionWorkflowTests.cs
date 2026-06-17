using ContentDeliveryStudio.Application.Projects;
using ContentDeliveryStudio.Application.Sources;
using ContentDeliveryStudio.Core.Projects;
using ContentDeliveryStudio.Core.Sources;
using ContentDeliveryStudio.Infrastructure.Sources;

namespace ContentDeliveryStudio.Tests;

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

    [Fact]
    public async Task SourceIngestionApplicationService_UsesLocalBinaryExtractionForPdfWhenLocalFileExists()
    {
        var repository = new InMemoryProjectRepository();
        var project = ImageProject.Create(
            "Binary PDF demo",
            DateTimeOffset.Parse("2026-06-16T00:20:00Z"));
        await repository.SaveAsync(project, CancellationToken.None);
        var rootDirectory = Path.Combine(Path.GetTempPath(), "ContentDeliveryStudio.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(rootDirectory);
        var pdfPath = Path.Combine(rootDirectory, "lesson.pdf");

        try
        {
            await BinaryDocumentTestFixtureBuilder.CreateSimplePdfAsync(
                pdfPath,
                "Local PDF extraction should preserve source evidence for planning.",
                CancellationToken.None);
            var service = new SourceIngestionApplicationService(
                repository,
                new SupportMatrixSourceIngestionProvider(
                    new LocalBinaryDocumentExtractionProvider(),
                    new FakeSourceIngestionProvider()));

            var result = await service.IngestSourceAsync(
                project.Id,
                new SourceIngestionRequest(
                    SourceAssetKind.Pdf,
                    "lesson.pdf",
                    "Fake fixture text should not win when a local PDF is present.",
                    OriginalPath: pdfPath,
                    MimeType: "application/pdf",
                    SizeBytes: 2048,
                    Sha256: "pdf-sha"),
                DateTimeOffset.Parse("2026-06-16T00:21:00Z"),
                CancellationToken.None);

            var loaded = await repository.LoadAsync(project.Id, CancellationToken.None);
            var asset = Assert.Single(loaded!.SourceAssets);
            var content = Assert.Single(asset.ExtractedContents);
            var anchor = Assert.Single(asset.EvidenceAnchors);

            Assert.Equal("local-binary-pdf-extraction", result.ProviderTraceId);
            Assert.Contains("preserve source evidence", content.Text, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("Fake fixture text", content.Text, StringComparison.OrdinalIgnoreCase);
            Assert.Equal("lesson.pdf: page 1", content.LocationHint);
            Assert.Equal(1, content.PageNumber);
            Assert.Equal(content.Id, anchor.ExtractedContentId);
            Assert.Equal("lesson.pdf: page 1", anchor.LocationHint);
        }
        finally
        {
            if (Directory.Exists(rootDirectory))
            {
                Directory.Delete(rootDirectory, recursive: true);
            }
        }
    }

    [Fact]
    public async Task SourceIngestionApplicationService_UsesLocalBinaryExtractionForDocxWhenLocalFileExists()
    {
        var repository = new InMemoryProjectRepository();
        var project = ImageProject.Create(
            "Binary DOCX demo",
            DateTimeOffset.Parse("2026-06-16T00:25:00Z"));
        await repository.SaveAsync(project, CancellationToken.None);
        var rootDirectory = Path.Combine(Path.GetTempPath(), "ContentDeliveryStudio.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(rootDirectory);
        var docxPath = Path.Combine(rootDirectory, "brief.docx");

        try
        {
            await BinaryDocumentTestFixtureBuilder.CreateSimpleDocxAsync(
                docxPath,
                [
                    "First paragraph establishes the source-backed teaching claim.",
                    "Second paragraph preserves the supporting classroom context.",
                ],
                CancellationToken.None);
            var service = new SourceIngestionApplicationService(
                repository,
                new SupportMatrixSourceIngestionProvider(
                    new LocalBinaryDocumentExtractionProvider(),
                    new FakeSourceIngestionProvider()));

            var result = await service.IngestSourceAsync(
                project.Id,
                new SourceIngestionRequest(
                    SourceAssetKind.Docx,
                    "brief.docx",
                    "Fixture fallback should not win when a local DOCX is present.",
                    OriginalPath: docxPath,
                    MimeType: "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                    SizeBytes: 4096,
                    Sha256: "docx-sha"),
                DateTimeOffset.Parse("2026-06-16T00:26:00Z"),
                CancellationToken.None);

            var loaded = await repository.LoadAsync(project.Id, CancellationToken.None);
            var asset = Assert.Single(loaded!.SourceAssets);

            Assert.Equal("local-binary-docx-extraction", result.ProviderTraceId);
            Assert.Equal(2, asset.ExtractedContents.Count);
            Assert.Contains(asset.ExtractedContents, content => content.LocationHint == "brief.docx: paragraph 1");
            Assert.Contains(asset.ExtractedContents, content => content.LocationHint == "brief.docx: paragraph 2");
            Assert.DoesNotContain(asset.ExtractedContents, content => content.Text.Contains("Fixture fallback", StringComparison.OrdinalIgnoreCase));
            Assert.Equal(2, asset.EvidenceAnchors.Count);
        }
        finally
        {
            if (Directory.Exists(rootDirectory))
            {
                Directory.Delete(rootDirectory, recursive: true);
            }
        }
    }

    [Fact]
    public async Task SourceIngestionApplicationService_FallsBackToFakeExtractionWhenBinaryFileIsUnavailable()
    {
        var repository = new InMemoryProjectRepository();
        var project = ImageProject.Create(
            "Binary fallback demo",
            DateTimeOffset.Parse("2026-06-16T00:30:00Z"));
        await repository.SaveAsync(project, CancellationToken.None);
        var service = new SourceIngestionApplicationService(
            repository,
            new SupportMatrixSourceIngestionProvider(
                new LocalBinaryDocumentExtractionProvider(),
                new FakeSourceIngestionProvider()));

        var result = await service.IngestSourceAsync(
            project.Id,
            new SourceIngestionRequest(
                SourceAssetKind.Pdf,
                "missing-paper.pdf",
                "Fallback fixture text should remain usable when no local binary file exists.",
                OriginalPath: @"D:\missing\missing-paper.pdf",
                MimeType: "application/pdf",
                SizeBytes: 512,
                Sha256: "missing-sha"),
            DateTimeOffset.Parse("2026-06-16T00:31:00Z"),
            CancellationToken.None);

        var loaded = await repository.LoadAsync(project.Id, CancellationToken.None);
        var asset = Assert.Single(loaded!.SourceAssets);
        var content = Assert.Single(asset.ExtractedContents);

        Assert.Equal("fake-source-ingestion", result.ProviderTraceId);
        Assert.Contains("Fallback fixture text", content.Text, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("missing-paper.pdf", asset.DisplayName);
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
