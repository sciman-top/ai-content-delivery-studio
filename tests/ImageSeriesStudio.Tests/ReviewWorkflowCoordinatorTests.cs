using System.Globalization;
using System.Text.Json;
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
        using var localStudioRoot = LocalStudioDataPathScope.Create();
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
    public async Task RunFakeReviewAsync_WritesLocalReviewPrepManifest()
    {
        using var localStudioRoot = LocalStudioDataPathScope.Create();
        var repository = new InMemoryProjectRepository();
        var capturingProvider = new CapturingVisionReviewProvider();
        var projectService = new ProjectApplicationService(
            repository,
            textPlanningProvider: null,
            imageGenerationProvider: null,
            visionReviewProvider: capturingProvider);
        var localizationService = new LocalizationService(() => new CultureInfo("en-US"));
        var coordinator = new ReviewWorkflowCoordinator(projectService, localizationService);
        var timestamp = DateTimeOffset.Parse("2026-06-09T10:00:00Z");
        var project = await projectService.CreateProjectAsync("Review manifest demo", timestamp, CancellationToken.None);

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

        Assert.Single(reviewRows);
        Assert.NotNull(capturingProvider.LastRequest?.ReviewPrep);
        Assert.False(string.IsNullOrWhiteSpace(capturingProvider.LastRequest!.ReviewPrep!.ManifestPath));
        Assert.True(File.Exists(capturingProvider.LastRequest.ReviewPrep.ManifestPath));

        var manifestJson = await File.ReadAllTextAsync(capturingProvider.LastRequest.ReviewPrep.ManifestPath, CancellationToken.None);
        var manifest = JsonSerializer.Deserialize<ReviewPrepArtifactManifest>(
            manifestJson,
            new JsonSerializerOptions(JsonSerializerDefaults.Web));

        Assert.NotNull(manifest);
        Assert.Equal("Opening frame", manifest.ItemTitle);
        Assert.Equal("Create a frame that will be routed for repair.", manifest.PromptText);
        Assert.Equal(Path.GetTempPath() + "candidate.png", manifest.AssetPath);
        Assert.Equal(Path.GetTempPath() + "candidate.json", manifest.MetadataPath);
        Assert.Collection(
            manifest.EvidenceSelections,
            selection =>
            {
                Assert.Equal("candidate-image", selection.Role);
                Assert.Equal("generated-asset", selection.SourceKind);
                Assert.Equal(Path.GetTempPath() + "candidate.png", selection.LocalPath);
            },
            selection =>
            {
                Assert.Equal("candidate-metadata", selection.Role);
                Assert.Equal("generation-metadata", selection.SourceKind);
                Assert.Equal(Path.GetTempPath() + "candidate.json", selection.LocalPath);
            },
            selection =>
            {
                Assert.Equal("prompt-summary", selection.Role);
                Assert.Equal("prompt-text", selection.SourceKind);
                Assert.Null(selection.LocalPath);
            });
        Assert.Equal(3, capturingProvider.LastRequest.ReviewPrep.EvidenceSelections.Count);
    }

    [Fact]
    public async Task RunFakeReviewAsync_TrimsCompactReviewPrepSummaryForLongPrompt()
    {
        using var localStudioRoot = LocalStudioDataPathScope.Create();
        var repository = new InMemoryProjectRepository();
        var capturingProvider = new CapturingVisionReviewProvider();
        var projectService = new ProjectApplicationService(
            repository,
            textPlanningProvider: null,
            imageGenerationProvider: null,
            visionReviewProvider: capturingProvider);
        var localizationService = new LocalizationService(() => new CultureInfo("en-US"));
        var coordinator = new ReviewWorkflowCoordinator(projectService, localizationService);
        var timestamp = DateTimeOffset.Parse("2026-06-09T10:30:00Z");
        var project = await projectService.CreateProjectAsync("Review bounded summary demo", timestamp, CancellationToken.None);
        var longPrompt = new string('A', VisionReviewExecutionPolicy.DefaultCompactSummaryCharacters + 120);

        await coordinator.RunFakeReviewAsync(
            project.Id,
            [
                new GalleryRowViewModel(
                    Guid.NewGuid(),
                    Guid.NewGuid(),
                    "Opening frame",
                    Path.Combine(Path.GetTempPath(), "candidate.png"),
                    Path.Combine(Path.GetTempPath(), "candidate.json"),
                    longPrompt),
            ],
            CancellationToken.None);

        Assert.NotNull(capturingProvider.LastRequest?.ReviewPrep);
        Assert.True(capturingProvider.LastRequest!.ReviewPrep!.Summary.Length <= VisionReviewExecutionPolicy.DefaultCompactSummaryCharacters);
        var promptEvidence = Assert.Single(
            capturingProvider.LastRequest.ReviewPrep.EvidenceSelections,
            selection => selection.Role == "prompt-summary");
        Assert.NotNull(promptEvidence.Summary);
        Assert.True(promptEvidence.Summary!.Length <= VisionReviewExecutionPolicy.DefaultCompactSummaryCharacters);
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

    private sealed class CapturingVisionReviewProvider : IVisionReviewProvider
    {
        public VisionReviewRequest? LastRequest { get; private set; }

        public IProviderCapabilities Capabilities { get; } = new ProviderCapabilities(
            "fake-capturing-vision",
            "Fake capturing vision provider",
            ["fake-vision"],
            SupportsTextPlanning: false,
            SupportsImageGeneration: false,
            SupportsVisionReview: true,
            SupportsImageEditing: false,
            SupportsStreaming: false);

        public Task<VisionReviewResult> ReviewAsync(VisionReviewRequest request, CancellationToken cancellationToken)
        {
            LastRequest = request;
            return Task.FromResult(
                new VisionReviewResult(
                    request.CandidateImageId,
                    ReviewDecision.Fail,
                    new Dictionary<string, int> { ["match"] = 1 },
                    ["fake-review-failed"],
                    "Fake review failed by configuration.",
                    "Revise the prompt or regenerate the candidate."));
        }
    }
}
