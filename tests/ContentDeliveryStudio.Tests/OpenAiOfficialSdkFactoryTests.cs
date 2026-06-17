using ContentDeliveryStudio.Infrastructure.OpenAI;

namespace ContentDeliveryStudio.Tests;

public sealed class OpenAiOfficialSdkFactoryTests
{
    [Fact]
    public void OfficialSdkFactory_ReportsOpenAiPackageIdentity()
    {
        var factory = new OpenAiOfficialSdkFactory();

        Assert.Equal("OpenAI", factory.PackageId);
        Assert.False(string.IsNullOrWhiteSpace(factory.PackageVersion));
    }

    [Fact]
    public void OfficialSdkSelection_UsesSdkForImageGenerationAndRawHttpForResponsesSurfaces()
    {
        var imageDecision = OpenAiOfficialSdkSelection.ForOperation(OpenAiProviderOperation.ImageGeneration);
        var planningDecision = OpenAiOfficialSdkSelection.ForOperation(OpenAiProviderOperation.TextPlanning);
        var reviewDecision = OpenAiOfficialSdkSelection.ForOperation(OpenAiProviderOperation.VisionReview);

        Assert.Equal(OpenAiProviderTransportKind.OfficialSdk, imageDecision.TransportKind);
        Assert.Equal(OpenAiProviderTransportKind.RawHttp, planningDecision.TransportKind);
        Assert.Equal(OpenAiProviderTransportKind.RawHttp, reviewDecision.TransportKind);
        Assert.Contains("OPENAI001", planningDecision.Reason, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("OPENAI001", reviewDecision.Reason, StringComparison.OrdinalIgnoreCase);
    }
}
