using System.Text.Json;
using ImageSeriesStudio.Application.Projects;
using ImageSeriesStudio.App.ViewModels;
using ImageSeriesStudio.Core.Projects;
using ImageSeriesStudio.Infrastructure.Delivery;

namespace ImageSeriesStudio.Tests;

public sealed class DeliveryWorkflowCoordinatorTests
{
    [Fact]
    public async Task ExportDeliveryAsync_ExportsOnlyApprovedPassRowsWithPromotedBlueprintMetadata()
    {
        using var localStudioRoot = LocalStudioDataPathScope.Create();
        var repository = new InMemoryProjectRepository();
        var projectService = new ProjectApplicationService(
            repository,
            textPlanningProvider: null,
            imageGenerationProvider: null,
            visionReviewProvider: null,
            deliveryPackageWriter: new DeliveryPackageWriter());
        var coordinator = new DeliveryWorkflowCoordinator(projectService);
        var timestamp = DateTimeOffset.Parse("2026-06-08T13:00:00Z");
        var project = await projectService.CreateProjectAsync("Delivery coordinator demo", timestamp, CancellationToken.None);

        var rootDirectory = Path.Combine(localStudioRoot.RootPath, "delivery-coordinator");
        var sourceDirectory = Path.Combine(rootDirectory, "source");
        Directory.CreateDirectory(sourceDirectory);
        var approvedImagePath = Path.Combine(sourceDirectory, "approved.png");
        var approvedMetadataPath = Path.Combine(sourceDirectory, "approved.json");
        var rejectedImagePath = Path.Combine(sourceDirectory, "rejected.png");
        var rejectedMetadataPath = Path.Combine(sourceDirectory, "rejected.json");
        await File.WriteAllBytesAsync(approvedImagePath, [1, 2, 3], CancellationToken.None);
        await File.WriteAllTextAsync(approvedMetadataPath, """{"providerId":"fake-image"}""", CancellationToken.None);
        await File.WriteAllBytesAsync(rejectedImagePath, [4, 5, 6], CancellationToken.None);
        await File.WriteAllTextAsync(rejectedMetadataPath, """{"providerId":"fake-image"}""", CancellationToken.None);

        try
        {
            var creativeBriefId = Guid.NewGuid();
            var blueprintId = Guid.NewGuid();
            var approvedCandidateId = Guid.NewGuid();
            var rejectedCandidateId = Guid.NewGuid();
            var result = await coordinator.ExportDeliveryAsync(
                project.Id,
                project.Name,
                [
                    new GalleryRowViewModel(approvedCandidateId, Guid.NewGuid(), "Approved panel", approvedImagePath, approvedMetadataPath, "Approved prompt"),
                    new GalleryRowViewModel(rejectedCandidateId, Guid.NewGuid(), "Rejected panel", rejectedImagePath, rejectedMetadataPath, "Rejected prompt"),
                ],
                [
                    new ReviewRowViewModel(
                        approvedCandidateId,
                        "Approved panel",
                        ReviewDecision.Pass.ToString(),
                        "match:5",
                        "Approved for delivery.",
                        string.Empty,
                        "None",
                        HumanApproved: true,
                        "Human approved",
                        "Teacher",
                        "Ready for package.",
                        timestamp.AddMinutes(1),
                        new StructuredReviewOutput(approvedCandidateId, ReviewDecision.Pass, [new StructuredReviewScore("match", "Matches prompt.", 3, 5)], [], "Approved for delivery.", null)),
                    new ReviewRowViewModel(
                        CandidateImageId: rejectedCandidateId,
                        ItemTitle: "Rejected panel",
                        Decision: ReviewDecision.Fail.ToString(),
                        ScoreText: "match:1",
                        Comments: "Rejected for export.",
                        SuggestedFix: "Revise prompt.",
                        RouteSummary: "Brief / Major: Regenerate",
                        HumanApproved: false,
                        HumanApprovalStatus: "Pending human approval",
                        FinalReviewer: "Teacher",
                        FinalApprovalNotes: "Do not export.",
                        FinalApprovalDecidedAt: null,
                        Review: new StructuredReviewOutput(rejectedCandidateId, ReviewDecision.Fail, [new StructuredReviewScore("match", "Matches prompt.", 3, 1)], ["fake-review-failed"], "Rejected for export.", "Revise prompt.")),
                ],
                [
                    new DesignBlueprintRowViewModel(
                        creativeBriefId,
                        blueprintId,
                        "panel-narrative-sequence",
                        "Panel narrative sequence",
                        "storyboard",
                        "Summary",
                        "Use",
                        "4-8",
                        "panel sequence",
                        "Hybrid",
                        "series-consistency",
                        "consistent panel storytelling",
                        "vary section emphasis",
                        "low risk",
                        IsPromoted: true,
                        PromotionStatus: "Promoted"),
                ],
                activeCreativeBriefId: creativeBriefId,
                CancellationToken.None);

            var deliveryRow = Assert.Single(result.DeliveryRows);
            using var manifestStream = File.OpenRead(deliveryRow.ManifestJsonPath);
            using var manifest = await JsonDocument.ParseAsync(manifestStream, cancellationToken: CancellationToken.None);
            var items = manifest.RootElement.GetProperty("items").EnumerateArray().ToArray();
            var item = Assert.Single(items);
            var blueprint = item.GetProperty("blueprint");

            Assert.Equal("Delivery coordinator demo", manifest.RootElement.GetProperty("projectName").GetString());
            Assert.Equal("Approved panel", item.GetProperty("title").GetString());
            Assert.Equal("panel-narrative-sequence", blueprint.GetProperty("key").GetString());
            Assert.Equal("Panel narrative sequence", blueprint.GetProperty("displayName").GetString());
            Assert.Equal("panel sequence", blueprint.GetProperty("sequenceMode").GetString());
            Assert.Contains("consistent panel storytelling", blueprint.GetProperty("consistencySummary").GetString());
        }
        finally
        {
            if (Directory.Exists(rootDirectory))
            {
                Directory.Delete(rootDirectory, recursive: true);
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
}
