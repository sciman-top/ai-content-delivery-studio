using System.Globalization;
using ContentDeliveryStudio.Application.Localization;
using ContentDeliveryStudio.Application.Projects;
using ContentDeliveryStudio.App.ViewModels;
using ContentDeliveryStudio.Core.Documents;
using ContentDeliveryStudio.Core.Projects;
using ContentDeliveryStudio.Infrastructure.Fakes;

namespace ContentDeliveryStudio.Tests;

public sealed class PlanningWorkflowCoordinatorTests
{
    [Fact]
    public async Task RunFakePlanningAsync_CreatesSeriesPlanAndReturnsSeriesId()
    {
        var repository = new InMemoryProjectRepository();
        var projectService = new ProjectApplicationService(repository, new FakeTextPlanningProvider());
        var coordinator = new PlanningWorkflowCoordinator(
            projectService,
            new LocalizationService(() => new CultureInfo("en-US")));
        var timestamp = DateTimeOffset.Parse("2026-06-09T10:00:00Z");
        var project = await projectService.CreateProjectAsync("Planning coordinator demo", timestamp, CancellationToken.None);

        var result = await coordinator.RunFakePlanningAsync(
            project.Id,
            "three article illustrations",
            "content authors",
            3,
            "clean editorial style",
            CancellationToken.None);

        var loaded = await projectService.LoadProjectAsync(project.Id, CancellationToken.None);
        var series = Assert.Single(loaded!.Series);

        Assert.Equal(series.Id, result.SeriesId);
        Assert.Equal(3, series.Items.Count);
        Assert.All(series.Items, item => Assert.Single(item.PromptVersions));
    }

    [Fact]
    public async Task RunFakeDocumentPlanningAsync_ReturnsSummaryAndSeriesId()
    {
        var repository = new InMemoryProjectRepository();
        var projectService = new ProjectApplicationService(repository, new FakeTextPlanningProvider());
        var coordinator = new PlanningWorkflowCoordinator(
            projectService,
            new LocalizationService(() => new CultureInfo("en-US")));
        var timestamp = DateTimeOffset.Parse("2026-06-09T11:00:00Z");
        var project = await projectService.CreateProjectAsync("Document planning demo", timestamp, CancellationToken.None);

        var result = await coordinator.RunFakeDocumentPlanningAsync(
            project.Id,
            project.Name,
            "Teachers need a clean visual explanation of superposition.",
            "physics teachers",
            IllustrationStrictnessLevel.ScholarlyDraft,
            defaultDocumentAudience: "teachers",
            CancellationToken.None);

        var loaded = await projectService.LoadProjectAsync(project.Id, CancellationToken.None);

        Assert.NotNull(result.SeriesId);
        Assert.Contains("Approved targets:", result.ResultSummary);
        Assert.Single(loaded!.DocumentBriefs);
        Assert.Single(loaded.IllustrationPlans);
        Assert.Single(loaded.Series);
    }
}
