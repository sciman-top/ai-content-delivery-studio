using ImageSeriesStudio.Application.Projects;
using ImageSeriesStudio.Core.Projects;
using ImageSeriesStudio.Core.Providers;
using ImageSeriesStudio.Core.Styles;
using ImageSeriesStudio.Infrastructure.Fakes;

namespace ImageSeriesStudio.Tests;

public sealed class BriefWorkflowApplicationServiceTests
{
    [Fact]
    public async Task BriefWorkflowApplicationService_CreatesDirectionsAndPromotesPromptVersion()
    {
        var repository = new InMemoryProjectRepository();
        var service = new BriefWorkflowApplicationService(repository, new FakeTextPlanningProvider());
        var timestamp = DateTimeOffset.Parse("2026-06-07T09:00:00Z");
        var project = ImageProject.Create("Brief workflow demo", timestamp);
        var series = project.AddSeries("Article images", "Series", timestamp.AddMinutes(1));
        var item = series.AddItem("Opening", "Opening visual", timestamp.AddMinutes(2));
        await repository.SaveAsync(project, CancellationToken.None);

        var brief = await service.CreateCreativeBriefAsync(
            project.Id,
            series.Id,
            "article illustration",
            "teachers",
            ImageTextPolicy.DeterministicPostRender,
            "clean editorial",
            ["accurate visual"],
            ["small fake text"],
            timestamp.AddMinutes(3),
            CancellationToken.None);

        var planned = await service.CreatePromptDirectionsAsync(
            project.Id,
            brief.Id,
            timestamp.AddMinutes(4),
            CancellationToken.None);

        var promoted = await service.PromotePromptDirectionAsync(
            project.Id,
            item.Id,
            brief.Id,
            "conservative",
            new GenerationSettings(1024, 1024, "standard", "png"),
            timestamp.AddMinutes(5),
            CancellationToken.None);

        var loaded = await repository.LoadAsync(project.Id, CancellationToken.None);
        var loadedBrief = loaded!.Series.Single().CreativeBriefs.Single();
        var loadedPrompt = loaded.Series.Single().Items.Single().PromptVersions.Single();
        var loadedDirection = loadedBrief.PromptDirections.Single(direction => direction.Key == "conservative");

        Assert.Equal(brief.Id, loadedBrief.Id);
        Assert.Equal(3, planned.PromptDirections.Count);
        Assert.NotNull(loadedDirection.Recommendation);
        Assert.Equal(ImageTypePresetCatalog.ArticleInlineIllustration, loadedDirection.Recommendation.ImageTypePresetId);
        Assert.Equal(1536, loadedDirection.Recommendation.Width);
        Assert.Equal(1024, loadedDirection.Recommendation.Height);
        Assert.Equal(ReviewRubricTemplateCatalog.EditorialIllustration, loadedDirection.Recommendation.ReviewRubricTemplateId);
        Assert.Equal(promoted.Id, loadedPrompt.Id);
        Assert.Contains("article illustration", loadedPrompt.PromptText);
    }

    [Fact]
    public async Task BriefWorkflowApplicationService_CreatesAndPromotesDesignBlueprints()
    {
        var repository = new InMemoryProjectRepository();
        var service = new BriefWorkflowApplicationService(repository, new FakeTextPlanningProvider());
        var timestamp = DateTimeOffset.Parse("2026-06-07T10:00:00Z");
        var project = ImageProject.Create("Blueprint workflow demo", timestamp);
        var series = project.AddSeries("Narrative images", "Series", timestamp.AddMinutes(1));
        await repository.SaveAsync(project, CancellationToken.None);

        var brief = await service.CreateCreativeBriefAsync(
            project.Id,
            series.Id,
            "panel story sequence",
            "students",
            ImageTextPolicy.DeterministicPostRender,
            "clear visual storytelling",
            ["same main character"],
            ["wall of unreadable text"],
            timestamp.AddMinutes(2),
            CancellationToken.None);

        var planned = await service.CreateDesignBlueprintsAsync(
            project.Id,
            brief.Id,
            timestamp.AddMinutes(3),
            CancellationToken.None);

        var firstBlueprint = planned.DesignBlueprints.First();
        var promoted = await service.PromoteDesignBlueprintAsync(
            project.Id,
            brief.Id,
            firstBlueprint.Id,
            timestamp.AddMinutes(4),
            CancellationToken.None);

        var loaded = await repository.LoadAsync(project.Id, CancellationToken.None);
        var loadedBrief = loaded!.Series.Single().CreativeBriefs.Single();

        Assert.Equal(3, planned.DesignBlueprints.Count);
        Assert.Equal("panel-narrative-sequence", firstBlueprint.Key);
        Assert.Equal(promoted.Id, loadedBrief.PromotedBlueprintId);
        Assert.Equal(promoted.Id, loadedBrief.DesignBlueprints.First().Id);
    }

