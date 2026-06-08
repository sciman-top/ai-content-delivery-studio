using ImageSeriesStudio.Infrastructure.OpenAI;

namespace ImageSeriesStudio.Tests;

public sealed class OpenAiLaunchPreflightTests
{
    [Fact]
    public async Task EvaluateAsync_ReportsReadyWhenTextImageSecretsAndOptInArePresent()
    {
        var configuration = ProviderEnvironmentConfiguration.FromValues(
            new Dictionary<string, string?>
            {
                ["TEXT_PROVIDER_BASE_URL"] = "https://text.example/v1",
                ["TEXT_PROVIDER_API_KEY"] = "sk-text",
                ["TEXT_PROVIDER_MODEL"] = "gpt-5.5",
                ["IMAGE_PROVIDER_BASE_URL"] = "https://image.example/v1",
                ["IMAGE_PROVIDER_MODEL"] = "gpt-image-2",
                ["IMAGE_PROVIDER_API_KEY_1"] = "sk-image-1",
            });
        var secretStore = new DictionarySecretStore(
            new Dictionary<string, string?>
            {
                ["TEXT_PROVIDER_API_KEY"] = "text-secret",
                ["IMAGE_PROVIDER_API_KEY_1"] = "image-secret",
            });
        var environment = new DictionaryEnvironment
        {
            [OpenAiSmokeTestGate.DefaultOptInEnvironmentVariable] = "1",
        };

        var report = await OpenAiLaunchPreflight.EvaluateAsync(
            configuration,
            secretStore,
            environment,
            new OpenAiSmokeTestOptions(),
            CancellationToken.None);

        Assert.Empty(report.ConfigurationErrors);
        Assert.True(report.TextPlanning.CanCallRealApi);
        Assert.True(report.VisionReview.CanCallRealApi);
        Assert.True(report.ImageGeneration.CanCallRealApi);
        Assert.True(report.TextSmoke.CanRunRealApiSmoke);
        Assert.True(report.ImageSmoke.CanRunRealApiSmoke);
        Assert.True(report.CanRunLiveV1SampleSeries);
        Assert.Empty(report.BlockingReasons);
    }

    [Fact]
    public async Task EvaluateAsync_BlocksLiveSeriesWhenOptInOrSecretIsMissing()
    {
        var configuration = ProviderEnvironmentConfiguration.FromValues(
            new Dictionary<string, string?>
            {
                ["TEXT_PROVIDER_BASE_URL"] = "https://text.example/v1",
                ["TEXT_PROVIDER_API_KEY"] = "sk-text",
                ["TEXT_PROVIDER_MODEL"] = "gpt-5.5",
                ["IMAGE_PROVIDER_BASE_URL"] = "https://image.example/v1",
                ["IMAGE_PROVIDER_MODEL"] = "gpt-image-2",
                ["IMAGE_PROVIDER_API_KEY_1"] = "sk-image-1",
            });
        var secretStore = new DictionarySecretStore(
            new Dictionary<string, string?>
            {
                ["TEXT_PROVIDER_API_KEY"] = "text-secret",
                ["IMAGE_PROVIDER_API_KEY_1"] = null,
            });

        var report = await OpenAiLaunchPreflight.EvaluateAsync(
            configuration,
            secretStore,
            new DictionaryEnvironment(),
            new OpenAiSmokeTestOptions(),
            CancellationToken.None);

        Assert.False(report.CanRunLiveV1SampleSeries);
        Assert.False(report.TextSmoke.CanRunRealApiSmoke);
        Assert.False(report.ImageSmoke.CanRunRealApiSmoke);
        Assert.Contains(report.BlockingReasons, reason => reason.Contains("not opted in", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(report.BlockingReasons, reason => reason.Contains("not found", StringComparison.OrdinalIgnoreCase));
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

    private sealed class DictionaryEnvironment : Dictionary<string, string>, IOpenAiEnvironment
    {
        public string? GetEnvironmentVariable(string variableName)
        {
            return TryGetValue(variableName, out var value) ? value : null;
        }
    }
}
