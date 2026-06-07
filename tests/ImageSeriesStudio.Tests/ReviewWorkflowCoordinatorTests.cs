using System.Globalization;
using ImageSeriesStudio.Application.Localization;
using ImageSeriesStudio.Application.Projects;
using ImageSeriesStudio.App.ViewModels;
using ImageSeriesStudio.Core.Projects;
using ImageSeriesStudio.Core.Providers;
using ImageSeriesStudio.Infrastructure.Fakes;

namespace ImageSeriesStudio.Tests;

public sealed class ReviewWorkflowCoordinatorTests
{
    [Fact]
    public async Task RunFakeReviewAsync_BuildsReviewRowsWithRepairRouteSummary()
    {
        var repository = new InMemoryProjectRepository();
        var projectService = new ProjectApplicationService(
            repository,
            textPlanningProvider: null,
            imageGenerationProvider: null,
            visionReviewProvider: new FakeVisionReviewProvider(defaultPasses: false));
        var localizationService = new LocalizationService(() => new CultureInfo("en-US"));
        var coordinator = new ReviewWorkflowCoordinator(projectService, localizationService);
        var timestamp = DateTimeOffset.Parse("2026-06-08T10:00:00Z");
        var project = await projectService.CreateProjectAsync("Review coordinator demo", timestamp, CancellationToken.None);

        var reviewRows = await coordinator.RunFakeReviewAsync(
            project.Id,
            [
                new GalleryRowViewModel(
                    Guid.NewGuid(),
                    Guid.NewGuid(),
                    "Opening frame",
                    Path.Combine(Path.GetTempPath(), "candidate.png"),
                    Path.Combine(Path.GetTempPath(), "candidate.json"),
                    "Create a frame that will be routed for repair."),
            ],
            CancellationToken.None);

        var reviewRow = Assert.Single(reviewRows);
        Assert.Equal("Fail", reviewRow.Decision);
        Assert.Contains("Brief", reviewRow.RouteSummary);
        Assert.Contains("Regenerate", reviewRow.RouteSummary);
        Assert.Equal(localizationService.GetText(LocalizationKey.HumanApprovalPending), reviewRow.HumanApprovalStatus);
    }

    [Fact]
    public async Task ApplyFinalApprovalAsync_UpdatesHumanApprovalStateAndPersistsReview()
    {
        var repository = new InMemoryProjectRepository();
        var projectService = new ProjectApplicationService(repository);
        var localizationService = new LocalizationService(() => new CultureInfo("en-US"));
        var coordinator = new ReviewWorkflowCoordinator(projectService, localizationService);
        var timestamp = DateTimeOffset.Parse("2026-06-08T11:00:00Z");
        var project = await projectService.CreateProjectAsync("Approval coordinator demo", timestamp, CancellationToken.None);
        var candidateId = Guid.NewGuid();
        var row = new ReviewRowViewModel(
            candidateId,
            "Opening frame",
            ReviewDecision.Pass.ToString(),
            "match:5",
            "Ready for approval.",
            string.Empty,
            ReviewOutcomeTargetLayer.None.ToString(),
            HumanApproved: false,
            localizationService.GetText(LocalizationKey.HumanApprovalPending),
            string.Empty,
            string.Empty,
            null,
            new StructuredReviewOutput(
                candidateId,
                ReviewDecision.Pass,
                [new StructuredReviewScore("match", "Matches the brief.", 3, 5)],
                [],
                "Ready for approval.",
                SuggestedFix: null));

        var updated = await coordinator.ApplyFinalApprovalAsync(
            project.Id,
            row,
            approve: true,
            reviewer: "Teacher",
            notes: "Looks ready.",
            CancellationToken.None);

        Assert.True(updated.HumanApproved);
        Assert.Equal("Teacher", updated.FinalReviewer);
        Assert.Equal("Looks ready.", updated.FinalApprovalNotes);
        Assert.Equal(localizationService.GetText(LocalizationKey.HumanApprovalApproved), updated.HumanApprovalStatus);
        Assert.NotNull(repository.SavedReviewResult);
        Assert.Equal("Teacher", repository.SavedReviewResult!.FinalReviewer);
        Assert.Equal("Looks ready.", repository.SavedReviewResult.FinalApprovalNotes);
    }

    private sealed class InMemoryProjectRepository : IProjectRepository
    {
        private readonly Dictionary<Guid, ImageProject> _projects = [];

        public ReviewResult? SavedReviewResult { get; private set; }

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
            SavedReviewResult = reviewResult;
            return Task.CompletedTask;
        }

        public Task<ReviewResult?> LoadLatestReviewResultAsync(Guid candidateImageId, CancellationToken cancellationToken)
        {
            return Task.FromResult<ReviewResult?>(null);
        }
    }
}