    [Fact]
    public async Task BriefWorkflowApplicationService_RejectsOversizedPromptDirectionRequestBeforeProviderDispatch()
    {
        var repository = new InMemoryProjectRepository();
        var provider = new CountingTextPlanningProvider();
        var service = new BriefWorkflowApplicationService(repository, provider);
        var timestamp = DateTimeOffset.Parse("2026-06-08T11:00:00Z");
        var project = ImageProject.Create("Oversized brief workflow demo", timestamp);
        var series = project.AddSeries("Article images", "Series", timestamp.AddMinutes(1));
        var oversizedGoal = new string('A', TextPlanningExecutionPolicy.DefaultMaxInputCharacters + 100);
        var brief = series.AddCreativeBrief(
            oversizedGoal,
            "teachers",
            ImageTextPolicy.DeterministicPostRender,
            "clean editorial",
            ["accurate visual"],
            ["small fake text"],
            timestamp.AddMinutes(2));
        await repository.SaveAsync(project, CancellationToken.None);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.CreatePromptDirectionsAsync(
                project.Id,
                brief.Id,
                timestamp.AddMinutes(3),
                CancellationToken.None));

        Assert.Contains("bounded", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("trim", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(0, provider.PromptDirectionCallCount);
    }

    [Fact]
    public async Task BriefWorkflowApplicationService_RejectsOversizedBlueprintRequestBeforeProviderDispatch()
    {
        var repository = new InMemoryProjectRepository();
        var provider = new CountingTextPlanningProvider();
        var service = new BriefWorkflowApplicationService(repository, provider);
        var timestamp = DateTimeOffset.Parse("2026-06-08T11:10:00Z");
        var project = ImageProject.Create("Oversized blueprint workflow demo", timestamp);
        var series = project.AddSeries("Narrative images", "Series", timestamp.AddMinutes(1));
        var oversizedGoal = new string('A', TextPlanningExecutionPolicy.DefaultMaxInputCharacters + 100);
        var brief = series.AddCreativeBrief(
            oversizedGoal,
            "students",
            ImageTextPolicy.DeterministicPostRender,
            "clear visual storytelling",
            ["same main character"],
            ["wall of unreadable text"],
            timestamp.AddMinutes(2));
        await repository.SaveAsync(project, CancellationToken.None);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.CreateDesignBlueprintsAsync(
                project.Id,
                brief.Id,
                timestamp.AddMinutes(3),
                CancellationToken.None));

        Assert.Contains("bounded", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("trim", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(0, provider.BlueprintCallCount);
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

    private sealed class CountingTextPlanningProvider : ITextPlanningProvider
    {
        public int PromptDirectionCallCount { get; private set; }

        public int BlueprintCallCount { get; private set; }

        public IProviderCapabilities Capabilities { get; } = new ProviderCapabilities(
            "counting-text-provider",
            "Counting text provider",
            ["fake-text"],
            SupportsTextPlanning: true,
            SupportsImageGeneration: false,
            SupportsVisionReview: false,
            SupportsImageEditing: false,
            SupportsStreaming: false);

        public Task<SeriesPlanResult> CreatePlanAsync(PlanningRequest request, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public Task<BriefPlanningResult> CreatePromptDirectionsAsync(
            BriefPlanningRequest request,
            CancellationToken cancellationToken)
        {
            PromptDirectionCallCount++;
            return Task.FromResult(
                new BriefPlanningResult(
                    [],
                    [],
                    [],
                    "counting-brief"));
        }

        public Task<BlueprintPlanningResult> CreateDesignBlueprintsAsync(
            BlueprintPlanningRequest request,
            CancellationToken cancellationToken)
        {
            BlueprintCallCount++;
            return Task.FromResult(
                new BlueprintPlanningResult(
                    [],
                    [],
                    [],
                    "counting-blueprint"));
        }

        public Task<DocumentIllustrationPlanningResult> CreateDocumentIllustrationPlanAsync(
            DocumentIllustrationPlanningRequest request,
            CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }
    }
}
