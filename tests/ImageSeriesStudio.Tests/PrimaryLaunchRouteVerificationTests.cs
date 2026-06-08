using System.Text.Json;
using ImageSeriesStudio.Application.Delivery;
using ImageSeriesStudio.Application.Projects;
using ImageSeriesStudio.Core.Projects;
using ImageSeriesStudio.Core.Providers;
using ImageSeriesStudio.Core.Styles;
using ImageSeriesStudio.Infrastructure.Delivery;
using ImageSeriesStudio.Infrastructure.Fakes;
using Microsoft.EntityFrameworkCore;

namespace ImageSeriesStudio.Tests;

public sealed class PrimaryLaunchRouteVerificationTests
{
    [Fact]
    public async Task PrimaryLaunchRoute_CompletesThreeConsecutiveFakeFirstRunsWithoutManualDatabaseEdits()
    {
        var rootDirectory = Path.Combine(Path.GetTempPath(), "ImageSeriesStudio.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(rootDirectory);

        try
        {
            for (var runIndex = 1; runIndex <= 3; runIndex++)
            {
                var databasePath = Path.Combine(rootDirectory, $"launch-route-{runIndex}.sqlite");
                var outputDirectory = Path.Combine(rootDirectory, $"run-{runIndex}");
                var generatedDirectory = Path.Combine(outputDirectory, "generated");
                var deliveryDirectory = Path.Combine(outputDirectory, "delivery");

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

                var timestamp = new DateTimeOffset(2026, 6, 9, 12, 0, 0, TimeSpan.Zero).AddMinutes(runIndex * 10);
                var project = await service.CreateProjectAsync($"Launch route run {runIndex}", timestamp, CancellationToken.None);
                var seedSeries = await service.AddSeriesAsync(
                    project.Id,
                    $"Run {runIndex} seed series",
                    "Brief seed",
                    timestamp.AddMinutes(1),
                    CancellationToken.None);

                var brief = await service.CreateCreativeBriefAsync(
                    project.Id,
                    seedSeries.Id,
                    $"classroom buoyancy concept run {runIndex}",
                    "teachers",
                    ImageTextPolicy.DeterministicPostRender,
                    "clean editorial classroom poster",
                    ["accurate concept sketch", "clear room for deterministic labels"],
                    ["tiny unreadable text"],
                    timestamp.AddMinutes(3),
                    CancellationToken.None);

                var plannedBlueprints = await service.CreateDesignBlueprintsAsync(
                    project.Id,
                    brief.Id,
                    timestamp.AddMinutes(4),
                    CancellationToken.None);
                var promotedBlueprint = await service.PromoteDesignBlueprintAsync(
                    project.Id,
                    brief.Id,
                    plannedBlueprints.DesignBlueprints.First().Id,
                    timestamp.AddMinutes(5),
                    CancellationToken.None);

                var promptDirections = await service.CreatePromptDirectionsAsync(
                    project.Id,
                    brief.Id,
                    timestamp.AddMinutes(6),
                    CancellationToken.None);
                Assert.NotEmpty(promptDirections.PromptDirections);

                var plannedSeries = await service.CreatePlanWithProviderAsync(
                    project.Id,
                    new PlanningRequest(
                        $"classroom buoyancy concept run {runIndex}",
                        "teachers",
                        2,
                        "clean editorial classroom poster"),
                    timestamp.AddMinutes(8),
                    CancellationToken.None);

                var generationRun = await service.RunGenerationQueueAsync(
                    project.Id,
                    generatedDirectory,
                    CancellationToken.None);

                var loadedProject = await service.LoadProjectAsync(project.Id, CancellationToken.None)
                    ?? throw new InvalidOperationException("Project should load after fake-first generation run.");
                var generatedCandidates = loadedProject.Series
                    .SelectMany(series => series.Items)
                    .SelectMany(item => item.CandidateImages.Select(candidate => (Item: item, Candidate: candidate)))
                    .Where(row => generationRun.Images.Any(image => image.CandidateImageId == row.Candidate.Id))
                    .OrderBy(row => row.Item.Title, StringComparer.OrdinalIgnoreCase)
                    .ToArray();

                var reviewInputs = generatedCandidates
                    .Select((row, index) =>
                    {
                        var prompt = row.Item.PromptVersions.Single(value => value.Id == row.Candidate.PromptVersionId);
                        return new ReviewCandidateInput(
                            row.Candidate.Id,
                            row.Item.Title,
                            row.Candidate.AssetPath,
                            prompt.PromptText,
                            new ReviewPrepArtifactContract(
                                $"Local launch review prep for run {runIndex} item {index + 1}.",
                                EvidenceSelections:
                                [
                                    new ReviewPrepEvidenceSelection(
                                        "candidate-image",
                                        "generated-asset",
                                        row.Candidate.AssetPath,
                                        "Primary fake-first candidate selected for review."),
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
                            Notes: $"Approved in fake-first verification run {runIndex}."),
                        timestamp.AddMinutes(9),
                        CancellationToken.None);

                    var image = generationRun.Images.Single(candidate => candidate.CandidateImageId == review.CandidateImageId);
                    var generatedCandidate = generatedCandidates.Single(row => row.Candidate.Id == review.CandidateImageId);
                    var item = generatedCandidate.Item;

                    approvedItems.Add(
                        new DeliveryExportItem(
                            item.Id.ToString("N"),
                            item.Title,
                            image.AssetPath,
                            image.MetadataPath,
                            item.PromptVersions.Single(prompt => prompt.Id == generatedCandidate.Candidate.PromptVersionId).PromptText,
                            review.Decision,
                            decision.HumanApproved,
                            decision.Reviewer,
                            decision.Notes,
                            decision.DecidedAt,
                            ArtifactRole: "launch-route-final-image",
                            Blueprint: new DeliveryBlueprintMetadata(
                                promotedBlueprint.Id,
                                promotedBlueprint.Key,
                                promotedBlueprint.DisplayName,
                                promotedBlueprint.Category,
                                promotedBlueprint.SupportsPanelSequence ? "panel_sequence" : "standard_items",
                                string.Join("; ", promotedBlueprint.ConsistencyRules),
                                string.Join("; ", promotedBlueprint.VariationRules))));
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
                Assert.Equal(2, approvedItems.Count);
                Assert.Equal(2, generationRun.Images.Count);
                Assert.Equal(2, await verification.GenerationTasks.CountAsync());
                Assert.Equal(2, await verification.CandidateImages.CountAsync());
                Assert.Equal(2, await verification.ReviewResults.CountAsync());

                using var manifestStream = File.OpenRead(delivery.ManifestJsonPath);
                using var manifest = await JsonDocument.ParseAsync(manifestStream, cancellationToken: CancellationToken.None);
                Assert.Equal(2, manifest.RootElement.GetProperty("items").GetArrayLength());
            }
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
