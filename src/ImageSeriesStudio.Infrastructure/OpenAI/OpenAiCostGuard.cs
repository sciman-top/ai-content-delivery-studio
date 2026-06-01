using ImageSeriesStudio.Core.Providers;

namespace ImageSeriesStudio.Infrastructure.OpenAI;

public sealed record OpenAiCostRateCard(
    string Name,
    decimal TextPlanningRequestUsd,
    decimal ImageGenerationRequestUsd,
    decimal VisionReviewRequestUsd)
{
    public static OpenAiCostRateCard Unpriced { get; } = new(
        "unpriced",
        TextPlanningRequestUsd: 0m,
        ImageGenerationRequestUsd: 0m,
        VisionReviewRequestUsd: 0m);
}

public sealed record OpenAiQuotaGuardOptions(
    int MaxTextPlanningRequests = 10,
    int MaxImageGenerationRequests = 50,
    int MaxVisionReviewRequests = 50,
    long MaxRequestedImagePixels = 50_000_000,
    decimal? MaxEstimatedUsd = null);

public sealed record OpenAiBatchCostRequest(
    int TextPlanningRequestCount,
    IReadOnlyList<ImageGenerationRequest> ImageGenerationRequests,
    int VisionReviewRequestCount);

public sealed record OpenAiCostEstimate(
    string RateCardName,
    int TextPlanningRequestCount,
    int ImageGenerationRequestCount,
    int VisionReviewRequestCount,
    long RequestedImagePixels,
    decimal EstimatedUsd);

public sealed record OpenAiQuotaDecision(
    bool Allowed,
    OpenAiCostEstimate Estimate,
    IReadOnlyList<string> Reasons);

public static class OpenAiCostGuard
{
    public static OpenAiQuotaDecision Evaluate(
        OpenAiBatchCostRequest request,
        OpenAiCostRateCard rateCard,
        OpenAiQuotaGuardOptions options)
    {
        var estimate = Estimate(request, rateCard);
        var reasons = new List<string>();

        if (request.TextPlanningRequestCount > options.MaxTextPlanningRequests)
        {
            reasons.Add($"Text planning request count {request.TextPlanningRequestCount} exceeds limit {options.MaxTextPlanningRequests}.");
        }

        if (request.ImageGenerationRequests.Count > options.MaxImageGenerationRequests)
        {
            reasons.Add($"Image generation request count {request.ImageGenerationRequests.Count} exceeds limit {options.MaxImageGenerationRequests}.");
        }

        if (request.VisionReviewRequestCount > options.MaxVisionReviewRequests)
        {
            reasons.Add($"Vision review request count {request.VisionReviewRequestCount} exceeds limit {options.MaxVisionReviewRequests}.");
        }

        if (estimate.RequestedImagePixels > options.MaxRequestedImagePixels)
        {
            reasons.Add($"Requested image pixels {estimate.RequestedImagePixels} exceeds limit {options.MaxRequestedImagePixels}.");
        }

        if (options.MaxEstimatedUsd is { } maxEstimatedUsd && estimate.EstimatedUsd > maxEstimatedUsd)
        {
            reasons.Add($"Estimated cost {estimate.EstimatedUsd:0.####} USD exceeds limit {maxEstimatedUsd:0.####} USD.");
        }

        return new OpenAiQuotaDecision(reasons.Count == 0, estimate, reasons);
    }

    public static OpenAiCostEstimate Estimate(
        OpenAiBatchCostRequest request,
        OpenAiCostRateCard rateCard)
    {
        var textPlanningCost = request.TextPlanningRequestCount * rateCard.TextPlanningRequestUsd;
        var imageGenerationCost = request.ImageGenerationRequests.Count * rateCard.ImageGenerationRequestUsd;
        var visionReviewCost = request.VisionReviewRequestCount * rateCard.VisionReviewRequestUsd;

        return new OpenAiCostEstimate(
            rateCard.Name,
            request.TextPlanningRequestCount,
            request.ImageGenerationRequests.Count,
            request.VisionReviewRequestCount,
            request.ImageGenerationRequests.Sum(EstimatePixels),
            textPlanningCost + imageGenerationCost + visionReviewCost);
    }

    private static long EstimatePixels(ImageGenerationRequest request)
    {
        return request.Settings.Width > 0 && request.Settings.Height > 0
            ? (long)request.Settings.Width * request.Settings.Height
            : 0;
    }
}
