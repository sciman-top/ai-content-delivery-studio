using ContentDeliveryStudio.App.Services;
using ContentDeliveryStudio.App.ViewModels;

namespace ContentDeliveryStudio.Tests;

public sealed class ProviderCenterPresentationCoordinatorTests
{
    [Fact]
    public void BuildProviderRows_UsesConfigurationSnapshots()
    {
        var coordinator = new ProviderCenterPresentationCoordinator();
        var snapshot = new ProviderCenterSnapshot(
            new ProviderEndpointConfigurationSnapshot(
                "Text provider",
                "TEXT_PROVIDER",
                "openai_compatible",
                "https://text.example/v1",
                "gpt-5.5",
                1,
                UsesAppCredentials: false,
                ConcurrencyPerKey: 1,
                TotalConcurrency: 1),
            new ProviderEndpointConfigurationSnapshot(
                "Image provider",
                "IMAGE_PROVIDER",
                "openai_compatible_image_only",
                "https://image.example/v1",
                "image-model",
                4,
                UsesAppCredentials: true,
                ConcurrencyPerKey: 10,
                TotalConcurrency: 40),
            []);

        var rows = coordinator.BuildProviderRows(snapshot);

        Assert.Collection(
            rows,
            row =>
            {
                Assert.Equal("Text provider", row.Title);
                Assert.Equal("1 key", row.SecretSummary);
                Assert.Equal("Not checked", row.HealthSummary);
            },
            row =>
            {
                Assert.Equal("Image provider", row.Title);
                Assert.Equal("4 keys + app credentials", row.SecretSummary);
                Assert.Equal("10 per key / 40 total", row.ConcurrencySummary);
            });
    }

    [Fact]
    public void BuildSummary_AndHealthSummary_UseSnapshotState()
    {
        var coordinator = new ProviderCenterPresentationCoordinator();
        var configuration = new ProviderCenterSnapshot(
            new ProviderEndpointConfigurationSnapshot(
                "Text provider",
                "TEXT_PROVIDER",
                "openai_compatible",
                "https://text.example/v1",
                "gpt-5.5",
                1,
                UsesAppCredentials: false,
                ConcurrencyPerKey: 1,
                TotalConcurrency: 1),
            new ProviderEndpointConfigurationSnapshot(
                "Image provider",
                "IMAGE_PROVIDER",
                "openai_compatible_image_only",
                "https://image.example/v1",
                "image-model",
                2,
                UsesAppCredentials: false,
                ConcurrencyPerKey: 10,
                TotalConcurrency: 20),
            []);
        var health = new ProviderCenterHealthSnapshot(
            [new ProviderKeyHealthSnapshot("TEXT_PROVIDER", "TEXT_PROVIDER_API_KEY", "Healthy", 200)],
            [
                new ProviderKeyHealthSnapshot("IMAGE_PROVIDER", "IMAGE_PROVIDER_API_KEY_1", "Healthy", 200),
                new ProviderKeyHealthSnapshot("IMAGE_PROVIDER", "IMAGE_PROVIDER_API_KEY_2", "AuthRejected", 401),
            ]);

        Assert.Equal(
            "Providers ready: text key configured; image keys 2; total image concurrency 20.",
            coordinator.BuildSummary(configuration));
        Assert.Equal(
            "Provider health: text Healthy; image 1 Healthy, 1 AuthRejected.",
            coordinator.BuildHealthSummary(health));
    }

    [Fact]
    public void UpdateHealthRows_RewritesMatchingHealthSummaries()
    {
        var coordinator = new ProviderCenterPresentationCoordinator();
        var rows = coordinator.BuildProviderRows(
            new ProviderCenterSnapshot(
                new ProviderEndpointConfigurationSnapshot(
                    "Text provider",
                    "TEXT_PROVIDER",
                    "openai_compatible",
                    "https://text.example/v1",
                    "gpt-5.5",
                    1,
                    UsesAppCredentials: false,
                    ConcurrencyPerKey: 1,
                    TotalConcurrency: 1),
                new ProviderEndpointConfigurationSnapshot(
                    "Image provider",
                    "IMAGE_PROVIDER",
                    "openai_compatible_image_only",
                    "https://image.example/v1",
                    "image-model",
                    2,
                    UsesAppCredentials: false,
                    ConcurrencyPerKey: 10,
                    TotalConcurrency: 20),
                []));
        var health = new ProviderCenterHealthSnapshot(
            [new ProviderKeyHealthSnapshot("TEXT_PROVIDER", "TEXT_PROVIDER_API_KEY", "Healthy", 200)],
            [
                new ProviderKeyHealthSnapshot("IMAGE_PROVIDER", "IMAGE_PROVIDER_API_KEY_1", "Healthy", 200),
                new ProviderKeyHealthSnapshot("IMAGE_PROVIDER", "IMAGE_PROVIDER_API_KEY_2", "AuthRejected", 401),
            ]);

        var updatedRows = coordinator.UpdateHealthRows(rows, health);

        Assert.Collection(
            updatedRows,
            row => Assert.Equal("Healthy", row.HealthSummary),
            row => Assert.Equal("1 Healthy, 1 AuthRejected", row.HealthSummary));
    }
}
