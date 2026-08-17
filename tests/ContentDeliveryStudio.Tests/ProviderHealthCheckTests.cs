using System.Net;
using ContentDeliveryStudio.App.Services;
using ContentDeliveryStudio.Infrastructure.OpenAI;

namespace ContentDeliveryStudio.Tests;

public sealed class ProviderHealthCheckTests
{
    [Fact]
    public async Task CheckModelsEndpointAsync_UsesModelsEndpointAndBearerSecret()
    {
        var configuration = ProviderEnvironmentConfiguration.FromValues(
            new Dictionary<string, string?>
            {
                ["TEXT_PROVIDER_BASE_URL"] = "https://text.example/v1",
                ["TEXT_PROVIDER_API_KEY"] = "sk-text",
                ["TEXT_PROVIDER_APP_ID"] = "configured-app-id",
                ["TEXT_PROVIDER_APP_SECRET"] = "configured-app-secret",
                ["TEXT_PROVIDER_MODEL"] = "gpt-5.5",
                ["IMAGE_PROVIDER_BASE_URL"] = "https://image.example/v1",
                ["IMAGE_PROVIDER_MODEL"] = "image-model",
                ["IMAGE_PROVIDER_API_KEY_1"] = "sk-image",
            });
        var handler = new CaptureHandler(request =>
            request.RequestUri!.AbsolutePath == "/v1/models"
            && request.Headers.Authorization?.Scheme == "Bearer"
            && request.Headers.Authorization.Parameter == "text-secret"
            && request.Headers.GetValues("X-App-ID").Single() == "app-id-secret"
            && request.Headers.GetValues("X-App-Secret").Single() == "app-secret-secret"
                ? new HttpResponseMessage(HttpStatusCode.OK)
                : new HttpResponseMessage(HttpStatusCode.Unauthorized));
        var service = new ProviderHealthCheckService(
            new HttpClient(handler),
            new DictionarySecretStore(
                new Dictionary<string, string?>
                {
                    ["TEXT_PROVIDER_API_KEY"] = "text-secret",
                    ["TEXT_PROVIDER_APP_ID"] = "app-id-secret",
                    ["TEXT_PROVIDER_APP_SECRET"] = "app-secret-secret",
                }));

        var result = await service.CheckModelsEndpointAsync(configuration.Text, CancellationToken.None);

        Assert.Equal(ProviderHealthStatus.Healthy, result.Status);
        Assert.Equal(HttpStatusCode.OK, result.HttpStatusCode);
        Assert.Equal("TEXT_PROVIDER_API_KEY", result.ApiKeySecretName);
        Assert.Equal("/v1/models", handler.LastRequest!.RequestUri!.AbsolutePath);
    }

    [Fact]
    public async Task CheckKeyPoolModelsEndpointAsync_ReturnsPerKeyStatusWithoutThrowing()
    {
        var configuration = ProviderEnvironmentConfiguration.FromValues(
            new Dictionary<string, string?>
            {
                ["TEXT_PROVIDER_BASE_URL"] = "https://text.example/v1",
                ["TEXT_PROVIDER_API_KEY"] = "sk-text",
                ["TEXT_PROVIDER_MODEL"] = "gpt-5.5",
                ["IMAGE_PROVIDER_BASE_URL"] = "https://image.example",
                ["IMAGE_PROVIDER_MODEL"] = "image-model",
                ["IMAGE_PROVIDER_API_KEY_1"] = "sk-image-1",
                ["IMAGE_PROVIDER_API_KEY_2"] = "sk-image-2",
            });
        var handler = new CaptureHandler(request =>
            request.Headers.Authorization?.Parameter == "healthy-secret"
                ? new HttpResponseMessage(HttpStatusCode.OK)
                : new HttpResponseMessage(HttpStatusCode.Unauthorized));
        var service = new ProviderHealthCheckService(
            new HttpClient(handler),
            new DictionarySecretStore(
                new Dictionary<string, string?>
                {
                    ["IMAGE_PROVIDER_API_KEY_1"] = "healthy-secret",
                    ["IMAGE_PROVIDER_API_KEY_2"] = "bad-secret",
                }));

        var results = await service.CheckKeyPoolModelsEndpointAsync(configuration.Image, CancellationToken.None);

        Assert.Equal(
            [ProviderHealthStatus.Healthy, ProviderHealthStatus.AuthRejected],
            results.Select(result => result.Status));
        Assert.Equal(["IMAGE_PROVIDER_API_KEY_1", "IMAGE_PROVIDER_API_KEY_2"], results.Select(result => result.ApiKeySecretName));
    }

