using ContentDeliveryStudio.Application.Projects;
using ContentDeliveryStudio.App.ViewModels;
using ContentDeliveryStudio.Core.Projects;
using ContentDeliveryStudio.Infrastructure.Fakes;

namespace ContentDeliveryStudio.Tests;

public sealed class BriefWorkflowCoordinatorTests
{
    [Fact]
    public void BuildBriefMustInclude_UsesSeriesItemsBeforeFallbackGoal()
    {
        var series = new SeriesSummaryViewModel(
            Guid.NewGuid(),
            "Article images",
            [
                new SeriesItemViewModel(
                    Guid.NewGuid(),
                    "Opening",
                    "Opening visual for teachers.",
                    SeriesItemKind.Standard,
                    SeriesItemStatus.Draft,
                    []),
                new SeriesItemViewModel(
                    Guid.NewGuid(),
                    "Closing",
                    "",
                    SeriesItemKind.Standard,
                    SeriesItemStatus.Draft,
                    []),
            ]);

        var mustInclude = BriefWorkflowCoordinator.BuildBriefMustInclude(series, "fallback goal");

        Assert.Equal(
            ["Opening: Opening visual for teachers.", "Closing"],
            mustInclude);
    }

    [Fact]
    public async Task EnsureActiveCreativeBriefIdAsync_CreatesBriefWhenSeriesHasNoBriefs()
    {
        var repository = new InMemoryProjectRepository();
        var projectService = new ProjectApplicationService(repository, new FakeTextPlanningProvider());
        var coordinator = new BriefWorkflowCoordinator(projectService);
        var timestamp = DateTimeOffset.Parse("2026-06-07T22:00:00Z");
        var project = await projectService.CreateProjectAsync("Brief coordinator demo", timestamp, CancellationToken.None);
        var createdSeries = await projectService.AddSeriesAsync(
            project.Id,
            "Article images",
            "Series",
            timestamp.AddMinutes(1),
            CancellationToken.None);
        var selectedSeries = new SeriesSummaryViewModel(createdSeries.Id, createdSeries.Title, []);

        var briefId = await coordinator.EnsureActiveCreativeBriefIdAsync(
            project.Id,
            selectedSeries,
            activeCreativeBriefId: null,
            "article illustration",
            "teachers",
            "clean editorial",
            CancellationToken.None);

        var loaded = await projectService.LoadProjectAsync(project.Id, CancellationToken.None);
        var brief = Assert.Single(loaded!.Series.Single().CreativeBriefs);

        Assert.Equal(brief.Id, briefId);
        Assert.Equal(["Article images"], brief.MustInclude);
    }

    [Fact]
    public async Task BuildBlueprintAndPromptDirectionRows_ReflectWorkflowArtifacts()
    {
        var repository = new InMemoryProjectRepository();
        var projectService = new ProjectApplicationService(repository, new FakeTextPlanningProvider());
        var coordinator = new BriefWorkflowCoordinator(projectService);
        var timestamp = DateTimeOffset.Parse("2026-06-07T23:00:00Z");
        var project = await projectService.CreateProjectAsync("Brief row demo", timestamp, CancellationToken.None);
        var createdSeries = await projectService.AddSeriesAsync(
            project.Id,
            "Storyboard images",
            "Series",
            timestamp.AddMinutes(1),
            CancellationToken.None);
        var selectedSeries = new SeriesSummaryViewModel(
            createdSeries.Id,
            createdSeries.Title,
            [
                new SeriesItemViewModel(
                    Guid.NewGuid(),
                    "Opening",
                    "Opening visual with the same main character.",
                    SeriesItemKind.Standard,
                    SeriesItemStatus.Draft,
                    []),
            ]);

        var briefId = await coordinator.CreateBriefAsync(
            project.Id,
            selectedSeries,
            "panel story sequence",
            "students",
            "clear visual storytelling",
            CancellationToken.None);

        await coordinator.GenerateDesignBlueprintsAsync(
            project.Id,
            selectedSeries,
            briefId,
            "panel story sequence",
            "students",
            "clear visual storytelling",
            CancellationToken.None);

        await coordinator.GeneratePromptDirectionsAsync(
            project.Id,
            selectedSeries,
            briefId,
            "panel story sequence",
            "students",
            "clear visual storytelling",
            CancellationToken.None);

        var loaded = await projectService.LoadProjectAsync(project.Id, CancellationToken.None);
        var blueprintRows = BriefWorkflowCoordinator.BuildDesignBlueprintRows(
            loaded!,
            promotedText: "Promoted",
            candidateText: "Candidate");
        var promptDirectionRows = BriefWorkflowCoordinator.BuildPromptDirectionRows(loaded!);

        var blueprintRow = Assert.Single(
            blueprintRows,
            row => row.Key == "panel-narrative-sequence");
        var promptDirectionRow = Assert.Single(
            promptDirectionRows,
            row => row.DirectionKey == "conservative");

        Assert.Equal("Candidate", blueprintRow.PromotionStatus);
        Assert.Contains("same main character", blueprintRow.ConsistencySummary, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("1536x1024", promptDirectionRow.RecommendationSummary);
        Assert.Contains("fake provider warning", promptDirectionRow.CapabilityWarningSummary);
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
