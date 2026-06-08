using ImageSeriesStudio.Application.Projects;
using ImageSeriesStudio.Core.Projects;
using ImageSeriesStudio.Core.Providers;
using ImageSeriesStudio.Infrastructure.Fakes;
using Microsoft.EntityFrameworkCore;

namespace ImageSeriesStudio.Tests;

public sealed class ReviewWorkflowApplicationServiceTests
{
    [Fact]
    public async Task ReviewWorkflowApplicationService_RunsStructuredVisionReviewWithRubricScores()
    {
        var repository = new InMemoryProjectRepository();
        var service = new ReviewWorkflowApplicationService(repository, new FakeVisionReviewProvider(defaultPasses: true));
        var timestamp = DateTimeOffset.Parse("2026-06-07T15:00:00Z");
        var project = ImageProject.Create("Review workflow demo", timestamp);
        await repository.SaveAsync(project, CancellationToken.None);

        var reviews = await service.RunStructuredVisionReviewAsync(
            project.Id,
            [
                new ReviewCandidateInput(
                    Guid.NewGuid(),
                    "Opening image",
                    Path.Combine(Path.GetTempPath(), "candidate.png"),
                    "A clean editorial candidate.",
                    new ReviewPrepArtifactContract("Local review prep: opening image / editorial candidate.")),
            ],
            CancellationToken.None);

        var review = Assert.Single(reviews);
        Assert.Equal(ReviewDecision.Pass, review.Decision);
        Assert.Contains(review.Scores, score => score.Name == "match" && score.Score == 5);
        Assert.False(review.NeedsRepair);
    }

    [Fact]
    public async Task ReviewWorkflowApplicationService_RejectsOversizedRemoteReviewBatchBeforeDispatch()
    {
        var repository = new InMemoryProjectRepository();
        var provider = new CountingVisionReviewProvider();
        var service = new ReviewWorkflowApplicationService(repository, provider);
        var timestamp = DateTimeOffset.Parse("2026-06-08T10:00:00Z");
        var project = ImageProject.Create("Oversized review demo", timestamp);
        await repository.SaveAsync(project, CancellationToken.None);

        var candidates = Enumerable.Range(0, 7)
            .Select(index => new ReviewCandidateInput(
                Guid.NewGuid(),
                $"Candidate {index + 1}",
                Path.Combine(Path.GetTempPath(), $"candidate-{index + 1}.png"),
                "Compact prompt summary."))
            .ToArray();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.RunStructuredVisionReviewAsync(project.Id, candidates, CancellationToken.None));

