namespace ContentDeliveryStudio.Infrastructure.OpenAI;

public static class OpenAiRoutingDefaults
{
    // Mirrors docs/PROVIDER_ROUTING_POLICY.md: V1 keeps local project state authoritative by default.
    public const string PlanningEndpointPath = "responses";

    public const string VisionReviewEndpointPath = "responses";

    public const string SingleShotImageGenerationEndpointPath = "images/generations";

    public const string StatefulImageGenerationEndpointPath = "responses";

    public const bool StoreRemoteStateByDefault = false;

    public const bool UsePreviousResponseIdByDefault = false;

    public const bool RequireStrictStructuredOutputsForPlanningAndReview = true;
}
