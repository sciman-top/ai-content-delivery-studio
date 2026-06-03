using ImageSeriesStudio.Application.Projects;
using ImageSeriesStudio.Application.RepairRouting;
using ImageSeriesStudio.Core.Projects;

namespace ImageSeriesStudio.Tests;

public sealed class RoutedRepairApplicationTests
{
    [Fact]
    public async Task CreateRoutedRepairPatch_PersistsPatchThroughProjectApplicationFacade()
    {
        var timestamp = DateTimeOffset.Parse("2026-06-03T22:00:00Z");
        var repository = new InMemoryProjectRepository();
        var service = new ProjectApplicationService(repository);
        var project = await service.CreateProjectAsync("Facade repair patch demo", timestamp, CancellationToken.None);
        var repairPlan = new RepairPlan(
            Guid.NewGuid(),
            Guid.NewGuid(),
            RepairSeverity.Major,
            [
                RepairPlanStep.Create(
                    1,
                    ReviewOutcomeTargetLayer.Blueprint,
                    RepairSeverity.Major,
                    ["Blueprint route needs stronger consistency."],
                    ["Add a recurring subject consistency rule."],
                    requiresOperator: false),
            ],
            timestamp.AddMinutes(1));

        var patch = await service.CreateRoutedRepairPatchAsync(
            new RoutedRepairPatchRequest(project.Id, repairPlan, timestamp.AddMinutes(2)),
            CancellationToken.None);

        var loaded = await service.LoadProjectAsync(project.Id, CancellationToken.None);
        var stored = Assert.Single(loaded!.RoutedRepairPatches);

        Assert.Equal(patch.Id, stored.Id);
        Assert.Equal(project.Id, stored.ProjectId);
        Assert.Equal(ReviewOutcomeTargetLayer.Blueprint, Assert.Single(stored.Items).TargetLayer);
    }

    [Fact]
    public async Task ApplyRoutedRepair_CreatesNewPromptVersionWithPromptAndSettingsRepairs()
    {
        var timestamp = DateTimeOffset.Parse("2026-06-03T19:00:00Z");
        var repository = new InMemoryProjectRepository();
        var service = new ProjectApplicationService(repository);
        var project = await service.CreateProjectAsync("Repair application demo", timestamp, CancellationToken.None);
        var series = await service.AddSeriesAsync(project.Id, "Series", "Brief", timestamp.AddMinutes(1), CancellationToken.None);
        var item = await service.AddItemAsync(project.Id, series.Id, "Opening", "Opening brief", timestamp.AddMinutes(2), CancellationToken.None);
        var original = await service.AddPromptVersionAsync(
            project.Id,
            item.Id,
            "Original prompt.",
            new GenerationSettings(1024, 1024, "standard", "png"),
            providerProfileId: null,
            timestamp.AddMinutes(3),
            CancellationToken.None);
        var repairPlan = new RepairPlan(
            Guid.NewGuid(),
            Guid.NewGuid(),
            RepairSeverity.Major,
            [
                RepairPlanStep.Create(
                    1,
                    ReviewOutcomeTargetLayer.Prompt,
                    RepairSeverity.Major,
                    ["Suggested fix: strengthen prompt wording."],
                    ["Revise prompt wording and regenerate the candidate."],
                    requiresOperator: false),
                RepairPlanStep.Create(
                    2,
                    ReviewOutcomeTargetLayer.Settings,
                    RepairSeverity.Major,
                    ["Low score: settings=2 (Aspect ratio should match delivery.)"],
                    ["Adjust generation settings or recipe parameters before regenerating."],
                    requiresOperator: false),
            ],
            timestamp.AddMinutes(4));

        var result = await service.ApplyRoutedRepairAsync(
            new RoutedRepairApplicationRequest(
                project.Id,
                item.Id,
                original.Id,
                repairPlan,
                "Revised prompt with stronger composition and aspect-ratio guidance.",
                new GenerationSettings(1536, 1024, "draft", "png"),
                timestamp.AddMinutes(5)),
            CancellationToken.None);

        var loaded = await service.LoadProjectAsync(project.Id, CancellationToken.None);
        var prompts = loaded!.Series.Single().Items.Single().PromptVersions.OrderBy(prompt => prompt.VersionNumber).ToArray();

        Assert.Equal(2, result.PromptVersion.VersionNumber);
        Assert.Equal([ReviewOutcomeTargetLayer.Prompt, ReviewOutcomeTargetLayer.Settings], result.AppliedLayers);
        Assert.Equal(2, prompts.Length);
        Assert.Contains("Revised prompt with stronger composition", prompts[1].PromptText);
        Assert.Contains("Applied repair routes:", prompts[1].PromptText);
        Assert.Equal(1536, prompts[1].Settings.Width);
        Assert.Equal(1024, prompts[1].Settings.Height);
        Assert.Equal("draft", prompts[1].Settings.Quality);
    }

    [Fact]
    public async Task ApplyRoutedRepair_RejectsPlansWithoutPromptOrSettingsRoutes()
    {
        var timestamp = DateTimeOffset.Parse("2026-06-03T19:00:00Z");
        var repository = new InMemoryProjectRepository();
        var service = new ProjectApplicationService(repository);
        var project = await service.CreateProjectAsync("Repair rejection demo", timestamp, CancellationToken.None);
        var series = await service.AddSeriesAsync(project.Id, "Series", "Brief", timestamp.AddMinutes(1), CancellationToken.None);
        var item = await service.AddItemAsync(project.Id, series.Id, "Opening", "Opening brief", timestamp.AddMinutes(2), CancellationToken.None);
        var original = await service.AddPromptVersionAsync(
            project.Id,
            item.Id,
            "Original prompt.",
            new GenerationSettings(1024, 1024, "standard", "png"),
            providerProfileId: null,
            timestamp.AddMinutes(3),
            CancellationToken.None);
        var repairPlan = new RepairPlan(
            Guid.NewGuid(),
            Guid.NewGuid(),
            RepairSeverity.Minor,
            [
                RepairPlanStep.Create(
                    1,
                    ReviewOutcomeTargetLayer.Brief,
                    RepairSeverity.Minor,
                    ["Missing requirement from the creative brief."],
                    ["Clarify the creative brief requirement."],
                    requiresOperator: false),
            ],
            timestamp.AddMinutes(4));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.ApplyRoutedRepairAsync(
                new RoutedRepairApplicationRequest(
                    project.Id,
                    item.Id,
                    original.Id,
                    repairPlan,
                    "Revised prompt.",
                    null,
                    timestamp.AddMinutes(5)),
                CancellationToken.None));
    }

    private sealed class InMemoryProjectRepository : IProjectRepository
    {
        private readonly Dictionary<Guid, ImageProject> _projects = new();

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
            IReadOnlyList<ProjectSummary> summaries = _projects.Values
                .Select(project => new ProjectSummary(project.Id, project.Name, project.CreatedAt, project.UpdatedAt))
                .ToArray();
            return Task.FromResult(summaries);
        }
    }
}
