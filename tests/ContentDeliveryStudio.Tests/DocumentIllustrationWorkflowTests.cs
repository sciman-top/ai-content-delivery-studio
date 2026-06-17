using ContentDeliveryStudio.Application.Projects;
using ContentDeliveryStudio.Core.Documents;
using ContentDeliveryStudio.Core.Projects;
using ContentDeliveryStudio.Core.Providers;
using ContentDeliveryStudio.Infrastructure.Fakes;

namespace ContentDeliveryStudio.Tests;

public sealed class DocumentIllustrationWorkflowTests
{
    [Fact]
    public async Task CreateDocumentIllustrationPlanWithProvider_AddsApprovedTargetsToProject()
    {
        var repository = new InMemoryProjectRepository();
        var projectService = new ProjectApplicationService(repository);
        var service = new DocumentIllustrationApplicationService(repository, new FakeTextPlanningProvider());
        var timestamp = new DateTimeOffset(2026, 6, 2, 9, 0, 0, TimeSpan.Zero);
        var project = await projectService.CreateProjectAsync("Document illustration demo", timestamp, CancellationToken.None);
        var request = CreateRequest();

        var result = await service.CreateDocumentIllustrationPlanWithProviderAsync(
            project.Id,
            request,
            approveAllTargets: true,
            timestamp.AddMinutes(1),
            CancellationToken.None);

        Assert.NotEqual(Guid.Empty, result.DocumentBriefId);
        Assert.NotEqual(Guid.Empty, result.IllustrationPlanId);
        Assert.True(result.SeriesId.HasValue);
        Assert.NotEqual(Guid.Empty, result.SeriesId.Value);
        Assert.True(result.ApprovedTargetCount > 0);

        var loaded = await repository.LoadAsync(project.Id, CancellationToken.None);
        var brief = Assert.Single(loaded!.DocumentBriefs);
        var plan = Assert.Single(loaded.IllustrationPlans);
        var approvedTargets = plan.ApprovedTargets;
        var series = Assert.Single(loaded.Series);

        Assert.Equal(project.Id, brief.ProjectId);
        Assert.Equal(project.Id, plan.ProjectId);
        Assert.Equal(brief.Id, plan.DocumentBriefId);
        Assert.Equal(brief.Id, result.DocumentBriefId);
        Assert.Equal(plan.Id, result.IllustrationPlanId);
        Assert.NotEmpty(approvedTargets);
        Assert.Equal(plan.Targets.Count, approvedTargets.Count);
        Assert.Equal(approvedTargets.Count, result.ApprovedTargetCount);

        Assert.Contains("Document illustrations", series.Title);
        Assert.NotEmpty(series.Items);
        Assert.All(series.Items, item =>
        {
            var prompt = Assert.Single(item.PromptVersions);
            var hasSourceEvidence =
                item.Brief.Contains("Source evidence", StringComparison.OrdinalIgnoreCase)
                || prompt.PromptText.Contains("source evidence", StringComparison.OrdinalIgnoreCase);

            Assert.True(hasSourceEvidence);
            Assert.Contains(
                "Do not imply real experimental, clinical, archival, or field evidence unless the user provided that evidence explicitly.",
                prompt.PromptText);
        });
    }

