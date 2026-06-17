using ContentDeliveryStudio.Application.ToolAdapters;
using ContentDeliveryStudio.Core.Operators;
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

    [Fact]
    public void LocalToolRegistry_ReflectsLockedV1OperatorAndCompositionDefaults()
    {
        var registry = LocalToolRegistry.CreateBuiltIn();

        var composition = registry.GetRequired("deterministic-text-composition");
        Assert.Equal(OperatorRiskLevel.Medium, composition.RiskLevel);
        Assert.Contains(composition.SideEffects, sideEffect => sideEffect.Contains("SkiaSharp", StringComparison.Ordinal));

        var validation = registry.GetRequired("artifact-validation");
        Assert.Equal(OperatorRiskLevel.Low, validation.RiskLevel);
        Assert.Null(validation.CleanupPath);
        Assert.Contains(validation.SideEffects, sideEffect => sideEffect.Contains("additive", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(validation.SideEffects, sideEffect => sideEffect.Contains("validation", StringComparison.OrdinalIgnoreCase));
    }
}
