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
}
