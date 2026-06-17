using ContentDeliveryStudio.Application.Projects;
using ContentDeliveryStudio.Core.Projects;
using ContentDeliveryStudio.Core.Providers;
using ContentDeliveryStudio.Infrastructure.Fakes;

namespace ContentDeliveryStudio.Tests;

public sealed class SeriesWorkflowApplicationServiceTests
{
    [Fact]
    public async Task SeriesWorkflowApplicationService_AddsSeriesItemsAndPromptVersion()
    {
        var repository = new InMemoryProjectRepository();
        var service = new SeriesWorkflowApplicationService(repository, textPlanningProvider: null);
        var timestamp = DateTimeOffset.Parse("2026-06-07T18:00:00Z");
        var project = ImageProject.Create("Series workflow demo", timestamp);
        await repository.SaveAsync(project, CancellationToken.None);

        var series = await service.AddSeriesAsync(
            project.Id,
            "Poster series",
            "A classroom poster set",
            timestamp.AddMinutes(1),
            CancellationToken.None);

        var item = await service.AddItemAsync(
            project.Id,
            series.Id,
            "Cover image",
            "Opening classroom visual",
            SeriesItemKind.Standard,
            timestamp.AddMinutes(2),
            CancellationToken.None);

        var prompt = await service.AddPromptVersionAsync(
            project.Id,
            item.Id,
            "Create a clean editorial image set.",
            new GenerationSettings(1024, 1024, "standard", "png", 11),
            providerProfileId: null,
            timestamp.AddMinutes(3),
            CancellationToken.None);

        var loaded = await repository.LoadAsync(project.Id, CancellationToken.None);
        var loadedSeries = Assert.Single(loaded!.Series);
        var loadedItem = Assert.Single(loadedSeries.Items);
        var loadedPrompt = Assert.Single(loadedItem.PromptVersions);
        var loadedProfile = Assert.Single(loaded.ProviderProfiles);

        Assert.Equal(series.Id, loadedSeries.Id);
        Assert.Equal(item.Id, loadedItem.Id);
        Assert.Equal(prompt.Id, loadedPrompt.Id);
        Assert.Equal(ProviderKind.Fake, loadedProfile.Kind);
        Assert.Equal(loadedProfile.Id, loadedPrompt.ProviderProfileId);
    }

    [Fact]
    public async Task SeriesWorkflowApplicationService_CreatesPlanWithFakeProvider()
    {
        var repository = new InMemoryProjectRepository();
        var service = new SeriesWorkflowApplicationService(repository, new FakeTextPlanningProvider());
        var timestamp = DateTimeOffset.Parse("2026-06-07T19:00:00Z");
        var project = ImageProject.Create("Fake planning demo", timestamp);
        await repository.SaveAsync(project, CancellationToken.None);

        var series = await service.CreatePlanWithProviderAsync(
            project.Id,
            new PlanningRequest(
                "three article illustrations",
                "content authors",
                3,
                "clean editorial style"),
            timestamp.AddMinutes(1),
            CancellationToken.None);

        var loaded = await repository.LoadAsync(project.Id, CancellationToken.None);
        var loadedSeries = Assert.Single(loaded!.Series);

        Assert.Equal(series.Id, loadedSeries.Id);
        Assert.Equal("three article illustrations", loadedSeries.Title);
        Assert.Equal(3, loadedSeries.Items.Count);
        Assert.All(loadedSeries.Items, row => Assert.Single(row.PromptVersions));
        Assert.Single(loaded.ProviderProfiles);
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