        Assert.Contains("review batch", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("split", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(0, provider.CallCount);
    }

    [Fact]
    public async Task ReviewWorkflowApplicationService_RejectsRemoteReviewWhenCompactPrepArtifactsAreMissing()
    {
        var repository = new InMemoryProjectRepository();
        var provider = new CountingVisionReviewProvider();
        var service = new ReviewWorkflowApplicationService(repository, provider);
        var timestamp = DateTimeOffset.Parse("2026-06-08T10:10:00Z");
        var project = ImageProject.Create("Missing prep demo", timestamp);
        await repository.SaveAsync(project, CancellationToken.None);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.RunStructuredVisionReviewAsync(
                project.Id,
                [
                    new ReviewCandidateInput(
                        Guid.NewGuid(),
                        "Candidate 1",
                        Path.Combine(Path.GetTempPath(), "candidate.png"),
                        "",
                        null),
                ],
                CancellationToken.None));

        Assert.Contains("compact local review artifacts", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(0, provider.CallCount);
    }

    [Fact]
    public async Task ReviewWorkflowApplicationService_RejectsRemoteReviewWhenTypedEvidenceSelectionsAreMissing()
    {
        var repository = new InMemoryProjectRepository();
        var provider = new CountingVisionReviewProvider();
        var service = new ReviewWorkflowApplicationService(repository, provider);
        var timestamp = DateTimeOffset.Parse("2026-06-08T10:20:00Z");
        var project = ImageProject.Create("Missing evidence demo", timestamp);
        await repository.SaveAsync(project, CancellationToken.None);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.RunStructuredVisionReviewAsync(
                project.Id,
                [
                    new ReviewCandidateInput(
                        Guid.NewGuid(),
                        "Candidate 1",
                        Path.Combine(Path.GetTempPath(), "candidate.png"),
                        "Compact prompt summary.",
                        new ReviewPrepArtifactContract("Compact local review prep.")),
                ],
                CancellationToken.None));

        Assert.Contains("typed local evidence selection", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(0, provider.CallCount);
    }

    [Fact]
    public async Task ReviewWorkflowApplicationService_RejectsRemoteReviewWhenCompactSummaryIsOversized()
    {
        var repository = new InMemoryProjectRepository();
        var provider = new CountingVisionReviewProvider();
        var service = new ReviewWorkflowApplicationService(repository, provider);
        var timestamp = DateTimeOffset.Parse("2026-06-08T10:25:00Z");
        var project = ImageProject.Create("Oversized compact summary demo", timestamp);
        await repository.SaveAsync(project, CancellationToken.None);
        var oversizedSummary = new string('A', VisionReviewExecutionPolicy.DefaultCompactSummaryCharacters + 20);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.RunStructuredVisionReviewAsync(
                project.Id,
                [
                    new ReviewCandidateInput(
                        Guid.NewGuid(),
                        "Candidate 1",
                        Path.Combine(Path.GetTempPath(), "candidate.png"),
                        "Compact prompt summary.",
                        new ReviewPrepArtifactContract(
                            oversizedSummary,
                            EvidenceSelections:
                            [
                                new ReviewPrepEvidenceSelection(
                                    "prompt-summary",
                                    "prompt-text",
                                    null,
                                    "Compact prompt summary."),
                            ])),
                ],
                CancellationToken.None));

        Assert.Contains("compact summary exceeds", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(0, provider.CallCount);
    }

    [Fact]
    public async Task ReviewWorkflowApplicationService_RecordsFinalApprovalDecision()
    {
        var databaseDirectory = Path.Combine(Path.GetTempPath(), "ImageSeriesStudio.Tests", Guid.NewGuid().ToString("N"));
        var databasePath = Path.Combine(databaseDirectory, "review-workflow-approval.sqlite");
        Directory.CreateDirectory(databaseDirectory);

        try
        {
            var options = new DbContextOptionsBuilder<Infrastructure.Persistence.AppDbContext>()
                .UseSqlite($"Data Source={databasePath};Pooling=False")
                .Options;

            var timestamp = new DateTimeOffset(2026, 6, 7, 16, 0, 0, TimeSpan.Zero);
            Guid projectId;
            Guid candidateId;

            await using (var setup = new Infrastructure.Persistence.AppDbContext(options))
            {
                await setup.Database.EnsureCreatedAsync();

                var project = ImageProject.Create("Review approval demo", timestamp);
                var profile = project.AddProviderProfile("Fake provider", ProviderKind.Fake, timestamp.AddMinutes(1));
                var series = project.AddSeries("Approval series", "Series", timestamp.AddMinutes(2));
                var item = series.AddItem("Opening", "Opening candidate.", timestamp.AddMinutes(3));
                var prompt = item.AddPromptVersion(
                    "Create an approval-ready image.",
                    new GenerationSettings(1024, 1024, "standard", "png"),
                    profile.Id,
                    timestamp.AddMinutes(4));
                var task = new GenerationTask(
                    Guid.NewGuid(),
                    item.Id,
                    prompt.Id,
                    profile.Id,
                    GenerationTaskStatus.Succeeded,
                    attemptCount: 1,
                    maxRetries: 1,
                    timestamp.AddMinutes(5),
                    timestamp.AddMinutes(6));
                var candidate = new CandidateImage(
                    Guid.NewGuid(),
                    item.Id,
                    prompt.Id,
                    task.Id,
                    profile.Id,
                    CandidateImageStatus.Final,
                    "candidate.png",
                    "candidate.json",
                    timestamp.AddMinutes(7));
                item.AddGenerationTask(task, timestamp.AddMinutes(6));
                item.AddCandidateImage(candidate, timestamp.AddMinutes(7));

                await setup.Projects.AddAsync(project, CancellationToken.None);
                await setup.SaveChangesAsync(CancellationToken.None);

                projectId = project.Id;
                candidateId = candidate.Id;
            }

            var repository = new Infrastructure.Persistence.EfProjectRepository(new Infrastructure.Persistence.AppDbContext(options));
            var service = new ReviewWorkflowApplicationService(repository, visionReviewProvider: null);

            var decision = await service.RecordFinalApprovalAsync(
                projectId,
                new FinalApprovalRequest(
                    new StructuredReviewOutput(
                        candidateId,
                        ReviewDecision.Pass,
                        [new StructuredReviewScore("match", "Matches prompt.", 3, 5)],
                        [],
                        "Ready for approval.",
                        SuggestedFix: null),
                    Approve: true,
                    "Teacher",
                    "Looks ready."),
                timestamp.AddMinutes(8),
                CancellationToken.None);

            await using var verification = new Infrastructure.Persistence.AppDbContext(options);
            var storedReview = await verification.ReviewResults.SingleAsync(review => review.CandidateImageId == candidateId);

            Assert.True(decision.HumanApproved);
            Assert.Equal("Teacher", storedReview.FinalReviewer);
            Assert.True(storedReview.HumanApproved);
            Assert.Equal("Looks ready.", storedReview.FinalApprovalNotes);
        }
        finally
        {
            if (Directory.Exists(databaseDirectory))
            {
                Directory.Delete(databaseDirectory, recursive: true);
            }
        }
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

    private sealed class CountingVisionReviewProvider : IVisionReviewProvider
    {
        public int CallCount { get; private set; }

        public IProviderCapabilities Capabilities { get; } = new ProviderCapabilities(
            "counting-vision-provider",
            "Counting vision provider",
            ["review-model"],
            SupportsTextPlanning: false,
            SupportsImageGeneration: false,
            SupportsVisionReview: true,
            SupportsImageEditing: false,
            SupportsStreaming: false);

        public Task<VisionReviewResult> ReviewAsync(VisionReviewRequest request, CancellationToken cancellationToken)
        {
            CallCount++;
            return Task.FromResult(
                new VisionReviewResult(
                    request.CandidateImageId,
                    ReviewDecision.Pass,
                    new Dictionary<string, int> { ["match"] = 5 },
                    [],
                    "Looks aligned.",
                    SuggestedFix: null));
        }
    }
}
