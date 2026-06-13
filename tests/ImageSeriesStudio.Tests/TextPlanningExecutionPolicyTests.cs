using ImageSeriesStudio.Core.Providers;

namespace ImageSeriesStudio.Tests;

public sealed class TextPlanningExecutionPolicyTests
{
    [Fact]
    public void Defaults_UseStatelessBoundedLocalDirectPlanning()
    {
        Assert.Equal(4000, TextPlanningExecutionPolicy.DefaultMaxInputCharacters);
        Assert.Equal(1, TextPlanningExecutionPolicy.MaxTransientUpstreamRetryCount);
        Assert.Equal(TimeSpan.FromSeconds(2), TextPlanningExecutionPolicy.TransientUpstreamRetryDelay);
        Assert.False(TextPlanningExecutionPolicy.StoreResponsesByDefault);
        Assert.False(TextPlanningExecutionPolicy.AllowPreviousResponseIdByDefault);
    }

    [Fact]
    public void CreateOperatorDescriptor_UsesStatelessLocalDirectDefaults()
    {
        var descriptor = TextPlanningExecutionPolicy.CreateOperatorDescriptor();

        Assert.Equal("local-direct-stateless", descriptor.ExecutionMode);
        Assert.Equal(4000, descriptor.MaxInputCharacters);
        Assert.Equal(1, descriptor.MaxTransientUpstreamRetryCount);
        Assert.Equal(TimeSpan.FromSeconds(2), descriptor.TransientUpstreamRetryDelay);
        Assert.False(descriptor.UsesStoredResponses);
        Assert.False(descriptor.AllowsPreviousResponseId);
    }
}
