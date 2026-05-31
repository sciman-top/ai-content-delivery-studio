using System.Text.Json;
using ImageSeriesStudio.Core.Generation;
using ImageSeriesStudio.Core.Projects;
using ImageSeriesStudio.Core.Providers;
using ImageSeriesStudio.Infrastructure.Delivery;
using ImageSeriesStudio.Infrastructure.Fakes;

namespace ImageSeriesStudio.Tests;

public sealed class FakeWorkflowTests
{
    [Fact]
    public async Task FakeProviders_CompletePlanningGenerationReviewAndDeliveryLoop()
    {
        var rootDirectory = Path.Combine(Path.GetTempPath(), "ImageSeriesStudio.Tests", Guid.NewGuid().ToString("N"));
        var generatedDirectory = Path.Combine(rootDirectory, "generated");
        var deliveryDirectory = Path.Combine(rootDirectory, "delivery");

        try
        {
            var textProvider = new FakeTextPlanningProvider();
            var imageProvider = new FakeImageGenerationProvider();
            var visionProvider = new FakeVisionReviewProvider(defaultPasses: true);
            var deliveryWriter = new DeliveryPackageWriter();
            var timestamp = new DateTimeOffset(2026, 6, 1, 9, 0, 0, TimeSpan.Zero);

            var plan = await textProvider.CreatePlanAsync(
                new PlanningRequest("one image education poster", "teachers", 1, "clean editorial style"),
                CancellationToken.None);

            var project = ImageProject.Create("Fake loop project", timestamp);
            var profile = project.AddProviderProfile("Fake image", ProviderKind.Fake, timestamp.AddMinutes(1));
            var series = project.AddSeries("Demo series", plan.Summary, timestamp.AddMinutes(2));
            var plannedItem = Assert.Single(plan.Items);
            var item = series.AddItem(plannedItem.Title, plannedItem.Brief, timestamp.AddMinutes(3));
            var prompt = item.AddPromptVersion(
                plannedItem.PromptDraft,
                new GenerationSettings(512, 512, "standard", "png", 7),
                profile.Id,
                timestamp.AddMinutes(4));

            item.MarkReady(timestamp.AddMinutes(5));
            item.MarkGenerating(timestamp.AddMinutes(6));

            var queue = new GenerationQueue(imageProvider, new GenerationQueueOptions(MaxConcurrency: 1, MaxRetries: 0));
            var queueRun = await queue.RunAsync(
                [
                    new ImageGenerationRequest(
                        item.Id,
                        prompt.Id,
                        prompt.PromptText,
                        prompt.Settings,
                        generatedDirectory,
                        "image-01.png"),
                ],
                CancellationToken.None);

            var image = Assert.Single(queueRun.Images);
            item.MarkNeedsReview(timestamp.AddMinutes(7));

            var review = await visionProvider.ReviewAsync(
                new VisionReviewRequest(
                    image.CandidateImageId,
                    image.AssetPath,
                    new ReviewRubric(
                        Guid.NewGuid(),
                        project.Id,
                        "Default rubric",
                        [new ReviewRubricDimension("subject", "Required subject is visible", 1)],
                        timestamp.AddMinutes(8)),
                    prompt.PromptText),
                CancellationToken.None);

            Assert.Equal(ReviewDecision.Pass, review.Decision);
            item.Approve(timestamp.AddMinutes(9));

            var delivery = await deliveryWriter.WriteAsync(
                new DeliveryPackageRequest(
                    project.Name,
                    deliveryDirectory,
                    [
                        new DeliveryPackageItem(
                            "image-01",
                            item.Title,
                            image.AssetPath,
                            image.MetadataPath,
                            prompt.PromptText,
                            review.Decision,
                            HumanApproved: true),
                    ]),
                CancellationToken.None);

            item.MarkDelivered(timestamp.AddMinutes(10));

            Assert.Equal(SeriesItemStatus.Delivered, item.Status);
            Assert.True(File.Exists(Assert.Single(delivery.FinalImagePaths)));

            using var manifestStream = File.OpenRead(delivery.ManifestJsonPath);
            using var manifest = await JsonDocument.ParseAsync(manifestStream, cancellationToken: CancellationToken.None);
            Assert.Equal(1, manifest.RootElement.GetProperty("items").GetArrayLength());
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
