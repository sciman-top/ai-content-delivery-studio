using ImageSeriesStudio.Application.Projects;
using ImageSeriesStudio.App.ViewModels;
using ImageSeriesStudio.Core.Projects;
using ImageSeriesStudio.Infrastructure.Fakes;

namespace ImageSeriesStudio.Tests;

public sealed class GenerationWorkflowCoordinatorTests
{
    [Fact]
    public async Task RunFakeGenerationAsync_BuildsQueueAndGalleryRows()
    {
        using var localStudioRoot = LocalStudioDataPathScope.Create();
        var repository = new InMemoryProjectRepository();
        var fakeImageProvider = new FakeImageGenerationProvider();
        var projectService = new ProjectApplicationService(
            repository,
            new FakeTextPlanningProvider(),
            fakeImageProvider,
            visionReviewProvider: null,
            deliveryPackageWriter: null,
            imageEditProvider: fakeImageProvider);
        var coordinator = new GenerationWorkflowCoordinator(projectService);
        var timestamp = DateTimeOffset.Parse("2026-06-08T08:00:00Z");
        var project = await projectService.CreateProjectAsync("Generation coordinator demo", timestamp, CancellationToken.None);
        var series = await projectService.AddSeriesAsync(
            project.Id,
            "Lesson visuals",
            "Series",
            timestamp.AddMinutes(1),
            CancellationToken.None);
        var item = await projectService.AddItemAsync(
            project.Id,
            series.Id,
            "Opening frame",
            "Opening visual for a lesson.",
            timestamp.AddMinutes(2),
            CancellationToken.None);
        await projectService.AddPromptVersionAsync(
            project.Id,
            item.Id,
            "Create a clean opening frame.",
            new GenerationSettings(1024, 1024, "standard", "png"),
            providerProfileId: null,
            timestamp.AddMinutes(3),
            CancellationToken.None);

        var seriesRows = new[]
        {
            new SeriesSummaryViewModel(
                series.Id,
                series.Title,
                [
                    new SeriesItemViewModel(
                        item.Id,
                        item.Title,
                        item.Brief,
                        item.Kind,
                        item.Status,
                        [
                            new PromptVersionViewModel(
                                item.PromptVersions.Single().Id,
                                1,
                                "Create a clean opening frame.",
                                "1024x1024 standard png",
                                timestamp.AddMinutes(3)),
                        ]),
                ]),
        };

        try
        {
            var result = await coordinator.RunFakeGenerationAsync(project.Id, seriesRows, CancellationToken.None);

            var queueRow = Assert.Single(result.QueueRows);
            var galleryRow = Assert.Single(result.GalleryRows);

            Assert.Equal("Opening frame", queueRow.ItemTitle);
            Assert.Equal("Succeeded", queueRow.Status);
            Assert.Equal("Opening frame", galleryRow.ItemTitle);
            Assert.Equal("Create a clean opening frame.", galleryRow.PromptText);
            Assert.True(File.Exists(galleryRow.AssetPath));
            Assert.True(File.Exists(galleryRow.MetadataPath));
        }
        finally
        {
            DeleteProjectOutputDirectories(project.Id, "generated");
        }
    }

    [Fact]
    public async Task RunFakeImageEditAsync_BuildsEditedGalleryRow()
    {
        using var localStudioRoot = LocalStudioDataPathScope.Create();
        var repository = new InMemoryProjectRepository();
        var fakeImageProvider = new FakeImageGenerationProvider();
        var projectService = new ProjectApplicationService(
            repository,
            new FakeTextPlanningProvider(),
            fakeImageProvider,
            visionReviewProvider: null,
            deliveryPackageWriter: null,
            imageEditProvider: fakeImageProvider);
        var coordinator = new GenerationWorkflowCoordinator(projectService);
        var timestamp = DateTimeOffset.Parse("2026-06-08T09:00:00Z");
        var project = await projectService.CreateProjectAsync("Edit coordinator demo", timestamp, CancellationToken.None);

        var sourceDirectory = localStudioRoot.GetProjectDirectory("generated", project.Id);
        Directory.CreateDirectory(sourceDirectory);
        var sourcePath = Path.Combine(sourceDirectory, "source.png");
        var metadataPath = Path.Combine(sourceDirectory, "source.json");
        await File.WriteAllBytesAsync(sourcePath, [1, 2, 3], CancellationToken.None);
        await File.WriteAllTextAsync(metadataPath, """{"providerId":"fake-image"}""", CancellationToken.None);

        var sourceRow = new GalleryRowViewModel(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Opening frame",
            sourcePath,
            metadataPath,
            "Create a clean opening frame.");

        try
        {
            var editedRow = await coordinator.RunFakeImageEditAsync(
                project.Id,
                sourceRow,
                "Clean the label area while preserving the composition.",
                maskPath: null,
                CancellationToken.None);

            Assert.Equal(sourceRow.SeriesItemId, editedRow.SeriesItemId);
            Assert.Contains("edited", editedRow.ItemTitle, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("Clean the label area", editedRow.PromptText);
            Assert.True(File.Exists(editedRow.AssetPath));
            Assert.True(File.Exists(editedRow.MetadataPath));
        }
        finally
        {
            DeleteProjectOutputDirectories(project.Id, "edited");
            DeleteProjectOutputDirectories(project.Id, "generated");
        }
    }

    private static void DeleteProjectOutputDirectories(Guid projectId, string folder)
    {
        var directory = LocalStudioDataPaths.ResolveProjectDirectory(folder, projectId);
        if (Directory.Exists(directory))
        {
            Directory.Delete(directory, recursive: true);
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
