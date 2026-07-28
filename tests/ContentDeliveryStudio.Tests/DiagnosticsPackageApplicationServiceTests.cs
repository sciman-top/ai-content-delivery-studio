using ContentDeliveryStudio.Application.Diagnostics;
using ContentDeliveryStudio.Application.Projects;
using ContentDeliveryStudio.Core.Projects;

namespace ContentDeliveryStudio.Tests;

public sealed class DiagnosticsPackageApplicationServiceTests
{
    [Fact]
    public async Task ExportAsync_LoadsProjectsWithoutSavingAndCreatesUniqueChildDirectory()
    {
        var timestamp = DateTimeOffset.Parse("2026-07-28T13:10:11.123Z");
        var project = ImageProject.Create("Diagnostics project", timestamp.AddMinutes(-1));
        var repository = new RecordingRepository(project);
        var writer = new RecordingWriter();
        var service = new DiagnosticsPackageApplicationService(
            repository,
            writer,
            new FixedTimeProvider(timestamp));

        var request = new DiagnosticsPackageApplicationRequest(
            Path.GetTempPath(),
            new DiagnosticsApplicationSnapshot("App", "1.0", "Test", DateTimeOffset.MinValue),
            new DiagnosticsMachineSnapshot("OS", "Framework", "X64", "en-US", "en-US"),
            [],
            []);

        var first = await service.ExportAsync(request, CancellationToken.None);
        var second = await service.ExportAsync(request, CancellationToken.None);

        Assert.NotEqual(first.PackageDirectory, second.PackageDirectory);
        Assert.StartsWith(
            Path.Combine(Path.GetFullPath(Path.GetTempPath()), "content-delivery-studio-diagnostics-20260728-131011-123-"),
            first.PackageDirectory,
            StringComparison.OrdinalIgnoreCase);
        Assert.Equal(2, repository.ListCount);
        Assert.Equal(2, repository.LoadCount);
        Assert.Equal(0, repository.SaveCount);
        Assert.All(writer.Requests, item => Assert.Equal(timestamp, item.Application.CreatedAt));
        Assert.All(writer.Requests, item => Assert.Equal("Diagnostics project", Assert.Single(item.Projects).Name));
    }

    private sealed class RecordingWriter : IDiagnosticsPackageWriter
    {
        public List<DiagnosticsExportRequest> Requests { get; } = [];

        public Task<DiagnosticsExportResult> WriteAsync(
            DiagnosticsExportRequest request,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Requests.Add(request);
            return Task.FromResult(new DiagnosticsExportResult(
                request.OutputDirectory,
                Path.Combine(request.OutputDirectory, "diagnostics.json"),
                Path.Combine(request.OutputDirectory, "diagnostics.md")));
        }
    }

    private sealed class RecordingRepository(ImageProject project) : IProjectRepository
    {
        public int ListCount { get; private set; }

        public int LoadCount { get; private set; }

        public int SaveCount { get; private set; }

        public Task SaveAsync(ImageProject value, CancellationToken cancellationToken)
        {
            SaveCount++;
            return Task.CompletedTask;
        }

        public Task<ImageProject?> LoadAsync(Guid projectId, CancellationToken cancellationToken)
        {
            LoadCount++;
            return Task.FromResult<ImageProject?>(projectId == project.Id ? project : null);
        }

        public Task<IReadOnlyList<ProjectSummary>> ListAsync(CancellationToken cancellationToken)
        {
            ListCount++;
            IReadOnlyList<ProjectSummary> result =
            [
                new ProjectSummary(project.Id, project.Name, project.CreatedAt, project.UpdatedAt),
            ];
            return Task.FromResult(result);
        }

        public Task SaveReviewResultAsync(Guid projectId, ReviewResult reviewResult, CancellationToken cancellationToken) =>
            Task.CompletedTask;

        public Task<ReviewResult?> LoadLatestReviewResultAsync(Guid candidateImageId, CancellationToken cancellationToken) =>
            Task.FromResult<ReviewResult?>(null);
    }

    private sealed class FixedTimeProvider(DateTimeOffset timestamp) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => timestamp;
    }
}
