using ContentDeliveryStudio.App.Services;
using ContentDeliveryStudio.Infrastructure.Fakes;

namespace ContentDeliveryStudio.Tests;

public sealed class DesktopDiagnosticsSnapshotFactoryTests
{
    [Fact]
    public async Task CreateAsync_UsesResolvedFakeCapabilitiesAndGenericSecretPresence()
    {
        var configuration = new ProviderCenterSnapshot(
            new ProviderEndpointConfigurationSnapshot("Text", "TEXT_PROVIDER", "openai", "https://example.invalid", "text-model", 1, true, 1, 1),
            new ProviderEndpointConfigurationSnapshot("Image", "IMAGE_PROVIDER", "openai", "https://example.invalid", "image-model", 2, false, 1, 2),
            []);
        var image = new FakeImageGenerationProvider();
        var factory = new DesktopDiagnosticsSnapshotFactory(
            new StubConfigurationService(configuration),
            new FakeTextPlanningProvider(),
            image,
            image,
            new FakeVisionReviewProvider());

        var result = await factory.CreateAsync(CancellationToken.None);

        Assert.Equal(3, result.Providers.Count);
        var imageProvider = Assert.Single(result.Providers, item => item.ProviderId == "fake-image");
        Assert.Equal(["image-generation", "image-editing", "reference-images"], imageProvider.Capabilities);
        Assert.False(imageProvider.RealApiEnabled);
        Assert.True(imageProvider.DryRunOnly);
        Assert.Equal(
            [
                new("text-provider-key-pool", true),
                new("text-provider-app-credentials", true),
                new("image-provider-key-pool", true),
                new("image-provider-app-credentials", false),
            ],
            result.Secrets);
        Assert.DoesNotContain(result.Secrets, item => item.Name.Contains("API_KEY", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(result.Secrets, item => item.Name.Contains("APP_SECRET", StringComparison.OrdinalIgnoreCase));
    }

    private sealed class StubConfigurationService(ProviderCenterSnapshot snapshot) : IProviderCenterConfigurationService
    {
        public Task<ProviderCenterSnapshot> LoadAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(snapshot);
        }
    }
}
