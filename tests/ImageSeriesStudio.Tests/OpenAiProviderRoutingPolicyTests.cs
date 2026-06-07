using ImageSeriesStudio.Infrastructure.OpenAI;

namespace ImageSeriesStudio.Tests;

public sealed class OpenAiProviderRoutingPolicyTests
{
    [Fact]
    public void RoutingPolicy_UsesResponsesWithStructuredOutputsAndStoreFalseForPlanningAndReview()
    {
        var planning = OpenAiProviderRoutingPolicy.ForTextPlanning();
        var review = OpenAiProviderRoutingPolicy.ForVisionReview();

        Assert.Equal(OpenAiEndpointFamily.Responses, planning.EndpointFamily);
        Assert.Equal("responses", planning.RelativePath);
        Assert.True(planning.UseStructuredOutputs);
        Assert.False(planning.Store);

        Assert.Equal(OpenAiEndpointFamily.Responses, review.EndpointFamily);
        Assert.Equal("responses", review.RelativePath);
        Assert.True(review.UseStructuredOutputs);
        Assert.False(review.Store);
    }

    [Fact]
    public void RoutingPolicy_UsesImagesApiAndStoreFalseForSingleShotGeneration()
    {
        var imageGeneration = OpenAiProviderRoutingPolicy.ForImageGeneration();

        Assert.Equal(OpenAiEndpointFamily.Images, imageGeneration.EndpointFamily);
        Assert.Equal("images/generations", imageGeneration.RelativePath);
        Assert.False(imageGeneration.UseStructuredOutputs);
        Assert.False(imageGeneration.Store);
    }
}
