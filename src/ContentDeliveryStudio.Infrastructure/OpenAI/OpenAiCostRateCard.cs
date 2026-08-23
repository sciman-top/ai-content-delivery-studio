namespace ContentDeliveryStudio.Infrastructure.OpenAI;

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
