using ImageSeriesStudio.Core.Projects;
using ImageSeriesStudio.Core.Providers;
using ImageSeriesStudio.Infrastructure.OpenAI;

namespace ImageSeriesStudio.Tests;

public sealed class OpenAiCostGuardTests
{
    [Fact]
    public void Estimate_ComputesRequestCountsPixelsAndConfiguredCost()
    {
        var estimate = OpenAiCostGuard.Estimate(
            new OpenAiBatchCostRequest(
                TextPlanningRequestCount: 1,
                ImageGenerationRequests:
                [
                    CreateImageRequest(1024, 1024),
                    CreateImageRequest(1536, 1024),
                ],
                VisionReviewRequestCount: 2),
            new OpenAiCostRateCard(
                "test-rate-card",
                TextPlanningRequestUsd: 0.01m,
                ImageGenerationRequestUsd: 0.20m,
                VisionReviewRequestUsd: 0.02m));

        Assert.Equal("test-rate-card", estimate.RateCardName);
        Assert.Equal(1, estimate.TextPlanningRequestCount);
        Assert.Equal(2, estimate.ImageGenerationRequestCount);
        Assert.Equal(2, estimate.VisionReviewRequestCount);
        Assert.Equal(2_621_440, estimate.RequestedImagePixels);
        Assert.Equal(0.45m, estimate.EstimatedUsd);
    }

    [Fact]
    public void Evaluate_AllowsBatchWithinConfiguredLimits()
    {
        var decision = OpenAiCostGuard.Evaluate(
            new OpenAiBatchCostRequest(
                TextPlanningRequestCount: 1,
                ImageGenerationRequests: [CreateImageRequest(1024, 1024)],
                VisionReviewRequestCount: 1),
            new OpenAiCostRateCard("test", 0.01m, 0.10m, 0.01m),
            new OpenAiQuotaGuardOptions(MaxEstimatedUsd: 0.20m));

        Assert.True(decision.Allowed);
        Assert.Empty(decision.Reasons);
    }

    [Fact]
    public void Evaluate_BlocksBatchWhenQuantityPixelsOrBudgetExceedLimits()
    {
        var decision = OpenAiCostGuard.Evaluate(
            new OpenAiBatchCostRequest(
                TextPlanningRequestCount: 2,
                ImageGenerationRequests:
                [
                    CreateImageRequest(1024, 1024),
                    CreateImageRequest(2048, 2048),
                ],
                VisionReviewRequestCount: 3),
            new OpenAiCostRateCard("test", 0.50m, 1.00m, 0.25m),
            new OpenAiQuotaGuardOptions(
                MaxTextPlanningRequests: 1,
                MaxImageGenerationRequests: 1,
                MaxVisionReviewRequests: 2,
                MaxRequestedImagePixels: 2_000_000,
                MaxEstimatedUsd: 1.00m));

        Assert.False(decision.Allowed);
        Assert.Contains(decision.Reasons, reason => reason.Contains("Text planning", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(decision.Reasons, reason => reason.Contains("Image generation", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(decision.Reasons, reason => reason.Contains("Vision review", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(decision.Reasons, reason => reason.Contains("pixels", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(decision.Reasons, reason => reason.Contains("cost", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Evaluate_WithUnpricedRateCardStillEnforcesQuotaLimits()
    {
        var decision = OpenAiCostGuard.Evaluate(
            new OpenAiBatchCostRequest(
                TextPlanningRequestCount: 0,
                ImageGenerationRequests: [CreateImageRequest(1024, 1024), CreateImageRequest(1024, 1024)],
                VisionReviewRequestCount: 0),
            OpenAiCostRateCard.Unpriced,
            new OpenAiQuotaGuardOptions(MaxImageGenerationRequests: 1));

        Assert.False(decision.Allowed);
        Assert.Equal(0m, decision.Estimate.EstimatedUsd);
        Assert.Contains(decision.Reasons, reason => reason.Contains("Image generation", StringComparison.OrdinalIgnoreCase));
    }

    private static ImageGenerationRequest CreateImageRequest(int width, int height)
    {
        return new ImageGenerationRequest(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "prompt",
            new GenerationSettings(width, height, "standard", "png"),
            Path.GetTempPath());
    }
}
