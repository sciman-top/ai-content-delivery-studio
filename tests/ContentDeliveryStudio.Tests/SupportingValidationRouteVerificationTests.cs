using System.Text.Json;
using ContentDeliveryStudio.Application.Delivery;
using ContentDeliveryStudio.Application.Projects;
using ContentDeliveryStudio.Core.Documents;
using ContentDeliveryStudio.Core.Projects;
using ContentDeliveryStudio.Core.Providers;
using ContentDeliveryStudio.Infrastructure.Delivery;
using ContentDeliveryStudio.Infrastructure.Fakes;
using Microsoft.EntityFrameworkCore;

namespace ContentDeliveryStudio.Tests;

public sealed class SupportingValidationRouteVerificationTests
{
    [Fact]
    public async Task SupportingValidationRoute_CompletesFakeFirstDocumentPlanningThroughDelivery()
    {
        var rootDirectory = Path.Combine(Path.GetTempPath(), "ContentDeliveryStudio.Tests", Guid.NewGuid().ToString("N"));
        var databasePath = Path.Combine(rootDirectory, "supporting-route.sqlite");
        var generatedDirectory = Path.Combine(rootDirectory, "generated");
        var deliveryDirectory = Path.Combine(rootDirectory, "delivery");
        Directory.CreateDirectory(rootDirectory);

        try
        {
            var options = new DbContextOptionsBuilder<Infrastructure.Persistence.AppDbContext>()
                .UseSqlite($"Data Source={databasePath};Pooling=False")
                .Options;

            await using (var setup = new Infrastructure.Persistence.AppDbContext(options))
            {
                await setup.Database.EnsureCreatedAsync();
            }

            var service = new ProjectApplicationService(
                new Infrastructure.Persistence.EfProjectRepository(new Infrastructure.Persistence.AppDbContext(options)),
                new FakeTextPlanningProvider(),
                new FakeImageGenerationProvider(),
                new FakeVisionReviewProvider(defaultPasses: true),
                new DeliveryPackageWriter());

            var timestamp = new DateTimeOffset(2026, 6, 9, 15, 0, 0, TimeSpan.Zero);
            var project = await service.CreateProjectAsync("Supporting validation route demo", timestamp, CancellationToken.None);

            var workflowResult = await service.CreateDocumentIllustrationPlanWithProviderAsync(
                project.Id,
                new DocumentIllustrationPlanningRequest(
                    "Archimedes note",
                    "Teachers need a short classroom explanation of Archimedes' principle and a diagram-friendly scene.",
                    "teachers",
                    DocumentFamily.Educational,
                    IllustrationStrictnessLevel.Educational,
                    ["Introduction", "Classroom analogy"],
                    ["Buoyant force depends on displaced fluid weight."],
                    ["avoid fake lab data", "reserve room for deterministic labels"]),
                approveAllTargets: true,
                timestamp.AddMinutes(1),
                CancellationToken.None);

            Assert.True(workflowResult.SeriesId.HasValue);
            Assert.True(workflowResult.ApprovedTargetCount > 0);

            var generationRun = await service.RunGenerationQueueAsync(
                project.Id,
                generatedDirectory,
                CancellationToken.None);

            var loadedProject = await service.LoadProjectAsync(project.Id, CancellationToken.None)
                ?? throw new InvalidOperationException("Project should load after supporting-route generation.");
            var generatedCandidates = loadedProject.Series
                .SelectMany(series => series.Items)
                .SelectMany(item => item.CandidateImages.Select(candidate => (Item: item, Candidate: candidate)))
                .ToArray();

            Assert.Equal(workflowResult.ApprovedTargetCount, generationRun.Images.Count);
            Assert.Equal(workflowResult.ApprovedTargetCount, generatedCandidates.Length);
            Assert.All(generatedCandidates, row =>
            {
                var prompt = row.Item.PromptVersions.Single(prompt => prompt.Id == row.Candidate.PromptVersionId);
                Assert.Contains("Source evidence", row.Item.Brief, StringComparison.OrdinalIgnoreCase);
                Assert.Contains("source evidence", prompt.PromptText, StringComparison.OrdinalIgnoreCase);
            });

            var reviewInputs = generatedCandidates
                .Select(row =>
                {
                    var prompt = row.Item.PromptVersions.Single(value => value.Id == row.Candidate.PromptVersionId);
                    return new ReviewCandidateInput(
                        row.Candidate.Id,
                        row.Item.Title,
                        row.Candidate.AssetPath,
                        prompt.PromptText,
                        new ReviewPrepArtifactContract(
                            $"Local supporting-route review prep for {row.Item.Title}.",
                            EvidenceSelections:
                            [
                                new ReviewPrepEvidenceSelection(
                                    "candidate-image",
                                    "generated-asset",
                                    row.Candidate.AssetPath,
                                    "Primary fake-first supporting-route candidate selected for review."),
                                new ReviewPrepEvidenceSelection(
                                    "candidate-metadata",
                                    "generation-metadata",
                                    row.Candidate.MetadataPath,
                                    "Local metadata sidecar kept for review provenance."),
                            ]));
                })
                .ToArray();

            var reviews = await service.RunStructuredVisionReviewAsync(
                project.Id,
                reviewInputs,
                CancellationToken.None);

            var approvedItems = new List<DeliveryExportItem>();
            foreach (var review in reviews)
            {
                var decision = await service.RecordFinalApprovalAsync(
                    project.Id,
                    new FinalApprovalRequest(
                        review,
                        Approve: true,
                        Reviewer: "Teacher",
                        Notes: "Approved in supporting validation route verification."),
                    timestamp.AddMinutes(2),
                    CancellationToken.None);

                var generatedCandidate = generatedCandidates.Single(row => row.Candidate.Id == review.CandidateImageId);
                var prompt = generatedCandidate.Item.PromptVersions.Single(value => value.Id == generatedCandidate.Candidate.PromptVersionId);

                approvedItems.Add(
                    new DeliveryExportItem(
                        generatedCandidate.Item.Id.ToString("N"),
                        generatedCandidate.Item.Title,
                        generatedCandidate.Candidate.AssetPath,
                        generatedCandidate.Candidate.MetadataPath,
                        prompt.PromptText,
                        review.Decision,
                        decision.HumanApproved,
                        decision.Reviewer,
                        decision.Notes,
                        decision.DecidedAt,
                        ArtifactRole: "supporting-validation-final-image"));
            }

            var delivery = await service.ExportDeliveryPackageAsync(
                new DeliveryExportRequest(
                    project.Name,
                    deliveryDirectory,
                    approvedItems),
                CancellationToken.None);

            await using var verification = new Infrastructure.Persistence.AppDbContext(options);
            Assert.True(File.Exists(delivery.ManifestJsonPath));
            Assert.True(File.Exists(delivery.ReviewReportPath));
            Assert.Equal(workflowResult.ApprovedTargetCount, approvedItems.Count);
            Assert.Equal(workflowResult.ApprovedTargetCount, await verification.GenerationTasks.CountAsync());
            Assert.Equal(workflowResult.ApprovedTargetCount, await verification.CandidateImages.CountAsync());
            Assert.Equal(workflowResult.ApprovedTargetCount, await verification.ReviewResults.CountAsync());

            using var manifestStream = File.OpenRead(delivery.ManifestJsonPath);
            using var manifest = await JsonDocument.ParseAsync(manifestStream, cancellationToken: CancellationToken.None);
            Assert.Equal(workflowResult.ApprovedTargetCount, manifest.RootElement.GetProperty("items").GetArrayLength());
        }
        finally
        {
            if (Directory.Exists(rootDirectory))
            {
                Directory.Delete(rootDirectory, recursive: true);
            }
        }
    }
}
