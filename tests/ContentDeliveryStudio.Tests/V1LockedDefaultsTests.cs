using ContentDeliveryStudio.Infrastructure.OpenAI;

namespace ContentDeliveryStudio.Tests;

public sealed class V1LockedDefaultsTests
{
    [Fact]
    public void OpenAiRoutingDefaults_ReflectLockedV1ProviderPolicy()
    {
        Assert.Equal("responses", OpenAiRoutingDefaults.PlanningEndpointPath);
        Assert.Equal("responses", OpenAiRoutingDefaults.VisionReviewEndpointPath);
        Assert.Equal("images/generations", OpenAiRoutingDefaults.SingleShotImageGenerationEndpointPath);
        Assert.Equal("responses", OpenAiRoutingDefaults.StatefulImageGenerationEndpointPath);
        Assert.False(OpenAiRoutingDefaults.StoreRemoteStateByDefault);
        Assert.False(OpenAiRoutingDefaults.UsePreviousResponseIdByDefault);
        Assert.True(OpenAiRoutingDefaults.RequireStrictStructuredOutputsForPlanningAndReview);
    }
}