    [Fact]
    public async Task CheckKeyPoolModelsEndpointAsync_UsesSharedTextKeyWhenImageKeyPoolFallsBack()
    {
        var configuration = ProviderEnvironmentConfiguration.FromValues(
            new Dictionary<string, string?>
            {
                ["TEXT_PROVIDER_BASE_URL"] = "https://gateway.example/v1",
                ["TEXT_PROVIDER_API_KEY"] = "sk-shared",
                ["TEXT_PROVIDER_MODEL"] = "gpt-5.5",
                ["IMAGE_PROVIDER_BASE_URL"] = "https://gateway.example/v1",
                ["IMAGE_PROVIDER_MODEL"] = "gpt-image-2",
            });
        var handler = new CaptureHandler(_ => new HttpResponseMessage(HttpStatusCode.OK));
        var service = new ProviderHealthCheckService(
            new HttpClient(handler),
            new DictionarySecretStore(new Dictionary<string, string?> { ["TEXT_PROVIDER_API_KEY"] = "shared-secret" }));

        var results = await service.CheckKeyPoolModelsEndpointAsync(configuration.Image, CancellationToken.None);

        var result = Assert.Single(results);
        Assert.Equal(ProviderHealthStatus.Healthy, result.Status);
        Assert.Equal("TEXT_PROVIDER_API_KEY", result.ApiKeySecretName);
        Assert.Equal("shared-secret", handler.LastRequest!.Headers.Authorization?.Parameter);
    }

    [Fact]
    public async Task ProviderCenterHealthCheck_IncludesPrimaryAndFallbackDestinations()
    {
        var directory = Path.Combine(Path.GetTempPath(), "ContentDeliveryStudio.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        var envPath = Path.Combine(directory, ".env");
        await File.WriteAllLinesAsync(
            envPath,
            [
                "TEXT_PROVIDER_BASE_URL=https://text.example/v1",
                "TEXT_PROVIDER_API_KEY=present",
                "TEXT_PROVIDER_MODEL=gpt-5.5",
                "TEXT_PROVIDER_FALLBACK_1_BASE_URL=https://text-backup.example/v1",
                "TEXT_PROVIDER_FALLBACK_1_API_KEY=present",
                "TEXT_PROVIDER_FALLBACK_1_MODEL=gpt-5.5",
                "IMAGE_PROVIDER_BASE_URL=https://image.example/v1",
                "IMAGE_PROVIDER_API_KEY_1=present",
                "IMAGE_PROVIDER_MODEL=gpt-image-2",
                "IMAGE_PROVIDER_FALLBACK_1_BASE_URL=https://image-backup.example/v1",
                "IMAGE_PROVIDER_FALLBACK_1_API_KEY_1=present",
                "IMAGE_PROVIDER_FALLBACK_1_MODEL=gpt-image-2",
            ]);

        try
        {
            var handler = new CaptureHandler(_ => new HttpResponseMessage(HttpStatusCode.OK));
            var secrets = new DictionarySecretStore(
                new Dictionary<string, string?>
                {
                    ["TEXT_PROVIDER_API_KEY"] = "text-secret",
                    ["TEXT_PROVIDER_FALLBACK_1_API_KEY"] = "text-backup-secret",
                    ["IMAGE_PROVIDER_API_KEY_1"] = "image-secret",
                    ["IMAGE_PROVIDER_FALLBACK_1_API_KEY_1"] = "image-backup-secret",
                });
            var health = new DotEnvProviderCenterHealthCheckService(
                new ProviderHealthCheckService(new HttpClient(handler), secrets),
                envPath);

            var snapshot = await health.CheckAsync(CancellationToken.None);

            Assert.Equal(
                ["TEXT_PROVIDER", "TEXT_PROVIDER_FALLBACK_1"],
                snapshot.Text.Select(item => item.ProviderPrefix));
            Assert.Equal(
                ["IMAGE_PROVIDER", "IMAGE_PROVIDER_FALLBACK_1"],
                snapshot.Image.Select(item => item.ProviderPrefix));
            Assert.Single(snapshot.ForPrefix("TEXT_PROVIDER_FALLBACK_1"));
            Assert.Single(snapshot.ForPrefix("IMAGE_PROVIDER_FALLBACK_1"));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    private sealed class DictionarySecretStore(IReadOnlyDictionary<string, string?> secrets) : IOpenAiSecretStore
    {
        public Task<string?> GetSecretAsync(string secretName, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            secrets.TryGetValue(secretName, out var secret);
            return Task.FromResult(secret);
        }
    }

    private sealed class CaptureHandler(Func<HttpRequestMessage, HttpResponseMessage> responseFactory) : HttpMessageHandler
    {
        public HttpRequestMessage? LastRequest { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            LastRequest = request;
            return Task.FromResult(responseFactory(request));
        }
    }
}
