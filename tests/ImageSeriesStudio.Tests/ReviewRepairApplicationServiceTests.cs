using ImageSeriesStudio.Application.Projects;
using ImageSeriesStudio.Application.RepairRouting;
using ImageSeriesStudio.Core.Projects;

namespace ImageSeriesStudio.Tests;

public sealed class ReviewRepairApplicationServiceTests
{
    [Fact]
    public async Task CreateRoutedRepairPatchAsync_PersistsPatchOnProject()
    {
        var timestamp = DateTimeOffset.Parse("2026-06-03T21:00:00Z");
        var repository = new InMemoryProjectRepository();
        var projectService = new ProjectApplicationService(repository);
        var repairService = new ReviewRepairApplicationService(repository);
        var project = await projectService.CreateProjectAsync("Repair patch persistence demo", timestamp, CancellationToken.None);
        var repairPlan = new RepairPlan(
            Guid.NewGuid(),
            Guid.NewGuid(),
            RepairSeverity.Major,
            [
                RepairPlanStep.Create(
                    1,
                    ReviewOutcomeTargetLayer.Brief,
                    RepairSeverity.Major,
                    ["Missing factual requirement."],
                    ["Add the requirement to the creative brief before regenerating."],
                    requiresOperator: false),
            ],
            timestamp.AddMinutes(1));

        var patch = await repairService.CreateRoutedRepairPatchAsync(
            new RoutedRepairPatchRequest(project.Id, repairPlan, timestamp.AddMinutes(2)),
            CancellationToken.None);

        var loaded = await projectService.LoadProjectAsync(project.Id, CancellationToken.None);
        var stored = Assert.Single(loaded!.RoutedRepairPatches);

        Assert.Equal(project.Id, patch.ProjectId);
        Assert.Equal(patch.Id, stored.Id);
        Assert.Equal(ReviewOutcomeTargetLayer.Brief, Assert.Single(stored.Items).TargetLayer);
    }

    [Fact]
    public void CreateRoutedRepairPatch_CapturesBriefAndBlueprintRoutesAsHumanApprovedProposals()
    {
        var timestamp = DateTimeOffset.Parse("2026-06-03T20:30:00Z");
        var repository = new InMemoryProjectRepository();
        var repairService = new ReviewRepairApplicationService(repository);
        var repairPlan = new RepairPlan(
            Guid.NewGuid(),
            Guid.NewGuid(),
            RepairSeverity.Major,
            [
                RepairPlanStep.Create(
                    1,
                    ReviewOutcomeTargetLayer.Brief,
                    RepairSeverity.Major,
                    ["Missing requirement from the brief."],
                    ["Clarify the must-include factual constraint."],
                    requiresOperator: false),
                RepairPlanStep.Create(
                    2,
                    ReviewOutcomeTargetLayer.Blueprint,
                    RepairSeverity.Major,
                    ["Style drift across series items."],
                    ["Add a stronger consistency rule to the promoted blueprint."],
                    requiresOperator: false),
                RepairPlanStep.Create(
                    3,
                    ReviewOutcomeTargetLayer.Prompt,
                    RepairSeverity.Minor,
                    ["Prompt wording is too vague."],
                    ["Tighten the prompt wording."],
                    requiresOperator: false),
            ],
            timestamp);

        var patch = repairService.CreateRoutedRepairPatch(repairPlan, timestamp.AddMinutes(1));

        Assert.Equal(repairPlan.Id, patch.RepairPlanId);
        Assert.Equal(repairPlan.CandidateImageId, patch.CandidateImageId);
        Assert.Equal([ReviewOutcomeTargetLayer.Brief, ReviewOutcomeTargetLayer.Blueprint], patch.Items.Select(item => item.TargetLayer));
        Assert.All(patch.Items, item => Assert.True(item.RequiresHumanApproval));
        Assert.Contains("Clarify the must-include", patch.Items[0].ProposedChanges.Single());
        Assert.Contains("consistency rule", patch.Items[1].ProposedChanges.Single());
    }

    [Fact]
    public async Task ApplyRoutedRepair_CreatesVersionedPromptRepairInReviewRepairModule()
    {
        var timestamp = DateTimeOffset.Parse("2026-06-03T20:00:00Z");
        var repository = new InMemoryProjectRepository();
        var projectService = new ProjectApplicationService(repository);
        var repairService = new ReviewRepairApplicationService(repository);
        var project = await projectService.CreateProjectAsync("Review repair module demo", timestamp, CancellationToken.None);
        var series = await projectService.AddSeriesAsync(project.Id, "Series", "Brief", timestamp.AddMinutes(1), CancellationToken.None);
        var item = await projectService.AddItemAsync(project.Id, series.Id, "Opening", "Opening brief", timestamp.AddMinutes(2), CancellationToken.None);
        var original = await projectService.AddPromptVersionAsync(
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
                    ["Suggested fix: improve subject placement."],
                    ["Revise prompt wording before regeneration."],
                    requiresOperator: false),
                RepairPlanStep.Create(
                    2,
                    ReviewOutcomeTargetLayer.Settings,
                    RepairSeverity.Major,
                    ["Low score: settings=2 (Resolution mismatch.)"],
                    ["Adjust generation settings before regeneration."],
                    requiresOperator: false),
            ],
            timestamp.AddMinutes(4));

        var result = await repairService.ApplyRoutedRepairAsync(
            new RoutedRepairApplicationRequest(
                project.Id,
                item.Id,
                original.Id,
                repairPlan,
                "Revised prompt from the review repair module.",
                new GenerationSettings(1536, 1024, "draft", "png"),
                timestamp.AddMinutes(5)),
            CancellationToken.None);

        var loaded = await projectService.LoadProjectAsync(project.Id, CancellationToken.None);
        var prompts = loaded!.Series.Single().Items.Single().PromptVersions.OrderBy(prompt => prompt.VersionNumber).ToArray();

        Assert.Equal(2, result.PromptVersion.VersionNumber);
        Assert.Equal([ReviewOutcomeTargetLayer.Prompt, ReviewOutcomeTargetLayer.Settings], result.AppliedLayers);
        Assert.Contains("Applied repair routes:", prompts[1].PromptText);
        Assert.Equal(original.ProviderProfileId, prompts[1].ProviderProfileId);
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
