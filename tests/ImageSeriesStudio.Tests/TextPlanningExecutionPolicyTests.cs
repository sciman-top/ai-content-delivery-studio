using ImageSeriesStudio.Core.Providers;

namespace ImageSeriesStudio.Tests;

public sealed class TextPlanningExecutionPolicyTests
{
    [Fact]
    public void Defaults_UseStatelessBoundedLocalDirectPlanning()
    {
        Assert.Equal(4000, TextPlanningExecutionPolicy.DefaultMaxInputCharacters);
        Assert.False(TextPlanningExecutionPolicy.StoreResponsesByDefault);
        Assert.False(TextPlanningExecutionPolicy.AllowPreviousResponseIdByDefault);
    }

    [Fact]
    public void CreateOperatorDescriptor_UsesStatelessLocalDirectDefaults()
    {
        var descriptor = TextPlanningExecutionPolicy.CreateOperatorDescriptor();

        Assert.Equal("local-direct-stateless", descriptor.ExecutionMode);
        Assert.Equal(4000, descriptor.MaxInputCharacters);
        Assert.False(descriptor.UsesStoredResponses);
        Assert.False(descriptor.AllowsPreviousResponseId);
    }
}
