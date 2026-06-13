using System.Globalization;
using ImageSeriesStudio.Application.Localization;
using ImageSeriesStudio.Application.Projects;
using ImageSeriesStudio.App.ViewModels;
using ImageSeriesStudio.Core.Documents;
using ImageSeriesStudio.Core.Projects;
using ImageSeriesStudio.Infrastructure.Fakes;

namespace ImageSeriesStudio.Tests;

public sealed class WorkbenchInspectorCoordinatorTests
{
    [Fact]
    public async Task CreateProjectAsync_CreatesProjectAndReturnsProjectedSelection()
    {
        var repository = new InMemoryProjectRepository();
        var projectService = new ProjectApplicationService(repository, new FakeTextPlanningProvider());
        var coordinator = CreateCoordinator(projectService);

        var result = await coordinator.CreateProjectAsync("Inspector workspace demo", CancellationToken.None);

        var selected = Assert.Single(result.Projects);
        Assert.Equal("Inspector workspace demo", selected.Name);
        Assert.Equal(selected.Id, result.SelectedProject?.Id);
    }

    [Fact]
    public async Task RunFakeDocumentPlanningAsync_RefreshesWorkspaceAndAppendsActivitySummary()
    {
        var repository = new InMemoryProjectRepository();
        var projectService = new ProjectApplicationService(repository, new FakeTextPlanningProvider());
        var coordinator = CreateCoordinator(projectService);
        var timestamp = DateTimeOffset.Parse("2026-06-13T12:00:00Z");
        var project = await projectService.CreateProjectAsync("Inspector document demo", timestamp, CancellationToken.None);
        var selectedProject = new ProjectSummaryViewModel(project.Id, project.Name, project.UpdatedAt);

        var result = await coordinator.RunFakeDocumentPlanningAsync(
            selectedProject,
            "Teachers need a clean visual explanation of superposition.",
            "physics teachers",
            IllustrationStrictnessLevel.ScholarlyDraft,
            defaultDocumentAudience: "teachers",
            currentActivityItems: ["Existing activity"],
            CancellationToken.None);

        Assert.Equal(project.Id, result.Workspace.SelectedProject?.Id);
        Assert.NotNull(result.SeriesId);
        Assert.Contains("Approved targets:", result.ResultSummary);
        Assert.Equal("Existing activity", result.ActivityItems[0]);
        Assert.Contains(result.ResultSummary, result.ActivityItems);
    }

    [Fact]
    public async Task RunFakeImageEditAsync_AppendsEditedRowAndPrependsActivitySummary()
    {
        var repository = new InMemoryProjectRepository();
        var fakeImageProvider = new FakeImageGenerationProvider();
        var projectService = new ProjectApplicationService(
            repository,
            new FakeTextPlanningProvider(),
            fakeImageProvider,
            visionReviewProvider: null,
            deliveryPackageWriter: null,
            imageEditProvider: fakeImageProvider);
        var coordinator = CreateCoordinator(projectService);
        var timestamp = DateTimeOffset.Parse("2026-06-13T13:00:00Z");
        var project = await projectService.CreateProjectAsync("Inspector edit demo", timestamp, CancellationToken.None);

        var sourceDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "ImageSeriesStudio",
            "generated",
            project.Id.ToString("N"));
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
            var result = await coordinator.RunFakeImageEditAsync(
                project.Id,
                sourceRow,
                "Clean the label area while preserving the composition.",
                maskPath: null,
                imageEditResultText: "Edited image ready.",
                currentGalleryRows: [sourceRow],
                currentActivityItems: ["Older activity"],
                CancellationToken.None);

            Assert.Equal(2, result.GalleryRows.Count);
            Assert.Equal(result.SelectedGalleryRow.CandidateImageId, result.GalleryRows.Last().CandidateImageId);
            Assert.Equal("Edited image ready.", result.ActivityItems[0]);
            Assert.Equal("Older activity", result.ActivityItems[1]);
        }
        finally
        {
            DeleteProjectOutputDirectories(project.Id, "edited");
            DeleteProjectOutputDirectories(project.Id, "generated");
        }
    }

    private static WorkbenchInspectorCoordinator CreateCoordinator(ProjectApplicationService projectService)
    {
        var localizationService = new LocalizationService(() => new CultureInfo("en-US"));
        var projectWorkspaceCoordinator = new ProjectWorkspaceCoordinator(projectService);
        var planningWorkflowCoordinator = new PlanningWorkflowCoordinator(projectService, localizationService);
        var generationWorkflowCoordinator = new GenerationWorkflowCoordinator(projectService);

        return new WorkbenchInspectorCoordinator(
            projectWorkspaceCoordinator,
            planningWorkflowCoordinator,
            generationWorkflowCoordinator);
    }

    private static void DeleteProjectOutputDirectories(Guid projectId, string folder)
    {
        var directory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "ImageSeriesStudio",
            folder,
            projectId.ToString("N"));
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
