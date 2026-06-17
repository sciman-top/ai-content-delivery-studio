using ContentDeliveryStudio.Application.Projects;
using ContentDeliveryStudio.Core.Projects;
using ContentDeliveryStudio.Core.Providers;
using ContentDeliveryStudio.Infrastructure.Fakes;

namespace ContentDeliveryStudio.Tests;

public sealed class GenerationWorkflowApplicationServiceTests
{
    [Fact]
    public async Task GenerationWorkflowApplicationService_RunsGenerationQueueWithFakeProvider()
    {
        var repository = new InMemoryProjectRepository();
        var provider = new FakeImageGenerationProvider();
        var service = new GenerationWorkflowApplicationService(repository, provider, provider);
        var timestamp = DateTimeOffset.Parse("2026-06-07T12:00:00Z");
        var project = ImageProject.Create("Generation workflow demo", timestamp);
        var series = project.AddSeries("Series", "Fake queue", timestamp.AddMinutes(1));
        var item = series.AddItem("Opening image", "Opening visual", timestamp.AddMinutes(2));
        var profile = project.AddProviderProfile("Fake provider", ProviderKind.Fake, timestamp.AddMinutes(3));
        item.AddPromptVersion(
            "Create a clean editorial image.",
            new GenerationSettings(1024, 1024, "standard", "png"),
            profile.Id,
            timestamp.AddMinutes(4));
        await repository.SaveAsync(project, CancellationToken.None);

        var outputDirectory = Path.Combine(Path.GetTempPath(), "ContentDeliveryStudio.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(outputDirectory);

        try
        {
            var run = await service.RunGenerationQueueAsync(project.Id, outputDirectory, CancellationToken.None);

            var loaded = await repository.LoadAsync(project.Id, CancellationToken.None);
            var loadedItem = loaded!.Series.Single().Items.Single();

            Assert.Single(run.Tasks);
            Assert.Equal(GenerationTaskStatus.Succeeded, run.Tasks.Single().Status);
            Assert.Single(run.Images);
            Assert.Single(loadedItem.GenerationTasks);
            Assert.Single(loadedItem.CandidateImages);
            Assert.True(File.Exists(run.Images.Single().AssetPath));
        }
        finally
        {
            if (Directory.Exists(outputDirectory))
            {
                Directory.Delete(outputDirectory, recursive: true);
            }
        }
    }

    [Fact]
    public async Task GenerationWorkflowApplicationService_RunsMaskEditWithFakeProvider()
    {
        var repository = new InMemoryProjectRepository();
        var provider = new FakeImageGenerationProvider();
        var service = new GenerationWorkflowApplicationService(repository, provider, provider);
        var timestamp = DateTimeOffset.Parse("2026-06-07T13:00:00Z");
        var project = ImageProject.Create("Image edit workflow demo", timestamp);
        await repository.SaveAsync(project, CancellationToken.None);

        var workingDirectory = Path.Combine(Path.GetTempPath(), "ContentDeliveryStudio.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(workingDirectory);
        var sourcePath = Path.Combine(workingDirectory, "source.png");
        var maskPath = Path.Combine(workingDirectory, "mask.png");
        var outputDirectory = Path.Combine(workingDirectory, "edited");
        await File.WriteAllBytesAsync(sourcePath, [1, 2, 3], CancellationToken.None);
        await File.WriteAllBytesAsync(maskPath, [4, 5, 6], CancellationToken.None);

        try
        {
            var result = await service.RunImageEditAsync(
                new ImageEditWorkflowRequest(
                    project.Id,
                    Guid.NewGuid(),
                    Guid.NewGuid(),
                    sourcePath,
                    maskPath,
                    "Clean the masked area.",
                    new GenerationSettings(1024, 1024, "standard", "png"),
                    outputDirectory,
                    "edited.png"),
                CancellationToken.None);

            Assert.True(File.Exists(result.AssetPath));
            Assert.Equal("fake-image-edit", result.ProviderTraceId);
        }
        finally
        {
            if (Directory.Exists(workingDirectory))
            {
                Directory.Delete(workingDirectory, recursive: true);
            }
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
