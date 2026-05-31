using ImageSeriesStudio.Infrastructure.OpenAI;

namespace ImageSeriesStudio.Tests;

public sealed class OpenAiProviderConfigurationTests
{
    [Fact]
    public void Defaults_KeepRealApiDisabledAndValidateCleanly()
    {
        var options = new OpenAiProviderOptions();

        Assert.False(options.RealApiEnabled);
        Assert.Equal("https", options.BaseUri.Scheme);
        Assert.Equal("OPENAI_API_KEY", options.ApiKeySecretName);
        Assert.Empty(options.Validate());
    }

    [Fact]
    public async Task CheckReadinessAsync_BlocksRealCallsByDefault()
    {
        var readiness = await OpenAiProviderGuard.CheckReadinessAsync(
            new OpenAiProviderOptions(),
            new StaticSecretStore("test-key"),
            CancellationToken.None);

        Assert.False(readiness.CanCallRealApi);
        Assert.Contains(readiness.Errors, error => error.Contains("disabled", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task CheckReadinessAsync_BlocksRealCallsWhenApiKeyIsMissing()
    {
        var readiness = await OpenAiProviderGuard.CheckReadinessAsync(
            new OpenAiProviderOptions { RealApiEnabled = true },
            new StaticSecretStore(null),
            CancellationToken.None);

        Assert.False(readiness.CanCallRealApi);
        Assert.Contains(readiness.Errors, error => error.Contains("not found", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task CheckReadinessAsync_AllowsRealCallsOnlyAfterExplicitOptInAndSecret()
    {
        var readiness = await OpenAiProviderGuard.CheckReadinessAsync(
            new OpenAiProviderOptions { RealApiEnabled = true },
            new StaticSecretStore("test-key"),
            CancellationToken.None);

        Assert.True(readiness.CanCallRealApi);
        Assert.Empty(readiness.Errors);
    }

    [Fact]
    public async Task EnsureCanCallRealApiAsync_ThrowsWhenGuardFails()
    {
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            OpenAiProviderGuard.EnsureCanCallRealApiAsync(
                new OpenAiProviderOptions(),
                new StaticSecretStore("test-key"),
                CancellationToken.None));

        Assert.Contains("not ready", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Validate_RejectsInsecureEndpointAndBlankModels()
    {
        var errors = new OpenAiProviderOptions
        {
            BaseUri = new Uri("http://api.openai.example/v1/"),
            TextPlanningModel = " ",
            ImageGenerationModel = "",
            VisionReviewModel = "\t",
        }.Validate();

        Assert.Contains(errors, error => error.Contains("HTTPS", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(errors, error => error.Contains("Text planning model", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(errors, error => error.Contains("Image generation model", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(errors, error => error.Contains("Vision review model", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task EnvironmentSecretStore_ReadsProcessEnvironmentVariableWithoutPersistingSecrets()
    {
        const string variableName = "IMAGE_SERIES_STUDIO_TEST_OPENAI_API_KEY";
        var previousValue = Environment.GetEnvironmentVariable(variableName);

        try
        {
            Environment.SetEnvironmentVariable(variableName, "process-test-key");

            var secret = await new EnvironmentOpenAiSecretStore().GetSecretAsync(variableName, CancellationToken.None);

            Assert.Equal("process-test-key", secret);
        }
        finally
        {
            Environment.SetEnvironmentVariable(variableName, previousValue);
        }
    }

    [Fact]
    public async Task EnvironmentSecretStore_RespectsCancellation()
    {
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            new EnvironmentOpenAiSecretStore().GetSecretAsync("OPENAI_API_KEY", cancellation.Token));
    }

    private sealed class StaticSecretStore(string? value) : IOpenAiSecretStore
    {
        public Task<string?> GetSecretAsync(string secretName, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            return Task.FromResult(value);
        }
    }
}
