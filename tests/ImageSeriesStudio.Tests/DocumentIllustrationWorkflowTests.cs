using ImageSeriesStudio.Application.Projects;
using ImageSeriesStudio.Core.Documents;
using ImageSeriesStudio.Core.Projects;
using ImageSeriesStudio.Core.Providers;
using ImageSeriesStudio.Infrastructure.Fakes;

namespace ImageSeriesStudio.Tests;

public sealed class DocumentIllustrationWorkflowTests
{
    [Fact]
    public async Task CreateDocumentIllustrationPlanWithProvider_CreatesSeriesItemsAndPromptsFromApprovedTargets()
    {
        var repository = new InMemoryProjectRepository();
        var service = new ProjectApplicationService(repository, new FakeTextPlanningProvider());
        var timestamp = new DateTimeOffset(2026, 6, 2, 9, 0, 0, TimeSpan.Zero);
        var project = await service.CreateProjectAsync("Document illustration demo", timestamp, CancellationToken.None);
        var request = new DocumentIllustrationPlanningRequest(
            "Quantum teaching note",
            "Teachers need an intuitive explanation of superposition.",
            "teachers",
            DocumentFamily.Educational,
            IllustrationStrictnessLevel.Educational,
            ["Introduction", "Classroom analogy"],
            ["Superposition needs a visual analogy."],
            ["avoid fake lab data"]);

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
        var series = Assert.Single(loaded!.Series);

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
    }
}
