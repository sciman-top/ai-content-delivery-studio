using System.Text.Json;
using ImageSeriesStudio.App.Services;
using ImageSeriesStudio.App.ViewModels;

namespace ImageSeriesStudio.Tests;

public sealed class ProviderCenterViewModelTests
{
    [Fact]
    public async Task DotEnvProviderCenterConfigurationService_LoadsSummaryWithoutSecretValues()
    {
        var envPath = Path.Combine(Path.GetTempPath(), $"provider-center-{Guid.NewGuid():N}.env");
        await File.WriteAllTextAsync(
            envPath,
            """
            TEXT_PROVIDER_BASE_URL=https://text.example/v1
            TEXT_PROVIDER_API_KEY=sk-text-secret
            TEXT_PROVIDER_MODEL=gpt-5.5
            IMAGE_PROVIDER_BASE_URL=https://image.example/v1
            IMAGE_PROVIDER_MODEL=image-model
            IMAGE_PROVIDER_API_KEY_1=sk-image-secret-1
            IMAGE_PROVIDER_API_KEY_2=sk-image-secret-2
            IMAGE_PROVIDER_APP_ID=image-app-id
            IMAGE_PROVIDER_APP_SECRET=image-app-secret
            IMAGE_PROVIDER_CONCURRENCY_PER_KEY=10
            IMAGE_PROVIDER_TOTAL_CONCURRENCY=20
            """);

        try
        {
            var service = new DotEnvProviderCenterConfigurationService(envPath);

            var snapshot = await service.LoadAsync(CancellationToken.None);

            Assert.Equal("gpt-5.5", snapshot.Text.Model);
            Assert.Equal(1, snapshot.Text.ApiKeyCount);
            Assert.Equal(2, snapshot.Image.ApiKeyCount);
            Assert.True(snapshot.Image.UsesAppCredentials);
            Assert.Equal(20, snapshot.Image.TotalConcurrency);
            Assert.Empty(snapshot.ValidationMessages);
            Assert.DoesNotContain("sk-", JsonSerializer.Serialize(snapshot), StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("image-app-secret", JsonSerializer.Serialize(snapshot), StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            File.Delete(envPath);
        }
    }

    [Fact]
    public async Task ProviderCenterViewModel_RefreshAsync_BuildsRowsAndSummary()
    {
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
        var viewModel = new ProviderCenterViewModel(new StaticProviderCenterConfigurationService(snapshot));

        await viewModel.RefreshAsync(CancellationToken.None);

        Assert.Equal("Providers ready: text key configured; image keys 4; total image concurrency 40.", viewModel.SummaryText);
        Assert.Empty(viewModel.ValidationMessages);
        Assert.Collection(
            viewModel.ProviderRows,
            row =>
            {
                Assert.Equal("Text provider", row.Title);
                Assert.Equal("gpt-5.5", row.Model);
                Assert.Equal("1 key", row.SecretSummary);
            },
            row =>
            {
                Assert.Equal("Image provider", row.Title);
                Assert.Equal("image-model", row.Model);
                Assert.Equal("4 keys + app credentials", row.SecretSummary);
                Assert.Equal("10 per key / 40 total", row.ConcurrencySummary);
                Assert.Equal("Not checked", row.HealthSummary);
            });
    }

    [Fact]
    public async Task ProviderCenterViewModel_CheckHealthAsync_UpdatesRowsWithMixedKeyPoolStatuses()
    {
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
        var viewModel = new ProviderCenterViewModel(
            new StaticProviderCenterConfigurationService(snapshot),
            new StaticProviderCenterHealthCheckService(health));

        await viewModel.RefreshAsync(CancellationToken.None);
        await viewModel.CheckHealthAsync(CancellationToken.None);

        Assert.Equal("Provider health: text Healthy; image 1 Healthy, 1 AuthRejected.", viewModel.SummaryText);
        Assert.Collection(
            viewModel.ProviderRows,
            row => Assert.Equal("Healthy", row.HealthSummary),
            row => Assert.Equal("1 Healthy, 1 AuthRejected", row.HealthSummary));
    }

    private sealed class StaticProviderCenterConfigurationService(ProviderCenterSnapshot snapshot)
        : IProviderCenterConfigurationService
    {
        public Task<ProviderCenterSnapshot> LoadAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            return Task.FromResult(snapshot);
        }
    }

    private sealed class StaticProviderCenterHealthCheckService(ProviderCenterHealthSnapshot snapshot)
        : IProviderCenterHealthCheckService
    {
        public Task<ProviderCenterHealthSnapshot> CheckAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            return Task.FromResult(snapshot);
        }
    }
}