    [Fact]
    public async Task CreateDocumentIllustrationPlanWithProvider_StoresPlanWithoutSeriesWhenTargetsRemainDraft()
    {
        var repository = new InMemoryProjectRepository();
        var service = new ProjectApplicationService(repository, new FakeTextPlanningProvider());
        var timestamp = new DateTimeOffset(2026, 6, 2, 9, 0, 0, TimeSpan.Zero);
        var project = await service.CreateProjectAsync("Document illustration demo", timestamp, CancellationToken.None);
        var request = CreateRequest();

        var result = await service.CreateDocumentIllustrationPlanWithProviderAsync(
            project.Id,
            request,
            approveAllTargets: false,
            timestamp.AddMinutes(1),
            CancellationToken.None);

        Assert.NotEqual(Guid.Empty, result.DocumentBriefId);
        Assert.NotEqual(Guid.Empty, result.IllustrationPlanId);
        Assert.Null(result.SeriesId);
        Assert.Equal(0, result.ApprovedTargetCount);

        var loaded = await repository.LoadAsync(project.Id, CancellationToken.None);
        var brief = Assert.Single(loaded!.DocumentBriefs);
        var plan = Assert.Single(loaded.IllustrationPlans);

        Assert.Equal(project.Id, brief.ProjectId);
        Assert.Equal(project.Id, plan.ProjectId);
        Assert.Equal(brief.Id, plan.DocumentBriefId);
        Assert.Equal(brief.Id, result.DocumentBriefId);
        Assert.Equal(plan.Id, result.IllustrationPlanId);
        Assert.NotEmpty(plan.Targets);
        Assert.Empty(plan.ApprovedTargets);
        Assert.All(plan.Targets, target => Assert.Equal(IllustrationTargetApprovalState.Draft, target.ApprovalState));
        Assert.Empty(loaded.Series);
    }

    [Fact]
    public async Task CreateDocumentIllustrationPlanWithProvider_RejectsOversizedSourceBeforeProviderCall()
    {
        var repository = new InMemoryProjectRepository();
        var service = new DocumentIllustrationApplicationService(repository, new FakeTextPlanningProvider());
        var timestamp = new DateTimeOffset(2026, 6, 8, 9, 0, 0, TimeSpan.Zero);
        var project = ImageProject.Create("Oversized document demo", timestamp);
        await repository.SaveAsync(project, CancellationToken.None);

        var oversizedSource = new string('A', DocumentIllustrationExecutionPolicy.DefaultMaxSourceTextCharacters + 100);
        var request = new DocumentIllustrationPlanningRequest(
            "Quantum teaching note",
            oversizedSource,
            "teachers",
            DocumentFamily.Educational,
            IllustrationStrictnessLevel.Educational,
            ["Introduction"],
            ["Key claim"],
            ["avoid fake lab data"]);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.CreateDocumentIllustrationPlanWithProviderAsync(
                project.Id,
                request,
                approveAllTargets: false,
                timestamp.AddMinutes(1),
                CancellationToken.None));

        Assert.Contains("bounded", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("summarize", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CreateDocumentIllustrationPlanWithProvider_RejectsOversizedEvidenceSelectionBeforeProviderCall()
    {
        var repository = new InMemoryProjectRepository();
        var service = new DocumentIllustrationApplicationService(repository, new FakeTextPlanningProvider());
        var timestamp = new DateTimeOffset(2026, 6, 8, 9, 10, 0, TimeSpan.Zero);
        var project = ImageProject.Create("Oversized evidence demo", timestamp);
        await repository.SaveAsync(project, CancellationToken.None);

        var request = new DocumentIllustrationPlanningRequest(
            "Quantum teaching note",
            "Teachers need an intuitive explanation of superposition.",
            "teachers",
            DocumentFamily.Educational,
            IllustrationStrictnessLevel.Educational,
            Enumerable.Range(1, 6).Select(index => $"Section {index}").ToArray(),
            Enumerable.Range(1, 4).Select(index => $"Key claim {index}").ToArray(),
            Enumerable.Range(1, 4).Select(index => $"Constraint {index}").ToArray());

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.CreateDocumentIllustrationPlanWithProviderAsync(
                project.Id,
                request,
                approveAllTargets: false,
                timestamp.AddMinutes(1),
                CancellationToken.None));

        Assert.Contains("evidence rows", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("smaller source-evidence subset", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    private static DocumentIllustrationPlanningRequest CreateRequest()
    {
        return new DocumentIllustrationPlanningRequest(
            "Quantum teaching note",
            "Teachers need an intuitive explanation of superposition.",
            "teachers",
            DocumentFamily.Educational,
            IllustrationStrictnessLevel.Educational,
            ["Introduction", "Classroom analogy"],
            ["Superposition needs a visual analogy."],
            ["avoid fake lab data"]);
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
